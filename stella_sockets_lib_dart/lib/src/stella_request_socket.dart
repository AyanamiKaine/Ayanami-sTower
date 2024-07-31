import 'package:stella_sockets_lib/stella_sockets.dart';

/// <summary>
///  In this pattern, a requester sends a message to one replier, who is expected to reply.
///  The request is resent if no reply arrives, until a reply is received or the request times out.
///
/// This protocol is useful in setting up RPC-like services.
/// It is also "reliable", in that a the requester will keep retrying until a reply is received.
/// </summary>
/// <param name="address"></param>
class StellaRequestSocket extends StellaSocket {
  /// <summary>
  ///  In this pattern, a requester sends a message to one replier, who is expected to reply.
  ///  The request is resent if no reply arrives, until a reply is received or the request times out.
  ///
  /// This protocol is useful in setting up RPC-like services.
  /// It is also "reliable", in that a the requester will keep retrying until a reply is received.
  /// </summary>
  /// <param name="address"></param>
  StellaRequestSocket(String address) : super(SocketType.request) {
    connect(address);
  }
}
