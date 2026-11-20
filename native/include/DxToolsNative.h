#pragma once

#include <stdint.h>
#include <wchar.h>

#define DXTOOLS_NATIVE_API_VERSION 3u
#define DXTOOLS_NATIVE_VERSION 0x00000500u

#ifdef __cplusplus
extern "C" {
#endif

#define DXTOOLS_NATIVE_EXPORT __declspec(dllexport)

typedef struct DxTools_MonitorInfo
{
    wchar_t deviceName[32];
    wchar_t monitorId[256];
    wchar_t friendlyName[128];
    int32_t x;
    int32_t y;
    int32_t width;
    int32_t height;
    uint32_t isPrimary;
    uint32_t dpi;
    uint32_t refreshRate;
    uint32_t bitsPerPixel;
} DxTools_MonitorInfo;

DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_GetApiVersion(void);
DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_GetVersion(void);
DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_Ping(void);
DXTOOLS_NATIVE_EXPORT void __cdecl DxTools_FreeMemory(void* ptr);

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_GetMonitorCount(uint32_t* count);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_GetMonitors(
    DxTools_MonitorInfo* buffer,
    uint32_t capacity,
    uint32_t* written);

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_GetState(uint8_t** data, uint32_t* size);
typedef struct DxTools_HotkeyBinding
{
    uint32_t id;
    uint32_t modifiers;
    uint32_t virtualKey;
    uint32_t prefixModifiers;
    uint32_t prefixVirtualKey;
    uint32_t reserved;
} DxTools_HotkeyBinding;

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_Start(void* hwnd, uint32_t message);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_Stop(void);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_SetBindings(
    const DxTools_HotkeyBinding* bindings,
    uint32_t count);

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_ApplyState(
    const uint8_t* data,
    uint32_t size,
    uint32_t flags);

#ifdef __cplusplus
}
#endif
