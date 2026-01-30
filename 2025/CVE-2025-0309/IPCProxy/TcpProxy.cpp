#include "pch.h"
#include "TcpProxy.h"

TcpProxy::TcpProxy() : serverSocket(INVALID_SOCKET), running(false) {
    printf("TcpProxy constructor called\n");
}

TcpProxy::~TcpProxy() {
    Stop();
}

bool TcpProxy::Start() {
    if (running) {
        return true;
    }

    printf("Starting TcpProxy\n");
    running = true;
    serverThread = std::thread(&TcpProxy::ServerLoop, this);
    serverThread.detach();
    return true;
}

void TcpProxy::Stop() {
    if (!running) {
        return;
    }

    printf("Stopping TcpProxy\n");
    running = false;

    if (serverSocket != INVALID_SOCKET) {
        closesocket(serverSocket);
        serverSocket = INVALID_SOCKET;
    }

    if (serverThread.joinable()) {
        printf("Waiting for server thread to finish...\n");
        serverThread.join();
        printf("Server thread finished\n");
    }

    // Clean up all active bridges
    std::lock_guard<std::mutex> lock(bridgesMutex);
    activeBridges.clear();
}

void TcpProxy::ServerLoop() {
    printf("Server thread started (Thread ID: %lu)\n", GetCurrentThreadId());

    // Initialize Winsock
    WSADATA wsaData;
    int result = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (result != 0) {
        printf("WSAStartup failed: %d\n", result);
        running = false;
        return;
    }
    printf("WSAStartup successful\n");

    // Create server socket
    serverSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (serverSocket == INVALID_SOCKET) {
        printf("Failed to create server socket: %d\n", WSAGetLastError());
        WSACleanup();
        running = false;
        return;
    }
    printf("Server socket created successfully\n");

    // Set socket option to reuse address
    int optval = 1;
    if (setsockopt(serverSocket, SOL_SOCKET, SO_REUSEADDR, (const char*)&optval, sizeof(optval)) == SOCKET_ERROR) {
        printf("Failed to set socket options: %d\n", WSAGetLastError());
        closesocket(serverSocket);
        WSACleanup();
        running = false;
        return;
    }

    // Bind socket to address and port
    sockaddr_in serverAddr;
    serverAddr.sin_family = AF_INET;
    inet_pton(AF_INET, LOCAL_HOST, &serverAddr.sin_addr);
    serverAddr.sin_port = htons(LOCAL_PORT);

    if (bind(serverSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        printf("Failed to bind server socket: %d\n", WSAGetLastError());
        closesocket(serverSocket);
        WSACleanup();
        running = false;
        return;
    }
    printf("Server socket bound to %s:%d\n", LOCAL_HOST, LOCAL_PORT);

    // Start listening for connections
    if (listen(serverSocket, SOMAXCONN) == SOCKET_ERROR) {
        printf("Failed to listen on server socket: %d\n", WSAGetLastError());
        closesocket(serverSocket);
        WSACleanup();
        running = false;
        return;
    }
    printf("Server listening for connections on %s:%d\n", LOCAL_HOST, LOCAL_PORT);

    // Accept client connections
    while (running) {
        // Periodically clean up inactive bridges
        CleanupBridges();

        printf("Waiting for client connection...\n");

        // Set timeout
        fd_set readSet;
        FD_ZERO(&readSet);
        FD_SET(serverSocket, &readSet);

        timeval timeout;
        timeout.tv_sec = 1;
        timeout.tv_usec = 0;

        // Wait for connection or timeout
        int selectResult = select(0, &readSet, NULL, NULL, &timeout);

        if (selectResult == SOCKET_ERROR) {
            printf("Select failed: %d\n", WSAGetLastError());
            break;
        }

        // If timeout occurred, continue the loop
        if (selectResult == 0) {
            continue;
        }

        // Accept the connection
        sockaddr_in clientAddr;
        int clientAddrLen = sizeof(clientAddr);
        SOCKET clientSocket = accept(serverSocket, (sockaddr*)&clientAddr, &clientAddrLen);

        if (clientSocket == INVALID_SOCKET) {
            printf("Failed to accept client connection: %d\n", WSAGetLastError());
            continue;
        }

        // Get client IP address
        char clientIP[INET_ADDRSTRLEN];
        inet_ntop(AF_INET, &clientAddr.sin_addr, clientIP, INET_ADDRSTRLEN);
        printf("Client connected from %s:%d\n", clientIP, ntohs(clientAddr.sin_port));

        // Handle the client in a new thread
        std::thread clientThread(&TcpProxy::HandleClient, this, clientSocket);
        clientThread.detach();
    }

    // Clean up
    if (serverSocket != INVALID_SOCKET) {
        closesocket(serverSocket);
        serverSocket = INVALID_SOCKET;
    }

    WSACleanup();
    printf("Server loop ended\n");
}

bool TcpProxy::HandleClient(SOCKET clientSocket) {
    printf("Handling client connection\n");

    // Create a bridge to handle the connection
    auto bridge = std::make_shared<TcpProxy::Bridge>(clientSocket);

    // Store the bridge
    {
        std::lock_guard<std::mutex> lock(bridgesMutex);
        activeBridges.push_back(bridge);
    }

    // Start the bridge
    bool result = bridge->Start();

    if (!result) {
        // If start failed, remove the bridge
        std::lock_guard<std::mutex> lock(bridgesMutex);
        auto it = std::find(activeBridges.begin(), activeBridges.end(), bridge);
        if (it != activeBridges.end()) {
            activeBridges.erase(it);
        }
    }

    return result;
}

void TcpProxy::CleanupBridges() {
    std::lock_guard<std::mutex> lock(bridgesMutex);

    // Remove bridges that are no longer running
    auto it = activeBridges.begin();
    while (it != activeBridges.end()) {
        if (!(*it)->IsRunning()) {
            it = activeBridges.erase(it);
        } else {
            ++it;
        }
    }
}

// Bridge implementation
TcpProxy::Bridge::Bridge(SOCKET clientSocket) : clientSocket(clientSocket), serverSocket(INVALID_SOCKET), running(false) {
    printf("Bridge constructor called\n");
}

TcpProxy::Bridge::~Bridge() {
    running = false;

    if (serverSocket != INVALID_SOCKET) {
        closesocket(serverSocket);
        serverSocket = INVALID_SOCKET;
    }

    if (clientSocket != INVALID_SOCKET) {
        closesocket(clientSocket);
        clientSocket = INVALID_SOCKET;
    }

    if (upstreamThread.joinable()) {
        upstreamThread.join();
    }

    if (downstreamThread.joinable()) {
        downstreamThread.join();
    }
}

bool TcpProxy::Bridge::Start() {
    // Connect to the server after receiving data from the client
    printf("Bridge started, waiting for client data before connecting to server\n");

    // Start the upstream thread
    running = true;
    upstreamThread = std::thread(&TcpProxy::Bridge::UpstreamLoop, this);
    return true;
}

void TcpProxy::Bridge::UpstreamLoop() {
    printf("Upstream thread started\n");

    char buffer[BUFFER_SIZE];
    int bytesRead;
    bool serverConnected = false;

    while (running) {
        // Read from client
        bytesRead = recv(clientSocket, buffer, BUFFER_SIZE, 0);
        if (bytesRead <= 0) {
            if (bytesRead == 0) {
                printf("Client disconnected\n");
            }
            else {
                printf("Error reading from client: %d\n", WSAGetLastError());
            }
            running = false;
            break;
        }

        printf("Received %d bytes from client\n", bytesRead);

        printf("First bytes: ");
        for (int i = 0; i < min(bytesRead, 16); i++) {
            printf("%02X ", (unsigned char)buffer[i]);
        }
        printf("\n");

        if (!serverConnected) {
            printf("Connecting to upstream server...\n");

            // Create server socket
            serverSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
            if (serverSocket == INVALID_SOCKET) {
                printf("Failed to create server socket for bridge: %d\n", WSAGetLastError());
                running = false;
                break;
            }

            // Connect to remote server
            sockaddr_in serverAddr;
            serverAddr.sin_family = AF_INET;
            inet_pton(AF_INET, REMOTE_HOST, &serverAddr.sin_addr);
            serverAddr.sin_port = htons(REMOTE_PORT);

            if (connect(serverSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
                printf("Failed to connect to remote server: %d\n", WSAGetLastError());
                closesocket(serverSocket);
                serverSocket = INVALID_SOCKET;
                running = false;
                break;
            }

            printf("Connected to remote server at %s:%d\n", REMOTE_HOST, REMOTE_PORT);
            serverConnected = true;

            // Start the downstream thread
            Sleep(100);
            downstreamThread = std::thread(&TcpProxy::Bridge::DownstreamLoop, this);
        }

        // Forward to server
        int bytesSent = 0;
        while (bytesSent < bytesRead && running) {
            int result = send(serverSocket, buffer + bytesSent, bytesRead - bytesSent, 0);
            if (result == SOCKET_ERROR) {
                int error = WSAGetLastError();
                printf("Error sending to server: %d\n", error);

                if (error == WSAEWOULDBLOCK) {
                    // Socket would block, wait and try again
                    Sleep(10);
                    continue;
                }

                running = false;
                break;
            }
            bytesSent += result;
        }

        if (bytesSent == bytesRead) {
            printf("Forwarded %d bytes to server\n", bytesSent);
        }
    }

    printf("Upstream thread ended\n");
}

void TcpProxy::Bridge::DownstreamLoop() {
    printf("Downstream thread started\n");

    char buffer[BUFFER_SIZE];
    int bytesRead;

    while (running) {
        // Check if server socket is valid
        if (serverSocket == INVALID_SOCKET) {
            // Sleep a bit and check again
            Sleep(100);
            continue;
        }

        // Read from server with timeout
        fd_set readSet;
        FD_ZERO(&readSet);
        FD_SET(serverSocket, &readSet);

        // Set timeout to 1 second
        timeval timeout;
        timeout.tv_sec = 1;
        timeout.tv_usec = 0;

        int selectResult = select(0, &readSet, NULL, NULL, &timeout);

        if (selectResult == SOCKET_ERROR) {
            printf("Select failed in downstream: %d\n", WSAGetLastError());
            running = false;
            break;
        }

        // If timeout occurred, continue the loop
        if (selectResult == 0) {
            continue;
        }

        // Read from server
        bytesRead = recv(serverSocket, buffer, BUFFER_SIZE, 0);
        if (bytesRead <= 0) {
            if (bytesRead == 0) {
                printf("Server disconnected\n");
            }
            else {
                printf("Error reading from server: %d\n", WSAGetLastError());
            }
            running = false;
            break;
        }

        printf("Received %d bytes from server\n", bytesRead);

        printf("First bytes: ");
        for (int i = 0; i < min(bytesRead, 16); i++) {
            printf("%02X ", (unsigned char)buffer[i]);
        }
        printf("\n");

        // Forward to client
        int bytesSent = 0;
        while (bytesSent < bytesRead && running) {
            int result = send(clientSocket, buffer + bytesSent, bytesRead - bytesSent, 0);
            if (result == SOCKET_ERROR) {
                int error = WSAGetLastError();
                printf("Error sending to client: %d\n", error);

                if (error == WSAEWOULDBLOCK) {
                    // Socket would block, wait and try again
                    Sleep(10);
                    continue;
                }

                running = false;
                break;
            }
            bytesSent += result;
        }

        if (bytesSent == bytesRead) {
            printf("Forwarded %d bytes to client\n", bytesSent);
        }
    }

    printf("Downstream thread ended\n");
} 