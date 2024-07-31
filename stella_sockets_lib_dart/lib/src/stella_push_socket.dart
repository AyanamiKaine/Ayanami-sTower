import 'package:stella_sockets_lib/stella_sockets.dart';

/// <summary>
/// Purpose: Creates a one-way data flow from one or more
/// senders (push) to one or more receivers (pull).
///
/// Ideal For: Building data pipelines, processing streams of data,
/// or any scenario where you need a linear flow of information.
/// Use Push and Pull Sockets if you want that many clients
/// can send messages to a server without expecting a message back
///
/// Pull is the counterpart to Push.
///
/// Pushers distribute messages to pullers.
/// Each message sent by a pusher will be sent to one of its
/// peer pullers, chosen in a round-robin fashion from the
/// set of connected peers available for receiving.
///
/// This means that clients can connect to multiple Servers,
/// and pick the one that is available,
/// in this case each server should be the same.
///
/// This property makes this pattern useful in load-balancing
/// scenarios.
/// </summary>
class StellaPushSocket extends StellaSocket {
  /// <summary>
  /// Purpose: Creates a one-way data flow from one or more
  /// senders (push) to one or more receivers (pull).
  ///
  /// Ideal For: Building data pipelines, processing streams of data,
  /// or any scenario where you need a linear flow of information.
  /// Use Push and Pull Sockets if you want that many clients
  /// can send messages to a server without expecting a message back
  ///
  /// Pull is the counterpart to Push.
  ///
  /// Pushers distribute messages to pullers.
  /// Each message sent by a pusher will be sent to one of its
  /// peer pullers, chosen in a round-robin fashion from the
  /// set of connected peers available for receiving.
  ///
  /// This means that clients can connect to multiple Servers,
  /// and pick the one that is available,
  /// in this case each server should be the same.
  ///
  /// This property makes this pattern useful in load-balancing
  /// scenarios.
  /// </summary>
  StellaPushSocket(String address) : super(SocketType.push) {
    connect(address);
  }
}
