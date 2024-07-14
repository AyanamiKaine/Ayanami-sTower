// Stella Quizes Should Provide Various Widgets to, create, show, edit quizes for learning.
import 'package:fluent_ui/fluent_ui.dart';

class QuizCard extends StatefulWidget {
  final String question;
  final List<String> answers;
  final int correctAnswerIndex;

  const QuizCard({
    Key? key,
    required this.question,
    required this.answers,
    required this.correctAnswerIndex,
  }) : super(key: key);

  @override
  _QuizCardState createState() => _QuizCardState();
}

class _QuizCardState extends State<QuizCard> {
  int? selectedAnswerIndex;

  @override
  Widget build(BuildContext context) {
    bool isCorrect = selectedAnswerIndex != null && selectedAnswerIndex == widget.correctAnswerIndex;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              widget.question,
              style: FluentTheme.of(context).typography.subtitle,
            ),           
            const SizedBox(height: 12),  
Wrap( // Use Wrap to arrange buttons horizontally
              spacing: 8.0,
              runSpacing: 8.0,
              children: widget.answers.map((answer) {
                final index = widget.answers.indexOf(answer);
                return Button(
                  onPressed: () {
                    setState(() {
                      selectedAnswerIndex = index;
                    });
                  },
                  child: Text(answer),
                  style: ButtonStyle(
                    backgroundColor: ButtonState.all(selectedAnswerIndex == index
                        ? (isCorrect ? Colors.green : Colors.red)
                        : null),
                  ),
                );
              }).toList(),
        ),
                                const SizedBox(height: 12),
            if (selectedAnswerIndex != null)
              Center (child:  InfoBar(
                title: isCorrect ? const Text('Correct!') : const Text('Incorrect'),
                severity: isCorrect ? InfoBarSeverity.success : InfoBarSeverity.error,
              )),],
        ),
      ),
    );
  }
}