import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:math' show max;
import 'package:stella_notes/faq.dart';
import 'package:stella_notes/guide.dart';
import 'package:uuid/uuid.dart';
import 'package:window_size/window_size.dart';

import 'package:fluent_ui/fluent_ui.dart'
    show
        Button,
        FilledButton,
        Checkbox,
        Colors,
        CommandBar,
        CommandBarButton,
        ContentDialog,
        FluentApp,
        FluentIcons,
        FluentTheme,
        FluentThemeData,
        InfoLabel,
        ListTile,
        ListView,
        NavigationAppBar,
        NavigationPane,
        NavigationView,
        NavigationViewState,
        NumberBox,
        PaneDisplayMode,
        PaneItem,
        PaneItemSeparator,
        ScaffoldPage,
        SpinButtonPlacementMode,
        TextBox; // Hide conflicting Colors
import 'package:flutter/material.dart'
    hide
        ListTile,
        Colors,
        Checkbox,
        FilledButton; // Explicitly import Flutter's Colors
import 'package:intl/intl.dart'; // Import the intl package

import 'dart:developer'; // Import for the log function
import 'package:dartzmq/dartzmq.dart';
import 'file_to_learn.dart';
//import 'stella_quizes.dart';

void main() {
  // Defining the window title if, we are one windows, linux, or mac-os

  if (Platform.isMacOS && Platform.isWindows && Platform.isLinux) {
    setWindowTitle("Stella Learning");
  }

  WidgetsFlutterBinding.ensureInitialized();

  runApp(const MyApp());
}

class SettingsModel extends ChangeNotifier {}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return FluentApp(
      // Using Fluent UI's app
      title: 'Stella Learning',
      theme: FluentThemeData(
        // Using Fluent UI's theme data
        accentColor: Colors.blue,
        brightness: Brightness.light,
        visualDensity: VisualDensity.adaptivePlatformDensity,
      ),
      home: const MyHomePage(title: 'Stella Learning'),
    );
  }
}

class MyHomePage extends StatefulWidget {
  const MyHomePage({super.key, required this.title});

  final String title;

  @override
  State<MyHomePage> createState() => _MyHomePageState();
}

class _MyHomePageState extends State<MyHomePage> {
  final ZContext _context = ZContext();
  late final ZSocket _loggerSocket;
  late final ZSocket _openFileWithDefaultProgramSocket;
  late final ZSyncSocket _spacedRepetitionDatabaseSocket;
  late final ZSyncSocket _spaceRepetitionAlgorithmSocket;

  int _selectedIndex = 0; // To keep track of the selected item
  final _viewKey = GlobalKey<NavigationViewState>();
  bool? connectedToBackend = false;

  final _formKey = GlobalKey<FormState>();

  // Text Editing Controllers
  var uuid = const Uuid(); // used to generate unique NoteIds
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _descriptionController = TextEditingController();
  final TextEditingController _filePathController = TextEditingController();
  double _priority = 0; // Default priority

  List<FileToLearn> filesToLearn = [];
  List<FileToLearn> _filteredNotes = [];
  List<FileToLearn> _filesToReview = [];

  final _noteSearchBoxController = TextEditingController();
  bool _showEditView = false;

  FileToLearn?
      nextReviewFile; // Initialize as null, holds the next file to review
  FileToLearn? _selectedFile; // Store the selected FileToLearn object

  void _showFileToLearnDetails(FileToLearn file) {
    setState(() {
      _selectedFile = file;
    });
  }

  String convertZMessageToString(ZMessage message) {
    List<int> allBytes = [];

    // Iterate through frames
    for (var frame in message) {
      allBytes.addAll(frame.payload);
    }

    // Check if the bytes are valid UTF-8
    try {
      utf8.decode(allBytes, allowMalformed: false);
    } catch (e) {
      // Handle invalid UTF-8 data
      log("Error: Invalid UTF-8 data");
      return "";
    }

    // Convert to string (assuming UTF-8 encoding)
    return String.fromCharCodes(allBytes);
  }

  void _gatherFilesToReview() async {
    _filesToReview = filesToLearn
        .where((file) => file.nextReviewDate.isBefore(DateTime.now()))
        .toList(); // Convert to list
  }

