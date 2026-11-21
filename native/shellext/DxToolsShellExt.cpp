#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <windows.h>
#include <shlobj.h>
#include <shobjidl.h>
#include <shlguid.h>
#include <shellapi.h>
#include <shlwapi.h>
#include <strsafe.h>

#include <string.h>
#include <wchar.h>

#include <new>
#include <string>
#include <utility>
#include <vector>

namespace
{
    // {C7B775ED-1A48-4F52-9B91-6FE8B9B22097}
    const CLSID kShellExtClsid =
    { 0xc7b775ed, 0x1a48, 0x4f52, { 0x9b, 0x91, 0x6f, 0xe8, 0xb9, 0xb2, 0x20, 0x97 } };

    const wchar_t kBackgroundHandlerKey[] =
        L"Software\\Classes\\Directory\\Background\\shellex\\ContextMenuHandlers\\DxTools";
    const wchar_t kImageHandlerKey[] =
        L"Software\\Classes\\SystemFileAssociations\\image\\shellex\\ContextMenuHandlers\\DxTools";
    const wchar_t kAppPathKey[] =
        L"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\DxTools.exe";

    HINSTANCE g_instance = nullptr;
    LONG g_objectCount = 0;
    LONG g_serverLocks = 0;

    bool IsRussian()
    {
        return PRIMARYLANGID(GetUserDefaultUILanguage()) == LANG_RUSSIAN;
    }

    const wchar_t* OpenSettingsText()
    {
        return IsRussian() ? L"DxTools: \u041e\u0442\u043a\u0440\u044b\u0442\u044c \u043d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438"
                           : L"DxTools: Open settings";
    }

    const wchar_t* ApplyWallpaperText()
    {
        return IsRussian() ? L"DxTools: \u041f\u0440\u0438\u043c\u0435\u043d\u0438\u0442\u044c \u043e\u0431\u043e\u0438"
                           : L"DxTools: Apply wallpaper";
    }

    const wchar_t* SetWallpaperText()
    {
        return IsRussian() ? L"DxTools: \u0421\u0434\u0435\u043b\u0430\u0442\u044c \u043e\u0431\u043e\u044f\u043c\u0438"
                           : L"DxTools: Set as wallpaper";
    }

    const wchar_t* OpenSettingsVerb()
    {
        return L"DxToolsOpenSettings";
    }

    const wchar_t* ApplyWallpaperVerb()
    {
        return L"DxToolsApplyWallpaper";
    }

    const wchar_t* SetWallpaperVerb()
    {
        return L"DxToolsSetWallpaper";
    }

    bool IsImageFile(const std::wstring& path)
    {
        const wchar_t* extension = PathFindExtensionW(path.c_str());
        if (extension == nullptr || *extension == L'\0')
        {
            return false;
        }

        return _wcsicmp(extension, L".bmp") == 0
            || _wcsicmp(extension, L".gif") == 0
            || _wcsicmp(extension, L".jpg") == 0
            || _wcsicmp(extension, L".jpeg") == 0
            || _wcsicmp(extension, L".png") == 0
            || _wcsicmp(extension, L".tif") == 0
            || _wcsicmp(extension, L".tiff") == 0;
    }

    std::wstring GetModulePath()
    {
        if (g_instance == nullptr)
        {
            return std::wstring();
        }

        std::wstring path(MAX_PATH, L'\0');
        for (;;)
        {
            const DWORD length = GetModuleFileNameW(
                g_instance,
                path.data(),
                static_cast<DWORD>(path.size()));
            if (length == 0)
            {
                return std::wstring();
            }

            if (length < path.size())
            {
                path.resize(length);
                return path;
            }

            path.resize(path.size() * 2);
        }
    }

    std::wstring QuoteArgument(const std::wstring& value)
    {
        std::wstring result;
        result.reserve(value.size() + 2);
        result.push_back(L'"');
        result.append(value);
        result.push_back(L'"');
        return result;
    }

