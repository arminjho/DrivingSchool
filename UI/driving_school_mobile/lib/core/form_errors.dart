import 'package:flutter/widgets.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';

import 'api_exception.dart';
import 'error_text.dart';

/// Shows the backend's validation messages under the matching form controls.
///
/// Field names in the form must match the API's camel-cased property names. Returns the text
/// for a banner above the form when something could not be placed under a field (a business
/// rule, a server failure, a field this form doesn't have); null when everything was placed.
String? applyApiErrors(GlobalKey<FormBuilderState> formKey, Object error, {bool isSignIn = false}) {
  final form = formKey.currentState;
  if (error is! ApiException || error.fieldErrors.isEmpty || form == null) {
    return failureCause(error, isSignIn: isSignIn);
  }

  final unplaced = <String>[];
  var focused = false;
  error.fieldErrors.forEach((field, messages) {
    final control = form.fields[field];
    if (control == null) {
      unplaced.addAll(messages);
      return;
    }
    control.invalidate(messages.first, shouldFocus: !focused);
    focused = true;
  });

  return unplaced.isEmpty ? null : unplaced.join(' ');
}
