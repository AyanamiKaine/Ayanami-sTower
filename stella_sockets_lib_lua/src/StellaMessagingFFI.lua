local ffi = require("ffi")

local StellaMessaging = {}

ffi.cdef[[
    typedef struct nng_socket_s { uint32_t id; } nng_socket;    
    
    nng_socket create_pair_socket();

    nng_socket  create_push_socket();
    nng_socket create_pull_socket();

    nng_socket create_reponse_socket();
    nng_socket create_request_socket();

    void socket_connect(nng_socket  sock, const char* address); // For closing the socket
    void socket_bind(nng_socket  sock, const char *address);
    void socket_close(nng_socket );
    
    void socket_send_string_message(nng_socket  sock, char *message);
    int socket_send_string_message_no_block(nng_socket sock, char *message);
    
    char* socket_receive_string_message(nng_socket  sock);
    void free_received_message(char *message);
]]

StellaMessaging = ffi.load(ffi.os == "Windows" and "stella_messaging" or "stella_messaging")

return StellaMessaging