    std::wstring FindDxToolsExecutable()
    {
        const std::wstring modulePath = GetModulePath();
        const std::wstring::size_type separator = modulePath.find_last_of(L"\\/");
        if (separator != std::wstring::npos)
        {
            const std::wstring candidate = modulePath.substr(0, separator + 1) + L"DxTools.exe";
            if (PathFileExistsW(candidate.c_str()))
            {
                return candidate;
            }
        }

        wchar_t value[32768] = {};
        DWORD size = static_cast<DWORD>(sizeof(value));
        if (RegGetValueW(
                HKEY_LOCAL_MACHINE,
                kAppPathKey,
                nullptr,
                RRF_RT_REG_SZ,
                nullptr,
                value,
                &size) == ERROR_SUCCESS)
        {
            return std::wstring(value);
        }

        size = static_cast<DWORD>(sizeof(value));
        if (RegGetValueW(
                HKEY_CURRENT_USER,
                kAppPathKey,
                nullptr,
                RRF_RT_REG_SZ,
                nullptr,
                value,
                &size) == ERROR_SUCCESS)
        {
            return std::wstring(value);
        }

        wchar_t found[MAX_PATH] = {};
        const DWORD length = SearchPathW(
            nullptr,
            L"DxTools.exe",
            nullptr,
            static_cast<DWORD>(ARRAYSIZE(found)),
            found,
            nullptr);
        if (length > 0 && length < ARRAYSIZE(found))
        {
            return std::wstring(found, length);
        }

        return std::wstring();
    }

    std::wstring GetMonitorDevice()
    {
        POINT point = {};
        if (!GetCursorPos(&point))
        {
            return std::wstring();
        }

        const HMONITOR monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
        MONITORINFOEXW info = {};
        info.cbSize = sizeof(info);
        if (!GetMonitorInfoW(monitor, reinterpret_cast<LPMONITORINFO>(&info)))
        {
            return std::wstring();
        }

        return std::wstring(info.szDevice);
    }

    HRESULT LaunchDxTools(const std::wstring& arguments)
    {
        const std::wstring executable = FindDxToolsExecutable();
        if (executable.empty())
        {
            return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }

        SHELLEXECUTEINFOW executeInfo = {};
        executeInfo.cbSize = sizeof(executeInfo);
        executeInfo.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI;
        executeInfo.lpVerb = L"open";
        executeInfo.lpFile = executable.c_str();
        executeInfo.lpParameters = arguments.empty() ? nullptr : arguments.c_str();
        executeInfo.nShow = SW_SHOWNORMAL;

        if (!ShellExecuteExW(&executeInfo))
        {
            const DWORD error = GetLastError();
            return HRESULT_FROM_WIN32(error == ERROR_SUCCESS ? ERROR_GEN_FAILURE : error);
        }

        return S_OK;
    }

    HRESULT SetRegistryString(
        HKEY root,
        const wchar_t* key,
        const wchar_t* name,
        const wchar_t* value)
    {
        HKEY handle = nullptr;
        LSTATUS status = RegCreateKeyExW(
            root,
            key,
            0,
            nullptr,
            REG_OPTION_NON_VOLATILE,
            KEY_WRITE,
            nullptr,
            &handle,
            nullptr);
        if (status != ERROR_SUCCESS)
        {
            return HRESULT_FROM_WIN32(status);
        }

        const DWORD valueSize = static_cast<DWORD>((wcslen(value) + 1) * sizeof(wchar_t));
        status = RegSetValueExW(
            handle,
            name,
            0,
            REG_SZ,
            reinterpret_cast<const BYTE*>(value),
            valueSize);
        RegCloseKey(handle);
        return HRESULT_FROM_WIN32(status);
    }

    HRESULT DeleteRegistryTree(HKEY root, const wchar_t* key)
    {
        const LSTATUS status = RegDeleteTreeW(root, key);
        if (status == ERROR_SUCCESS || status == ERROR_FILE_NOT_FOUND || status == ERROR_PATH_NOT_FOUND)
        {
            return S_OK;
        }

        return HRESULT_FROM_WIN32(status);
    }

    std::wstring GetClsidString()
    {
        wchar_t buffer[64] = {};
        const int length = StringFromGUID2(
            kShellExtClsid,
            buffer,
            static_cast<int>(ARRAYSIZE(buffer)));
        if (length <= 0)
        {
            return std::wstring();
        }

        return std::wstring(buffer, static_cast<std::wstring::size_type>(length - 1));
    }

    class DxToolsContextMenu final : public IContextMenu, public IShellExtInit
    {
    public:
        DxToolsContextMenu()
        {
            InterlockedIncrement(&g_objectCount);
        }

        DxToolsContextMenu(const DxToolsContextMenu&) = delete;
        DxToolsContextMenu& operator=(const DxToolsContextMenu&) = delete;

        ~DxToolsContextMenu()
        {
            InterlockedDecrement(&g_objectCount);
        }