  void _deleteFileToLearn(FileToLearn fileToLearn) async {
    String fileToLearnId = fileToLearn.id;

    String jsonString = jsonEncode({
      "Command": "DELETE",
      "Id": fileToLearnId,
    });

    log("Trying to delete the following file $jsonString");

    _spacedRepetitionDatabaseSocket.sendString(jsonString);
    _spacedRepetitionDatabaseSocket.recv();

    setState(() {
      filesToLearn.removeWhere((file) => file.id == fileToLearn.id);
      _filesToReview = filesToLearn
          .where((file) => file.nextReviewDate.isBefore(DateTime.now()))
          .toList(); // Convert to list
    });

    _setFileWithHighestPriority();
  }

  void _retrieveAllItems() async {
    _spacedRepetitionDatabaseSocket
        .sendString('{ "Command": "RETRIVE_ALL_ITEMS"}');
    final response = _spacedRepetitionDatabaseSocket.recv();
    //log(String.fromCharCodes(response.first.payload));

    filesToLearn =
        parseFilesToLearn(String.fromCharCodes(response.first.payload));
  }

  void editItem(FileToLearn fileToLearn) async {
    fileToLearn.pathToFile = fileToLearn.pathToFile.replaceAll('"', '');

    final index = filesToLearn.indexWhere((item) => item.id == fileToLearn.id);
    if (index != -1) {
      filesToLearn[index] = fileToLearn;
      _sortNotesByPriority();
    } else {
      // Handle the case where the item with the matching ID wasn't found
      // You could throw an exception, print an error, or take other action
      log('Error: File to learn not found');
    }
    String jsonString = jsonEncode({
      "Command": "UPDATE",
      "FileToLearn": fileToLearn.toJson(),
    });

    log("Trying to update the following file $jsonString");

    _spacedRepetitionDatabaseSocket.sendString(jsonString);
    _spacedRepetitionDatabaseSocket.recv();
  }

  void _createNewItem(FileToLearn fileToLearn) async {
    fileToLearn.pathToFile = fileToLearn.pathToFile.replaceAll('"', '');

    String jsonString = jsonEncode({
      "Command": "CREATE",
      "FileToLearn": fileToLearn.toJson(),
    });

    log("Trying to create the following file $jsonString");

    _spacedRepetitionDatabaseSocket.sendString(jsonString);
    _spacedRepetitionDatabaseSocket.recv();
  }

  void _openFileWithDefaultProgram(String? pathToFile) async {
    if (pathToFile != null) {
      _openFileWithDefaultProgramSocket.sendString(pathToFile);
    } else {
      log("FilePath Was Empty");
    }
  }

  void _updateItem(FileToLearn fileToLearn) async {
    String jsonString = jsonEncode({
      "Command": "UPDATE",
      "FileToLearn": fileToLearn.toJson(),
    });

    log("Trying to update the following file $jsonString");

    _spacedRepetitionDatabaseSocket.sendString(jsonString);
    _spacedRepetitionDatabaseSocket.recv();
  }

  void _logToFile(String message) async {
    _loggerSocket.sendString("{ \"Message\": \"$message\" }");
  }

  void _handleReview(FileToLearn file, String reviewResult) async {
    log('Review for "${file.name}": $reviewResult');

    String jsonReponse = "";

    switch (reviewResult) {
      case "GOOD":
        String jsonRequest = jsonEncode({
          "RecallEvaluation": "Good",
          "FileToLearn": file.toJson(),
        });
        _spaceRepetitionAlgorithmSocket.sendString(jsonRequest);
        jsonReponse =
            convertZMessageToString(_spaceRepetitionAlgorithmSocket.recv());

        if (file.numberOfTimeSeen > 5) {
          file.easeFactor *= 1.5;
        }

        file.numberOfTimeSeen += 1;
        break;
      case "BAD":
        String jsonRequest = jsonEncode({
          "RecallEvaluation": "Bad",
          "FileToLearn": file.toJson(),
        });
        _spaceRepetitionAlgorithmSocket.sendString(jsonRequest);
        jsonReponse =
            convertZMessageToString(_spaceRepetitionAlgorithmSocket.recv());
        file.numberOfTimeSeen += 1;
        file.easeFactor -= 0.4;
        file.easeFactor = max(1.3, file.easeFactor);
        break;
      case "AGAIN":
        file.nextReviewDate =
            file.nextReviewDate.add(const Duration(minutes: 5));
        break;
      default:
        break;
    }

    try {
      file.nextReviewDate = DateTime.parse(jsonReponse.replaceAll('"', ""));
    } catch (e) {
      log(e.toString());
      file.nextReviewDate = file.nextReviewDate.add(const Duration(minutes: 5));
    }

    log(jsonReponse);

    _filesToReview = filesToLearn
        .where((file) => file.nextReviewDate.isBefore(DateTime.now()))
        .toList(); // Convert to list

    _setFileWithHighestPriority();
    _updateItem(file);
  }

