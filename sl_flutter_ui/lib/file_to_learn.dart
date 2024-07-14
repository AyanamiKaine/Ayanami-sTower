import 'package:fluent_ui/fluent_ui.dart'
    show
        Button,
        ContentDialog,
        FilledButton,
        FluentTheme,
        InfoLabel,
        NumberBox,
        PageHeader,
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

// Import for the log function

class FileToLearn {
  String id; // Use String for GUIDs in Dart
  String name;
  String pathToFile;
  String description;
  double easeFactor;
  int priority;
  DateTime nextReviewDate;
  int numberOfTimeSeen;

  FileToLearn({
    required this.id,
    required this.name,
    required this.pathToFile,
    required this.description,
    required this.easeFactor,
    required this.priority,
    required this.nextReviewDate,
    required this.numberOfTimeSeen,
  });

  factory FileToLearn.fromJson(Map<String, dynamic> json) {
    return FileToLearn(
      id: json['Id'],
      name: json['Name'],
      pathToFile: json['PathToFile'],
      description: json['Description'],
      easeFactor: json['EaseFactor'].toDouble(),
      priority: json['Priority'],
      nextReviewDate: DateTime.parse(json['NextReviewDate']),
      numberOfTimeSeen: json['NumberOfTimeSeen'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'Id': id,
      'Name': name,
      'PathToFile': pathToFile,
      'Description': description,
      'EaseFactor': easeFactor,
      'Priority': priority,
      'NextReviewDate': nextReviewDate.toIso8601String(),
      'NumberOfTimeSeen': numberOfTimeSeen,
    };
  }
}

class FileToLearnLearningWidget extends StatefulWidget {
  final FileToLearn file;
  final Function(String) onReview;
  final Function() onCheckAnswer;
  const FileToLearnLearningWidget({
    super.key,
    required this.file,
    required this.onReview,
    required this.onCheckAnswer,
  });

  @override
  FileToLearnLearningWidgetState createState() =>
      FileToLearnLearningWidgetState();
}

class FileToLearnLearningWidgetState extends State<FileToLearnLearningWidget> {
  bool showReviewButtons = false;

  void onCheckAnswer() {
    setState(() {
      showReviewButtons = true;
    });
    widget.onCheckAnswer(); // Call the callback from the parent widget
  }