        // IUnknown
        IFACEMETHODIMP QueryInterface(REFIID riid, void** result) override
        {
            if (result == nullptr)
            {
                return E_POINTER;
            }

            *result = nullptr;
            if (riid == IID_IUnknown || riid == IID_IContextMenu)
            {
                *result = static_cast<IContextMenu*>(this);
            }
            else if (riid == IID_IShellExtInit)
            {
                *result = static_cast<IShellExtInit*>(this);
            }
            else
            {
                return E_NOINTERFACE;
            }

            AddRef();
            return S_OK;
        }

        IFACEMETHODIMP_(ULONG) AddRef() override
        {
            return static_cast<ULONG>(InterlockedIncrement(&m_references));
        }

        IFACEMETHODIMP_(ULONG) Release() override
        {
            const ULONG references = static_cast<ULONG>(InterlockedDecrement(&m_references));
            if (references == 0)
            {
                delete this;
            }

            return references;
        }

        // IShellExtInit
        IFACEMETHODIMP Initialize(
            PCIDLIST_ABSOLUTE folder,
            IDataObject* dataObject,
            HKEY programKey) override
        {
            (void)folder;
            (void)programKey;

            m_files.clear();

            if (dataObject != nullptr)
            {
                FORMATETC format = {};
                format.cfFormat = CF_HDROP;
                format.dwAspect = DVASPECT_CONTENT;
                format.lindex = -1;
                format.tymed = TYMED_HGLOBAL;

                STGMEDIUM medium = {};
                if (SUCCEEDED(dataObject->GetData(&format, &medium)))
                {
                    if (medium.hGlobal != nullptr)
                    {
                        const HDROP drop = static_cast<HDROP>(medium.hGlobal);
                        const UINT count = DragQueryFileW(drop, 0xFFFFFFFFu, nullptr, 0);
                        for (UINT index = 0; index < count && index < 32; ++index)
                        {
                            const UINT length = DragQueryFileW(drop, index, nullptr, 0);
                            if (length == 0)
                            {
                                continue;
                            }

                            std::vector<wchar_t> buffer(static_cast<std::vector<wchar_t>::size_type>(length) + 1, L'\0');
                            const UINT copied = DragQueryFileW(drop, index, buffer.data(), length + 1);
                            if (copied > 0)
                            {
                                m_files.emplace_back(buffer.data(), copied);
                            }
                        }
                    }

                    ReleaseStgMedium(&medium);
                }
            }

            return S_OK;
        }

        // IContextMenu
        IFACEMETHODIMP QueryContextMenu(
            HMENU menu,
            UINT indexMenu,
            UINT idCommandFirst,
            UINT idCommandLast,
            UINT flags) override
        {
            if ((flags & CMF_DEFAULTONLY) != 0)
            {
                return S_OK;
            }

            UINT added = 0;
            const auto addCommand = [&](const wchar_t* text) -> bool
            {
                const UINT id = idCommandFirst + added;
                if (id > idCommandLast)
                {
                    return false;
                }

                if (!InsertMenuW(
                        menu,
                        indexMenu + added,
                        MF_STRING | MF_BYPOSITION,
                        id,
                        text))
                {
                    return false;
                }

                ++added;
                return true;
            };

            if (m_files.empty())
            {
                addCommand(OpenSettingsText());
                addCommand(ApplyWallpaperText());
            }
            else if (CanSetWallpaper())
            {
                addCommand(SetWallpaperText());
                addCommand(OpenSettingsText());
            }
            else
            {
                addCommand(OpenSettingsText());
            }

            return MAKE_HRESULT(SEVERITY_SUCCESS, FACILITY_NULL, added);
        }

        IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO commandInfo) override
        {
            if (commandInfo == nullptr || commandInfo->lpVerb == nullptr)
            {
                return E_INVALIDARG;
            }

            UINT id = 0;
            const ULONG_PTR verb = reinterpret_cast<ULONG_PTR>(commandInfo->lpVerb);
            if (HIWORD(verb) != 0)
            {
                if (_stricmp(commandInfo->lpVerb, "open") != 0)
                {
                    return E_INVALIDARG;
                }
            }
            else
            {
                id = LOWORD(verb);
            }

            if (m_files.empty())
            {
                if (id == 0)
                {
                    return LaunchDxTools(L"--show");
                }

                if (id == 1)
                {
                    return LaunchDxTools(L"--apply-wallpaper");
                }

                return E_INVALIDARG;
            }

            if (CanSetWallpaper())
            {
                if (id == 0)
                {
                    std::wstring arguments = L"--wallpaper " + QuoteArgument(m_files[0]);
                    const std::wstring monitor = GetMonitorDevice();
                    if (!monitor.empty())
                    {
                        arguments += L" --monitor ";
                        arguments += QuoteArgument(monitor);
                    }

                    return LaunchDxTools(arguments);
                }

                if (id == 1)
                {
                    return LaunchDxTools(L"--show");
                }

                return E_INVALIDARG;
            }

            return id == 0 ? LaunchDxTools(L"--show") : E_INVALIDARG;
        }

