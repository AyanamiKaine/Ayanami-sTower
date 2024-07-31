using System.Runtime.InteropServices;
// NNG socket handle type
using nng_socket = nuint;

partial class StellaMessagingInterop
{
    // Function imports from stella_messaging
    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_pair_socket();
    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_bus_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_reponse_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_request_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_sub_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_pub_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_pull_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_push_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_respondent_socket();

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial nng_socket create_surveyor_socket();

    [LibraryImport("stella_messaging", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void socket_connect(nng_socket sock, string address);

    [LibraryImport("stella_messaging", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void socket_bind(nng_socket sock, string address);

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void socket_close(nng_socket sock);

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void socket_send_string_message(nng_socket sock,
                                                        [MarshalAs(UnmanagedType.LPStr)] string message);

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int socket_send_string_message_non_block(nng_socket sock,
                                                        [MarshalAs(UnmanagedType.LPStr)] string message);

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static partial string socket_receive_string_message(nng_socket sock);

    [LibraryImport("stella_messaging")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void free_received_message(IntPtr message);
}