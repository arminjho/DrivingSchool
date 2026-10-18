import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';
import '../../models/test_summary.dart';
import '../../providers/test_provider.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import 'test_question_screen.dart';

/// 4.2.5 - "Master" side of the test master-details: every test, each test's last result if any
/// ("Nije riješeno" otherwise).
class TestsScreen extends StatefulWidget {
  const TestsScreen({super.key});

  @override
  State<TestsScreen> createState() => _TestsScreenState();
}

class _TestsScreenState extends State<TestsScreen> {
  late final TestProvider _provider;

  List<TestSummary>? _tests;
  String? _error;

  @override
  void initState() {
    super.initState();
    _provider = TestProvider(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final tests = await _provider.getMy();
      if (mounted) setState(() => _tests = tests);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  Future<void> _startTest(TestSummary test) async {
    final changed = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => TestQuestionScreen(testId: test.id, testTitle: test.title)));
    if (changed == true) _load();
  }

  @override
  Widget build(BuildContext context) {
    final tests = _tests;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.testsTitle)),
      body: tests == null
          ? Padding(
              padding: AppSpacing.page,
              child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : tests.isEmpty
              ? const AppEmptyState(icon: Icons.quiz_outlined, title: AppStrings.emptyTitle, message: AppStrings.emptyMessage)
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.separated(
                    padding: AppSpacing.page,
                    itemCount: tests.length,
                    separatorBuilder: (context, index) => const SizedBox(height: AppSpacing.sm),
                    itemBuilder: (context, index) => _testCard(context, tests[index]),
                  ),
                ),
    );
  }

  Widget _testCard(BuildContext context, TestSummary test) {
    final c = context.colors;

    return AppCard(
      onTap: () => _startTest(test),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(test.title, style: context.text.titleMedium)),
              if (test.hasAttempt)
                AppStatusBadge(
                  label: test.lastPassed == true ? AppStrings.testPassed : AppStrings.testFailed,
                  tone: test.lastPassed == true ? AppTone.success : AppTone.danger,
                ),
            ],
          ),
          const SizedBox(height: AppSpacing.xs),
          if (test.hasAttempt) ...[
            Text(AppStrings.testScoreLine(test.lastCorrectCount ?? 0, test.lastTotal ?? test.questionCount), style: context.text.bodySmall?.copyWith(color: c.textSecondary)),
            const SizedBox(height: AppSpacing.xs),
            ClipRRect(
              borderRadius: AppRadius.all(AppRadius.xs),
              child: LinearProgressIndicator(
                value: (test.lastPercent ?? 0) / 100,
                minHeight: 6,
                backgroundColor: c.surfaceMuted,
                color: test.lastPassed == true ? c.success : c.danger,
              ),
            ),
            const SizedBox(height: 2),
            Align(alignment: Alignment.centerRight, child: Text('${(test.lastPercent ?? 0).round()}%', style: context.text.labelMedium)),
          ] else
            Text(AppStrings.testNotAttempted, style: context.text.bodySmall?.copyWith(color: c.textMuted)),
        ],
      ),
    );
  }
}
