import 'dart:convert';
import 'dart:developer';
import 'dart:io';
import 'dart:isolate';

import 'package:dartnng/dartnng.dart';
import 'file_to_learn.dart';

class FilesToLearnDatabase {
  Isolate? _isolate;

  var _sendPort;

  FilesToLearnDatabase() {
    start();
  }

  messageHandler(SendPort sendPort) async {
    final RequestSocket client = RequestSocket("ipc:///test");
    var port = ReceivePort();
    sendPort.send(port.sendPort);

    await for (var msg in port) {
      client.send_string(msg);
      var s = client.receive_string();
      print('server response: $s');
    }
  }

  List<FileToLearn> getFilesToLearn() {
    if (_sendPort != null) {
      _sendPort!.send("{'Command': 'RETRIVE_ALL_ITEMS'}");
    } else {
      log('Isolate not spawned yet.');
    }
    return [];
  }

  void update() {}
  void delete() {}
  void retriev() {}
  void create(FileToLearn fileToLearn) {
    fileToLearn.pathToFile = fileToLearn.pathToFile.replaceAll('"', '');

    String jsonString = jsonEncode({
      "Command": "RETRIVE_ALL_ITEMS",
      "FileToLearn": fileToLearn.toJson(),
    });

    _sendPort!.send(jsonString);
    log("Trying to create the following file $jsonString");
  }

  Future<void> spawnIsolate() async {
    final receivePort = ReceivePort();
    _isolate = await Isolate.spawn(messageHandler, receivePort.sendPort);
    _sendPort = await receivePort.first;
  }

  start() async {
    await spawnIsolate();
  }
}
