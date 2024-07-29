# Stella Messaging

- A thin wrapper around NNG
- Higher Level Abstraction for ease of use
- Easier for other language bindings
- Based on sending strings
- Less flexible
- Less performance

Its expected that each server/client synchronously works on each message one by one but that the servers and clients work parrallel to each other similar how you do it in Elixir.

## Example Client

```C
int main() {
    nng_socket sock = create_pair_socket();

    connect_dial(sock, "ipc:///hello_world");
    send_msg(sock, "Hello World! from a C Client!");

    nng_close(sock);
    return 0;
}
```

## Example Server

```C
int main() {

    nng_socket sock = create_pair_socket();
    socket_bind(sock, "ipc:///hello_world");

    while (1) {
        printf("Received request: %s\n", socket_receive_string_message(sock));
    }

    nng_close(sock);
    return 0;
}
```