  List<FileToLearn> parseFilesToLearn(String jsonString) {
    // Parse the JSON string into a List<dynamic>
    final List<dynamic> jsonList = json.decode(jsonString);

    // Map each JSON object to a FileToLearn instance
    return jsonList.map((fileJson) => FileToLearn.fromJson(fileJson)).toList();
  }

  void showContentDialog(BuildContext context) async {
    await showDialog<String>(
      context: context,
      builder: (context) => ContentDialog(
        title: const Text('Delete file permanently?'),
        content: const Text(
          'If you delete this file, you won\'t be able to recover it. Do you want to delete it?',
        ),
        actions: [
          Button(
            child: const Text('Delete'),
            onPressed: () {
              Navigator.pop(context, 'User deleted file');
              // Delete file here
            },
          ),
          FilledButton(
            child: const Text('Cancel'),
            onPressed: () => Navigator.pop(context, 'User canceled dialog'),
          ),
        ],
      ),
    );
    setState(() {});
  }

  @override
  void initState() {
    super.initState();

    try {
      _loggerSocket = _context.createSocket(SocketType.dealer);
      _loggerSocket.connect("tcp://localhost:60010");

      _spacedRepetitionDatabaseSocket =
          _context.createdSynSocket(SocketType.req);
      _spacedRepetitionDatabaseSocket.connect("tcp://localhost:60002");

      _openFileWithDefaultProgramSocket =
          _context.createSocket(SocketType.dealer);
      _openFileWithDefaultProgramSocket.connect("tcp://localhost:60001");

      _spaceRepetitionAlgorithmSocket =
          _context.createdSynSocket(SocketType.req);
      _spaceRepetitionAlgorithmSocket.connect("tcp://localhost:60005");

      connectedToBackend = true;
    } catch (e) {
      connectedToBackend = false;
    }

    _logToFile("Flutter GUI Client Started!");
    _retrieveAllItems();

    _filteredNotes = filesToLearn; // Initialize _filteredNotes
    _sortNotesByPriority(); // Sort initially

    _filesToReview = filesToLearn
        .where((file) => file.nextReviewDate.isBefore(DateTime.now()))
        .toList(); // Convert to list
    _setFileWithHighestPriority();

    Timer.periodic(const Duration(minutes: 5), (timer) {
      _gatherFilesToReview();
    });
  }

  @override
  void dispose() {
    _loggerSocket.close();
    _context.stop();
    super.dispose();
  }

  void _filterNotes(String query) async {
    setState(() {
      _filteredNotes = filesToLearn
          .where(
              (note) => note.name.toLowerCase().contains(query.toLowerCase()))
          .toList();
      _sortNotesByPriority(); // Sort after filtering
    });
  }

  void _sortNotesByPriority() async {
    _filteredNotes.sort((a, b) => b.priority.compareTo(a.priority));
  }

  // Function to go back to the list view
  void _goBackToList() async {
    setState(() {
      _selectedFile = null;
      _showEditView = false;
    });
  }

