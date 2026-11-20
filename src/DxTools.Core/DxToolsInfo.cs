using System.Reflection;

namespace DxTools.Core;

public static class DxToolsInfo
{
    public const string ProductName = "DxTools";
    public const string LicenseId = "GPL-2.0-only";
    public const string RepositoryUrl = "https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools";
    public const string Copyright = "Copyright (C) 2026 DxTools contributors";

    private static readonly Assembly ProductAssembly = Assembly.GetEntryAssembly() ?? typeof(DxToolsInfo).Assembly;

    public static string Version =>
        ProductAssembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static string InformationalVersion
    {
        get
        {
            var version = ProductAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (string.IsNullOrWhiteSpace(version))
            {
                return Version;
            }

            var metadataIndex = version.IndexOf('+', StringComparison.Ordinal);
            return metadataIndex < 0 ? version : version[..metadataIndex];
        }
    }
}
