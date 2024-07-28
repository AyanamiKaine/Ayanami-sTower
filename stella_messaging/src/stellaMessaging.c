#include "stellaMessaging.h"

nng_socket create_pair_socket()
{
    nng_socket sock; // Client socket

    // Create the request socket
    if ((nng_pair0_open(&sock)) != 0) {
        //fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

void connect_dial(nng_socket sock, const char *address)
{
    if ((nng_dial(sock, address, NULL, NNG_FLAG_NONBLOCK)) != 0) {
        //fprintf(stderr, "Failed to connect: %s\n", nng_strerror(rv));
        nng_close(sock);
    }
}

void listen_for_connections(nng_socket sock, const char *address)
{
    // Listen for connections
    if ((nng_listen(sock, address, NULL, 0)) != 0) {
        //fprintf(stderr, "Failed to listen: %s\n", nng_strerror(rv));
        nng_close(sock);
    }
}

void send_string_message(nng_socket sock, char *message)
{
    nng_msg *msg;   // Message to send and receive
    size_t dataSize = strlen(message) + 1; // +1 for null terminator
    
    // Create a request message
    if ((nng_msg_alloc(&msg, dataSize)) != 0) {
        //fprintf(stderr, "Failed to allocate message: %s\n", NULL);
        nng_close(sock);
    }
    
    // Copy your data into the message body
    memcpy(nng_msg_body(msg), message, dataSize);

    if ((nng_sendmsg(sock, msg, NNG_FLAG_ALLOC)) != 0) {
        //fprintf(stderr, "Failed to send: %s\n", NULL);
        nng_msg_free(msg);
        nng_close(sock);
    }
}

char *receive_string_message(nng_socket sock)
{
    nng_msg *msg;   
    // Message to receive
    // Receive the reply
    if ((nng_recvmsg(sock, &msg, 0)) != 0) {
        //fprintf(stderr, "Failed to receive: %s\n", NULL);
        nng_close(sock);
    }

    return nng_msg_body(msg);
}
