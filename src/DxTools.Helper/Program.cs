using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace DxTools.Helper;

internal static partial class Program
{
    private const int SwRestore = 9;
    private const int SwMaximize = 3;
    private const int GaRoot = 2;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNotTopmost = new(-2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static int Main(string[] args)
    {
        var pipeName = GetArgument(args, "--pipe");
        var parentText = GetArgument(args, "--parent");
        if (string.IsNullOrWhiteSpace(pipeName)
            || !uint.TryParse(parentText, out var parentProcessId)
            || parentProcessId == 0)
        {
            Log("Missing or invalid --pipe/--parent arguments.");
            return 2;
        }

        try
        {
            Run(pipeName, parentProcessId);
            return 0;
        }
        catch (Exception ex)
        {
            Log($"Fatal helper error: {ex}");
            return 1;
        }
    }

    private static void Run(string pipeName, uint parentProcessId)
    {
        using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.CurrentUserOnly);

        server.WaitForConnection();

        if (!GetNamedPipeClientProcessId(server.SafePipeHandle.DangerousGetHandle(), out var clientProcessId)
            || clientProcessId != parentProcessId
            || !IsExpectedParent(parentProcessId))
        {
            Log($"Rejected pipe client pid={clientProcessId}, expected={parentProcessId}.");
            return;
        }

        Log($"Connected to parent pid={parentProcessId}.");

        using var reader = new StreamReader(server, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        using var writer = new StreamWriter(server, new UTF8Encoding(false), bufferSize: 4096, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n",
        };

        while (reader.ReadLine() is { } line)
        {
            HelperResponse response;
            try
            {
                var request = JsonSerializer.Deserialize<HelperRequest>(line, JsonOptions);
                response = request is null ? Error(87) : Handle(request);
            }
            catch (JsonException)
            {
                response = Error(87);
            }
            catch (Exception ex)
            {
                Log($"Command failed: {ex}");
                response = Error(31);
            }

            writer.WriteLine(JsonSerializer.Serialize(response, JsonOptions));
        }

        Log("Parent disconnected.");
    }

    private static HelperResponse Handle(HelperRequest request)
    {
        var hwnd = new IntPtr(request.Hwnd);
        if (!IsUsableWindow(hwnd))
        {
            return Error(87);
        }

        return request.Action switch
        {
            "move" => Move(hwnd, request),
            "topmost" => SetTopmost(hwnd, request.Topmost),
            "activate" => Activate(hwnd),
            _ => Error(87),
        };
    }

    private static HelperResponse Move(IntPtr hwnd, HelperRequest request)
    {
        if (request.Restore)
        {
            _ = ShowWindow(hwnd, SwRestore);
        }

        var width = Math.Clamp(request.Width, 50, 100_000);
        var height = Math.Clamp(request.Height, 50, 100_000);
        if (!SetWindowPos(
                hwnd,
                IntPtr.Zero,
                request.X,
                request.Y,
                width,
                height,
                SwpNoZOrder | SwpNoActivate))
        {
            return Error(Marshal.GetLastWin32Error());
        }

        if (request.Maximize)
        {
            _ = ShowWindow(hwnd, SwMaximize);
        }

        return Ok();
    }

    private static HelperResponse SetTopmost(IntPtr hwnd, bool topmost)
    {
        if (!SetWindowPos(
                hwnd,
                topmost ? HwndTopmost : HwndNotTopmost,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoActivate))
        {
            return Error(Marshal.GetLastWin32Error());
        }

        return Ok();
    }

    private static HelperResponse Activate(IntPtr hwnd)
    {
        if (IsIconic(hwnd))
        {
            _ = ShowWindow(hwnd, SwRestore);
        }

        if (SetForegroundWindow(hwnd))
        {
            return Ok();
        }

        var foreground = GetForegroundWindow();
        if (foreground != IntPtr.Zero)
        {
            var foregroundThread = GetWindowThreadProcessId(foreground, out _);
            var currentThread = GetCurrentThreadId();
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                var attached = AttachThreadInput(currentThread, foregroundThread, true);
                try
                {
                    if (SetForegroundWindow(hwnd))
                    {
                        return Ok();
                    }
                }
                finally
                {
                    if (attached)
                    {
                        _ = AttachThreadInput(currentThread, foregroundThread, false);
                    }
                }
            }
        }

        _ = BringWindowToTop(hwnd);
        return SetForegroundWindow(hwnd) ? Ok() : Error(Marshal.GetLastWin32Error());
    }

    private static bool IsUsableWindow(IntPtr hwnd) =>
        hwnd != IntPtr.Zero
        && IsWindow(hwnd)
        && GetAncestor(hwnd, GaRoot) == hwnd;

    private static bool IsExpectedParent(uint processId)
    {
        try
        {
            using var parent = Process.GetProcessById((int)processId);
            var path = parent.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var expected = Path.Combine(AppContext.BaseDirectory, "DxTools.exe");
            return string.Equals(
                Path.GetFullPath(path),
                Path.GetFullPath(expected),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException)
        {
            return false;
        }
    }

    private static string? GetArgument(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static HelperResponse Ok() => new(true, null);

    private static HelperResponse Error(int code) => new(false, code);

    private static void Log(string message)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DxTools",
                "logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "uiaccess-helper.log"),
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetNamedPipeClientProcessId(IntPtr pipe, out uint clientProcessId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hwnd, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BringWindowToTop(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool attach);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    private sealed class HelperRequest
    {
        public string? Action { get; set; }

        public long Hwnd { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool Topmost { get; set; }

        public bool Restore { get; set; }

        public bool Maximize { get; set; }
    }

    private sealed record HelperResponse(bool Ok, int? Error);
}