        IFACEMETHODIMP GetCommandString(
            UINT_PTR idCommand,
            UINT type,
            UINT* reserved,
            LPSTR name,
            UINT nameLength) override
        {
            (void)reserved;

            if (type == GCS_VALIDATEA || type == GCS_VALIDATEW)
            {
                return IsCommandValid(idCommand) ? S_OK : E_INVALIDARG;
            }

            if (name == nullptr || nameLength == 0)
            {
                return E_INVALIDARG;
            }

            const wchar_t* text = nullptr;
            if (type == GCS_VERBW || type == GCS_VERBA)
            {
                text = GetCommandVerb(static_cast<UINT>(idCommand));
            }
            else if (type == GCS_HELPTEXTW || type == GCS_HELPTEXTA)
            {
                text = GetCommandText(static_cast<UINT>(idCommand));
            }

            if (text == nullptr)
            {
                return E_INVALIDARG;
            }

            if (type == GCS_VERBW || type == GCS_HELPTEXTW)
            {
                const HRESULT result = StringCchCopyW(
                    reinterpret_cast<LPWSTR>(name),
                    nameLength,
                    text);
                return SUCCEEDED(result) ? S_OK : E_INVALIDARG;
            }

            const int written = WideCharToMultiByte(
                CP_ACP,
                0,
                text,
                -1,
                name,
                static_cast<int>(nameLength),
                nullptr,
                nullptr);
            return written > 0 ? S_OK : E_INVALIDARG;
        }

    private:
        bool CanSetWallpaper() const
        {
            return m_files.size() == 1 && IsImageFile(m_files[0]);
        }

        bool IsCommandValid(UINT id) const
        {
            if (m_files.empty())
            {
                return id <= 1;
            }

            if (CanSetWallpaper())
            {
                return id <= 1;
            }

            return id == 0;
        }

        const wchar_t* GetCommandText(UINT id) const
        {
            if (m_files.empty())
            {
                if (id == 0) return OpenSettingsText();
                if (id == 1) return ApplyWallpaperText();
                return nullptr;
            }

            if (CanSetWallpaper())
            {
                if (id == 0) return SetWallpaperText();
                if (id == 1) return OpenSettingsText();
                return nullptr;
            }

            return id == 0 ? OpenSettingsText() : nullptr;
        }

        const wchar_t* GetCommandVerb(UINT id) const
        {
            if (m_files.empty())
            {
                if (id == 0) return OpenSettingsVerb();
                if (id == 1) return ApplyWallpaperVerb();
                return nullptr;
            }

            if (CanSetWallpaper())
            {
                if (id == 0) return SetWallpaperVerb();
                if (id == 1) return OpenSettingsVerb();
                return nullptr;
            }

            return id == 0 ? OpenSettingsVerb() : nullptr;
        }

