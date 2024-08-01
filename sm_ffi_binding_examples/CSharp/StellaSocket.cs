using System.Runtime.InteropServices;
using nng_socket = System.UIntPtr;

namespace StellaSockets;

public class StellaSocket : IDisposable
{
    private nng_socket _socketHandle;
    public SocketType Type { get; set; }

    public StellaSocket(SocketType type)
    {
        _socketHandle = type switch
        {
            SocketType.Pair => StellaMessagingInterop.create_pair_socket(),
            SocketType.Bus => StellaMessagingInterop.create_bus_socket(),
            SocketType.Pub => StellaMessagingInterop.create_pub_socket(),
            SocketType.Pull => StellaMessagingInterop.create_pull_socket(),
            SocketType.Push => StellaMessagingInterop.create_push_socket(),
            SocketType.Request => StellaMessagingInterop.create_request_socket(),
            SocketType.Respondent => StellaMessagingInterop.create_respondent_socket(),
            SocketType.Response => StellaMessagingInterop.create_reponse_socket(),
            SocketType.Sub => StellaMessagingInterop.create_sub_socket(),
            SocketType.Surveyor => StellaMessagingInterop.create_surveyor_socket(),
            _ => throw new ArgumentException("Invalid socket type."),
        };
    }

    /// <summary>
    ///  Use Connect to create a client socket
    /// </summary>
    /// <param name="address"></param>
    public void Connect(string address)
    {
        StellaMessagingInterop.socket_connect(_socketHandle, address);
    }

    /// <summary>
    /// Use Bind if you want to create a server socket
    /// </summary>
    /// <param name="address"></param>
    public void Bind(string address)
    {
        StellaMessagingInterop.socket_bind(_socketHandle, address);
    }

    public void Send(string message)
    {
        StellaMessagingInterop.socket_send_string_message(_socketHandle, message);
    }

    public string Receive()
    {
        string message = StellaMessagingInterop.socket_receive_string_message(_socketHandle);
        // Make a copy of the string to prevent it from being collected before we free it
        string messageCopy = new string(message);
        StellaMessagingInterop.free_received_message(Marshal.StringToCoTaskMemAuto(message)); // Free the message
        return message;
    }

    public void Dispose()
    {
        StellaMessagingInterop.socket_close(_socketHandle);
    }
}

public enum SocketType
{
    /// <summary>
    /// Normally, this pattern will block when attempting to send a message if no peer is able to receive the message.
    /// Applications that require reliable delivery semantics should consider using req sockets, or implement their own acknowledgment layer on top of pair sockets.
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_pair.7.adoc
    /// </summary>
    Pair,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_bus.7.adoc
    /// </summary>
    Bus,
    /// <summary>
    /// Fore more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_rep.7.adoc
    /// </summary>
    Response,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_req.7.adoc
    /// </summary>
    Request,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_sub.7.adoc
    /// </summary>
    Sub,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_pub.7.adoc
    /// </summary>
    Pub,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_pull.7.adoc
    /// </summary>
    Pull,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_push.7.adoc
    /// </summary>
    Push,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_respondent.7.adoc
    /// </summary>
    Respondent,
    /// <summary>
    /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_surveyor.7.adoc
    /// </summary>
    Surveyor
}

