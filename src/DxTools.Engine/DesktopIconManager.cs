using DxTools.Core;

namespace DxTools.Engine;

public sealed record DesktopIconOperationResult(bool Success, string? Error, DesktopIconProfile? Profile);

public sealed class DesktopIconManager
{
    private readonly object _gate = new();
    private readonly DesktopIconService _service;
    private readonly DesktopIconProfileStore _store;
    private readonly List<DesktopIconProfile> _profiles;

    public DesktopIconManager(DesktopIconService? service = null, DesktopIconProfileStore? store = null)
    {
        _service = service ?? new DesktopIconService();
        _store = store ?? new DesktopIconProfileStore();
        _profiles = _store.Load().ToList();
    }

    public IReadOnlyList<DesktopIconProfile> Profiles => _profiles;

    public (int Count, string? Error) GetCurrentCount()
    {
        var snapshot = _service.Capture();
        return snapshot.Success
            ? (snapshot.IconCount, null)
            : (0, snapshot.Error);
    }

    public DesktopIconOperationResult Capture(string? name)
    {
        lock (_gate)
        {
            var snapshot = _service.Capture();
            if (!snapshot.Success)
            {
                return new DesktopIconOperationResult(false, snapshot.Error, null);
            }

            var profile = new DesktopIconProfile
            {
                Name = string.IsNullOrWhiteSpace(name) ? $"Desktop {_profiles.Count + 1}" : name.Trim(),
                CreatedAt = DateTimeOffset.Now,
                Icons = snapshot.Icons.ToList(),
            };

            _profiles.Add(profile);
            var saved = TrySave();
            return saved.Success
                ? new DesktopIconOperationResult(true, null, profile)
                : new DesktopIconOperationResult(false, saved.Error, profile);
        }
    }

    public DesktopIconOperationResult Delete(string profileId)
    {
        lock (_gate)
        {
            var profile = _profiles.FirstOrDefault(item => item.Id == profileId);
            if (profile is null)
            {
                return new DesktopIconOperationResult(false, "Profile not found.", null);
            }

            _profiles.Remove(profile);
            var saved = TrySave();
            return saved.Success
                ? new DesktopIconOperationResult(true, null, profile)
                : new DesktopIconOperationResult(false, saved.Error, profile);
        }
    }

    public DesktopIconApplyResult Apply(DesktopIconProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return _service.Apply(profile.Icons);
    }

    private (bool Success, string? Error) TrySave()
    {
        try
        {
            _store.Save(_profiles);
            return (true, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            DxLog.Exception("Failed to save desktop icon profiles.", ex);
            return (false, ex.Message);
        }
    }
}
