#include "DxToolsNative.h"

#include <windows.h>
#include <mmreg.h>
#include <mmdeviceapi.h>
#include <functiondiscoverykeys_devpkey.h>
#include <propsys.h>
#include <propvarutil.h>
#include <combaseapi.h>

#include <algorithm>
#include <atomic>
#include <mutex>
#include <thread>
#include <cstdlib>
#include <cstring>
#include <string>
#include <utility>
#include <vector>

#ifndef QDC_ONLY_ACTIVE_PATHS
#define QDC_ONLY_ACTIVE_PATHS 0x00000002
#endif
#ifndef QDC_VIRTUAL_MODE_AWARE
#define QDC_VIRTUAL_MODE_AWARE 0x00000010
#endif
#ifndef SDC_TOPOLOGY_SUPPLIED
#define SDC_TOPOLOGY_SUPPLIED 0x00000010
#endif
#ifndef SDC_USE_SUPPLIED_DISPLAY_CONFIG
#define SDC_USE_SUPPLIED_DISPLAY_CONFIG 0x00000020
#endif
#ifndef SDC_VALIDATE
#define SDC_VALIDATE 0x00000040
#endif
#ifndef SDC_APPLY
#define SDC_APPLY 0x00000080
#endif
#ifndef SDC_SAVE_TO_DATABASE
#define SDC_SAVE_TO_DATABASE 0x00000200
#endif
#ifndef SDC_ALLOW_CHANGES
#define SDC_ALLOW_CHANGES 0x00000400
#endif

