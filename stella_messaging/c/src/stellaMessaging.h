#ifndef  STELLAMESSAGING_H
#define STELLAMESSAGING_H

#include <stdio.h>
#include <string.h>

#include <nng/nng.h>
#include <nng/protocol/pair1/pair.h> 
#include <nng/protocol/bus0/bus.h>

#include <nng/protocol/reqrep0/rep.h>
#include <nng/protocol/reqrep0/req.h>

#include <nng/protocol/pubsub0/pub.h>
#include <nng/protocol/pubsub0/sub.h>

#include <nng/protocol/pipeline0/push.h>
#include <nng/protocol/pipeline0/pull.h>

#include <nng/protocol/survey0/respond.h>
#include <nng/protocol/survey0/survey.h>

// Export macros (as you provided)
#ifdef BUILDING_STELLA_MESSAGING
    #define STELLA_API extern __declspec(dllexport)
#else
    #define STELLA_API extern __declspec(dllimport)
#endif

STELLA_API nng_socket create_pair_socket();
STELLA_API nng_socket create_bus_socket();

STELLA_API nng_socket create_reponse_socket();
STELLA_API nng_socket create_request_socket();

STELLA_API nng_socket create_sub_socket();
STELLA_API nng_socket create_pub_socket();

STELLA_API nng_socket create_pull_socket();
STELLA_API nng_socket create_push_socket();

STELLA_API nng_socket create_respondent_socket();
STELLA_API nng_socket create_surveyor_socket();

STELLA_API void socket_connect(nng_socket sock, const char* address);

STELLA_API void socket_bind(nng_socket sock, const char *address);

STELLA_API void socket_close(nng_socket);

STELLA_API void socket_send_string_message(nng_socket sock, char *message);
STELLA_API int socket_send_string_message_no_block(nng_socket sock, char *message);

STELLA_API nng_msg* create_msg_with_string(char *string_message);

STELLA_API void trim_topic_from_message(nng_msg *msg);

STELLA_API char* socket_receive_string_message(nng_socket sock);
STELLA_API char* socket_receive_topic_message(nng_socket sock);
STELLA_API char* get_string_from_msg(nng_msg *msg);
STELLA_API void free_received_message(char *message);

STELLA_API void subscribed_to_topic(nng_socket sock, char *topic);
STELLA_API void subscribe_to_topic_for_context(nng_ctx ctx, char *topic);

STELLA_API void unsubscribed_to_topic(nng_socket sock, char *topic);
STELLA_API void socket_send_topic_message(nng_socket sock, char *topic, char *message);


/*
Various different functions related to async work
*/
typedef struct work work;

STELLA_API work* alloc_work(nng_socket sock, void CALL_BACK(void *));
// Higher level abstraction for an async response server, when we receive an request the callback function
// can use 	struct work *work = arg; assuming you defined the callback as void call_back(void *arg) 
// You then can use the work struct to work with the data the server got
// work {
//    enum { INIT, RECV, WAIT, SEND } state;
//    nng_aio *aio;
//    nng_msg *msg;
//    nng_ctx ctx;
// }
STELLA_API void async_rep_server(const char *url, void CALL_BACK(void *));
STELLA_API struct work *async_sub_server(char *url, void CALL_BACK(void *));

STELLA_API void async_receive(work *work);
STELLA_API void sleep_async_request(nng_duration time, work *work);
STELLA_API void send_async(work *work);
STELLA_API void send_async_aio(work *work);
STELLA_API void receive_async_aio(work *work);

STELLA_API void set_async_message(work *work);
STELLA_API void check_async_result(work *work);
STELLA_API nng_msg *get_message_from_async(work *work);
STELLA_API struct work *create_async_work(nng_socket sock, void CALL_BACK(void *));

#endif