using System.Security.Principal;

namespace DxTools.Core;

public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DxTools";

    private readonly string _executablePath;

    public AutostartService(string? executablePath = null)
    {
        _executablePath = string.IsNullOrWhiteSpace(executablePath)
            ? Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DxTools.exe")
            : executablePath;
    }

    public bool IsEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(ValueName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to open the current user Run registry key.");

        if (enabled)
        {
            key.SetValue(ValueName, $"\"{_executablePath}\" --autostart", Microsoft.Win32.RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    public string? GetRegisteredCommand()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) as string;
    }

    private static string GetUserSid()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? Environment.UserName;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Environment.UserName;
        }
    }

    public static string InstanceSuffix => GetUserSid();
}
