import 'dart:developer';
import 'dart:convert';

import 'package:dartzmq/dartzmq.dart';
import 'package:fluent_ui/fluent_ui.dart';
import 'package:flutter/material.dart' as material;
import 'package:fluent_ui/fluent_ui.dart' as fluent;

void main() {
  fluent.runApp(const MyApp());
}

class ProjectCreationRequest {
  final String projectName;
  final String projectPath;
  final String cVersion;
  final String mainTemplate;
  final bool createExecutable;
  final bool createLibrary;
  final bool addVCPKG;
  final bool autoAddFiles;
  final bool addCJson;
  final bool addCzmq;
  final bool addSokol;
  final bool addNuklear;
  final bool addArenaAllocator;
  final bool addCString;
  final bool addFlecs;
  final bool addLuaJIT;
  final bool addNNG;

  ProjectCreationRequest({
    required this.projectName,
    required this.projectPath,
    required this.cVersion,
    required this.mainTemplate,
    required this.createExecutable,
    required this.createLibrary,
    required this.addVCPKG,
    required this.autoAddFiles,
    required this.addCJson,
    required this.addCzmq,
    required this.addSokol,
    required this.addNuklear,
    required this.addArenaAllocator,
    required this.addCString,
    required this.addFlecs,
    required this.addLuaJIT,
    required this.addNNG,
  });

  // Method to convert the object into a JSON-compatible Map
  Map<String, dynamic> toJson() {
    return {
      'ProjectName': projectName,
      'ProjectPath': projectPath,
      'CVersion': cVersion,
      'MainTemplate': mainTemplate,
      'CreateExecutable': createExecutable,
      'CreateLibrary': createLibrary,
      'AddVCPKG': addVCPKG,
      'AutoAddFiles': autoAddFiles,
      'AddCJson': addCJson,
      'AddCzmq': addCzmq,
      'AddSokol': addSokol,
      'AddNuklear': addNuklear,
      'AddArenaAllocator': addArenaAllocator,
      'AddCString': addCString,
      'AddFlecs': addFlecs,
      'AddLuaJIT': addLuaJIT,
      'AddNNG': addNNG,
    };
  }
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return FluentApp(
      title: 'C Project Maker',
      theme: FluentThemeData(
        brightness: Brightness.dark, // Simulate dark theme
      ),
      home: const ProjectMakerPage(),
    );
  }
}

class MyHomePage extends fluent.StatefulWidget {
  const MyHomePage({super.key, required this.title});

  // This widget is the home page of your application. It is stateful, meaning
  // that it has a State object (defined below) that contains fields that affect
  // how it looks.

  // This class is the configuration for the state. It holds the values (in this
  // case the title) provided by the parent (in this case the App widget) and
  // used by the build method of the State. Fields in a Widget subclass are
  // always marked "final".

  final String title;

  @override
  fluent.State<MyHomePage> createState() => _MyHomePageState();
}

class _MyHomePageState extends fluent.State<MyHomePage> {
  int _counter = 0;

  void _incrementCounter() {
    setState(() {
      // This call to setState tells the Flutter framework that something has
      // changed in this State, which causes it to rerun the build method below
      // so that the display can reflect the updated values. If we changed
      // _counter without calling setState(), then the build method would not be
      // called again, and so nothing would appear to happen.
      _counter++;
    });
  }

  void _createProject(ProjectCreationRequest projectCreationRequest) async {}

  @override
  fluent.Widget build(fluent.BuildContext context) {
    // This method is rerun every time setState is called, for instance as done
    // by the _incrementCounter method above.
    //
    // The Flutter framework has been optimized to make rerunning build methods
    // fast, so that you can just rebuild anything that needs updating rather
    // than having to individually change instances of widgets.
    return material.Scaffold(
      appBar: material.AppBar(
        // TRY THIS: Try changing the color here to a specific color (to
        // Colors.amber, perhaps?) and trigger a hot reload to see the AppBar
        // change color while the other colors stay the same.
        backgroundColor: material.Theme.of(context).colorScheme.inversePrimary,
        // Here we take the value from the MyHomePage object that was created by
        // the App.build method, and use it to set our appbar title.
        title: fluent.Text(widget.title),
      ),
      body: fluent.Center(
        // Center is a layout widget. It takes a single child and positions it
        // in the middle of the parent.
        child: fluent.Column(
          // Column is also a layout widget. It takes a list of children and
          // arranges them vertically. By default, it sizes itself to fit its
          // children horizontally, and tries to be as tall as its parent.
          //
          // Column has various properties to control how it sizes itself and
          // how it positions its children. Here we use mainAxisAlignment to
          // center the children vertically; the main axis here is the vertical
          // axis because Columns are vertical (the cross axis would be
          // horizontal).
          //
          // TRY THIS: Invoke "debug painting" (choose the "Toggle Debug Paint"
          // action in the IDE, or press "p" in the console), to see the
          // wireframe for each widget.
          mainAxisAlignment: fluent.MainAxisAlignment.center,
          children: <fluent.Widget>[
            const fluent.Text(
              'You have pushed the button this many times:',
            ),
            fluent.Text(
              '$_counter',
              style: material.Theme.of(context).textTheme.headlineMedium,
            ),
          ],
        ),
      ),
      floatingActionButton: fluent.IconButton(
        onPressed: _incrementCounter,
        icon: const material.Icon(material.Icons.add),
      ), // This trailing comma makes auto-formatting nicer for build methods.
    );
  }
}