namespace
{
constexpr uint32_t kStateMagic = 0x44585354u;
constexpr uint16_t kStateVersion = 1;

#pragma pack(push, 8)
struct StateHeader
{
    uint32_t magic;
    uint16_t version;
    uint16_t headerSize;
    uint32_t pathSize;
    uint32_t modeSize;
    uint32_t pathCount;
    uint32_t modeCount;
    uint32_t flags;
};
#pragma pack(pop)

struct TargetName
{
    std::wstring monitorId;
    std::wstring friendlyName;
};

void CopyWide(wchar_t* destination, size_t capacity, const wchar_t* source)
{
    if (capacity == 0)
    {
        return;
    }

    if (source == nullptr)
    {
        destination[0] = L'\0';
        return;
    }

    size_t index = 0;
    for (; index < capacity - 1 && source[index] != L'\0'; ++index)
    {
        destination[index] = source[index];
    }

    destination[index] = L'\0';
}

LONG QueryActiveConfig(std::vector<DISPLAYCONFIG_PATH_INFO>& paths, std::vector<DISPLAYCONFIG_MODE_INFO>& modes)
{
    for (int attempt = 0; attempt < 3; ++attempt)
    {
        UINT32 pathCount = 0;
        UINT32 modeCount = 0;
        LONG result = GetDisplayConfigBufferSizes(
            QDC_ONLY_ACTIVE_PATHS | QDC_VIRTUAL_MODE_AWARE,
            &pathCount,
            &modeCount);

        if (result != ERROR_SUCCESS)
        {
            return result;
        }

        paths.resize(pathCount);
        modes.resize(modeCount);

        result = QueryDisplayConfig(
            QDC_ONLY_ACTIVE_PATHS | QDC_VIRTUAL_MODE_AWARE,
            &pathCount,
            paths.data(),
            &modeCount,
            modes.data(),
            nullptr);

        if (result == ERROR_SUCCESS)
        {
            paths.resize(pathCount);
            modes.resize(modeCount);
            return ERROR_SUCCESS;
        }

        if (result != ERROR_INSUFFICIENT_BUFFER)
        {
            return result;
        }
    }

    return ERROR_INSUFFICIENT_BUFFER;
}

std::vector<std::pair<std::wstring, TargetName>> QueryTargetNames()
{
    std::vector<DISPLAYCONFIG_PATH_INFO> paths;
    std::vector<DISPLAYCONFIG_MODE_INFO> modes;
    std::vector<std::pair<std::wstring, TargetName>> names;

    if (QueryActiveConfig(paths, modes) != ERROR_SUCCESS)
    {
        return names;
    }

    for (const auto& path : paths)
    {
        DISPLAYCONFIG_SOURCE_DEVICE_NAME sourceName{};
        sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
        sourceName.header.size = sizeof(sourceName);
        sourceName.header.adapterId = path.sourceInfo.adapterId;
        sourceName.header.id = path.sourceInfo.id;

        if (DisplayConfigGetDeviceInfo(&sourceName.header) != ERROR_SUCCESS)
        {
            continue;
        }

        DISPLAYCONFIG_TARGET_DEVICE_NAME targetName{};
        targetName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        targetName.header.size = sizeof(targetName);
        targetName.header.adapterId = path.targetInfo.adapterId;
        targetName.header.id = path.targetInfo.id;

        const bool hasTarget = DisplayConfigGetDeviceInfo(&targetName.header) == ERROR_SUCCESS;

        TargetName name{};
        if (hasTarget)
        {
            name.monitorId = targetName.monitorDevicePath;
            name.friendlyName = targetName.monitorFriendlyDeviceName;
        }

        names.emplace_back(sourceName.viewGdiDeviceName, std::move(name));
    }

    return names;
}

UINT GetMonitorDpi(HMONITOR monitor)
{
    using GetDpiForMonitorFn = HRESULT(WINAPI*)(HMONITOR, int, UINT*, UINT*);

    static GetDpiForMonitorFn getDpiForMonitor = []() -> GetDpiForMonitorFn
    {
        HMODULE library = LoadLibraryW(L"shcore.dll");
        if (library == nullptr)
        {
            return nullptr;
        }

        FARPROC procedure = GetProcAddress(library, "GetDpiForMonitor");
        GetDpiForMonitorFn function = nullptr;
        static_assert(sizeof(function) == sizeof(procedure), "Unexpected function pointer size.");
        memcpy(&function, &procedure, sizeof(function));
        return function;
    }();

    UINT dpiX = 96;
    UINT dpiY = 96;
    if (getDpiForMonitor != nullptr && SUCCEEDED(getDpiForMonitor(monitor, 0, &dpiX, &dpiY)))
    {
        return dpiX;
    }

    return 96;
}

struct MonitorEnumContext
{
    std::vector<DxTools_MonitorInfo>* monitors;
    const std::vector<std::pair<std::wstring, TargetName>>* targetNames;
};

BOOL CALLBACK MonitorEnumProc(HMONITOR monitor, HDC, LPRECT, LPARAM parameter)
{
    auto* context = reinterpret_cast<MonitorEnumContext*>(parameter);

    MONITORINFOEXW monitorInfo{};
    monitorInfo.cbSize = sizeof(monitorInfo);
    if (!GetMonitorInfoW(monitor, &monitorInfo))
    {
        return TRUE;
    }

    DxTools_MonitorInfo info{};
    CopyWide(info.deviceName, 32, monitorInfo.szDevice);
    info.x = monitorInfo.rcMonitor.left;
    info.y = monitorInfo.rcMonitor.top;
    info.width = monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left;
    info.height = monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top;
    info.isPrimary = (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0 ? 1u : 0u;
    info.dpi = GetMonitorDpi(monitor);

    DEVMODEW mode{};
    mode.dmSize = sizeof(mode);
    if (EnumDisplaySettingsExW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, &mode, 0))
    {
        info.refreshRate = mode.dmDisplayFrequency > 1 ? mode.dmDisplayFrequency : 0;
        info.bitsPerPixel = mode.dmBitsPerPel;
    }

    if (context->targetNames != nullptr)
    {
        for (const auto& entry : *context->targetNames)
        {
            if (_wcsicmp(entry.first.c_str(), monitorInfo.szDevice) == 0)
            {
                CopyWide(info.monitorId, 256, entry.second.monitorId.c_str());
                CopyWide(info.friendlyName, 128, entry.second.friendlyName.c_str());
                break;
            }
        }
    }

    if (info.monitorId[0] == L'\0')
    {
        DISPLAY_DEVICEW displayDevice{};
        displayDevice.cb = sizeof(displayDevice);
        if (EnumDisplayDevicesW(monitorInfo.szDevice, 0, &displayDevice, 0))
        {
            CopyWide(info.monitorId, 256, displayDevice.DeviceID);
            if (info.friendlyName[0] == L'\0')
            {
                CopyWide(info.friendlyName, 128, displayDevice.DeviceString);
            }
        }
    }

    if (info.friendlyName[0] == L'\0')
    {
        CopyWide(info.friendlyName, 128, monitorInfo.szDevice);
    }

    context->monitors->push_back(info);
    return TRUE;
}

std::vector<DxTools_MonitorInfo> EnumerateMonitors()
{
    std::vector<DxTools_MonitorInfo> monitors;
    const auto targetNames = QueryTargetNames();
    MonitorEnumContext context{&monitors, &targetNames};

    EnumDisplayMonitors(nullptr, nullptr, MonitorEnumProc, reinterpret_cast<LPARAM>(&context));

    std::sort(monitors.begin(), monitors.end(), [](const DxTools_MonitorInfo& left, const DxTools_MonitorInfo& right)
    {
        if (left.x != right.x)
        {
            return left.x < right.x;
        }

        return left.y < right.y;
    });

    return monitors;
}
}

