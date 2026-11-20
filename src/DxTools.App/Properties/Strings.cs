using System.Globalization;
using System.Resources;

namespace DxTools.App.Properties;

public static class Strings
{
    private static readonly ResourceManager ResourceManager = new(
        "DxTools.App.Properties.Resources",
        typeof(Strings).Assembly);

    public static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    public static string AppTitle => Get(nameof(AppTitle));
    public static string AppSubtitle => Get(nameof(AppSubtitle));
    public static string PhaseLabel => Get(nameof(PhaseLabel));
    public static string GeneralTab => Get(nameof(GeneralTab));
    public static string DiagnosticsTab => Get(nameof(DiagnosticsTab));
    public static string AboutTab => Get(nameof(AboutTab));
    public static string StartWithWindows => Get(nameof(StartWithWindows));
    public static string StartWithWindowsHint => Get(nameof(StartWithWindowsHint));
    public static string StartMinimized => Get(nameof(StartMinimized));
    public static string StartMinimizedHint => Get(nameof(StartMinimizedHint));
    public static string Language => Get(nameof(Language));
    public static string LanguageHint => Get(nameof(LanguageHint));
    public static string LanguageAuto => Get(nameof(LanguageAuto));
    public static string LanguageRussian => Get(nameof(LanguageRussian));
    public static string LanguageEnglish => Get(nameof(LanguageEnglish));
    public static string Theme => Get(nameof(Theme));
    public static string ThemeHint => Get(nameof(ThemeHint));
    public static string ThemeSystem => Get(nameof(ThemeSystem));
    public static string ThemeLight => Get(nameof(ThemeLight));
    public static string ThemeDark => Get(nameof(ThemeDark));
    public static string RestartRequired => Get(nameof(RestartRequired));
    public static string DiagnosticsTitle => Get(nameof(DiagnosticsTitle));
    public static string NativeStatus => Get(nameof(NativeStatus));
    public static string NativeAvailable => Get(nameof(NativeAvailable));
    public static string NativeUnavailable => Get(nameof(NativeUnavailable));
    public static string SafeMode => Get(nameof(SafeMode));
    public static string SafeModeHint => Get(nameof(SafeModeHint));
    public static string ApiVersion => Get(nameof(ApiVersion));
    public static string LibraryVersion => Get(nameof(LibraryVersion));
    public static string Ping => Get(nameof(Ping));
    public static string PingOk => Get(nameof(PingOk));
    public static string PingFailed => Get(nameof(PingFailed));
    public static string NotAvailable => Get(nameof(NotAvailable));
    public static string CheckedAt => Get(nameof(CheckedAt));
    public static string LogsTitle => Get(nameof(LogsTitle));
    public static string LogsHint => Get(nameof(LogsHint));
    public static string OpenLogs => Get(nameof(OpenLogs));
    public static string SelfCheck => Get(nameof(SelfCheck));
    public static string AboutTitle => Get(nameof(AboutTitle));
    public static string Version => Get(nameof(Version));
    public static string License => Get(nameof(License));
    public static string Repository => Get(nameof(Repository));
    public static string OpenRepository => Get(nameof(OpenRepository));
    public static string Exit => Get(nameof(Exit));
    public static string Open => Get(nameof(Open));
    public static string TrayStatus => Get(nameof(TrayStatus));
    public static string StatusReady => Get(nameof(StatusReady));
    public static string StatusUnavailable => Get(nameof(StatusUnavailable));
    public static string StatusSafeMode => Get(nameof(StatusSafeMode));
    public static string SaveFailed => Get(nameof(SaveFailed));
    public static string AutostartFailed => Get(nameof(AutostartFailed));
    public static string SelfCheckDone => Get(nameof(SelfCheckDone));
    public static string FirstRunTitle => Get(nameof(FirstRunTitle));
    public static string FirstRunText => Get(nameof(FirstRunText));
    public static string CrashTitle => Get(nameof(CrashTitle));
    public static string CrashText => Get(nameof(CrashText));
    public static string HelpText => Get(nameof(HelpText));
    public static string CloseToTray => Get(nameof(CloseToTray));
    public static string NativePath => Get(nameof(NativePath));
    public static string Status => Get(nameof(Status));
    public static string Healthy => Get(nameof(Healthy));
    public static string Unhealthy => Get(nameof(Unhealthy));
    public static string SettingsSaved => Get(nameof(SettingsSaved));
    public static string LogFolderUnavailable => Get(nameof(LogFolderUnavailable));
    public static string Phase0Note => Get(nameof(Phase0Note));
    public static string LicenseName => Get(nameof(LicenseName));
}
