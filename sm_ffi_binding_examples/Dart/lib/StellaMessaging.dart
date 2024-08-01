import 'dart:ffi';
import 'dart:io';
import 'package:ffi/ffi.dart';

typedef nng_socket = Pointer<Uint8>;
typedef socket_send_string_message_native = Void Function(
    nng_socket sock, Pointer<Utf8> message);
typedef socket_send_string_message_dart = void Function(
    Pointer<Uint8> sock, Pointer<Utf8> message);
typedef NativeSendStringFunction = Void Function(
    Pointer<Uint8> sock, Pointer<Utf8> message);
typedef DartSendStringFunction = void Function(
    Pointer<Uint8> sock, Pointer<Utf8> message);
// Common Function Types:
typedef NativeNngFunction = Pointer<Uint8> Function();
typedef DartNngFunction = Pointer<Uint8> Function();

typedef NativeVoidFunction = Void Function(Pointer<Uint8>);
typedef DartVoidFunction = void Function(Pointer<Uint8>);

typedef NativeSockAddrFunction = Void Function(
    Pointer<Uint8> sock, Pointer<Utf8> addr);
typedef DartSockAddrFunction = void Function(
    Pointer<Uint8> sock, Pointer<Utf8> addr);

typedef NativeReceiveStringFunction = Pointer<Utf8> Function(
    Pointer<Uint8> sock);
typedef DartReceiveStringFunction = Pointer<Utf8> Function(Pointer<Uint8> sock);

typedef NativeFreeReceivedMessageFunction = Void Function(
    Pointer<Utf8> message);
typedef DartFreeReceivedMessageFunction = void Function(Pointer<Utf8> message);

class StellaMessaging {
  late final DynamicLibrary stellaMessagingLib;

  late DartNngFunction createPairSocket;
  late DartNngFunction createPullSocket; // For Servers
  late DartNngFunction createPushSocket; // For Clients
  late DartNngFunction createRequestSocket; // For Clients
  late DartSockAddrFunction socketConnect;
  late DartSockAddrFunction socketBind;
  late DartVoidFunction socketClose;
  late DartSendStringFunction socketSendStringMessage;
  late DartReceiveStringFunction socketReceiveStringMessage;
  late DartVoidFunction freeReceivedMessage;

  StellaMessaging() {
    stellaMessagingLib = _loadLibrary();
    createPairSocket = stellaMessagingLib
        .lookup<NativeFunction<NativeNngFunction>>('create_pair_socket')
        .asFunction();

    createRequestSocket = stellaMessagingLib
        .lookup<NativeFunction<NativeNngFunction>>('create_request_socket')
        .asFunction();

    createPullSocket = stellaMessagingLib
        .lookup<NativeFunction<NativeNngFunction>>('create_pull_socket')
        .asFunction();

    createPushSocket = stellaMessagingLib
        .lookup<NativeFunction<NativeNngFunction>>('create_push_socket')
        .asFunction();

    socketConnect = stellaMessagingLib
        .lookup<NativeFunction<NativeSockAddrFunction>>('socket_connect')
        .asFunction();

    socketBind = stellaMessagingLib
        .lookup<NativeFunction<NativeSockAddrFunction>>('socket_bind')
        .asFunction();

    socketClose = stellaMessagingLib
        .lookup<NativeFunction<NativeVoidFunction>>('socket_close')
        .asFunction();

    socketSendStringMessage = stellaMessagingLib // Your loaded library
        .lookup<NativeFunction<NativeSendStringFunction>>(
            'socket_send_string_message')
        .asFunction();

    socketReceiveStringMessage = stellaMessagingLib
        .lookup<NativeFunction<NativeReceiveStringFunction>>(
            'socket_receive_string_message')
        .asFunction();

    freeReceivedMessage = stellaMessagingLib
        .lookup<NativeFunction<NativeVoidFunction>>('free_received_message')
        .asFunction();
  }

  Pointer<Uint8> openPairSocket(String address) {
    final sock = createPairSocket();
    socketConnect(sock, address.toNativeUtf8().cast<Utf8>());
    return sock;
  }

  Pointer<Uint8> openRequestSocket(String address) {
    final sock = createRequestSocket();
    socketConnect(sock, address.toNativeUtf8().cast<Utf8>());
    return sock;
  }

  Pointer<Uint8> openPullSocket(String address) {
    final sock = createPullSocket();
    // Servers Bind to an Address
    socketBind(sock, address.toNativeUtf8().cast<Utf8>());
    return sock;
  }

  Pointer<Uint8> openPushSocket(String address) {
    final sock = createPushSocket();
    // Clients Connect to an Address
    socketConnect(sock, address.toNativeUtf8().cast<Utf8>());
    return sock;
  }

  String receiveStringMessage(Pointer<Uint8> sock) {
    Pointer<Utf8> msgPtr = socketReceiveStringMessage(sock);
    String dartString = msgPtr.toDartString();
    return dartString;
  }

  void sendStringMessage(Pointer<Uint8> sock, String message) {
    final messagePtr = message.toNativeUtf8();
    socketSendStringMessage(sock, messagePtr.cast<Utf8>());
    malloc.free(messagePtr);
  }

  void closeSocket(Pointer<Uint8> sock) {
    socketClose(sock);
  }

  // Private helper method to load the library
  DynamicLibrary _loadLibrary() {
    if (Platform.isWindows) {
      return DynamicLibrary.open('stella_messaging.dll');
    } else if (Platform.isMacOS) {
      return DynamicLibrary.open('libstella_messaging.dylib');
    } else if (Platform.isLinux) {
      return DynamicLibrary.open('libstella_messaging.so');
    } else {
      throw UnsupportedError('Platform not supported');
    }
  }
}
