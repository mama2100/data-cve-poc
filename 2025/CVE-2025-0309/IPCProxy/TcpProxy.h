#pragma once

#include <string>
#include <thread>
#include <atomic>
#include <vector>
#include <mutex>
#include <memory>
#include <algorithm>
#include <WinSock2.h>
#include <WS2tcpip.h>

#pragma comment(lib, "ws2_32.lib")

class TcpProxy {
private:
    class Bridge;

public:
    TcpProxy();
    ~TcpProxy();

    bool Start();
    void Stop();

private:
    static constexpr int LOCAL_PORT = 8888;
    static constexpr int REMOTE_PORT = 57130;
    static constexpr const char* LOCAL_HOST = "127.0.0.1";
    static constexpr const char* REMOTE_HOST = "127.0.0.1";
    static constexpr int BUFFER_SIZE = 8192;

    SOCKET serverSocket;
    std::thread serverThread;
    std::atomic<bool> running;
    std::vector<std::shared_ptr<Bridge>> activeBridges;
    std::mutex bridgesMutex;

    void ServerLoop();
    bool HandleClient(SOCKET clientSocket);
    void CleanupBridges();
};

class TcpProxy::Bridge {
public:
    Bridge(SOCKET clientSocket);
    ~Bridge();

    bool Start();
    bool IsRunning() const { return running; }

private:
    SOCKET clientSocket;
    SOCKET serverSocket;
    std::thread upstreamThread;
    std::thread downstreamThread;
    std::atomic<bool> running;

    void UpstreamLoop();   // Client -> Server
    void DownstreamLoop(); // Server -> Client

    static constexpr int BUFFER_SIZE = TcpProxy::BUFFER_SIZE;
    static constexpr const char* REMOTE_HOST = TcpProxy::REMOTE_HOST;
    static constexpr int REMOTE_PORT = TcpProxy::REMOTE_PORT;
}; 