namespace
{
std::thread g_hotkeyThread;
std::mutex g_hotkeyMutex;
std::vector<DxTools_HotkeyBinding> g_hotkeyBindings;
HWND g_hotkeyTarget = nullptr;
UINT g_hotkeyMessage = 0;
HHOOK g_hotkey = nullptr;
DWORD g_hotkeyThreadId = 0;
HANDLE g_hotkeyReady = nullptr;
std::atomic<bool> g_hotkeyRunning{false};
std::atomic<uint32_t> g_hotkeyPendingId{0};
std::atomic<ULONGLONG> g_hotkeyPendingTick{0};

uint32_t CurrentModifiers()
{
    uint32_t modifiers = 0;
    if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0) modifiers |= 0x0002;
    if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0) modifiers |= 0x0001;
    if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0) modifiers |= 0x0004;
    if ((GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0) modifiers |= 0x0008;
    return modifiers;
}

LRESULT CALLBACK HotkeyHookProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
    {
        const auto* info = reinterpret_cast<KBDLLHOOKSTRUCT*>(lParam);
        const uint32_t modifiers = CurrentModifiers();

        const bool isModifierKey = info->vkCode == VK_CONTROL
            || info->vkCode == VK_LCONTROL
            || info->vkCode == VK_RCONTROL
            || info->vkCode == VK_MENU
            || info->vkCode == VK_LMENU
            || info->vkCode == VK_RMENU
            || info->vkCode == VK_SHIFT
            || info->vkCode == VK_LSHIFT
            || info->vkCode == VK_RSHIFT
            || info->vkCode == VK_LWIN
            || info->vkCode == VK_RWIN;

        if (g_hotkeyPendingId != 0 && isModifierKey)
        {
            return CallNextHookEx(g_hotkey, code, wParam, lParam);
        }

        const ULONGLONG now = GetTickCount64();
        if (g_hotkeyPendingId != 0 && now - g_hotkeyPendingTick <= 1500)
        {
            std::lock_guard<std::mutex> lock(g_hotkeyMutex);
            for (const auto& binding : g_hotkeyBindings)
            {
                if (binding.id == g_hotkeyPendingId
                    && (binding.modifiers & 0x000Fu) == modifiers
                    && binding.virtualKey == info->vkCode)
                {
                    PostMessageW(g_hotkeyTarget, g_hotkeyMessage, static_cast<WPARAM>(binding.id), 0);
                    g_hotkeyPendingId = 0;
                    return 1;
                }
            }
        }

        g_hotkeyPendingId = 0;

        {
            std::lock_guard<std::mutex> lock(g_hotkeyMutex);
            for (const auto& binding : g_hotkeyBindings)
            {
                if (binding.prefixVirtualKey != 0
                    && (binding.prefixModifiers & 0x000Fu) == modifiers
                    && binding.prefixVirtualKey == info->vkCode)
                {
                    g_hotkeyPendingId = binding.id;
                    g_hotkeyPendingTick = now;
                    return 1;
                }
            }

            for (const auto& binding : g_hotkeyBindings)
            {
                if (binding.prefixVirtualKey == 0
                    && (binding.modifiers & 0x000Fu) == modifiers
                    && binding.virtualKey == info->vkCode)
                {
                    PostMessageW(g_hotkeyTarget, g_hotkeyMessage, static_cast<WPARAM>(binding.id), 0);
                    return 1;
                }
            }
        }
    }

    return CallNextHookEx(g_hotkey, code, wParam, lParam);
}

