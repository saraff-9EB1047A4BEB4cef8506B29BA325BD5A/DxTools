namespace DxTools.Core;

public enum DxLogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    None = 5,
}

public static class DxLog
{
    private static readonly object Gate = new();
    private static string? _directory;
    private static DxLogLevel _minimumLevel = DxLogLevel.Info;

    public static string? Directory => _directory;

    public static string? CurrentFile { get; private set; }

    public static void Initialize(string? directory = null, DxLogLevel minimumLevel = DxLogLevel.Info)
    {
        lock (Gate)
        {
            _directory = string.IsNullOrWhiteSpace(directory) ? AppPaths.LogDirectory : directory;
            _minimumLevel = minimumLevel;

            try
            {
                System.IO.Directory.CreateDirectory(_directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _directory = null;
            }
        }
    }

    public static void Info(string message) => Write(DxLogLevel.Info, message, null);

    public static void Warn(string message) => Write(DxLogLevel.Warn, message, null);

    public static void Error(string message) => Write(DxLogLevel.Error, message, null);

    public static void Exception(string message, Exception exception) => Write(DxLogLevel.Error, message, exception);

    public static void Write(DxLogLevel level, string message, Exception? exception)
    {
        if (level < _minimumLevel || _minimumLevel == DxLogLevel.None)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var text = string.IsNullOrEmpty(exception?.ToString())
            ? $"{now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] [pid:{Environment.ProcessId}] {message}"
            : $"{now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] [pid:{Environment.ProcessId}] {message}{Environment.NewLine}{exception}";

        System.Diagnostics.Debug.WriteLine(text);

        lock (Gate)
        {
            if (_directory is null)
            {
                return;
            }

            try
            {
                var file = Path.Combine(_directory, $"DxTools-{now:yyyyMMdd}.log");
                CurrentFile = file;
                File.AppendAllText(file, text + Environment.NewLine, System.Text.Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
