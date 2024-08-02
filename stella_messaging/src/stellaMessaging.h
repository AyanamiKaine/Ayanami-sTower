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

STELLA_API char* socket_receive_string_message(nng_socket sock);
STELLA_API char* socket_receive_topic_message(nng_socket sock);
STELLA_API void free_received_message(char *message);

STELLA_API void subscribed_to_topic(nng_socket sock, char *topic);
STELLA_API void unsubscribed_to_topic(nng_socket sock, char *topic);
STELLA_API void socket_send_topic_message(nng_socket sock, char *topic, char *message);

#endif