void HotkeyThreadProc()
{
    g_hotkeyThreadId = GetCurrentThreadId();
    g_hotkey = SetWindowsHookExW(WH_KEYBOARD_LL, HotkeyHookProc, GetModuleHandleW(nullptr), 0);
    if (g_hotkeyReady != nullptr)
    {
        SetEvent(g_hotkeyReady);
    }

    if (g_hotkey == nullptr)
    {
        g_hotkeyRunning = false;
        return;
    }

    MSG message{};
    while (GetMessageW(&message, nullptr, 0, 0) > 0)
    {
        TranslateMessage(&message);
        DispatchMessageW(&message);
    }

    UnhookWindowsHookEx(g_hotkey);
    g_hotkey = nullptr;
}
}
namespace
{
std::thread g_mouseHookThread;
HWND g_mouseHookTarget = nullptr;
UINT g_mouseHookMessage = 0;
HHOOK g_mouseHook = nullptr;
DWORD g_mouseHookThreadId = 0;
HANDLE g_mouseHookReady = nullptr;
std::atomic<bool> g_mouseHookRunning{false};
std::atomic<bool> g_mouseHookEnabled{false};
std::atomic<bool> g_mouseRightSuppressed{false};
std::atomic<int32_t> g_taskbarLeft{0};
std::atomic<int32_t> g_taskbarTop{0};
std::atomic<int32_t> g_taskbarRight{0};
std::atomic<int32_t> g_taskbarBottom{0};

bool IsPointOnTaskbar(const POINT& point)
{
    return point.x >= g_taskbarLeft && point.x < g_taskbarRight
        && point.y >= g_taskbarTop && point.y < g_taskbarBottom;
}

LRESULT CALLBACK MouseHookProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION && g_mouseHookEnabled.load())
    {
        if (wParam == WM_RBUTTONUP && g_mouseRightSuppressed.exchange(false))
        {
            const auto* info = reinterpret_cast<MSLLHOOKSTRUCT*>(lParam);
            if (info != nullptr && g_mouseHookTarget != nullptr)
            {
                PostMessageW(
                    g_mouseHookTarget,
                    g_mouseHookMessage,
                    static_cast<WPARAM>(info->pt.x),
                    static_cast<LPARAM>(info->pt.y));
            }

            return 1;
        }

        if (wParam == WM_RBUTTONDOWN)
        {
            const auto* info = reinterpret_cast<MSLLHOOKSTRUCT*>(lParam);
            if (info != nullptr && IsPointOnTaskbar(info->pt)
                && (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                g_mouseRightSuppressed = true;
                return 1;
            }
        }
    }

    return CallNextHookEx(g_mouseHook, code, wParam, lParam);
}

