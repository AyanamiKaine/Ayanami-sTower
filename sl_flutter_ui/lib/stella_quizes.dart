// Stella Quizes Should Provide Various Widgets to, create, show, edit quizes for learning.
import 'dart:developer'; // Import for the log function

import 'package:fluent_ui/fluent_ui.dart';
import 'package:intl/intl.dart';

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
          "No Questions Available",
          style: TextStyle(fontSize: 20),
        ),
      );
    } else if (quizFinished) {
      // Handle finished quiz
      return const Center(
        child: Text(
          "No More Quizes for Today!",
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

class QuizDetailWidget extends StatelessWidget {
  final Quiz quiz;
  final Function() onEditQuizButton; // Callback to edit the quiz
  final Function() onDeleteQuizButton; // Callback to delete the quiz

  const QuizDetailWidget({
    super.key,
    required this.quiz,
    required this.onEditQuizButton,
    required this.onDeleteQuizButton,
  });

  void showDeleteConfirmationDialog(BuildContext context) async {
    await showDialog<String>(
      context: context,
      builder: (context) => ContentDialog(
        title: const Text('Delete Quiz?'),
        content: const Text('Are you sure you want to delete this quiz?'),
        actions: [
          Button(
            child: const Text('Delete'),
            onPressed: () {
              Navigator.pop(context); // Close dialog
              onDeleteQuizButton(); // Call the delete callback
            },
          ),
          FilledButton(
            child: const Text('Cancel'),
            onPressed: () => Navigator.pop(context),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: PageHeader(
        title: Text(quiz.question),
      ),
      content: Padding(
        padding: const EdgeInsets.all(16.0),
        child: SingleChildScrollView(
          // Wrap in SingleChildScrollView for longer content
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'Answers:',
                style: FluentTheme.of(context).typography.subtitle,
              ),
              // List of answers
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: quiz.answers.asMap().entries.map((entry) {
                  int index = entry.key;
                  String answer = entry.value;
                  bool isCorrect = index == quiz.correctAnswerIndex;
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Text('${index + 1}. $answer'),
                        if (isCorrect) ...[
                          const SizedBox(width: 8), // Add some spacing
                          const Icon(FluentIcons.check_mark,
                              color: Colors.black),
                        ],
                      ],
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: 16),
              // Additional Details
              Text('Priority: ${quiz.priority}',
                  style: FluentTheme.of(context).typography.subtitle),
              Text('Ease Factor: ${quiz.easeFactor.toStringAsFixed(2)}',
                  style: FluentTheme.of(context).typography.subtitle),
              Text(
                  'Next Review: ${DateFormat('dd/MM/yyyy').format(quiz.nextReviewDate)}',
                  style: FluentTheme.of(context).typography.subtitle),
              Text('Times Seen: ${quiz.numberOfTimeSeen}',
                  style: FluentTheme.of(context).typography.subtitle),
              const SizedBox(height: 16),
              // Buttons
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  Button(
                    child: const Text('Edit'),
                    onPressed: onEditQuizButton,
                  ),
                  const SizedBox(width: 10),
                  FilledButton(
                    child: const Text('Delete'),
                    onPressed: () => showDeleteConfirmationDialog(context),
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
                onPressed:
                    selectedAnswerIndex == null // Disable if already selected
                        ? () {
                            setState(() {
                              selectedAnswerIndex = index;
                              showCorrectAnswer = !isCorrect;
                              showNextButton =
                                  true; // Show next button after selection

                              if (isCorrect) {
                                widget.onCorrectAnswer();
                              }
                            });
                          }
                        : null,
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
            const SizedBox(height: 12), // Add spacing above the button
          if (showNextButton) // Only show if an answer is selected
            Center(
              child: FilledButton(
                child: const Text('Next Question'),
                onPressed: () {
                  setState(() {
                    selectedAnswerIndex = null;
                    showCorrectAnswer = false;
                    showNextButton = false;
                    widget.onCorrectAnswer();
                  });
                },
              ),
            )
        ],
      ),
    );
  }
}
