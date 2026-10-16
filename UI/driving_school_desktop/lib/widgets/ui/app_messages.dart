import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../core/error_text.dart';

/// Transient feedback after an action. Callers pass the specific message for what just happened
/// ("Vozilo E22-A-123 je uspješno dodano.") - never a generic "Uspjeh".
void showSuccessMessage(BuildContext context, String message) => _show(context, message, isError: false);

/// Turns whatever went wrong into an actionable message; a backend rejection is shown verbatim.
void showErrorMessage(BuildContext context, Object error, {String? outcome}) {
  final cause = failureCause(error);
  _show(context, outcome == null ? cause : '$outcome $cause', isError: true);
}

void _show(BuildContext context, String message, {required bool isError}) {
  final c = context.colors;
  final messenger = ScaffoldMessenger.of(context);

  messenger
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Icon(
              isError ? Icons.error_outline_rounded : Icons.check_circle_outline_rounded,
              size: 18,
              color: isError ? c.danger : c.success,
            ),
            const SizedBox(width: AppSpacing.xs),
            Expanded(child: Text(message)),
          ],
        ),
      ),
    );
}
