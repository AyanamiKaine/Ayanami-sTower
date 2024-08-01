#include "C://Program Files//Erlang OTP//erts-15.0.1//include//erl_nif.h"
#include  <string.h>
#include <nng/nng.h>
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

static ErlNifResourceType* PULL_SOCKET_RESOURCE_TYPE = NULL;

static void pull_socket_resource_destructor(ErlNifEnv* env, void* obj) {
    nng_socket* sock_ptr = (nng_socket*)obj;
    nng_close(*sock_ptr);
}

static ERL_NIF_TERM
elixir_create_pull_socket(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[]) {
    nng_socket sock;
    int rv;

    // Create the pull socket
    nng_pull0_open(&sock);

    nng_socket* sock_ptr = enif_alloc_resource(PULL_SOCKET_RESOURCE_TYPE, sizeof(nng_socket));
    *sock_ptr = sock;
    ERL_NIF_TERM result = enif_make_resource(env, sock_ptr);
    enif_release_resource(sock_ptr);
    return result;
}

// Module initialization (required for resource types)
static int load(ErlNifEnv* env, void** priv_data, ERL_NIF_TERM load_info) {
    // Register the resource type if you're using it
    ErlNifResourceType* rt = enif_open_resource_type(env, NULL, "pull_socket", pull_socket_resource_destructor, ERL_NIF_RT_CREATE, NULL);
    if (rt == NULL) {
        return -1; // Failed to create resource type
    }
    PULL_SOCKET_RESOURCE_TYPE = rt;
    return 0;
}

// Let's define the array of ErlNifFunc beforehand:
static ErlNifFunc nif_funcs[] = {
  // {erl_function_name, erl_function_arity, c_function}
  {"create_pull_socket", 0, elixir_create_pull_socket}
};

ERL_NIF_INIT(Elixir.StellaMessagingFfi, nif_funcs, NULL, NULL, NULL, NULL)
