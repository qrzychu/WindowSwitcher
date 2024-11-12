#include <windows.h>
#include <ShObjIdl.h>
#include <combaseapi.h>
#include <propkey.h>
#include <propvarutil.h>
#include <wrl/client.h>
#include <memory>
#include <gdiplus.h>
#include <shobjidl_core.h>
#include <shellapi.h>
#pragma comment(lib, "gdiplus.lib")

using namespace Microsoft::WRL;
using namespace Gdiplus;

// Helper macro for safe cleanup
#define SAFE_RELEASE(ptr) if(ptr) { ptr->Release(); ptr = nullptr; }
#define SAFE_DELETE(ptr) if(ptr) { delete ptr; ptr = nullptr; }
#define SAFE_DELETE_OBJECT(handle) if(handle) { DeleteObject(handle); handle = nullptr; }
#define SAFE_DESTROY_ICON(icon) if(icon) { DestroyIcon(icon); icon = nullptr; }

extern "C" {
    __declspec(dllexport) HRESULT GetWindowDesktopId(HWND hwnd, GUID* desktopId) {
        if (!hwnd || !desktopId) return E_INVALIDARG;

        IVirtualDesktopManager* pDesktopManager = nullptr;
        HRESULT hr = CoCreateInstance(
            CLSID_VirtualDesktopManager, nullptr, CLSCTX_ALL,
            IID_PPV_ARGS(&pDesktopManager)
        );

        if (SUCCEEDED(hr)) {
            hr = pDesktopManager->GetWindowDesktopId(hwnd, desktopId);
            SAFE_RELEASE(pDesktopManager);
        }
        return hr;
    }
}

extern "C" __declspec(dllexport) int GetWindowIconData(void* windowHandle, unsigned char* buffer, int bufferSize);

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    switch (fdwReason) {
    case DLL_PROCESS_ATTACH:
        {
            HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
            if (FAILED(hr)) return FALSE;
        }
        break;
    case DLL_PROCESS_DETACH:
        CoUninitialize();
        break;
    }
    return TRUE;
}

// Helper function to get icon handle with error handling
static HICON GetWindowIconHandle(HWND hWnd)
{
    if (!hWnd || !IsWindow(hWnd)) return nullptr;

    __try {
        // Try standard Win32 way first
        HICON hIcon = (HICON)SendMessage(hWnd, WM_GETICON, ICON_SMALL, 0);
        if (hIcon) return CopyIcon(hIcon);  // Make a copy to ensure we own the icon

        hIcon = (HICON)SendMessage(hWnd, WM_GETICON, ICON_BIG, 0);
        if (hIcon) return CopyIcon(hIcon);

        hIcon = (HICON)GetClassLongPtr(hWnd, GCLP_HICON);
        if (hIcon) return CopyIcon(hIcon);

        // Fallback to extracting icon from window's executable
        DWORD processId;
        if (!GetWindowThreadProcessId(hWnd, &processId)) return nullptr;
        
        HANDLE hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId);
        if (!hProcess) return nullptr;

        WCHAR exePath[MAX_PATH];
        DWORD pathLength = MAX_PATH;
        BOOL success = QueryFullProcessImageNameW(hProcess, 0, exePath, &pathLength);
        CloseHandle(hProcess);

        if (success) {
            HICON hIconSmall = nullptr;
            if (ExtractIconExW(exePath, 0, nullptr, &hIconSmall, 1) > 0) {
                return hIconSmall;
            }
        }

        return nullptr;
    }
    __except(EXCEPTION_EXECUTE_HANDLER) {
        return nullptr;
    }
}

// Main exported function with improved error handling
int GetWindowIconData(void* windowHandle, unsigned char* buffer, int bufferSize)
{
    if (!windowHandle) return 0;

    __try {
        // Initialize GDI+
        ULONG_PTR gdiplusToken;
        GdiplusStartupInput gdiplusStartupInput;
        Status status = GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);
        if (status != Ok) return 0;

        HWND hWnd = (HWND)windowHandle;
        HICON hIcon = GetWindowIconHandle(hWnd);
        if (!hIcon) {
            GdiplusShutdown(gdiplusToken);
            return 0;
        }

        // Create GDI+ bitmap from icon
        Bitmap* bitmap = Bitmap::FromHICON(hIcon);
        SAFE_DESTROY_ICON(hIcon);

        if (!bitmap || bitmap->GetLastStatus() != Ok) {
            SAFE_DELETE(bitmap);
            GdiplusShutdown(gdiplusToken);
            return 0;
        }

        // Create an IStream to hold the PNG data
        IStream* istream = nullptr;
        HRESULT hr = CreateStreamOnHGlobal(NULL, TRUE, &istream);
        if (FAILED(hr) || !istream) {
            SAFE_DELETE(bitmap);
            GdiplusShutdown(gdiplusToken);
            return 0;
        }

        // Save bitmap as PNG to stream
        CLSID pngClsid;
        hr = CLSIDFromString(L"{557CF406-1A04-11D3-9A73-0000F81EF32E}", &pngClsid);
        if (SUCCEEDED(hr)) {
            status = bitmap->Save(istream, &pngClsid);
        }

        SAFE_DELETE(bitmap);

        if (FAILED(hr) || status != Ok) {
            SAFE_RELEASE(istream);
            GdiplusShutdown(gdiplusToken);
            return 0;
        }

        // Get stream size
        ULARGE_INTEGER streamSize;
        LARGE_INTEGER zero = {0};
        hr = istream->Seek(zero, STREAM_SEEK_END, &streamSize);
        if (SUCCEEDED(hr)) {
            hr = istream->Seek(zero, STREAM_SEEK_SET, NULL);
        }

        if (FAILED(hr)) {
            SAFE_RELEASE(istream);
            GdiplusShutdown(gdiplusToken);
            return 0;
        }

        // If buffer is null, return required size
        if (buffer == nullptr) {
            SAFE_RELEASE(istream);
            GdiplusShutdown(gdiplusToken);
            return (int)streamSize.QuadPart;
        }

        // If buffer is too small, return 0
        if (bufferSize < (int)streamSize.QuadPart) {
            SAFE_RELEASE(istream);
            GdiplusShutdown(gdiplusToken);
            return 0;
        }

        // Copy stream to buffer
        ULONG bytesRead = 0;
        hr = istream->Read(buffer, (ULONG)streamSize.QuadPart, &bytesRead);
        
        // Clean up
        SAFE_RELEASE(istream);
        GdiplusShutdown(gdiplusToken);

        return SUCCEEDED(hr) ? bytesRead : 0;
    }
    __except(EXCEPTION_EXECUTE_HANDLER) {
        return 0;
    }
}