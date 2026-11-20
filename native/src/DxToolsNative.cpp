#include "DxToolsNative.h"

#include <windows.h>

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

}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)module;
    (void)reason;
    (void)reserved;
    return TRUE;
}
