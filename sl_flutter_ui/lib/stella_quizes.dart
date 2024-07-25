// Stella Quizes Should Provide Various Widgets to, create, show, edit quizes for learning.
import 'dart:developer'; // Import for the log function

import 'package:fluent_ui/fluent_ui.dart';

class Quiz {
  String id;
  String question;
  List<String> answers;
  int correctAnswerIndex;
  double easeFactor;
  int priority;
  DateTime nextReviewDate;
  int numberOfTimeSeen;

  factory Quiz.fromJson(Map<String, dynamic> json) {
    return Quiz(
      id: json['Id'],
      question: json['Question'],
      answers: json['Answers'],
      easeFactor: json['EaseFactor'].toDouble(),
      priority: json['Priority'],
      nextReviewDate: DateTime.parse(json['NextReviewDate']),
      numberOfTimeSeen: json['NumberOfTimeSeen'],
      correctAnswerIndex: json['CorrectAnswerIndex'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'Id': id,
      'Question': question,
      'Answers': answers,
      'EaseFactor': easeFactor,
      'Priority': priority,
      'NextReviewDate': nextReviewDate.toIso8601String(),
      'NumberOfTimeSeen': numberOfTimeSeen,
      'CorrectAnswerIndex': correctAnswerIndex
    };
  }

  Quiz(
      {required this.id,
      required this.question,
      required this.answers,
      required this.correctAnswerIndex,
      required this.priority,
      required this.easeFactor,
      required this.nextReviewDate,
      required this.numberOfTimeSeen});
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
  bool quizFinished = false;

  void nextQuiz() {
    if (currentQuizIndex < widget.quizes.length - 1) {
      setState(() {
        currentQuizIndex++;
        log("Next Quiz function trigger");
      });
    } else {
      setState(() {
        quizFinished = true; // Mark quiz as finished
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (widget.quizes.isEmpty) {
      // Handle empty quiz list
      return const Center(
        child: Text(
          "No questions available",
          style: TextStyle(fontSize: 20),
        ),
      );
    } else if (quizFinished) {
      // Handle finished quiz
      return const Center(
        child: Text(
          "No more questions",
          style: TextStyle(fontSize: 20),
        ),
      );
    } else {
      // Render the QuizCard
      return QuizCard(
        key: ValueKey(currentQuizIndex),
        question: widget.quizes[currentQuizIndex].question,
        answers: widget.quizes[currentQuizIndex].answers,
        correctAnswerIndex: widget.quizes[currentQuizIndex].correctAnswerIndex,
        onCorrectAnswer: nextQuiz,
      );
    }
  }
}

class _QuizCardState extends State<QuizCard> {
  int? selectedAnswerIndex;
  bool showCorrectAnswer = false; // To control highlighting
  bool showNextButton = false; // Added to control the button's visibility

  @override
  Widget build(BuildContext context) {
    bool isCorrect = selectedAnswerIndex != null &&
        selectedAnswerIndex == widget.correctAnswerIndex;

    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
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
              bool isCorrect = index == widget.correctAnswerIndex;
              bool isSelected = index == selectedAnswerIndex;

              return Button(
                onPressed: () {
                  setState(() {
                    selectedAnswerIndex = index;
                    showCorrectAnswer = !isCorrect; // Show if incorrect

                    if (isCorrect) {
                      widget.onCorrectAnswer();
                      log("Quiz Answer was correct");
                    }
                  });
                },
                child: Text(answer),
                style: ButtonStyle(
                  backgroundColor: ButtonState.all(
                    isSelected
                        ? (isCorrect ? Colors.green : Colors.red)
                        : (showCorrectAnswer && isCorrect
                            ? Colors.green
                            : null),
                  ),
                ),
              );
            }).toList(),
          ),
          const SizedBox(height: 12),
          if (selectedAnswerIndex != null)
            Center(
                child: InfoBar(
              title: showCorrectAnswer
                  ? const Text('The correct answer is highlighted')
                  : (selectedAnswerIndex == widget.correctAnswerIndex
                      ? const Text('Correct!')
                      : const Text('Incorrect')),
              severity: showCorrectAnswer
                  ? InfoBarSeverity.warning
                  : (selectedAnswerIndex == widget.correctAnswerIndex
                      ? InfoBarSeverity.success
                      : InfoBarSeverity.error),
            )),
          const SizedBox(height: 12), // Add spacing above the button
          Center(
            child: FilledButton(
              child: const Text('Next Question'),
              onPressed: () {
                setState(() {
                  selectedAnswerIndex = null;
                  showCorrectAnswer = false;
                  showNextButton = false;
                  widget.onCorrectAnswer(); // Move to next question
                });
              },
            ),
          ),
        ],
      ),
    );
  }
}
