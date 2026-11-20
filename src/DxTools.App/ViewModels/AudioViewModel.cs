using System.Collections.ObjectModel;
using System.Windows.Input;
using DxTools.App.Properties;
using DxTools.Engine;
using DxTools.Interop;

namespace DxTools.App.ViewModels;

public sealed class AudioDeviceItemViewModel
{
    public AudioDeviceItemViewModel(AudioDevice device, Action<AudioDevice> setDefault)
    {
        Device = device;
        SetDefaultCommand = new RelayCommand(() => setDefault(device));
    }

    public AudioDevice Device { get; }

    public string Name => Device.Name;

    public bool IsDefault => Device.IsDefault;

    public string StatusText => IsDefault ? Strings.AudioDefault : string.Empty;

    public ICommand SetDefaultCommand { get; }
}

public sealed class AudioViewModel : ObservableObject
{
    private readonly AudioService _service;
    private string _statusMessage = string.Empty;

    public AudioViewModel(AudioService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        RefreshCommand = new RelayCommand(Refresh);
        Refresh();
    }

    public ObservableCollection<AudioDeviceItemViewModel> Devices { get; } = [];

    public ICommand RefreshCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        Devices.Clear();
        foreach (var device in _service.GetDevices().OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Devices.Add(new AudioDeviceItemViewModel(device, SetDefault));
        }
    }

    private void SetDefault(AudioDevice device)
    {
        var result = _service.SetDefault(device.Id);
        StatusMessage = result.Success ? Strings.AudioDefaultSet : $"{Strings.AudioDefaultFailed} {result.Error}";
        Refresh();
    }
}
