using System.IO.Pipes;
using System.Text;

namespace DxTools.Core;

public sealed class SingleInstance : IDisposable
{
    private const string ShowMessage = "show";
    private const string ExitMessage = "exit";
    private const string CommandPrefix = "cmd:";
    private const int MaxCommandMessageLength = 64 * 1024;

    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;
    private readonly string _pipeName;
    private CancellationTokenSource? _cancellation;
    private Task? _serverTask;

    public SingleInstance(string? instanceSuffix = null)
    {
        var suffix = string.IsNullOrWhiteSpace(instanceSuffix) ? AutostartService.InstanceSuffix : instanceSuffix;
        var mutexName = $@"Local\DxTools.SingleInstance.{suffix}";
        _pipeName = $"DxTools.Ipc.{suffix}";

        _mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        _ownsMutex = createdNew;
        IsFirstInstance = createdNew;
    }

    public bool IsFirstInstance { get; }

    public event Action? ShowRequested;

    public event Action? ExitRequested;

    public event Action<IReadOnlyList<string>>? CommandRequested;

    public static string CreateCommandMessage(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var payload = string.Join('\0', arguments);
        return CommandPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    public static bool TryParseCommandMessage(
        string? message,
        out IReadOnlyList<string> arguments)
    {
        arguments = Array.Empty<string>();
        if (string.IsNullOrEmpty(message)
            || message.Length > MaxCommandMessageLength
            || !message.StartsWith(CommandPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(
                Convert.FromBase64String(message[CommandPrefix.Length..]));
            var parsed = payload.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            if (parsed.Length == 0)
            {
                return false;
            }

            arguments = parsed;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public void StartServer()
    {
        if (!IsFirstInstance || _serverTask is not null)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        _serverTask = Task.Run(() => ServerLoopAsync(_cancellation.Token));
    }

    public bool TrySignalExisting(int timeoutMilliseconds = 1500, string message = ShowMessage)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            client.Connect(timeoutMilliseconds);

            using var writer = new StreamWriter(client, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: false)
            {
                AutoFlush = true,
            };
            writer.WriteLine(message);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    private async Task ServerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);
                var message = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.Equals(message, ShowMessage, StringComparison.Ordinal))
                {
                    RaiseShowRequested();
                }
                else if (string.Equals(message, ExitMessage, StringComparison.Ordinal))
                {
                    RaiseExitRequested();
                }
                else if (TryParseCommandMessage(message, out var arguments))
                {
                    RaiseCommandRequested(arguments);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
            {
                DxLog.Warn($"Single-instance pipe error: {ex.Message}");
            }
        }
    }

    private void RaiseExitRequested()
    {
        try
        {
            ExitRequested?.Invoke();
        }
        catch (Exception ex)
        {
            DxLog.Exception("Single-instance ExitRequested handler failed.", ex);
        }
    }

    private void RaiseShowRequested()
    {
        try
        {
            ShowRequested?.Invoke();
        }
        catch (Exception ex)
        {
            DxLog.Exception("Single-instance ShowRequested handler failed.", ex);
        }
    }

    private void RaiseCommandRequested(IReadOnlyList<string> arguments)
    {
        try
        {
            CommandRequested?.Invoke(arguments);
        }
        catch (Exception ex)
        {
            DxLog.Exception("Single-instance CommandRequested handler failed.", ex);
        }
    }

    public void Dispose()
    {
        try
        {
            _cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        if (_ownsMutex)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        _mutex.Dispose();
        _cancellation?.Dispose();
    }
}
