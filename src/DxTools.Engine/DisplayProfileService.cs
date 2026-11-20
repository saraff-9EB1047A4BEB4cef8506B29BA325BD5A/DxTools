using DxTools.Core;

namespace DxTools.Engine;

public sealed record DisplayProfileOperationResult(bool Success, string? Error, DisplayProfile? Profile);

public sealed class DisplayProfileService
{
    private readonly object _gate = new();
    private readonly DisplayService _display;
    private readonly DisplayProfileStore _store;
    private readonly List<DisplayProfile> _profiles;
    private bool _applying;
    private DateTimeOffset _suppressAutoApplyUntil;

    public DisplayProfileService(DisplayService display, DisplayProfileStore? store = null)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _store = store ?? new DisplayProfileStore();
        _profiles = _store.Load().ToList();
    }

    public IReadOnlyList<DisplayProfile> Profiles => _profiles;

    public bool IsSuppressed => _applying || DateTimeOffset.Now < _suppressAutoApplyUntil;

    public DisplayProfileOperationResult CaptureCurrent(string? name, bool autoApply)
    {
        lock (_gate)
        {
            var monitors = _display.GetMonitors();
            if (monitors.Count == 0)
            {
                return new DisplayProfileOperationResult(false, "No monitors were detected.", null);
            }

            var state = _display.CaptureState();
            if (!state.Success || state.State is null)
            {
                return new DisplayProfileOperationResult(false, state.Error ?? "Display state capture failed.", null);
            }

            var profileName = string.IsNullOrWhiteSpace(name)
                ? $"Profile {_profiles.Count + 1}"
                : name.Trim();

            var profile = new DisplayProfile
            {
                Name = profileName,
                CreatedAt = DateTimeOffset.Now,
                TopologyHash = _display.ComputeTopologyHash(monitors),
                StateBase64 = Convert.ToBase64String(state.State),
                Monitors = monitors.ToList(),
                AutoApply = autoApply,
            };

            _profiles.Add(profile);
            var saved = TrySave();

            return saved.Success
                ? new DisplayProfileOperationResult(true, null, profile)
                : new DisplayProfileOperationResult(false, saved.Error, profile);
        }
    }

    public DisplayProfileOperationResult Delete(string profileId)
    {
        lock (_gate)
        {
            var profile = _profiles.FirstOrDefault(item => item.Id == profileId);
            if (profile is null)
            {
                return new DisplayProfileOperationResult(false, "Profile not found.", null);
            }

            _profiles.Remove(profile);
            var saved = TrySave();
            return saved.Success
                ? new DisplayProfileOperationResult(true, null, profile)
                : new DisplayProfileOperationResult(false, saved.Error, profile);
        }
    }

    public DisplayProfileOperationResult Rename(string profileId, string name)
    {
        lock (_gate)
        {
            var profile = _profiles.FirstOrDefault(item => item.Id == profileId);
            if (profile is null)
            {
                return new DisplayProfileOperationResult(false, "Profile not found.", null);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return new DisplayProfileOperationResult(false, "Profile name is empty.", profile);
            }

            profile.Name = name.Trim();
            var saved = TrySave();
            return saved.Success
                ? new DisplayProfileOperationResult(true, null, profile)
                : new DisplayProfileOperationResult(false, saved.Error, profile);
        }
    }

    public DisplayProfileOperationResult SetAutoApply(string profileId, bool autoApply)
    {
        lock (_gate)
        {
            var profile = _profiles.FirstOrDefault(item => item.Id == profileId);
            if (profile is null)
            {
                return new DisplayProfileOperationResult(false, "Profile not found.", null);
            }

            var previous = profile.AutoApply;
            profile.AutoApply = autoApply;
            var saved = TrySave();
            if (!saved.Success)
            {
                profile.AutoApply = previous;
            }

            return saved.Success
                ? new DisplayProfileOperationResult(true, null, profile)
                : new DisplayProfileOperationResult(false, saved.Error, profile);
        }
    }

    public DisplayApplyResult Apply(DisplayProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!profile.HasState)
        {
            return new DisplayApplyResult(false, "Profile does not contain a display state.", null);
        }

        byte[] state;
        try
        {
            state = Convert.FromBase64String(profile.StateBase64);
        }
        catch (FormatException)
        {
            return new DisplayApplyResult(false, "Profile state is corrupted.", null);
        }

        lock (_gate)
        {
            if (_applying)
            {
                return new DisplayApplyResult(false, "Another display change is in progress.", null);
            }

            _applying = true;
            try
            {
                var result = _display.ApplyState(state);
                if (!result.Success)
                {
                    _suppressAutoApplyUntil = DateTimeOffset.Now.AddSeconds(4);
                }

                return result;
            }
            finally
            {
                _applying = false;
                _suppressAutoApplyUntil = DateTimeOffset.Now.AddSeconds(4);
            }
        }
    }

    public DisplayApplyResult Rollback(byte[] state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_gate)
        {
            _applying = true;
            try
            {
                return _display.ApplyState(state);
            }
            finally
            {
                _applying = false;
                _suppressAutoApplyUntil = DateTimeOffset.Now.AddSeconds(4);
            }
        }
    }

    public DisplayApplyResult? TryAutoApply(string topologyHash)
    {
        if (IsSuppressed)
        {
            return null;
        }

        var profile = _profiles.FirstOrDefault(
            item => item.AutoApply && string.Equals(item.TopologyHash, topologyHash, StringComparison.OrdinalIgnoreCase));

        return profile is null ? null : Apply(profile);
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
            DxLog.Exception("Failed to save display profiles.", ex);
            return (false, ex.Message);
        }
    }
}
