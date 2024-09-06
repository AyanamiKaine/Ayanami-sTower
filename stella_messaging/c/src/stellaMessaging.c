#include "stellaMessaging.h"
#include <stdlib.h>
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
    nng_socket sock; // Server socket
    int rv;

    // Create the response socket
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

    // Create the subscriber socket
    if ((rv = nng_sub0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}
STELLA_API nng_socket create_pub_socket()
{
    nng_socket sock; // Server socket
    int rv;

    // Create the publisher socket
    if ((rv = nng_pub0_open(&sock)) != 0) {
        //printf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_pull_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the pull socket
    if ((rv = nng_pull0_open(&sock)) != 0) {
        fprintf(stderr, "Failed to create socket: %s\n", nng_strerror(rv));
    }

    return sock;
}

STELLA_API nng_socket create_push_socket()
{
    nng_socket sock; // Client socket
    int rv;

    // Create the push socket
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
    if ((nng_dial(sock, address, NULL, 0)) != 0) {
        fprintf(stderr, "Failed to connect: %s\n", nng_strerror(rv));
    }
}

STELLA_API void socket_bind(nng_socket sock, const char *address)
{
    int rv;
    // Listen for connections
    if ((rv = nng_listen(sock, address, NULL, 0)) != 0) {
        fprintf(stderr, "Failed to listen: %s\n", nng_strerror(rv));
    }
}

STELLA_API void socket_close(nng_socket sock)
{
    nng_close(sock);
}

STELLA_API nng_msg* create_msg_with_string(char *string_message)
{
    int rv;
    nng_msg* msg;
    if ((rv = nng_msg_alloc(&msg, 0)) != 0) {
		//fatal("nng_msg_alloc", rv);
    }
    nng_msg_append(msg, string_message, strlen(string_message) + 1);
    return msg;
}

STELLA_API void socket_send_string_message(nng_socket sock, char *message)
{
    int rv;
    nng_msg *msg;   // Message to send and receive
    size_t dataSize = strlen(message) + 1; // +1 for null terminator
    
    // Create a request message
    if ((rv = nng_msg_alloc(&msg, dataSize)) != 0) {
        fprintf(stderr, "Failed to allocate message: %s\n",  nng_strerror(rv));
    }
    
    // Copy your data into the message body
    memcpy(nng_msg_body(msg), message, dataSize);

    if ((rv = nng_sendmsg(sock, msg, NNG_FLAG_ALLOC)) != 0) {
        fprintf(stderr, "Failed to send: %s\n",  nng_strerror(rv));
        nng_msg_free(msg);
    }
    nng_msg_free(msg); // Free the message regardless of success or failure
}

STELLA_API int socket_send_string_message_no_block(nng_socket sock, char *message)
{
    int rv;
    nng_msg *msg;   // Message to send and receive
    size_t dataSize = strlen(message) + 1; // +1 for null terminator
    
    // Create a request message
    if ((rv = nng_msg_alloc(&msg, dataSize)) != 0) {
        fprintf(stderr, "Failed to allocate message: %s\n", nng_strerror(rv));
    }
    
    // Copy data
    memcpy(nng_msg_body(msg), message, dataSize);

    // Send (non-blocking)
    if ((rv = nng_sendmsg(sock, msg, NNG_FLAG_NONBLOCK))!= 0) {
        fprintf(stderr, "Failed to send message: %s\n", nng_strerror(rv));
        nng_msg_free(msg); // Always free the message on error
        return rv;
    }

    nng_msg_free(msg); // Free the message regardless of success or failure
    // This will return NNG_EAGAIN (8) when the message couldnt not be send 
    // (most likely the server was not up and running) 
    // For now we expect the client to implement a queue behavior i.e when NNG_EAGAIN gets
    // returned we should put the message into a queue to be send again later.
    // Otherwise the message simply gets dropped.
    return 0;
}

STELLA_API void socket_send_topic_message(nng_socket sock, char *topic, char *message)
{
    int rv;
    nng_msg *topic_msg;   // Message to send and receive

    // Create a request message
    if ((rv = nng_msg_alloc(&topic_msg, 0)) != 0) {
        fprintf(stderr, "Failed to allocate message: %s\n",  nng_strerror(rv));
        return;
    }
    
    nng_msg_append(topic_msg, topic, strlen(topic) + 1);
    nng_msg_append(topic_msg, message, strlen(message) + 1); // +1 for null terminator

    if ((rv = nng_sendmsg(sock, topic_msg, 0)) != 0) {
        fprintf(stderr, "Failed to send: %s\n",  nng_strerror(rv));
        nng_msg_free(topic_msg); // Free the message regardless of success or failure
    }
}

STELLA_API void trim_topic_from_message(nng_msg *msg)
{
    const char *topic_of_msg = (const char *) nng_msg_body(msg);
    // Move to the actual message content
    size_t topic_len = strlen(topic_of_msg) + 1; // +1 for null terminator
    nng_msg_trim(msg, topic_len);
}

STELLA_API char *socket_receive_string_message(nng_socket sock)
{
    int rv;
    nng_msg *msg = NULL;
    char *messageCopy = NULL; 

    // Receive the reply
    if ((rv = nng_recvmsg(sock, &msg, 0)) != 0) {
        fprintf(stderr, "Failed to receive: %s\n", nng_strerror(rv));
        return ""; // Indicate failure by returning NULL
    }

    // Calculate length and allocate memory for the copy
    size_t dataSize = nng_msg_len(msg) + 1; // +1 for the null terminator
    messageCopy = nng_alloc(dataSize);
    if (!messageCopy) {
        fprintf(stderr, "Failed to allocate memory for message copy\n");
        nng_msg_free(msg);
        return "";; // Indicate failure
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

STELLA_API char* get_string_from_msg(nng_msg *msg) {
    return (char *)nng_msg_body(msg);
}

STELLA_API char* socket_receive_topic_message(nng_socket sock) {
    nng_msg *msg;
    int rv;
    char *messageCopy = NULL; 

    if ((rv = nng_recvmsg(sock, &msg, 0)) != 0) {
        printf("Error receiving message: %s\n", nng_strerror(rv));
        return "";
    }

    const char *topic_of_msg = (const char *) nng_msg_body(msg);

     // Move to the actual message content
    size_t topic_len = strlen(topic_of_msg) + 1; // +1 for null terminator
    nng_msg_trim(msg, topic_len);
    
    // Calculate length and allocate memory for the copy
    size_t dataSize = nng_msg_len(msg) + 1; // +1 for the null terminator
    messageCopy = nng_alloc(dataSize);
    if (!messageCopy) {
        //fprintf(stderr, "Failed to allocate memory for message copy\n");
        nng_msg_free(msg);
    }

    char *message = (char *)nng_msg_body(msg);
    // Copy the message body and add null terminator
    memcpy(messageCopy, nng_msg_body(msg), nng_msg_len(msg));
    messageCopy[nng_msg_len(msg)] = '\0'; // Ensure null-termination

    nng_msg_free(msg); // Free the original message after copying
    return messageCopy; // Return the copy
}


STELLA_API void subscribed_to_topic(nng_socket sock, char *topic) {
    int rv = nng_socket_set_string(sock, NNG_OPT_SUB_SUBSCRIBE, topic);
    if (rv != 0) {
        fprintf(stderr, "Failed to subscribe to topic: %s\n", nng_strerror(rv));
    }
}

STELLA_API void subscribe_to_topic_for_context(nng_ctx ctx, char *topic) {
    int rv = nng_ctx_set_string(ctx, NNG_OPT_SUB_SUBSCRIBE, topic);
    if (rv != 0) {
        fprintf(stderr, "Failed to subscribe to topic: %s\n", nng_strerror(rv));
    }
}


STELLA_API void unsubscribed_to_topic(nng_socket sock, char *topic) {
    int rv = nng_socket_set_string(sock, NNG_OPT_SUB_UNSUBSCRIBE, topic);
    if (rv != 0) {
        fprintf(stderr, "Failed to unsubscribe from topic: %s\n", nng_strerror(rv));
    }
}

void fatal(const char *func, int rv)
{
	fprintf(stderr, "%s: %s\n", func, nng_strerror(rv));
	exit(1);
}

struct work {
    enum { INIT, RECV, WAIT, SEND } state;
    nng_aio *aio;
    nng_msg *msg;
    nng_ctx ctx;
    nng_socket  sock;
};


/*
Here we create a work struct to be used for a server,

It starts as INIT, we define a callback that must return void and takes a opaque argument
Example: 
void server_cb(void *arg){
    struct work *work = arg;
    ...
    Do something with the work
}

*/
STELLA_API struct work* alloc_work(nng_socket sock, void CALL_BACK(void*))
{
	struct work *w;
	int          rv;

	if ((w = nng_alloc(sizeof(*w))) == NULL) {
		fatal("nng_alloc", NNG_ENOMEM);
	}
	if ((rv = nng_aio_alloc(&w->aio, CALL_BACK, w)) != 0) {
		fatal("nng_aio_alloc", rv);
	}
	if ((rv = nng_ctx_open(&w->ctx, sock)) != 0) {
		fatal("nng_ctx_open", rv);
	}
    w->state = INIT;
	return (w);
}

STELLA_API struct work* create_async_work(nng_socket sock, void CALL_BACK(void*))
{
	struct work *work;

    if ((work = nng_alloc(sizeof(*work))) == NULL) {
		fatal("nng_alloc", NNG_ENOMEM);
	}

    nng_aio_alloc(&work->aio, CALL_BACK,work);
    work->state = INIT;

    work->sock = sock;
    return work;
}

/* The server runs forever. 

// 128 is the maximum number of outstanding requests we can handle.
// This is *NOT* the number of threads in use, but instead represents
// outstanding work items.  Select a small number to reduce memory size.
// (Each one of these can be thought of as a request-reply loop.)  Note
// that you will probably run into limitations on the number of open file
// descriptors if you set this too high. (If not for that limit, this could
// be set in the thousands, each context consumes a couple of KB.)
*/
STELLA_API void async_rep_server(const char* url, void CALL_BACK(void*))
{
    nng_socket sock = create_reponse_socket();
    
    socket_bind(sock, url);
    struct work *works[128];
	int          i;

	for (i = 0; i < 128; i++) {
		works[i] = alloc_work(sock, CALL_BACK);
	}

	for (i = 0; i < 128; i++) {
		// Here we setup the callback function that runs, we setup more then 100 that can work in parallel
        CALL_BACK(works[i]); // this starts them going (INIT state)
	}
}

STELLA_API struct work* async_sub_server(char* url, void CALL_BACK(void*))
{
    nng_socket sock = create_sub_socket();

    socket_connect(sock, url);
    struct work *work  = alloc_work(sock, CALL_BACK);
	// Here we setup the callback function that runs, we setup more then 100 that can work in parallel
    CALL_BACK(work); // this starts them going (INIT state)
    return work;
}

STELLA_API void async_request_client(char* url, nng_socket sock, void CALL_BACK(void*))
{

	struct work *w;

    if ((w = nng_alloc(sizeof(*w))) == NULL) {
		fatal("nng_alloc", NNG_ENOMEM);
	}

    nng_aio_alloc(&w->aio, CALL_BACK, w);
    w->state = INIT;

    w->sock = sock;
    socket_connect(w->sock, url);
    CALL_BACK(w);
}


STELLA_API void sleep_async_request(nng_duration time, work* work)
{
	nng_sleep_aio(time, work->aio);
}

STELLA_API void async_receive(work *work)
{
	nng_ctx_recv(work->ctx, work->aio);
}

// Should be used after set_async_message
STELLA_API void send_async(work *work)
{
    nng_ctx_send(work->ctx, work->aio);
}

STELLA_API void send_async_aio(work *work)
{
    nng_send_aio(work->sock, work->aio);
}

STELLA_API void receive_async_aio(work *work)
{
    nng_recv_aio(work->sock,work->aio);
}

STELLA_API void set_async_message(work *work)
{
    // Before this function is called we can define the message we want to send

    // nng_aio_set_msg sets the message structure to use for asynchronous
    // message send operations.
    nng_aio_set_msg(work->aio, work->msg);
    work->msg   = NULL;
	work->state = SEND;
}

STELLA_API void check_async_result(work *work)
{
    if ((nng_aio_result(work->aio)) != 0) {
			nng_msg_free(work->msg);
		}
}

STELLA_API nng_msg *get_message_from_async(work *work)
{
    return nng_aio_get_msg(work->aio);
}