import 'package:stella_sockets_lib/src/stella_messaging_interop.dart';
import 'package:ffi/ffi.dart';

class StellaSocket {
  final stella = StellaMessagingInterop();
  late NngSocket _socketHandle;

  StellaSocket(SocketType type) {
    switch (type) {
      case SocketType.push:
        _socketHandle = stella.createPushSocket();
        break;
      case SocketType.request:
        _socketHandle = stella.createRequestSocket();
        break;
      default:
        throw ArgumentError("Invalid Socket Type");
    }
  }

  void connect(String address) {
    stella.socketConnect(_socketHandle, address.toNativeUtf8().cast<Utf8>());
  }

  void bind(String address) {
    stella.socketBind(_socketHandle, address.toNativeUtf8().cast<Utf8>());
  }

  void send(String message) {
    stella.sendStringMessage(_socketHandle, message);
  }

  String receive() {
    return stella.receiveStringMessage(_socketHandle);
  }
}

enum SocketType {
  /// <summary>
  /// Normally, this pattern will block when attempting to send a message if no peer is able to receive the message.
  /// Applications that require reliable delivery semantics should consider using req sockets, or implement their own acknowledgment layer on top of pair sockets.
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_pair.7.adoc
  /// </summary>
  pair,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_bus.7.adoc
  /// </summary>
  bus,

  /// <summary>
  /// Fore more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_rep.7.adoc
  /// </summary>
  response,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_req.7.adoc
  /// </summary>
  request,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_sub.7.adoc
  /// </summary>
  sub,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_pub.7.adoc
  /// </summary>
  pub,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_pull.7.adoc
  /// </summary>
  pull,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_push.7.adoc
  /// </summary>
  push,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_respondent.7.adoc
  /// </summary>
  respondent,

  /// <summary>
  /// For more see https://github.com/nanomsg/nng/blob/master/docs/man/nng_surveyor.7.adoc
  /// </summary>
  surveyor
}