void MouseHookThreadProc()
{
    g_mouseHookThreadId = GetCurrentThreadId();
    g_mouseHook = SetWindowsHookExW(WH_MOUSE_LL, MouseHookProc, GetModuleHandleW(nullptr), 0);
    if (g_mouseHookReady != nullptr)
    {
        SetEvent(g_mouseHookReady);
    }

    if (g_mouseHook == nullptr)
    {
        g_mouseHookRunning = false;
        return;
    }

    MSG message{};
    while (GetMessageW(&message, nullptr, 0, 0) > 0)
    {
        TranslateMessage(&message);
        DispatchMessageW(&message);
    }

    UnhookWindowsHookEx(g_mouseHook);
    g_mouseHook = nullptr;
}
}
namespace
{

const CLSID kMmDeviceEnumeratorClsid = {0xBCDE0395, 0xE52F, 0x467C, {0x8E, 0x3D, 0xC4, 0x57, 0x92, 0x91, 0x69, 0x2E}};
const IID kMmDeviceEnumeratorIid = {0xA95664D2, 0x9614, 0x4F35, {0xA7, 0x46, 0xDE, 0x8D, 0xB6, 0x36, 0x17, 0xE6}};
const CLSID kPolicyConfigClientClsid = {0x870AF99C, 0x171D, 0x4F9E, {0xAF, 0x0D, 0xE6, 0x3D, 0xF4, 0x0C, 0x2B, 0xC9}};
const IID kPolicyConfigIid = {0xF8679F50, 0x850A, 0x41CF, {0x9C, 0x72, 0x43, 0x0F, 0x1B, 0x8F, 0xF2, 0xD1}};

struct IPolicyConfig : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE GetMixFormat(PCWSTR, WAVEFORMATEX**) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetDeviceFormat(PCWSTR, INT, WAVEFORMATEX**) = 0;
    virtual HRESULT STDMETHODCALLTYPE ResetDeviceFormat(PCWSTR) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetDeviceFormat(PCWSTR, WAVEFORMATEX*, WAVEFORMATEX*) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetProcessingPeriod(PCWSTR, INT, PINT64, PINT64) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetProcessingPeriod(PCWSTR, PINT64) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetShareMode(PCWSTR, void*) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetShareMode(PCWSTR, void*) = 0;
    virtual HRESULT STDMETHODCALLTYPE GetPropertyValue(PCWSTR, const PROPERTYKEY&, PROPVARIANT*) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetPropertyValue(PCWSTR, const PROPERTYKEY&, PROPVARIANT*) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetDefaultEndpoint(PCWSTR, ERole) = 0;
    virtual HRESULT STDMETHODCALLTYPE SetEndpointVisibility(PCWSTR, INT) = 0;
};

class ComScope
{
public:
    ComScope()
    {
        result_ = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    }

    ~ComScope()
    {
        if (SUCCEEDED(result_))
        {
            CoUninitialize();
        }
    }

    bool Ok() const
    {
        return SUCCEEDED(result_) || result_ == RPC_E_CHANGED_MODE;
    }

private:
    HRESULT result_ = E_FAIL;
};

