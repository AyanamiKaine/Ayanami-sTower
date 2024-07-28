#ifndef  STELLAMESSAGING_H
#define STELLAMESSAGING_H

#include <stdio.h>
#include <string.h>

#include <nng/nng.h>
#include <nng/protocol/reqrep0/rep.h>
#include <nng/protocol/pair0/pair.h> // Or your preferred protocol

nng_socket create_pair_socket();
void connect_dial(nng_socket sock, const char* address);
void listen_for_connections(nng_socket sock, const char *address);

void close_socket(nng_socket);

void send_string_message(nng_socket sock, char *message);
char *receive_string_message(nng_socket sock);

#endif