using System.Windows.Input;
using System.Windows.Media;
using DxTools.App.Services;

namespace DxTools.App.ViewModels;

public sealed class TrayItemViewModel
{
    private readonly TrayItem _item;

    public TrayItemViewModel(TrayItem item, Action<TrayItem> click)
    {
        _item = item;
        ClickCommand = new RelayCommand(() => click(_item));
    }

    public string Name => _item.Name;

    public ImageSource? Icon => _item.Icon;

    public ICommand ClickCommand { get; }
}