std::vector<DxTools_AudioDevice> EnumerateAudioDevices()
{
    std::vector<DxTools_AudioDevice> devices;
    ComScope com;
    if (!com.Ok())
    {
        return devices;
    }

    IMMDeviceEnumerator* enumerator = nullptr;
    if (FAILED(CoCreateInstance(kMmDeviceEnumeratorClsid, nullptr, CLSCTX_ALL, kMmDeviceEnumeratorIid, reinterpret_cast<void**>(&enumerator))))
    {
        return devices;
    }

    IMMDeviceCollection* collection = nullptr;
    if (FAILED(enumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &collection)))
    {
        enumerator->Release();
        return devices;
    }

    LPWSTR defaultId = nullptr;
    IMMDevice* defaultDevice = nullptr;
    if (SUCCEEDED(enumerator->GetDefaultAudioEndpoint(eRender, eConsole, &defaultDevice)))
    {
        defaultDevice->GetId(&defaultId);
    }

    UINT count = 0;
    collection->GetCount(&count);
    for (UINT index = 0; index < count; ++index)
    {
        IMMDevice* device = nullptr;
        if (FAILED(collection->Item(index, &device)))
        {
            continue;
        }

        LPWSTR id = nullptr;
        device->GetId(&id);

        DxTools_AudioDevice info{};
        CopyWide(info.id, 256, id);

        IPropertyStore* store = nullptr;
        if (SUCCEEDED(device->OpenPropertyStore(STGM_READ, &store)))
        {
            PROPVARIANT value;
            PropVariantInit(&value);
            if (SUCCEEDED(store->GetValue(PKEY_Device_FriendlyName, &value)) && value.vt == VT_LPWSTR)
            {
                CopyWide(info.name, 128, value.pwszVal);
            }

            PropVariantClear(&value);
            store->Release();
        }

        if (info.name[0] == L'\0')
        {
            CopyWide(info.name, 128, L"Audio device");
        }

        info.isDefault = id != nullptr && defaultId != nullptr && wcscmp(id, defaultId) == 0 ? 1u : 0u;
        devices.push_back(info);

        if (id != nullptr) CoTaskMemFree(id);
        device->Release();
    }

    if (defaultId != nullptr) CoTaskMemFree(defaultId);
    if (defaultDevice != nullptr) defaultDevice->Release();
    collection->Release();
    enumerator->Release();
    return devices;
}
}
extern "C" {

DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_GetApiVersion(void)
{
    return DXTOOLS_NATIVE_API_VERSION;
}

DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_GetVersion(void)
{
    return DXTOOLS_NATIVE_VERSION;
}

DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_Ping(void)
{
    return 0x4458544Fu;
}

DXTOOLS_NATIVE_EXPORT void __cdecl DxTools_FreeMemory(void* ptr)
{
    free(ptr);
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Audio_GetDeviceCount(uint32_t* count)
{
    if (count == nullptr)
    {
        return ERROR_INVALID_PARAMETER;
    }

    try
    {
        *count = static_cast<uint32_t>(EnumerateAudioDevices().size());
        return ERROR_SUCCESS;
    }
    catch (...)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Audio_GetDevices(
    DxTools_AudioDevice* buffer,
    uint32_t capacity,
    uint32_t* written)
{
    try
    {
        const auto devices = EnumerateAudioDevices();
        const auto required = static_cast<uint32_t>(devices.size());
        if (written != nullptr) *written = required;
        if (buffer == nullptr || capacity < required) return ERROR_INSUFFICIENT_BUFFER;
        if (required > 0) memcpy(buffer, devices.data(), required * sizeof(DxTools_AudioDevice));
        return ERROR_SUCCESS;
    }
    catch (...)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Audio_SetDefault(const wchar_t* deviceId)
{
    if (deviceId == nullptr) return ERROR_INVALID_PARAMETER;

    ComScope com;
    if (!com.Ok()) return E_FAIL;

    IPolicyConfig* policy = nullptr;
    HRESULT result = CoCreateInstance(kPolicyConfigClientClsid, nullptr, CLSCTX_ALL, kPolicyConfigIid, reinterpret_cast<void**>(&policy));
    if (FAILED(result) || policy == nullptr) return result;

    result = policy->SetDefaultEndpoint(deviceId, eConsole);
    if (SUCCEEDED(result)) result = policy->SetDefaultEndpoint(deviceId, eMultimedia);
    if (SUCCEEDED(result)) result = policy->SetDefaultEndpoint(deviceId, eCommunications);
    policy->Release();
    return result;
}
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_Start(void* hwnd, uint32_t message)
{
    if (hwnd == nullptr || message == 0)
    {
        return ERROR_INVALID_PARAMETER;
    }

    if (g_hotkeyRunning.exchange(true))
    {
        return ERROR_ALREADY_EXISTS;
    }

    g_hotkeyTarget = static_cast<HWND>(hwnd);
    g_hotkeyMessage = message;
    g_hotkeyPendingId = 0;
    g_hotkeyReady = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    g_hotkeyThread = std::thread(HotkeyThreadProc);

    if (g_hotkeyReady != nullptr)
    {
        WaitForSingleObject(g_hotkeyReady, 3000);
        CloseHandle(g_hotkeyReady);
        g_hotkeyReady = nullptr;
    }

    if (g_hotkey == nullptr)
    {
        g_hotkeyRunning = false;
        if (g_hotkeyThread.joinable())
        {
            g_hotkeyThread.join();
        }

        return ERROR_INVALID_HOOK_HANDLE;
    }

    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_Stop(void)
{
    if (g_hotkeyRunning.exchange(false))
    {
        if (g_hotkeyThreadId != 0)
        {
            PostThreadMessageW(g_hotkeyThreadId, WM_QUIT, 0, 0);
        }
    }

    if (g_hotkeyThread.joinable())
    {
        g_hotkeyThread.join();
    }

    g_hotkeyThreadId = 0;
    g_hotkeyTarget = nullptr;
    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_SetBindings(
    const DxTools_HotkeyBinding* bindings,
    uint32_t count)
{
    if (bindings == nullptr && count > 0)
    {
        return ERROR_INVALID_PARAMETER;
    }

    std::lock_guard<std::mutex> lock(g_hotkeyMutex);
    g_hotkeyBindings.clear();
    g_hotkeyPendingId = 0;
    for (uint32_t index = 0; index < count; ++index)
    {
        g_hotkeyBindings.push_back(bindings[index]);
    }

    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_Start(void* hwnd, uint32_t message)
{
    if (hwnd == nullptr || message == 0)
    {
        return ERROR_INVALID_PARAMETER;
    }

    if (g_mouseHookRunning.exchange(true))
    {
        g_mouseHookTarget = static_cast<HWND>(hwnd);
        g_mouseHookMessage = message;
        return ERROR_SUCCESS;
    }

    g_mouseHookTarget = static_cast<HWND>(hwnd);
    g_mouseHookMessage = message;
    g_mouseRightSuppressed = false;
    g_mouseHookReady = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    g_mouseHookThread = std::thread(MouseHookThreadProc);

    if (g_mouseHookReady != nullptr)
    {
        WaitForSingleObject(g_mouseHookReady, 3000);
        CloseHandle(g_mouseHookReady);
        g_mouseHookReady = nullptr;
    }

    if (g_mouseHook == nullptr)
    {
        g_mouseHookRunning = false;
        if (g_mouseHookThread.joinable())
        {
            g_mouseHookThread.join();
        }

        return ERROR_INVALID_HOOK_HANDLE;
    }

    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_Stop(void)
{
    g_mouseHookEnabled = false;

    if (g_mouseHookRunning.exchange(false))
    {
        if (g_mouseHookThreadId != 0)
        {
            PostThreadMessageW(g_mouseHookThreadId, WM_QUIT, 0, 0);
        }
    }

    if (g_mouseHookThread.joinable())
    {
        g_mouseHookThread.join();
    }

    g_mouseHookThreadId = 0;
    g_mouseHookTarget = nullptr;
    g_mouseHookMessage = 0;
    g_mouseRightSuppressed = false;
    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_SetTaskbarRect(
    int32_t left,
    int32_t top,
    int32_t right,
    int32_t bottom)
{
    g_taskbarLeft = left;
    g_taskbarTop = top;
    g_taskbarRight = right;
    g_taskbarBottom = bottom;
    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_SetEnabled(int32_t enabled)
{
    g_mouseHookEnabled = enabled != 0;
    if (!enabled)
    {
        g_mouseRightSuppressed = false;
    }

    return ERROR_SUCCESS;
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_GetMonitorCount(uint32_t* count)
{
    if (count == nullptr)
    {
        return ERROR_INVALID_PARAMETER;
    }

    try
    {
        *count = static_cast<uint32_t>(EnumerateMonitors().size());
        return ERROR_SUCCESS;
    }
    catch (...)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_GetMonitors(
    DxTools_MonitorInfo* buffer,
    uint32_t capacity,
    uint32_t* written)
{
    try
    {
        const auto monitors = EnumerateMonitors();
        const auto required = static_cast<uint32_t>(monitors.size());

        if (written != nullptr)
        {
            *written = required;
        }

        if (buffer == nullptr || capacity < required)
        {
            return ERROR_INSUFFICIENT_BUFFER;
        }

        if (required > 0)
        {
            memcpy(buffer, monitors.data(), required * sizeof(DxTools_MonitorInfo));
        }

        return ERROR_SUCCESS;
    }
    catch (...)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_GetState(uint8_t** data, uint32_t* size)
{
    if (data == nullptr || size == nullptr)
    {
        return ERROR_INVALID_PARAMETER;
    }

    *data = nullptr;
    *size = 0;

    try
    {
        std::vector<DISPLAYCONFIG_PATH_INFO> paths;
        std::vector<DISPLAYCONFIG_MODE_INFO> modes;
        const LONG result = QueryActiveConfig(paths, modes);
        if (result != ERROR_SUCCESS)
        {
            return result;
        }

        if (paths.empty())
        {
            return ERROR_NOT_FOUND;
        }

        const size_t pathsBytes = paths.size() * sizeof(DISPLAYCONFIG_PATH_INFO);
        const size_t modesBytes = modes.size() * sizeof(DISPLAYCONFIG_MODE_INFO);
        const size_t total = sizeof(StateHeader) + pathsBytes + modesBytes;

        if (total > UINT32_MAX)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        auto* buffer = static_cast<uint8_t*>(malloc(total));
        if (buffer == nullptr)
        {
            return ERROR_NOT_ENOUGH_MEMORY;
        }

        StateHeader header{};
        header.magic = kStateMagic;
        header.version = kStateVersion;
        header.headerSize = sizeof(StateHeader);
        header.pathSize = sizeof(DISPLAYCONFIG_PATH_INFO);
        header.modeSize = sizeof(DISPLAYCONFIG_MODE_INFO);
        header.pathCount = static_cast<uint32_t>(paths.size());
        header.modeCount = static_cast<uint32_t>(modes.size());
        header.flags = 0;

        memcpy(buffer, &header, sizeof(header));
        memcpy(buffer + sizeof(header), paths.data(), pathsBytes);
        memcpy(buffer + sizeof(header) + pathsBytes, modes.data(), modesBytes);

        *data = buffer;
        *size = static_cast<uint32_t>(total);
        return ERROR_SUCCESS;
    }
    catch (...)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
}

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_ApplyState(
    const uint8_t* data,
    uint32_t size,
    uint32_t flags)
{
    if (data == nullptr || size < sizeof(StateHeader))
    {
        return ERROR_INVALID_PARAMETER;
    }

    if (flags != 0)
    {
        return ERROR_INVALID_PARAMETER;
    }

    try
    {
        StateHeader header{};
        memcpy(&header, data, sizeof(header));

        if (header.magic != kStateMagic
            || header.version != kStateVersion
            || header.headerSize != sizeof(StateHeader)
            || header.pathSize != sizeof(DISPLAYCONFIG_PATH_INFO)
            || header.modeSize != sizeof(DISPLAYCONFIG_MODE_INFO)
            || header.pathCount == 0)
        {
            return ERROR_INVALID_DATA;
        }

        const size_t pathsBytes = static_cast<size_t>(header.pathCount) * header.pathSize;
        const size_t modesBytes = static_cast<size_t>(header.modeCount) * header.modeSize;
        const size_t expected = sizeof(StateHeader) + pathsBytes + modesBytes;

        if (expected != size)
        {
            return ERROR_INVALID_DATA;
        }

        std::vector<DISPLAYCONFIG_PATH_INFO> paths(header.pathCount);
        std::vector<DISPLAYCONFIG_MODE_INFO> modes(header.modeCount);

        memcpy(paths.data(), data + sizeof(header), pathsBytes);
        if (modesBytes > 0)
        {
            memcpy(modes.data(), data + sizeof(header) + pathsBytes, modesBytes);
        }

        const UINT32 combinations[] = {
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_ALLOW_CHANGES,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES | SDC_VIRTUAL_MODE_AWARE,
            SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES | SDC_TOPOLOGY_SUPPLIED,
        };

        LONG lastError = ERROR_INVALID_PARAMETER;
        for (const UINT32 combinationFlags : combinations)
        {
            const LONG result = SetDisplayConfig(
                header.pathCount,
                paths.data(),
                header.modeCount,
                modes.data(),
                combinationFlags);

            if (result == ERROR_SUCCESS)
            {
                return ERROR_SUCCESS;
            }

            lastError = result;
        }

        return lastError;
    }
    catch (...)
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }
}

}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module;
    (void)reason;
    (void)reserved;
    return TRUE;
}