  void _setFileWithHighestPriority() async {
    // Force the widget to rebuild to display the new nextReviewFile
    setState(() {
      if (_filesToReview.isNotEmpty) {
        // Find the highest priority among the due files
        nextReviewFile =
            _filesToReview.reduce((a, b) => a.priority > b.priority ? a : b);
      } else {
        nextReviewFile = null; // Explicitly set to null if no files are due
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return NavigationView(
      key: _viewKey,
      pane: NavigationPane(
        selected: _selectedIndex,
        onChanged: (index) {
          setState(() {
            _selectedIndex = index;
          });
          log('Index changed to $index');
        },
        displayMode: PaneDisplayMode.compact,
        items: [
          PaneItem(
              icon: const Icon(FluentIcons.home_solid),
              body: const Guide(),
              title: const Text("Guide")),
          PaneItem(
            icon: const Icon(FluentIcons.document),
            title: const Text('List of Notes'),
            body: ScaffoldPage(
              header: _selectedFile == null
                  ? Padding(
                      padding: const EdgeInsets.all(8.0),
                      child: TextBox(
                        placeholder: 'Search Notes',
                        controller: _noteSearchBoxController,
                        onChanged: _filterNotes,
                      ),
                    )
                  : null, // Hide the search bar when a file is selected
              content: _selectedFile != null
                  ? Column(
                      children: [
                        CommandBar(
                          primaryItems: [
                            CommandBarButton(
                              icon: const Icon(FluentIcons.back),
                              label: const Text('Back'),
                              onPressed: _goBackToList,
                            ),
                          ],
                        ),
                        Expanded(
                          // Make detail widget take up remaining space
                          child: _showEditView
                              ? FileToLearnEditWidget(
                                  // Show edit widget
                                  fileToLearn: _selectedFile!,
                                  onEdit: (editedFile) {
                                    editItem(editedFile);
                                    _goBackToList();
                                  },
                                )
                              : FileToLearnDetailWidget(
                                  file: _selectedFile!,
                                  onEditNoteButton: () => {
                                    setState(() {
                                      _showEditView = true;
                                    })
                                  },
                                  onClickOpenFileButton: () => {
                                    _openFileWithDefaultProgram(
                                        _selectedFile?.pathToFile)
                                  },
                                  onClickDeleteButton: () =>
                                      {_deleteFileToLearn(_selectedFile!)},
                                ),
                        ),
                      ],
                    )
                  : ListView.builder(
                      itemCount: _filteredNotes.length,
                      itemBuilder: (context, index) {
                        final file = _filteredNotes[index];
                        return Card(
                          child: ListTile(
                            title: Text(file.name),
                            subtitle: Text(file.description),
                            trailing: Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                Text(
                                    'Priority: ${file.priority.toStringAsFixed(0)}'),
                                Text(
                                    'Next Review: ${DateFormat('dd/MM/yyyy').format(file.nextReviewDate)}'),
                              ],
                            ),
                            onPressed: () => _showFileToLearnDetails(file),
                          ),
                        );
                      },
                    ),
            ),
          ),
          PaneItem(
            icon: const Icon(FluentIcons.learning_tools),
            title: const Text('Start Learning'),
            onTap: () {},
            body: nextReviewFile != null
                ? FileToLearnLearningWidget(
                    file: nextReviewFile!,
                    onReview: (result) =>
                        _handleReview(nextReviewFile!, result),
                    onCheckAnswer: () => {
                      _openFileWithDefaultProgram(nextReviewFile?.pathToFile)
                    },
                  )
                : const ScaffoldPage(
                    content: Center(
                      child: Text('No notes scheduled for review yet.'),
                    ),
                  ),
          ),
          PaneItem(
            icon: const Icon(FluentIcons.table),
            title: const Text('See Statistics'),
            body: ScaffoldPage(
              content: Align(
                alignment: Alignment.center,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        'Learning Statistics',
                        style: FluentTheme.of(context)
                            .typography
                            .title, // Title style
                      ),
                      const SizedBox(height: 10),
                      InfoLabel(
                        // Total number of items
                        label: 'Total Items:',
                        child: Text(filesToLearn.length.toString()),
                      ),
                      const SizedBox(height: 8),
                      InfoLabel(
                        // Number of files to learn
                        label: 'Files to Learn:',
                        child: Text(filesToLearn
                            .where((file) =>
                                file.nextReviewDate.isBefore(DateTime.now()))
                            .length
                            .toString()),
                      ),
                      const SizedBox(height: 8),
                      InfoLabel(
                        // Number of items learned
                        label: 'Items Learned:',
                        child: const Text(
                            '0'), // You'll need to track this in your logic
                      ),
                      const SizedBox(height: 16), // More space before the chart
                      //const Text('Learning Progress'), // Chart title
                      //const SizedBox(height: 8),
                    ],
                  ),
                ),
              ),
            ),
          ),
          PaneItem(
            icon: const Icon(FluentIcons.add),
            title: const Text('Create New Stella Note'),
            body: ScaffoldPage(
                content: Padding(
                    // Add padding for better visual spacing
                    padding: const EdgeInsets.all(26.0),
                    child: Form(
                        key: _formKey,
                        // Use a Form for better structure
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            InfoLabel(
                              label: 'Enter the name of the Note',
                              child: Tooltip(
                                  message:
                                      "The name of a note will be used as its title, its not its unqiue identifier,\nmultiple notes with the same name are allowed",
                                  child: TextBox(
                                    controller: _nameController,
                                    placeholder: 'Name',
                                    expands: false,
                                  )),
                            ),
                            const SizedBox(
                                height: 20), // Add spacing between elements

                            InfoLabel(
                              label: 'Enter a Description:',
                              child: Tooltip(
                                  message:
                                      "Provide a description of what you should\ndo when you want to learn the note",
                                  child: TextBox(
                                    controller: _descriptionController,
                                    placeholder: 'Description',
                                    minLines: null,
                                    maxLines: null,
                                    maxLength: null,
                                    expands: true,
                                  )),
                            ),
                            const SizedBox(
                                height: 20), // Add spacing between elements

                            InfoLabel(
                              label: 'Enter the file path of the note:',
                              child: Tooltip(
                                  message:
                                      "When you start learning a note you automatically\nopen the file with the default program",
                                  child: TextBox(
                                    controller: _filePathController,
                                    placeholder: 'File Path',
                                    expands: false,
                                  )),
                            ),
                            const SizedBox(
                                height: 20), // Add spacing between elements
                            InfoLabel(
                              label: 'Priority of Note:',
                              child: Tooltip(
                                  message:
                                      "Notes are ordered based on priority",
                                  child: NumberBox(
                                    value: _priority,
                                    min: 0.0,
                                    max: double.infinity,
                                    onChanged: (value) =>
                                        setState(() => _priority = value ?? 0),
                                    mode: SpinButtonPlacementMode.inline,
                                  )),
                            ),
                            const SizedBox(
                                height: 20), // Add spacing between elements

                            Button(
                              onPressed: () {
                                if (_formKey.currentState!.validate()) {
                                  final newFileToLearn = FileToLearn(
                                    id: uuid.v4(), // Example random ID
                                    name: _nameController.text,
                                    description: _descriptionController.text,
                                    pathToFile: _filePathController.text,
                                    easeFactor:
                                        2.5, // Initial ease factor (adjust as needed)
                                    priority: _priority.toInt(),
                                    nextReviewDate: DateTime
                                        .now(), // Set initial review date
                                    numberOfTimeSeen: 0,
                                  );
                                  _createNewItem(newFileToLearn);
                                  _nameController.text = "";
                                  _descriptionController.text = "";
                                  _filePathController.text = "";
                                  filesToLearn.add(newFileToLearn);
                                  _sortNotesByPriority();
                                }

                                Builder(
                                  builder: (context) {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      // Show the Snackbar
                                      SnackBar(
                                        content: const Text(
                                            "Note created successfully!"),
                                        duration: const Duration(seconds: 2),
                                        backgroundColor: Colors.green,
                                      ),
                                    );

                                    // Return an empty Container widget
                                    return Container();
                                  },
                                );
                              },
                              child: const Text("Create Note"),
                            ),
                          ],
                        )))),
          ),
          /*
          PaneItem(
            icon: const Icon(FluentIcons.guid),
            title: const Text('Quizzes'),
            body: QuizCard( // QuizCard widget inside PaneItem
              question: 'What is the capital of France?',
              answers: ['Berlin', 'Paris', 'Rome', "Moskau"],
              correctAnswerIndex: 1, // Index of 'Paris'
            ),
          ),
          */
          PaneItem(
              icon: const Icon(FluentIcons.people),
              title: const Text("Frequently Asked Questions"),
              body: const FAQ())
        ],
        footerItems: [
          PaneItemSeparator(),
          PaneItem(
            icon: const Icon(FluentIcons.settings),
            title: const Text('Settings'),
            body: Center(
              child: Column(
                // Use a Column to align elements vertically
                mainAxisAlignment:
                    MainAxisAlignment.center, // Center the column content
                crossAxisAlignment: CrossAxisAlignment
                    .center, // Center items horizontally within the column
                children: [
                  Row(
                    // Nest another Row for the second checkbox
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text('Connected To Backend Server?'),
                      const SizedBox(
                          width: 15), // Replace with your desired text
                      Checkbox(checked: connectedToBackend, onChanged: null),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