class ProjectMakerPage extends StatefulWidget {
  const ProjectMakerPage({Key? key}) : super(key: key);

  @override
  _ProjectMakerPageState createState() => _ProjectMakerPageState();
}

class _ProjectMakerPageState extends State<ProjectMakerPage> {
  final ZContext _context = ZContext();
  late final ZSocket _cProjectMakerSocket;

  final _formKey = GlobalKey<FormState>();
  final _projectNameController = TextEditingController();
  final _projectPathController = TextEditingController();

  String _selectedCVersion = '23';
  String _selectedMainTemplate = 'Hello World';

  bool _createExecutable = true;
  bool _createLibrary = false;
  bool _addVCPKG = true;
  bool _autoAddFiles = true;

  bool _addCJson = false; // Added missing variable
  bool _addCzmq = false; // Added missing variable
  bool _addSokol = false;
  bool _addNuklear = false;
  bool _addArenaAllocator = false;
  bool _addCString = false;
  bool _addFlecs = false;
  bool _addLuaJIT = false;
  bool _addNNG = false;
  // ... similar booleans for other checkboxes

  @override
  void initState() {
    super.initState();
    _cProjectMakerSocket = _context.createSocket(SocketType.dealer);
    _cProjectMakerSocket.connect("tcp://localhost:50001");
  }

