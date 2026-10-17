import 'package:material_ui/material_ui.dart';

import '../../l10n/app_strings.dart';
import 'app_badge.dart';

/// The one place a lesson status becomes a label and a colour. The enum names are the
/// backend's state machine (`Pending -> Confirmed -> Completed / Cancelled`), so they are
/// matched as-is; the colour scheme follows the text of docs/prijava.md §4.2.3:
/// Pending orange, Confirmed blue, Completed green, Cancelled red.
class LessonStatusBadge extends StatelessWidget {
  final String status;

  const LessonStatusBadge({super.key, required this.status});

  static String labelFor(String status) => switch (status) {
        'Pending' => AppStrings.lessonPending,
        'Confirmed' => AppStrings.lessonConfirmed,
        'Completed' => AppStrings.lessonCompleted,
        'Cancelled' => AppStrings.lessonCancelled,
        _ => status,
      };

  static AppTone toneFor(String status) => switch (status) {
        'Pending' => AppTone.warning,
        'Confirmed' => AppTone.info,
        'Completed' => AppTone.success,
        'Cancelled' => AppTone.danger,
        _ => AppTone.neutral,
      };

  @override
  Widget build(BuildContext context) => AppStatusBadge(label: labelFor(status), tone: toneFor(status));
}
