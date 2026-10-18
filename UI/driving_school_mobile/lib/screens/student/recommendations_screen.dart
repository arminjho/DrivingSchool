import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';
import '../../models/recommendations_result.dart';
import '../../providers/recommendation_api.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import 'study_lesson_detail_screen.dart';

/// 4.2.8 - the AI assistant's explanation card, then the recommended study lessons it points at.
class RecommendationsScreen extends StatefulWidget {
  const RecommendationsScreen({super.key});

  @override
  State<RecommendationsScreen> createState() => _RecommendationsScreenState();
}

class _RecommendationsScreenState extends State<RecommendationsScreen> {
  late final RecommendationApi _api;
  RecommendationsResult? _result;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = RecommendationApi(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final result = await _api.get();
      if (mounted) setState(() => _result = result);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final result = _result;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.recommendationsTitle)),
      body: result == null
          ? Padding(
              padding: AppSpacing.page,
              child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : result.recommendations.isEmpty
              ? AppEmptyState(
                  icon: Icons.lightbulb_outline_rounded,
                  title: AppStrings.recommendationsEmpty,
                  message: result.coldStartMessage ?? AppStrings.recommendationsEmptyHint,
                )
              : ListView(
                  padding: AppSpacing.page,
                  children: [
                    _explanationCard(context, result),
                    const SizedBox(height: AppSpacing.lg),
                    Text(AppStrings.recommendedLessonsTitle, style: context.text.titleSmall),
                    const SizedBox(height: AppSpacing.sm),
                    for (final rec in result.recommendations)
                      Padding(
                        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                        child: AppCard(
                          onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => StudyLessonDetailScreen(studyLessonId: rec.studyLessonId))),
                          child: Row(
                            children: [
                              Icon(Icons.menu_book_outlined, color: rec.isCriticalArea ? context.colors.danger : context.colors.primary),
                              const SizedBox(width: AppSpacing.sm),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(rec.title, style: context.text.titleSmall),
                                    Text('${rec.chapterLabel} · ${AppStrings.readingMinutesLabel(rec.readingMinutes)}', style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                                  ],
                                ),
                              ),
                              Icon(Icons.chevron_right_rounded, color: context.colors.textMuted),
                            ],
                          ),
                        ),
                      ),
                  ],
                ),
    );
  }

  Widget _explanationCard(BuildContext context, RecommendationsResult result) {
    final c = context.colors;
    final explanation = result.recommendations.isNotEmpty ? result.recommendations.first.explanation : (result.coldStartMessage ?? '');

    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(color: c.warningSoft, borderRadius: AppRadius.all(AppRadius.lg), border: Border.all(color: c.warning.withValues(alpha: 0.35))),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('💡', style: TextStyle(fontSize: 22)),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(AppStrings.recommenderCardTitle, style: context.text.titleSmall),
                const SizedBox(height: AppSpacing.xxs),
                Text(explanation, style: context.text.bodyMedium),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
