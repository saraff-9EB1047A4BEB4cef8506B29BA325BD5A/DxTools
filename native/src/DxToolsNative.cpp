#include "DxToolsNative.h"

#include <windows.h>

#include <algorithm>
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
