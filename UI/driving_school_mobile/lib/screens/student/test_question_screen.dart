import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';
import '../../models/test_attempt_start.dart';
import '../../providers/me_api.dart';
import '../../providers/test_attempt_api.dart';
import '../../providers/test_provider.dart';
import '../../widgets/ui/answer_option_card.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_states.dart';
import 'test_result_screen.dart';

/// 4.2.6 - one question at a time, A-D radio-style cards, "Prethodno"/"Sljedeće", and a
/// confirmation before the final, irreversible submit.
class TestQuestionScreen extends StatefulWidget {
  const TestQuestionScreen({super.key, required this.testId, required this.testTitle});

  final int testId;
  final String testTitle;

  @override
  State<TestQuestionScreen> createState() => _TestQuestionScreenState();
}

class _TestQuestionScreenState extends State<TestQuestionScreen> {
  late final TestProvider _testProvider;
  late final TestAttemptApi _attemptApi;
  late final AuthSession _session;

  TestAttemptStart? _attempt;
  String? _error;
  bool _isSubmitting = false;

  int _index = 0;
  final Map<int, int?> _answers = {};

  @override
  void initState() {
    super.initState();
    _session = context.read<AuthSession>();
    _testProvider = TestProvider(_session);
    _attemptApi = TestAttemptApi(_session);
    _start();
  }

  Future<void> _start() async {
    setState(() => _error = null);
    try {
      final attempt = await _testProvider.startAttempt(widget.testId);
      if (!mounted) return;
      setState(() {
        _attempt = attempt;
        _answers.addEntries(attempt.questions.map((q) => MapEntry(q.id, null)));
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  Future<void> _submit() async {
    final attempt = _attempt;
    if (attempt == null) return;

    final unanswered = _answers.values.where((v) => v == null).length;
    final confirmed = await showAppDialog<bool>(
      context: context,
      builder: (dialogContext) => AppDialog(
        title: AppStrings.confirmSubmitTitle,
        icon: Icons.send_rounded,
        width: 420,
        actions: [
          OutlinedButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text(AppStrings.cancel)),
          FilledButton(onPressed: () => Navigator.of(dialogContext).pop(true), child: const Text(AppStrings.confirmSubmitAction)),
        ],
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(AppStrings.confirmSubmitMessage, style: dialogContext.text.bodyMedium),
            if (unanswered > 0) ...[
              const SizedBox(height: AppSpacing.sm),
              Text(AppStrings.unansweredWarning(unanswered), style: dialogContext.text.bodySmall?.copyWith(color: dialogContext.colors.warning)),
            ],
          ],
        ),
      ),
    );
    if (confirmed != true) return;

    setState(() => _isSubmitting = true);
    try {
      final result = await _attemptApi.submit(attempt.attemptId, _answers);
      if (!mounted) return;
      // Replaces this screen rather than stacking on top of it - once submitted, there is
      // nothing left to come back to on the question screen.
      Navigator.of(context).pushReplacement(
        MaterialPageRoute<void>(builder: (_) => TestResultScreen(result: result, testId: widget.testId, testTitle: widget.testTitle)),
      );
    } on Object catch (error) {
      if (mounted) {
        setState(() => _isSubmitting = false);
        showDialog<void>(
          context: context,
          builder: (dialogContext) => AlertDialog(content: Text(failureCause(error)), actions: [TextButton(onPressed: () => Navigator.pop(dialogContext), child: const Text(AppStrings.close))]),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final attempt = _attempt;

    if (attempt == null) {
      return Scaffold(
        appBar: AppBar(title: Text(widget.testTitle)),
        body: Padding(padding: AppSpacing.page, child: _error != null ? AppErrorState(message: _error!, onRetry: _start) : const Center(child: CircularProgressIndicator())),
      );
    }

    final question = attempt.questions[_index];
    final total = attempt.questions.length;

    return Scaffold(
      appBar: AppBar(title: Text(AppStrings.questionProgress(_index + 1, total))),
      body: Padding(
        padding: AppSpacing.page,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                for (var i = 0; i < total; i++)
                  Expanded(
                    child: Container(
                      height: 4,
                      margin: EdgeInsets.only(right: i == total - 1 ? 0 : 3),
                      decoration: BoxDecoration(
                        color: i <= _index ? context.colors.primary : context.colors.surfaceMuted,
                        borderRadius: AppRadius.all(2),
                      ),
                    ),
                  ),
              ],
            ),
            Expanded(
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const SizedBox(height: AppSpacing.lg),
                    if (question.imagePath != null) _questionImage(question.imagePath!),
                    const SizedBox(height: AppSpacing.lg),
                    Text(question.text, style: context.text.titleMedium),
                    const SizedBox(height: AppSpacing.lg),
                    for (var i = 0; i < question.answerOptions.length; i++)
                      Padding(
                        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                        child: AnswerOptionCard(
                          letter: String.fromCharCode(65 + i),
                          text: question.answerOptions[i].text,
                          state: _answers[question.id] == question.answerOptions[i].id ? AnswerOptionState.selected : AnswerOptionState.plain,
                          onTap: () => setState(() => _answers[question.id] = question.answerOptions[i].id),
                        ),
                      ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _index == 0 ? null : () => setState(() => _index--),
                    icon: const Icon(Icons.chevron_left_rounded),
                    label: const Text(AppStrings.previousQuestion),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: _index == total - 1
                      ? FilledButton.icon(
                          onPressed: _isSubmitting ? null : _submit,
                          icon: _isSubmitting
                              ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                              : const Icon(Icons.send_rounded, size: 18),
                          label: const Text(AppStrings.submitTest),
                        )
                      : FilledButton.icon(
                          onPressed: () => setState(() => _index++),
                          icon: const Icon(Icons.chevron_right_rounded),
                          label: const Text(AppStrings.nextQuestion),
                        ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _questionImage(String imagePath) {
    final c = Theme.of(context).colorScheme;
    return ClipRRect(
      borderRadius: AppRadius.all(AppRadius.lg),
      child: AspectRatio(
        aspectRatio: 16 / 9,
        child: Image.network(
          MeApi.urlFor(imagePath).toString(),
          headers: {if (_session.token != null) 'Authorization': 'Bearer ${_session.token}'},
          fit: BoxFit.cover,
          errorBuilder: (context, error, stackTrace) => Container(
            color: c.surfaceContainerHighest,
            alignment: Alignment.center,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [Icon(AppIcons.vehicle, color: c.onSurfaceVariant), Text(AppStrings.signImagePlaceholder, style: TextStyle(color: c.onSurfaceVariant))],
            ),
          ),
        ),
      ),
    );
  }
}