  @override
  void dispose() {
    _cProjectMakerSocket.close();
    _context.stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
        header: const PageHeader(title: Text("C Project Maker")),
        content: Center(
          child: Padding(
            padding: const EdgeInsets.all(26.0),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        // Make input fields take up available space
                        child: InfoLabel(
                          label: 'Project Name:',
                          child: TextBox(
                            controller: _projectNameController,
                          ),
                        ),
                      ),
                      const SizedBox(width: 20), // Add spacing between fields
                      Expanded(
                        child: Tooltip(
                          message:
                              'If the folder does not exist it will be created',
                          child: InfoLabel(
                            label: 'Project Path:',
                            child: TextBox(
                              controller: _projectPathController,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 20), // Add spacing

                  Row(
                    children: [
                      Expanded(
                        child: InfoLabel(
                          label: 'C Version:',
                          child: ComboBox<String>(
                            value: _selectedCVersion,
                            onChanged: (value) =>
                                setState(() => _selectedCVersion = value!),
                            items:
                                ['90', '99', '11', '17', '23'].map((version) {
                              return ComboBoxItem<String>(
                                value: version,
                                child: Text(version),
                              );
                            }).toList(),
                          ),
                        ),
                      ),
                      const SizedBox(width: 20),
                      Expanded(
                        child: InfoLabel(
                          label: 'main.c Template:',
                          child: ComboBox<String>(
                            value: _selectedMainTemplate,
                            onChanged: (value) {
                              setState(() => _selectedMainTemplate = value!);
                              if (value == "LuaJIT") {
                                setState(() {
                                  _addLuaJIT = true;
                                });
                              }
                              if (value == "Sokol+Nuklear") {
                                _addSokol = true;
                                _addNuklear = true;
                              }
                            },
                            items: [
                              'Empty',
                              'Hello World',
                              'Sokol+Nuklear',
                              'LuaJIT'
                            ].map((template) {
                              return ComboBoxItem<String>(
                                value: template,
                                child: Text(template),
                              );
                            }).toList(),
                          ),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 20), // Add spacing

// Checkboxes (group related ones)
                  // Checkboxes (arranged in a GridView)
                  GridView.count(
                    crossAxisCount: 3, // Two columns in the grid
                    childAspectRatio:
                        4.0, // Adjust aspect ratio for wider items
                    shrinkWrap:
                        true, // Allow GridView to shrink to content size
                    children: [
                      Checkbox(
                        checked: _createExecutable,
                        onChanged: (value) => setState(() {
                          _createExecutable = value!;
                          _createLibrary = false;
                        }),
                        content: const Text('Create Executable?'),
                      ),
                      Checkbox(
                        checked: _createLibrary,
                        onChanged: (value) => setState(() {
                          _createLibrary = value!;
                          _createExecutable = false;
                        }),
                        content: const Text('Create Library?'),
                      ),
                      Tooltip(
                        message:
                            "Adds VCPKG to CMAKE.\nYou must have VCPKG installed and set as a environment variable",
                        child: Checkbox(
                          checked: _addVCPKG,
                          onChanged: (value) =>
                              setState(() => _addVCPKG = value!),
                          content: const Text('Add VCPKG'),
                        ),
                      ),
                      Tooltip(
                        // Wrap Checkbox and Tooltip together
                        message:
                            'All files found in src/ will be added to your CMake file',
                        child: Checkbox(
                          checked: _autoAddFiles,
                          onChanged: (value) =>
                              setState(() => _autoAddFiles = value!),
                          content: const Text('Automatically add .h/.c files?'),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),

                  const Tooltip(
                    message:
                        'Some libraries are included via VCPKG. If the version is too old, we will use Git instead.\nNot every third-party library inclusion uses VCPKG.\nIt should be gurannteed that you can include any combination of third-party libraries provided here',
                    child: Text(
                      'Adding Third Party Libraries',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ), // Empty widget to trigger tooltip
                  ),

                  const SizedBox(height: 10),

                  // More Checkboxes for Libraries (in a GridView)
                  GridView.count(
                    crossAxisCount: 3, // Two columns in the grid
                    childAspectRatio:
                        4.0, // Adjust aspect ratio for wider items
                    shrinkWrap:
                        true, // Allow GridView to shrink to content size
                    children: [
                      Checkbox(
                        checked: _addCJson,
                        onChanged: (value) => setState(() {
                          _addCJson = value!;
                          if (value == true) {
                            _addVCPKG = true;
                          }
                        }),
                        content: const Text('c-json (VCPKG)'),
                      ),
                      Tooltip(
                        message: "Message Passing Library",
                        child: Checkbox(
                          checked: _addCzmq,
                          onChanged: (value) => setState(() {
                            _addCzmq = value!;
                            if (value == true) {
                              _addVCPKG = true;
                            }
                          }),
                          content: const Text('czmq (VCPKG)'),
                        ),
                      ),
                      Tooltip(
                        message:
                            "Simple STB-style cross-platform libraries for C and C++, written in C.",
                        child: Checkbox(
                          checked: _addSokol,
                          onChanged: (value) =>
                              setState(() => _addSokol = value!),
                          content: const Text('sokol (Git)'),
                        ),
                      ),
                      Tooltip(
                        message:
                            "Intermediate GUI Library\nWorks great with sokol",
                        child: Checkbox(
                          checked: _addNuklear,
                          onChanged: (value) =>
                              setState(() => _addNuklear = value!),
                          content: const Text('nuklear (Git)'),
                        ),
                      ),
                      Checkbox(
                        checked: _addArenaAllocator,
                        onChanged: (value) =>
                            setState(() => _addArenaAllocator = value!),
                        content: const Text('Arena Allocator (Git)'),
                      ),
                      Checkbox(
                        checked: _addCString,
                        onChanged: (value) =>
                            setState(() => _addCString = value!),
                        content: const Text('C-String (Git)'),
                      ),
                      Checkbox(
                        checked: _addFlecs,
                        onChanged: (value) => setState(() {
                          _addFlecs = value!;
                          if (value == true) {
                            _addVCPKG = true;
                          }
                        }),
                        content: const Text('Flecs (ECS) (VCPKG)'),
                      ),
                      Checkbox(
                        checked: _addLuaJIT,
                        onChanged: (value) =>
                            setState(() => _addLuaJIT = value!),
                        content: const Text('LuaJIT (Git)'),
                      ),
                      Checkbox(
                        checked: _addNNG,
                        onChanged: (value) => setState(() => _addNNG = value!),
                        content: const Text('NGG (VCPKG)'),
                      ),
                    ],
                  ),
                  // Add a button below (aligned to the center)
                  const SizedBox(height: 20), // Add spacing above the button
                  Center(
                    child: FilledButton(
                      child: const Text('Create Project'),
                      onPressed: () {
                        var request = ProjectCreationRequest(
                            projectName: _projectNameController.text,
                            projectPath: _projectPathController.text,
                            cVersion: _selectedCVersion,
                            mainTemplate: _selectedMainTemplate,
                            createExecutable: _createExecutable,
                            createLibrary: _createLibrary,
                            addVCPKG: _addVCPKG,
                            autoAddFiles: _autoAddFiles,
                            addCJson: _addCJson,
                            addCzmq: _addCzmq,
                            addSokol: _addSokol,
                            addNuklear: _addNuklear,
                            addArenaAllocator: _addArenaAllocator,
                            addCString: _addCString,
                            addFlecs: _addFlecs,
                            addLuaJIT: _addLuaJIT,
                            addNNG: _addNNG);

                        log(jsonEncode(request));
                        _cProjectMakerSocket.sendString(jsonEncode(request));
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
        ));
  }
}
