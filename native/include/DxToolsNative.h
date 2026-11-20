#pragma once

#include <stdint.h>

#define DXTOOLS_NATIVE_API_VERSION 1u
#define DXTOOLS_NATIVE_VERSION 0x00000100u

#ifdef __cplusplus
extern "C" {
#endif

#define DXTOOLS_NATIVE_EXPORT __declspec(dllexport)

DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_GetApiVersion(void);
DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_GetVersion(void);
DXTOOLS_NATIVE_EXPORT uint32_t __cdecl DxTools_Ping(void);

#ifdef __cplusplus
}
#endif
