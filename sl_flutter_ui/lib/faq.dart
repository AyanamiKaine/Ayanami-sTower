import 'package:fluent_ui/fluent_ui.dart'
    show
        Button,
        ContentDialog,
        Expander,
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
// Import the intl package

class FAQ extends StatefulWidget {
  const FAQ({super.key});

  @override
  _FAQState createState() => _FAQState();
}

class _FAQState extends State<FAQ> {
  final List<Map<String, String>> faqItems = [
    {
      'question': 'What is spaced repetition?',
      'answer':
          'Spaced repetition is a learning technique that schedules reviews of material at increasing intervals over time, optimizing retention.',
    },
    {
      'question': 'How does the priority queue work?',
      'answer':
          'The priority queue prioritizes items for learning based on a value given by you.\n\nItems are learned in order of priority first so even though an item can be much longer due for learning you will always learn the item with the highest priority regardless of due dates.',
    },
    {
      'question': 'Can I add my own files for learning?',
      'answer':
          'Absolutely! You can link files to your learning items and access them directly within the app.',
    },
    {
      'question':
          'Can I create new notes or flash cards like in anki directly in Stella Notes?',
      'answer':
          'No, Stella Notes is not a note taking or flash card creation app. Anki and programs like Obsidian are much better suited for such tasks.',
    },
    {
      'question': 'When I delete an item will it get deleted from my disk?',
      'answer':
          'No, Stella Notes should never delete a file directly it will only delete the entry in the list',
    },
    {
      'question': 'Where can I find my save file?',
      'answer':
          'Windows: "~/AppData/Roaming/Stella Knowledge Manager/main_save_data.json" \n\nLinux: "~/.config/Stella Knowledge Manager/main_save_data.json"',
    },
    // Add more FAQ items here
  ];

  List<bool> _expandedItems = []; // To track which items are expanded

  @override
  void initState() {
    super.initState();
    _expandedItems = List<bool>.filled(faqItems.length, false);
  }

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: const PageHeader(title: Text('Frequently Asked Questions')),
      content: ListView.separated(
        separatorBuilder: (context, index) => const Divider(),
        itemCount: faqItems.length,
        itemBuilder: (context, index) {
          return Expander(
            header: Text(faqItems[index]['question']!),
            //expanded: _expandedItems[index],
            onStateChanged: (value) => setState(() {
              _expandedItems[index] = value; // Toggle expansion state
            }),
            content: Text(faqItems[index]['answer']!),
          );
        },
      ),
    );
  }
}
