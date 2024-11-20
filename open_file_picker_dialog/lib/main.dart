import 'dart:developer';
import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:args/args.dart';

void main(List<String> arguments) async {
  exitCode = 0; // presume success

  // Parse command-line arguments
  final parser = ArgParser()
    ..addOption('type',
        abbr: 't',
        allowed: ['any', 'audio', 'image', 'video', 'custom'],
        defaultsTo: 'any',
        help: 'The type of file to pick')
    ..addMultiOption('allowed-extensions',
        abbr: 'e', help: 'Allowed file extensions (only for custom type)')
    ..addFlag('multiple', abbr: 'm', help: 'Allow picking multiple files');

  ArgResults argResults;
  try {
    argResults = parser.parse(arguments);
  } catch (e) {
    stderr.writeln(e.toString());
    stderr.writeln(parser.usage);
    exitCode = 2;
    return;
  }

  FileType type;
  switch (argResults['type']) {
    case 'any':
      type = FileType.any;
      break;
    case 'audio':
      type = FileType.audio;
      break;
    case 'image':
      type = FileType.image;
      break;
    case 'video':
      type = FileType.video;
      break;
    case 'custom':
      type = FileType.custom;
      break;
    default:
      stderr.writeln('Invalid file type: ${argResults['type']}');
      exitCode = 2;
      return;
  }

  FilePickerResult? result = await FilePicker.platform.pickFiles(
    type: type,
    allowedExtensions: argResults['allowed-extensions'] as List<String>?,
    allowMultiple: argResults['multiple'] as bool,
  );

  if (result != null) {
    if (argResults['multiple'] as bool) {
      for (var file in result.files) {
        log(file.path!);
        stdout.writeln(file.path!); 
      }
    } else {
      log(result.files.single.path!);
      stdout.writeln(result.files.single.path!);
    }
  } else {
    stderr.writeln('File selection canceled.');
    exitCode = 1;
  }
  exit(0);
}
