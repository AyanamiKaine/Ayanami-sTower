import 'dart:ffi';
import 'dart:io';

import 'package:ffi/ffi.dart';

typedef NngSocket = Pointer<Uint8>;
typedef SocketSendStringMessageNative = Void Function(
    NngSocket sock, Pointer<Utf8> message);
typedef SocketSendStringMessageDart = void Function(
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

class StellaMessagingInterop {
  late final DynamicLibrary stellaMessagingLib;

  late DartNngFunction createPairSocket;
  late DartNngFunction createRequestSocket;
  late DartNngFunction createPushSocket;
  late DartSockAddrFunction socketConnect;
  late DartSockAddrFunction socketBind;
  late DartVoidFunction socketClose;
  late DartSendStringFunction socketSendStringMessage;
  late DartReceiveStringFunction socketReceiveStringMessage;
  late DartVoidFunction freeReceivedMessage;

  StellaMessagingInterop() {
    stellaMessagingLib = _loadLibrary();
    createPairSocket = stellaMessagingLib
        .lookup<NativeFunction<NativeNngFunction>>('create_pair_socket')
        .asFunction();

    createRequestSocket = stellaMessagingLib
        .lookup<NativeFunction<NativeNngFunction>>('create_request_socket')
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

  Pointer<Uint8> openPushSocket(String address) {
    final sock = createPushSocket();
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
      return DynamicLibrary.open('assets/native/windows/stella_messaging.dll');
    } else if (Platform.isMacOS) {
      return DynamicLibrary.open(
          'assets/native/macos/libstella_messaging.dylib');
    } else if (Platform.isLinux) {
      return DynamicLibrary.open('assets/native/linux/libstella_messaging.so');
    } else {
      throw UnsupportedError('Platform not supported');
    }
  }
}
