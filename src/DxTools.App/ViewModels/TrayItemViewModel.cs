using System.Windows.Input;
using System.Windows.Media;
using DxTools.App.Services;

namespace DxTools.App.ViewModels;

public sealed class TrayItemViewModel
{
    private readonly TrayItem _item;

    public TrayItemViewModel(
        TrayItem item,
        Action<TrayItem> click,
        Action<TrayItem> contextMenu)
    {
        _item = item;
        ClickCommand = new RelayCommand(() => click(_item));
        ContextMenuCommand = new RelayCommand(() => contextMenu(_item));
    }

    public string Name => _item.Name;

    public ImageSource? Icon => _item.Icon;

    public ICommand ClickCommand { get; }

    public ICommand ContextMenuCommand { get; }
}
