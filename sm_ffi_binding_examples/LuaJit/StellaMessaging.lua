local ffi = require("ffi")

ffi.cdef[[
    typedef struct nng_socket_s { uint32_t id; } nng_socket;    
    nng_socket  create_push_socket();
    void socket_connect(nng_socket  sock, const char* address); // For closing the socket
    void socket_bind(nng_socket  sock, const char *address);
    void socket_close(nng_socket );
    void socket_send_string_message(nng_socket  sock, char *message);
    char* socket_receive_string_message(nng_socket  sock);
]]
local StellaMessaging = ffi.load(ffi.os == "Windows" and "stella_messaging")

local message = '{ "LogType": "Info", "LogTime": "2024.01.09", "LogMessage": "Hello World Test", "Sender": "NNG_LUA_TEST" }'

local c_str = ffi.new("char[?]", #message + 1)
ffi.copy(c_str, message)

local address = "ipc:///StellaLogger"

local c_str_address = ffi.new("char[?]", #address + 1)
ffi.copy(c_str_address, address)

local Server = StellaMessaging.create_push_socket();

StellaMessaging.socket_connect(Server, c_str_address);
StellaMessaging.socket_send_string_message(Server, c_str);
