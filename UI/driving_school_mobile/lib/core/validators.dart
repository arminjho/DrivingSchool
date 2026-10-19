import 'package:flutter/widgets.dart';

import '../l10n/app_strings.dart';

/// Client-side mirror of the backend's `FieldValidationRules`: same patterns, same limits, and
/// messages that state the expected format. The backend stays the authority; these only let the
/// user see what is wrong under the field as they go, instead of after a round trip.
///
/// A rule that is stricter than the server's would silently block valid input - keep them in sync.
abstract final class AppValidators {
  static final RegExp _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final RegExp _phone = RegExp(r'^\+387\s?6\d(\s?\d{3}){2}$');

  static const int minPasswordLength = 8;

  static FormFieldValidator<T> required<T>() => (value) {
        if (value == null) return AppStrings.required;
        if (value is String && value.trim().isEmpty) return AppStrings.required;
        return null;
      };

  /// Like [required] for a dropdown: the message asks the user to pick, not to type.
  static FormFieldValidator<T> requiredSelection<T>() =>
      (value) => value == null ? AppStrings.selectRequired : null;

  static FormFieldValidator<String> email({bool required = true}) => (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) return required ? AppStrings.invalidEmail : null;
        return _email.hasMatch(text) ? null : AppStrings.invalidEmail;
      };

  static FormFieldValidator<String> phone({bool required = true}) => (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) return required ? AppStrings.invalidPhone : null;
        return _phone.hasMatch(text) ? null : AppStrings.invalidPhone;
      };

  static FormFieldValidator<String> password() => (value) {
        final text = value ?? '';
        if (text.isEmpty) return AppStrings.required;
        return text.length < minPasswordLength ? AppStrings.passwordTooShort : null;
      };

  static FormFieldValidator<String> sameAs(String Function() other) =>
      (value) => value == other() ? null : AppStrings.passwordsDoNotMatch;

  static FormFieldValidator<String> maxLength(int max, String label) =>
      (value) => (value?.trim().length ?? 0) > max ? AppStrings.maxLength(label, max) : null;

  /// A decimal such as a price. Accepts a comma or a dot as the separator, as a Bosnian user types it.
  static FormFieldValidator<String> decimal({double? min, double? max, bool required = true}) => (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) return required ? AppStrings.required : null;

        final number = double.tryParse(text.replaceAll(',', '.'));
        if (number == null) return AppStrings.invalidNumber;
        return _range(number, min, max);
      };

  static String? _range(num number, num? min, num? max) {
    if (min != null && number < min) return AppStrings.numberAtLeast(min);
    if (max != null && number > max) return AppStrings.numberAtMost(max);
    return null;
  }

  /// Runs the validators in order and reports the first failure, so a field shows one message.
  static FormFieldValidator<T> compose<T>(List<FormFieldValidator<T>> validators) => (value) {
        for (final validator in validators) {
          final error = validator(value);
          if (error != null) return error;
        }
        return null;
      };
}
