#pragma once

#include <stdint.h>
#include <wchar.h>

#define DXTOOLS_NATIVE_API_VERSION 6u
#define DXTOOLS_NATIVE_VERSION 0x00000800u

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
#define DXTOOLS_HOTKEY_MAX_STEPS 4
#define DXTOOLS_HOTKEY_PROCESS_NAME 64

typedef struct DxTools_HotkeyStep
{
    uint32_t modifiers;
    uint32_t virtualKey;
} DxTools_HotkeyStep;

typedef struct DxTools_HotkeyBinding
{
    uint32_t id;
    uint32_t stepCount;
    DxTools_HotkeyStep steps[DXTOOLS_HOTKEY_MAX_STEPS];
    uint32_t scope;
    wchar_t processName[DXTOOLS_HOTKEY_PROCESS_NAME];
    uint32_t reserved;
} DxTools_HotkeyBinding;

typedef struct DxTools_AudioDevice
{
    wchar_t id[256];
    wchar_t name[128];
    uint32_t isDefault;
    uint32_t reserved;
} DxTools_AudioDevice;

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Audio_GetDeviceCount(uint32_t* count);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Audio_GetDevices(
    DxTools_AudioDevice* buffer,
    uint32_t capacity,
    uint32_t* written);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Audio_SetDefault(const wchar_t* deviceId);

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_Start(void* hwnd, uint32_t message);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_Stop(void);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Hotkey_SetBindings(
    const DxTools_HotkeyBinding* bindings,
    uint32_t count);

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_Start(void* hwnd, uint32_t message);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_Stop(void);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_SetTaskbarRect(
    int32_t left,
    int32_t top,
    int32_t right,
    int32_t bottom);
DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_MouseHook_SetEnabled(int32_t enabled);

DXTOOLS_NATIVE_EXPORT int32_t __cdecl DxTools_Display_ApplyState(
    const uint8_t* data,
    uint32_t size,
    uint32_t flags);

#ifdef __cplusplus
}
#endif
