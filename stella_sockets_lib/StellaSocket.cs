﻿using System.Runtime.InteropServices;
using nng_socket = System.UIntPtr;

namespace StellaSockets;

public class StellaSocket : IDisposable
{
    protected readonly nng_socket _socketHandle;
    public SocketType Type { get; set; }

    public StellaSocket(SocketType type)
    {
        _socketHandle = type switch
        {
            SocketType.Pair => StellaMessagingInterop.create_pair_socket(),
            //SocketType.Bus => StellaMessagingInterop.create_bus_socket(),
            SocketType.Pub => StellaMessagingInterop.create_pub_socket(),
            SocketType.Pull => StellaMessagingInterop.create_pull_socket(),
            SocketType.Push => StellaMessagingInterop.create_push_socket(),
            SocketType.Request => StellaMessagingInterop.create_request_socket(),
            //SocketType.Respondent => StellaMessagingInterop.create_respondent_socket(),
            SocketType.Response => StellaMessagingInterop.create_reponse_socket(),
            SocketType.Sub => StellaMessagingInterop.create_sub_socket(),
            //SocketType.Surveyor => StellaMessagingInterop.create_surveyor_socket(),
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

    /// <summary>
    /// Send a string message
    /// </summary>
    /// <param name="message"></param>
    public void Send(string message)
    {
        StellaMessagingInterop.socket_send_string_message(_socketHandle, message);
    }

    public void Send(string message, string topic)
    {
        StellaMessagingInterop.socket_send_topic_message(_socketHandle, topic, message);
    }

    /// <summary>
    /// Send a string message and returns immedially, if the message couldnt not be send it will discard the message.
    /// It returns a zero if the message was send and returns NNG_EAGAIN (8) if the message was not send, its your responsibility to handle this case
    /// </summary>
    /// <param name="message"></param>
    public ReturnValue SendNonBlock(string message)
    {
        int rv = StellaMessagingInterop.socket_send_string_message_no_block(_socketHandle, message);
        if (rv == 0)
        {
            return ReturnValue.Success;
        }
        else
        {
            return ReturnValue.CouldNotSend;
        }
    }

    /// <summary>
    /// Receive a string message
    /// </summary>
    /// <returns></returns>
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
        GC.SuppressFinalize(this);
        StellaMessagingInterop.socket_close(_socketHandle);
    }
}

public enum ReturnValue
{
    Success = 0,
    CouldNotSend = 8
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

