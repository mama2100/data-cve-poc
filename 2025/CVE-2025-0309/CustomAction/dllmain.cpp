#include "pch.h"
#include <windows.h>
#include <msi.h>
#include <msiquery.h>
#include "spawn.h"

#pragma comment(lib, "msi.lib")

extern "C" __declspec(dllexport) UINT __stdcall Install(MSIHANDLE hInstall) {
    PopThyShell();
    return ERROR_SUCCESS;
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}