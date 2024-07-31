import 'package:stella_sockets_lib/stella_sockets.dart';

void main() {
  final client = StellaPushSocket("ipc:///StellaLogger");
  client.connect("ipc:///StellaLogger");
  client.send("""
  {
    "ErrorType": "Info",
    "ErrorTime": "${DateTime.now().toIso8601String()}",
    "ErrorMessage": "High CPU usage detected.",
    "Sender": "Stella Learning Flutter UI Client"
  }
  """);
}
