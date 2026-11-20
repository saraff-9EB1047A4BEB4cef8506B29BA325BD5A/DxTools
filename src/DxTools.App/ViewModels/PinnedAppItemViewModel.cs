using System.IO;
using System.Windows.Input;
using DxTools.Core;

namespace DxTools.App.ViewModels;

public sealed class PinnedAppItemViewModel
{
    private readonly PinnedApp _app;
    private readonly Action<PinnedApp> _remove;

    public PinnedAppItemViewModel(PinnedApp app, Action<PinnedApp> remove)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        RemoveCommand = new RelayCommand(() => _remove(_app));
    }

    public PinnedApp App => _app;

    public string Name => string.IsNullOrWhiteSpace(_app.Name)
        ? Path.GetFileNameWithoutExtension(_app.Path)
        : _app.Name;

    public string PathText => _app.Path;

    public ICommand RemoveCommand { get; }
}
