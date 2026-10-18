import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../l10n/app_strings.dart';
import '../../models/study_lesson_detail.dart';
import '../../providers/study_lesson_provider.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';

/// The lekcija's own sections, read top to bottom - the "Pregled lekcija" reading screen.
class StudyLessonDetailScreen extends StatefulWidget {
  const StudyLessonDetailScreen({super.key, required this.studyLessonId});

  final int studyLessonId;

  @override
  State<StudyLessonDetailScreen> createState() => _StudyLessonDetailScreenState();
}

class _StudyLessonDetailScreenState extends State<StudyLessonDetailScreen> {
  late final StudyLessonProvider _provider;
  StudyLessonDetail? _detail;
  String? _error;

  @override
  void initState() {
    super.initState();
    _provider = StudyLessonProvider(context.read<AuthSession>());
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final detail = await _provider.getDetail(widget.studyLessonId);
      if (mounted) setState(() => _detail = detail);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final detail = _detail;

    return Scaffold(
      appBar: AppBar(title: Text(detail?.title ?? AppStrings.studyTitle)),
      body: detail == null
          ? Padding(
              padding: AppSpacing.page,
              child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : ListView(
              padding: AppSpacing.page,
              children: [
                Text('${detail.chapterLabel} · ${AppStrings.readingMinutesLabel(detail.readingMinutes)}', style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                const SizedBox(height: AppSpacing.lg),
                for (final section in detail.sections)
                  Padding(
                    padding: const EdgeInsets.only(bottom: AppSpacing.md),
                    child: AppCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(section.title, style: context.text.titleMedium),
                          const SizedBox(height: AppSpacing.sm),
                          Text(section.content, style: context.text.bodyMedium),
                        ],
                      ),
                    ),
                  ),
              ],
            ),
    );
  }
}
