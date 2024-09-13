#include <windows.h>
#include <ShObjIdl.h>
#include <combaseapi.h>

extern "C" {
    __declspec(dllexport) HRESULT GetWindowDesktopId(HWND hwnd, GUID* desktopId) {
        IVirtualDesktopManager* pDesktopManager = nullptr;
        HRESULT hr = CoCreateInstance(
            CLSID_VirtualDesktopManager, nullptr, CLSCTX_ALL,
            IID_PPV_ARGS(&pDesktopManager)
        );

        if (SUCCEEDED(hr)) {
            hr = pDesktopManager->GetWindowDesktopId(hwnd, desktopId);
            pDesktopManager->Release();
        }
        return hr;
    }
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    switch (fdwReason) {
    case DLL_PROCESS_ATTACH:
        CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
        break;
    case DLL_PROCESS_DETACH:
        CoUninitialize();
        break;
    default:
        break;
    }
    return TRUE;
}