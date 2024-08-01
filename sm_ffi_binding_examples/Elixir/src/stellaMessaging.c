#include "stellaMessaging.h"

// Ensure the macro is defined for building (add this if not already in CMakeLists.txt)
#ifndef BUILDING_STELLA_MESSAGING
#define BUILDING_STELLA_MESSAGING
#endif


STELLA_API nng_socket create_pair_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_pair1_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_bus_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_bus0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_reponse_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_rep0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_request_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_req0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_sub_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_sub0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}
STELLA_API nng_socket create_pub_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_pub0_open(&sock)) != 0) {
        printf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_pull_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_pull0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_push_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_push0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}


STELLA_API nng_socket create_respondent_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_respondent0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_surveyor_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the request socket
    if ((rv = nng_surveyor0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API void socket_connect(nng_socket sock, const char *address)
{
    int rv;
    if ((nng_dial(sock, address, NULL, NNG_FLAG_NONBLOCK)) != 0) {
        fprintf(stderr, "Failed to connect: %s\n", nng_strerror(rv));
        nng_close(sock);
    }
}

STELLA_API void socket_bind(nng_socket sock, const char *address)
{
    int rv;
    // Listen for connections
    if ((rv = nng_listen(sock, address, NULL, 0)) != 0) {
        fprintf(stderr, "Failed to listen: %s\n", nng_strerror(rv));
        nng_close(sock);
    }
}

STELLA_API void socket_close(nng_socket sock)
{
    nng_close(sock);
}

STELLA_API void socket_send_string_message(nng_socket sock, char *message)
{
    int rv;
    nng_msg *msg;   // Message to send and receive
    size_t dataSize = strlen(message) + 1; // +1 for null terminator
    
    // Create a request message
    if ((rv = nng_msg_alloc(&msg, dataSize)) != 0) {
        fprintf(stderr, "Failed to allocate message: %s\n", rv);
        nng_close(sock);
    }
    
    // Copy your data into the message body
    memcpy(nng_msg_body(msg), message, dataSize);

    if ((rv = nng_sendmsg(sock, msg, NNG_FLAG_ALLOC)) != 0) {
        fprintf(stderr, "Failed to send: %s\n", rv);
        nng_msg_free(msg);
        nng_close(sock);
    }
}

STELLA_API char *socket_receive_string_message(nng_socket sock)
{
    int rv;
    nng_msg *msg = NULL;
    char *messageCopy = NULL; 

    // Receive the reply
    if ((rv = nng_recvmsg(sock, &msg, 0)) != 0) {
        fprintf(stderr, "Failed to receive: %s\n", nng_strerror(rv));
        nng_close(sock);
        return NULL; // Indicate failure by returning NULL
    }

    // Calculate length and allocate memory for the copy
    size_t dataSize = nng_msg_len(msg) + 1; // +1 for the null terminator
    messageCopy = nng_alloc(dataSize);
    if (!messageCopy) {
        fprintf(stderr, "Failed to allocate memory for message copy\n");
        nng_msg_free(msg);
        nng_close(sock);
        return NULL; // Indicate failure
    }
    
    // Copy the message body and add null terminator
    memcpy(messageCopy, nng_msg_body(msg), nng_msg_len(msg));
    messageCopy[nng_msg_len(msg)] = '\0'; // Ensure null-termination

    nng_msg_free(msg); // Free the original message after copying
    return messageCopy; // Return the copy
}

STELLA_API void free_received_message(char* message) {
    nng_free(message, strlen(message) + 1); // Free the allocated message
}