        LONG m_references = 1;
        std::vector<std::wstring> m_files;
    };

    class DxToolsClassFactory final : public IClassFactory
    {
    public:
        DxToolsClassFactory()
        {
            InterlockedIncrement(&g_serverLocks);
        }

        DxToolsClassFactory(const DxToolsClassFactory&) = delete;
        DxToolsClassFactory& operator=(const DxToolsClassFactory&) = delete;

        ~DxToolsClassFactory()
        {
            InterlockedDecrement(&g_serverLocks);
        }

        IFACEMETHODIMP QueryInterface(REFIID riid, void** result) override
        {
            if (result == nullptr)
            {
                return E_POINTER;
            }

            *result = nullptr;
            if (riid == IID_IUnknown || riid == IID_IClassFactory)
            {
                *result = static_cast<IClassFactory*>(this);
                AddRef();
                return S_OK;
            }

            return E_NOINTERFACE;
        }

        IFACEMETHODIMP_(ULONG) AddRef() override
        {
            return static_cast<ULONG>(InterlockedIncrement(&m_references));
        }

        IFACEMETHODIMP_(ULONG) Release() override
        {
            const ULONG references = static_cast<ULONG>(InterlockedDecrement(&m_references));
            if (references == 0)
            {
                delete this;
            }

            return references;
        }

        IFACEMETHODIMP CreateInstance(
            IUnknown* outer,
            REFIID riid,
            void** result) override
        {
            if (outer != nullptr)
            {
                return CLASS_E_NOAGGREGATION;
            }

            if (result == nullptr)
            {
                return E_POINTER;
            }

            *result = nullptr;
            DxToolsContextMenu* instance = new (std::nothrow) DxToolsContextMenu();
            if (instance == nullptr)
            {
                return E_OUTOFMEMORY;
            }

            const HRESULT hr = instance->QueryInterface(riid, result);
            instance->Release();
            return hr;
        }

        IFACEMETHODIMP LockServer(BOOL lock) override
        {
            if (lock)
            {
                InterlockedIncrement(&g_serverLocks);
            }
            else
            {
                InterlockedDecrement(&g_serverLocks);
            }

            return S_OK;
        }

    private:
        LONG m_references = 1;
    };
}

extern "C" __declspec(dllexport) HRESULT __stdcall DllGetClassObject(
    REFCLSID clsid,
    REFIID riid,
    void** result)
{
    if (result == nullptr)
    {
        return E_POINTER;
    }

    *result = nullptr;
    if (!IsEqualCLSID(clsid, kShellExtClsid))
    {
        return CLASS_E_CLASSNOTAVAILABLE;
    }

    DxToolsClassFactory* factory = new (std::nothrow) DxToolsClassFactory();
    if (factory == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    const HRESULT hr = factory->QueryInterface(riid, result);
    factory->Release();
    return hr;
}

extern "C" __declspec(dllexport) HRESULT __stdcall DllCanUnloadNow()
{
    return (InterlockedCompareExchange(&g_objectCount, 0, 0) == 0
        && InterlockedCompareExchange(&g_serverLocks, 0, 0) == 0)
        ? S_OK
        : S_FALSE;
}

extern "C" __declspec(dllexport) HRESULT __stdcall DllRegisterServer()
{
    const std::wstring clsid = GetClsidString();
    const std::wstring dllPath = GetModulePath();
    if (clsid.empty() || dllPath.empty())
    {
        return E_FAIL;
    }

    const std::wstring inprocKey = L"Software\\Classes\\CLSID\\" + clsid + L"\\InprocServer32";
    HRESULT hr = SetRegistryString(HKEY_CURRENT_USER, inprocKey.c_str(), nullptr, dllPath.c_str());
    if (FAILED(hr))
    {
        return hr;
    }

    hr = SetRegistryString(HKEY_CURRENT_USER, inprocKey.c_str(), L"ThreadingModel", L"Apartment");
    if (FAILED(hr))
    {
        return hr;
    }

    hr = SetRegistryString(HKEY_CURRENT_USER, kBackgroundHandlerKey, nullptr, clsid.c_str());
    if (FAILED(hr))
    {
        return hr;
    }

    hr = SetRegistryString(HKEY_CURRENT_USER, kImageHandlerKey, nullptr, clsid.c_str());
    if (FAILED(hr))
    {
        return hr;
    }

    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, nullptr, nullptr);
    return S_OK;
}

extern "C" __declspec(dllexport) HRESULT __stdcall DllUnregisterServer()
{
    const std::wstring clsid = GetClsidString();
    if (clsid.empty())
    {
        return E_FAIL;
    }

    const std::wstring inprocKey = L"Software\\Classes\\CLSID\\" + clsid;
    HRESULT firstError = DeleteRegistryTree(HKEY_CURRENT_USER, inprocKey.c_str());
    const HRESULT background = DeleteRegistryTree(HKEY_CURRENT_USER, kBackgroundHandlerKey);
    const HRESULT image = DeleteRegistryTree(HKEY_CURRENT_USER, kImageHandlerKey);
    if (FAILED(firstError))
    {
        firstError = background;
    }

    if (FAILED(firstError))
    {
        firstError = image;
    }

    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, nullptr, nullptr);
    return firstError;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    (void)reserved;
    if (reason == DLL_PROCESS_ATTACH)
    {
        g_instance = module;
        DisableThreadLibraryCalls(module);
    }

    return TRUE;
}
