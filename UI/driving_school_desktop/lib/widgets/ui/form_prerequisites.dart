import 'package:material_ui/material_ui.dart';

import '../../core/api_exception.dart';
import 'app_messages.dart';

/// upute.md §6: an add-form is not opened while a table it points at is empty - a vehicle needs a
/// category, a city needs a country. Returns true when [count] reports at least one record;
/// otherwise says which table to fill first and returns false, so the form stays closed.
Future<bool> requireRecords(
  BuildContext context, {
  required Future<int> Function() count,
  required String missingMessage,
}) async {
  try {
    if (await count() > 0) return true;
  } on Object catch (error) {
    if (context.mounted) showErrorMessage(context, error);
    return false;
  }

  if (context.mounted) showErrorMessage(context, ApiException(statusCode: 409, message: missingMessage));
  return false;
}
