import 'package:fluent_ui/fluent_ui.dart';

class Guide extends StatelessWidget {
  const Guide({super.key});

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: const PageHeader(title: Text('Welcome to Stella Learning')),
      content: SingleChildScrollView(
        // Add SingleChildScrollView here

        child: Padding(
          padding: const EdgeInsets.all(23.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Getting Started',
                  style: FluentTheme.of(context).typography.title),
              const SizedBox(height: 16),

              // Step 1
              InfoLabel(
                  label: '1. Add New Notes:',
                  labelStyle: (FluentTheme.of(context).typography.bodyStrong),
                  child: const Text(
                      'Open the sidebar with the button on the left top corner to see the full sidebar. Here click on "Create New Stella Note" to link your study files.')),
              const SizedBox(height: 16),

              // Step 2
              InfoLabel(
                  label: '2. Start Learning:',
                  labelStyle: (FluentTheme.of(context).typography.bodyStrong),
                  child: const Text(
                      'Click "Start Learning" to begin your personalized review session.')),
              const SizedBox(height: 16),

              // Step 3
              InfoLabel(
                  label: '3. Review & Rate:',
                  labelStyle: (FluentTheme.of(context).typography.bodyStrong),
                  child: const Text(
                      'After reviewing a note, rate your recall ("Good," "Bad," or "Again") to optimize learning.')),
              const SizedBox(height: 16),

              // Step 4
              InfoLabel(
                  label: '4. Track Progress:',
                  labelStyle: (FluentTheme.of(context).typography.bodyStrong),
                  child: const Text(
                      'View your learning statistics on the "See Statistics" tab.')),
              const SizedBox(height: 16),

              InfoLabel(
                  label: 'Edit Items:',
                  labelStyle: (FluentTheme.of(context).typography.bodyStrong),
                  child: const Text(
                      'Open the list of notes and click on the note you want to edit. You open a note view where you can click on the edit button to see a window where you can edit it.')),
              const SizedBox(height: 16),

              InfoLabel(
                  label: 'Delete Items:',
                  labelStyle: (FluentTheme.of(context).typography.bodyStrong),
                  child: const Text(
                      'Open the list of notes and click on the note you want to delete. You open a note view where you can click on the delete button')),
              const SizedBox(height: 16),

              // Additional Tips
              Text('Additional Tips:',
                  style: FluentTheme.of(context).typography.subtitle),
              const SizedBox(height: 8),
              const Text(
                  '- Prioritize notes based on their importance using the "Priority" field.'),
              const Text(
                  '- Regularly review your items for effective long-term learning.'),
              const Text(
                  '- Change priority of items based on neediness (Is the information really important?)'),
              const SizedBox(height: 16),

              // FAQ and Support
              Text(
                  'Need More Help? Check out our FAQ (Found on the sidebar) for answers to common questions.',
                  style: FluentTheme.of(context).typography.bodyStrong),
              const SizedBox(height: 8),
            ],
          ),
        ),
      ),
    );
  }
}
