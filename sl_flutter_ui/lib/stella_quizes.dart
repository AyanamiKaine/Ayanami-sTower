// Stella Quizes Should Provide Various Widgets to, create, show, edit quizes for learning.
import 'dart:developer'; // Import for the log function

import 'package:fluent_ui/fluent_ui.dart';

class Quiz {
  String id;
  String question;
  List<String> answers;
  int correctAnswerIndex;

  Quiz(
      {required this.id,
      required this.question,
      required this.answers,
      required this.correctAnswerIndex});
}

class QuizCard extends StatefulWidget {
  final String question;
  final List<String> answers;
  final int correctAnswerIndex;
  final void Function() onCorrectAnswer; // Required callback

  const QuizCard({
    Key? key,
    required this.question,
    required this.answers,
    required this.correctAnswerIndex,
    required this.onCorrectAnswer,
  }) : super(key: key);

  @override
  _QuizCardState createState() => _QuizCardState();
}

class QuizPanel extends StatefulWidget {
  final List<Quiz> quizes;

  const QuizPanel({
    super.key,
    required this.quizes,
  });

  @override
  _QuizPanelState createState() => _QuizPanelState();
}

class _QuizPanelState extends State<QuizPanel> {
  int currentQuizIndex = 0;

  void nextQuiz() {
    if (currentQuizIndex < widget.quizes.length - 1) {
      setState(() {
        currentQuizIndex++;
        log("Next Quiz function trigger");
      });
    } else {
      // Handle end of quiz (e.g., show a completion message)
    }
  }

  @override
  Widget build(BuildContext context) {
    return QuizCard(
      key: ValueKey(currentQuizIndex),
      question: widget.quizes[currentQuizIndex].question,
      answers: widget.quizes[currentQuizIndex].answers,
      correctAnswerIndex: widget.quizes[currentQuizIndex].correctAnswerIndex,
      onCorrectAnswer: nextQuiz, // Pass the callback
    );
  }
}

class _QuizCardState extends State<QuizCard> {
  int? selectedAnswerIndex;

  @override
  Widget build(BuildContext context) {
    bool isCorrect = selectedAnswerIndex != null &&
        selectedAnswerIndex == widget.correctAnswerIndex;

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
            Wrap(
              // Use Wrap to arrange buttons horizontally
              spacing: 8.0,
              runSpacing: 8.0,
              children: widget.answers.map((answer) {
                final index = widget.answers.indexOf(answer);
                return Button(
                  onPressed: () {
                    setState(() {
                      selectedAnswerIndex = index;
                      if (selectedAnswerIndex != null &&
                          selectedAnswerIndex == widget.correctAnswerIndex) {
                        widget.onCorrectAnswer(); // Call the callback
                        log("Quiz Answer was correct");
                      }
                    });
                  },
                  child: Text(answer),
                  style: ButtonStyle(
                    backgroundColor: ButtonState.all(
                        selectedAnswerIndex == index
                            ? (isCorrect ? Colors.green : Colors.red)
                            : null),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 12),
            if (selectedAnswerIndex != null)
              Center(
                  child: InfoBar(
                title: isCorrect
                    ? const Text('Correct!')
                    : const Text('Incorrect'),
                severity:
                    isCorrect ? InfoBarSeverity.success : InfoBarSeverity.error,
              )),
          ],
        ),
      ),
    );
  }
}
