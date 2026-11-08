import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';
import '../../models/instructor_statistics.dart';
import '../../providers/instructor_api.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';

/// 4.3.1's "Izvještaji" menu item - a dedicated view of the monthly completed-lesson statistics
/// the home screen's stat row only summarizes (`GET /api/instructors/statistics`).
class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  late final InstructorApi _api;
  InstructorStatistics? _statistics;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = InstructorApi(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final statistics = await _api.getStatistics();
      if (mounted) setState(() => _statistics = statistics);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final statistics = _statistics;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.reportsTitle)),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: AppSpacing.page,
          children: [
            Text(AppStrings.reportsSubtitle, style: context.text.bodyMedium?.copyWith(color: context.colors.textMuted)),
            const SizedBox(height: AppSpacing.lg),
            if (statistics == null)
              _error != null
                  ? AppErrorState(message: _error!, onRetry: _load)
                  : const Center(child: Padding(padding: EdgeInsets.all(AppSpacing.lg), child: CircularProgressIndicator()))
            else ...[
              _statCard(context, value: '${statistics.completedThisMonth}', label: AppStrings.statCompletedThisMonth),
              const SizedBox(height: AppSpacing.sm),
              _statCard(context, value: '${statistics.completedTotal}', label: AppStrings.statCompletedTotal),
              const SizedBox(height: AppSpacing.sm),
              _statCard(context, value: statistics.averageRating.toStringAsFixed(1), label: AppStrings.statAverageRating),
            ],
          ],
        ),
      ),
    );
  }

  Widget _statCard(BuildContext context, {required String value, required String label}) {
    return AppCard(
      child: Row(
        children: [
          Text(value, style: context.text.headlineMedium),
          const SizedBox(width: AppSpacing.md),
          Expanded(child: Text(label, style: context.text.titleSmall)),
        ],
      ),
    );
  }
}