  void onReview(String reviewType) {
    setState(() {
      showReviewButtons = false; // Hide buttons after review
    });
    widget.onReview(reviewType); // Call the callback from the parent widget
  }

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: null,
      content: Align(
        alignment: Alignment.center,
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(widget.file.name,
                  style: FluentTheme.of(context).typography.title),
              const SizedBox(height: 25),
              Text('Description:',
                  style: FluentTheme.of(context).typography.subtitle),
              Text(widget.file.description),
              const SizedBox(height: 20),
              Button(
                onPressed: onCheckAnswer,
                child: const Text(
                    "Check Answer"), // Use the method from this state class
              ),
              // Conditionally show review buttons
              if (showReviewButtons) ...[
                const SizedBox(height: 20),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  children: [
                    FilledButton(
                      child: const Text('GOOD'),
                      onPressed: () => onReview('GOOD'),
                    ),
                    FilledButton(
                      child: const Text('BAD'),
                      onPressed: () => onReview('BAD'),
                    ),
                    FilledButton(
                      child: const Text('AGAIN'),
                      onPressed: () => onReview('AGAIN'),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class FileToLearnDetailWidget extends StatelessWidget {
  final FileToLearn file;
  final Function() onEditNoteButton;
  final Function() onClickOpenFileButton;
  final Function() onClickDeleteButton;
  const FileToLearnDetailWidget(
      {super.key,
      required this.file,
      required this.onEditNoteButton,
      required this.onClickOpenFileButton,
      required this.onClickDeleteButton});

  void showContentDialog(BuildContext context) async {
    await showDialog<String>(
      context: context,
      builder: (context) => ContentDialog(
        title: const Text('Delete Note'),
        content: const Text(
          'This will not delete the file from disk',
        ),
        actions: [
          Button(
            child: const Text('Delete'),
            onPressed: () {
              Navigator.pop(context, 'User deleted file');
              onClickDeleteButton();
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
  }

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: PageHeader(
        title: Text(file.name),
      ),
      content: Padding(
        padding: const EdgeInsets.all(16.0),
        child: SingleChildScrollView(
          // Wrap with SingleChildScrollView
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min, // Allow Column to shrink
            children: [
              // Description Section
              Text(
                'Description:',
                style: FluentTheme.of(context).typography.subtitle,
              ),
              Text(file.description),
              const SizedBox(height: 16),

              // Path Section
              Text('Path:', style: FluentTheme.of(context).typography.subtitle),
              Text(file.pathToFile),
              const SizedBox(height: 16),

              // Additional Details
              Text(
                'Priority: ${file.priority.toStringAsFixed(0)}',
                style: FluentTheme.of(context).typography.subtitle,
              ),
              Text(
                'Ease Factor: ${file.easeFactor.toStringAsFixed(2)}',
                style: FluentTheme.of(context).typography.subtitle,
              ),
              Text(
                'Next Review: ${DateFormat('dd/MM/yyyy').format(file.nextReviewDate)}',
                style: FluentTheme.of(context).typography.subtitle,
              ),
              Text(
                'Times Seen: ${file.numberOfTimeSeen}',
                style: FluentTheme.of(context).typography.subtitle,
              ),

              const SizedBox(height: 16),
              Row(
                // Wrap buttons in a Row for horizontal layout
                mainAxisAlignment:
                    MainAxisAlignment.end, // Align buttons to the right
                children: [
                  Button(
                    child: const Text('Edit'),
                    onPressed: () {
                      onEditNoteButton();
                    },
                  ),
                  const SizedBox(width: 10), // Add spacing between buttons
                  Button(
                    child: const Text('Open File'),
                    onPressed: () {
                      onClickOpenFileButton();
                    },
                  ),
                  const SizedBox(width: 10), // Add spacing between buttons
                  FilledButton(
                    child: const Text('Delete'),
                    onPressed: () {
                      showContentDialog(context);
                    },
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class FileToLearnEditWidget extends StatefulWidget {
  final FileToLearn fileToLearn;
  final Function(FileToLearn) onEdit;

  const FileToLearnEditWidget(
      {super.key, required this.fileToLearn, required this.onEdit});

  @override
  _FileToLearnEditWidgetState createState() => _FileToLearnEditWidgetState();
}

class _FileToLearnEditWidgetState extends State<FileToLearnEditWidget> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _filePathController = TextEditingController();
  double _priority = 0;

  @override
  void initState() {
    super.initState();
    _nameController.text = widget.fileToLearn.name;
    _descriptionController.text = widget.fileToLearn.description;
    _filePathController.text = widget.fileToLearn.pathToFile;
    _priority = widget.fileToLearn.priority.toDouble();
  }

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      content: Padding(
        padding: const EdgeInsets.all(26.0),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              InfoLabel(
                label: 'Name:',
                child: TextBox(
                  controller: _nameController,
                  placeholder: 'Name',
                  expands: false,
                ),
              ),
              const SizedBox(height: 20),
              InfoLabel(
                label: 'Description:',
                child: TextBox(
                  controller: _descriptionController,
                  placeholder: 'Description',
                  expands: true,
                  maxLines: null,
                ),
              ),
              const SizedBox(height: 20),
              InfoLabel(
                label: 'File Path:',
                child: TextBox(
                  controller: _filePathController,
                  placeholder: 'File Path',
                  expands: false,
                ),
              ),
              const SizedBox(height: 20),
              InfoLabel(
                label: 'Priority:',
                child: NumberBox(
// Pre-fill with existing value
                  min: 0.0,
                  max: double.infinity,
                  value: _priority,
                  onChanged: (value) {
                    setState(() => _priority = value ?? 0);
                  },
                  mode: SpinButtonPlacementMode.inline,
                ),
              ),
              const SizedBox(height: 20),
              // ... (Similar structure for filePath and priority)
              Button(
                onPressed: () {
                  if (_formKey.currentState!.validate()) {
                    FileToLearn editedFile = FileToLearn(
                      id: widget.fileToLearn.id, // Keep the same ID
                      name: _nameController.text,
                      description: _descriptionController.text,
                      pathToFile: _filePathController.text,
                      easeFactor: widget.fileToLearn.easeFactor,
                      priority: _priority.toInt(),
                      nextReviewDate: widget.fileToLearn.nextReviewDate,
                      numberOfTimeSeen: widget.fileToLearn.numberOfTimeSeen,
                      // ... other fields (keep the same values or update as needed)
                    );
                    widget.onEdit(editedFile);
                  }
                },
                child: const Text("Save Changes"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
