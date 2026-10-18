import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';
import '../../models/test_attempt_review.dart';
import '../../providers/test_attempt_api.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';

/// "Pogledaj test" (docs/prijava.md §4.2.7 text) - the incorrectly-answered questions only,
/// each with the student's own (wrong/missing) answer and the correct one.
class TestReviewScreen extends StatefulWidget {
  const TestReviewScreen({super.key, required this.attemptId});

  final int attemptId;

  @override
  State<TestReviewScreen> createState() => _TestReviewScreenState();
}

class _TestReviewScreenState extends State<TestReviewScreen> {
  late final TestAttemptApi _api;
  TestAttemptReview? _review;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = TestAttemptApi(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final review = await _api.getReview(widget.attemptId);
      if (mounted) setState(() => _review = review);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final review = _review;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.testReviewTitle)),
      body: review == null
          ? Padding(
              padding: AppSpacing.page,
              child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : ListView.separated(
              padding: AppSpacing.page,
              itemCount: review.incorrectAnswers.length,
              separatorBuilder: (context, index) => const SizedBox(height: AppSpacing.sm),
              itemBuilder: (context, index) {
                final item = review.incorrectAnswers[index];
                final c = context.colors;

                return AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.questionText, style: context.text.titleSmall),
                      const SizedBox(height: AppSpacing.sm),
                      Row(
                        children: [
                          Icon(Icons.cancel_outlined, size: 16, color: c.danger),
                          const SizedBox(width: AppSpacing.xs),
                          Text('${AppStrings.yourAnswer}: ', style: context.text.bodySmall?.copyWith(color: c.textSecondary)),
                          Expanded(child: Text(item.yourAnswerText ?? AppStrings.noAnswerGiven, style: context.text.bodyMedium?.copyWith(color: c.danger))),
                        ],
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      Row(
                        children: [
                          Icon(Icons.check_circle_outline_rounded, size: 16, color: c.success),
                          const SizedBox(width: AppSpacing.xs),
                          Text('${AppStrings.correctAnswer}: ', style: context.text.bodySmall?.copyWith(color: c.textSecondary)),
                          Expanded(child: Text(item.correctAnswerText, style: context.text.bodyMedium?.copyWith(color: c.success))),
                        ],
                      ),
                    ],
                  ),
                );
              },
            ),
    );
  }
}
