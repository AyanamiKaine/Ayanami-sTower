# Example Usage

```dart
final stella = StellaMessaging();

final pullSocket =
    stella.openPullSocket("ipc:///hello_world_dart_unit_test");
final pushSocket =
    stella.openPushSocket("ipc:///hello_world_dart_unit_test");

stella.sendStringMessage(pushSocket, "Hello World");

String expectedMessage = "Hello World";
String actualMessage = stella.receiveStringMessage(pullSocket);

stella.socketClose(pullSocket);
stella.socketClose(pushSocket);

expect(actualMessage, expectedMessage);
```
