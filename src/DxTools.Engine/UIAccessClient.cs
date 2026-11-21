using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using DxTools.Core;

namespace DxTools.Engine;

public sealed class UIAccessClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly object _gate = new();
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private Process? _process;
    private bool _disabled;
    private bool _disposed;

    public UIAccessClient(bool enabled = true)
    {
        _disabled = !enabled;
    }

    public void Warmup()
    {
        lock (_gate)
        {
            if (_disposed || _disabled)
            {
                return;
            }

            _ = EnsureConnected();
        }
    }

    public bool TryMoveWindow(
        IntPtr window,
        int x,
        int y,
        int width,
        int height,
        bool restore,
        bool maximize) =>
        Send(new HelperCommand(
            "move",
            window.ToInt64(),
            x,
            y,
            width,
            height,
            Restore: restore,
            Maximize: maximize));

    public bool TrySetTopmost(IntPtr window, bool topmost) =>
        Send(new HelperCommand("topmost", window.ToInt64(), Topmost: topmost));

    public bool TryActivate(IntPtr window) =>
        Send(new HelperCommand("activate", window.ToInt64()));

    private bool Send(HelperCommand command)
    {
        lock (_gate)
        {
            if (_disposed || _disabled)
            {
                return false;
            }

            if (!EnsureConnected())
            {
                return false;
            }

            try
            {
                _writer!.WriteLine(JsonSerializer.Serialize(command, JsonOptions));
                var line = _reader!.ReadLine();
                if (line is null)
                {
                    Disconnect();
                    return false;
                }

                var response = JsonSerializer.Deserialize<HelperResponse>(line, JsonOptions);
                if (response is null)
                {
                    DxLog.Warn("UIAccess helper returned an empty response.");
                    return false;
                }

                if (!response.Ok)
                {
                    DxLog.Warn($"UIAccess helper rejected '{command.Action}' (error={response.Error}).");
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException
                or TimeoutException
                or ObjectDisposedException
                or InvalidOperationException
                or JsonException)
            {
                DxLog.Warn($"UIAccess helper request failed: {ex.Message}");
                Disconnect();
                return false;
            }
        }
    }

    private bool EnsureConnected()
    {
        if (_pipe is { IsConnected: true })
        {
            return true;
        }

        Disconnect();

        var helperPath = Path.Combine(AppContext.BaseDirectory, "DxTools.Helper.exe");
        if (!File.Exists(helperPath))
        {
            DxLog.Warn($"UIAccess helper not found: '{helperPath}'.");
            _disabled = true;
            return false;
        }

        var pipeName = $"DxTools.UIAccess.{Environment.ProcessId}.{Guid.NewGuid():N}";
        try
        {
            var startInfo = new ProcessStartInfo(helperPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            startInfo.ArgumentList.Add("--pipe");
            startInfo.ArgumentList.Add(pipeName);
            startInfo.ArgumentList.Add("--parent");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

            _process = Process.Start(startInfo);
            if (_process is null)
            {
                DxLog.Warn("UIAccess helper process was not started.");
                _disabled = true;
                return false;
            }

            var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15000);
            pipe.ReadTimeout = 3000;

            _pipe = pipe;
            _reader = new StreamReader(
                pipe,
                new UTF8Encoding(false),
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true);
            _writer = new StreamWriter(
                pipe,
                new UTF8Encoding(false),
                bufferSize: 4096,
                leaveOpen: true)
            {
                AutoFlush = true,
                NewLine = "\n",
            };

            DxLog.Info("UIAccess helper connected.");
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception
            or IOException
            or TimeoutException
            or UnauthorizedAccessException
            or InvalidOperationException
            or NotSupportedException)
        {
            DxLog.Warn($"UIAccess helper unavailable: {ex.Message}");
            Disconnect();
            _disabled = true;
            return false;
        }
    }

    private void Disconnect()
    {
        try
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _pipe?.Dispose();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
        }

        if (_process is not null)
        {
            try
            {
                if (!_process.HasExited && !_process.WaitForExit(500))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException
                or Win32Exception
                or NotSupportedException)
            {
            }
            finally
            {
                _process.Dispose();
            }
        }

        _reader = null;
        _writer = null;
        _pipe = null;
        _process = null;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Disconnect();
        }
    }

    private sealed record HelperCommand(
        string Action,
        long Hwnd,
        int X = 0,
        int Y = 0,
        int Width = 0,
        int Height = 0,
        bool Topmost = false,
        bool Restore = false,
        bool Maximize = false);

    private sealed record HelperResponse(bool Ok, int? Error);
}
