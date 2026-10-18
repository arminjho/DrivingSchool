import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';
import '../../models/test_result.dart';
import '../../widgets/ui/app_card.dart';
import 'recommendations_screen.dart';
import 'test_question_screen.dart';
import 'test_review_screen.dart';

/// 4.2.7 - circular percentage, correct/incorrect counts, weak areas, and the three actions
/// the text and the mockup each describe: "Ponovi test" (mockup), "Pogledaj test" + "Preporuke"
/// (text) - CLAUDE.md §8 says implement all three.
class TestResultScreen extends StatelessWidget {
  const TestResultScreen({super.key, required this.result, required this.testId, required this.testTitle});

  final TestResult result;
  final int testId;
  final String testTitle;

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.testResultTitle)),
      body: ListView(
        padding: AppSpacing.page,
        children: [
          Center(
            child: SizedBox(
              height: 180,
              width: 180,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  SizedBox.expand(
                    child: CircularProgressIndicator(
                      value: result.percent / 100,
                      strokeWidth: 12,
                      backgroundColor: c.surfaceMuted,
                      color: result.passed ? c.success : c.danger,
                    ),
                  ),
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text('${result.percent.round()}%', style: context.text.headlineMedium),
                      Text(AppStrings.resultLabel, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          Center(child: Text(testTitle, style: context.text.titleMedium)),
          const SizedBox(height: AppSpacing.xs),
          Center(child: Text(result.passed ? AppStrings.testPassed : AppStrings.testFailed, style: context.text.bodyMedium?.copyWith(color: result.passed ? c.success : c.danger))),
          const SizedBox(height: AppSpacing.lg),
          Row(
            children: [
              Expanded(child: _countCard(context, icon: Icons.check_circle_outline_rounded, tone: c.success, count: result.correctCount, label: AppStrings.correctAnswers)),
              const SizedBox(width: AppSpacing.sm),
              Expanded(child: _countCard(context, icon: Icons.cancel_outlined, tone: c.danger, count: result.incorrectCount, label: AppStrings.incorrectAnswers)),
            ],
          ),
          if (result.weakAreas.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.lg),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(AppStrings.weakAreasTitle, style: context.text.titleSmall),
                  const SizedBox(height: AppSpacing.sm),
                  for (final area in result.weakAreas)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 2),
                      child: Row(children: [Icon(Icons.warning_amber_rounded, size: 16, color: c.warning), const SizedBox(width: AppSpacing.xs), Expanded(child: Text(area))]),
                    ),
                ],
              ),
            ),
          ],
          const SizedBox(height: AppSpacing.lg),
          if (result.incorrectCount > 0)
            OutlinedButton.icon(
              onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => TestReviewScreen(attemptId: result.attemptId))),
              icon: const Icon(Icons.fact_check_outlined, size: 18),
              label: const Text(AppStrings.viewTestReview),
              style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
            ),
          const SizedBox(height: AppSpacing.sm),
          OutlinedButton.icon(
            onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const RecommendationsScreen())),
            icon: const Icon(Icons.lightbulb_outline_rounded, size: 18),
            label: const Text(AppStrings.viewRecommendations),
            style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
          ),
          const SizedBox(height: AppSpacing.sm),
          FilledButton.icon(
            onPressed: () => Navigator.of(context).pushReplacement(MaterialPageRoute<void>(builder: (_) => TestQuestionScreen(testId: testId, testTitle: testTitle))),
            icon: const Icon(Icons.replay_rounded, size: 18),
            label: const Text(AppStrings.retakeTest),
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
          ),
        ],
      ),
    );
  }

  Widget _countCard(BuildContext context, {required IconData icon, required Color tone, required int count, required String label}) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        children: [
          Icon(icon, color: tone, size: 26),
          const SizedBox(height: AppSpacing.xs),
          Text('$count', style: context.text.headlineSmall),
          Text(label, style: context.text.bodySmall, textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
