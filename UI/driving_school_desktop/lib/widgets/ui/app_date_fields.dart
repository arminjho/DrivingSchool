import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';

import '../../core/design_tokens.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';

/// Shared body of the date/time controls: a read-only box that opens a picker on tap, with the
/// validation message under it like any other field.
class _PickerBox extends StatelessWidget {
  final FormFieldState<dynamic> field;
  final String label;
  final String hint;
  final IconData icon;
  final String? text;
  final bool enabled;
  final bool clearable;
  final VoidCallback onTap;
  final VoidCallback onClear;

  const _PickerBox({
    required this.field,
    required this.label,
    required this.hint,
    required this.icon,
    required this.text,
    required this.enabled,
    required this.clearable,
    required this.onTap,
    required this.onClear,
  });

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return InkWell(
      borderRadius: AppRadius.all(AppRadius.md),
      onTap: enabled ? onTap : null,
      child: InputDecorator(
        isEmpty: text == null,
        isFocused: false,
        decoration: InputDecoration(
          labelText: label,
          hintText: hint,
          errorText: field.errorText,
          prefixIcon: Icon(icon, size: 18),
          suffixIcon: clearable && text != null && enabled
              ? IconButton(tooltip: AppStrings.clearValue, icon: const Icon(Icons.close_rounded, size: 16), onPressed: onClear)
              : null,
          enabled: enabled,
        ),
        child: Text(text ?? '', style: context.text.bodyMedium?.copyWith(color: enabled ? c.textPrimary : c.textMuted)),
      ),
    );
  }
}

/// A calendar date, shown as DD.MM.GGGG. The value is a [DateTime] at local midnight.
class AppDateField extends StatelessWidget {
  final String name;
  final String label;
  final DateTime? initialValue;
  final DateTime? firstDate;
  final DateTime? lastDate;
  final FormFieldValidator<DateTime>? validator;
  final ValueChanged<DateTime?>? onChanged;
  final bool enabled;
  final bool clearable;

  const AppDateField({
    super.key,
    required this.name,
    required this.label,
    this.initialValue,
    this.firstDate,
    this.lastDate,
    this.validator,
    this.onChanged,
    this.enabled = true,
    this.clearable = false,
  });

  @override
  Widget build(BuildContext context) {
    return FormBuilderField<DateTime>(
      name: name,
      initialValue: initialValue,
      enabled: enabled,
      validator: validator,
      onChanged: onChanged,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      builder: (field) => _PickerBox(
        field: field,
        label: label,
        hint: AppStrings.dateHint,
        icon: AppIcons.date,
        text: field.value == null ? null : AppFormat.date(field.value!),
        enabled: enabled,
        clearable: clearable,
        onClear: () => field.didChange(null),
        onTap: () async {
          final now = DateTime.now();
          final picked = await showDatePicker(
            context: context,
            helpText: AppStrings.pickDate,
            initialDate: _clamp(field.value ?? now),
            firstDate: firstDate ?? DateTime(now.year - 100),
            lastDate: lastDate ?? DateTime(now.year + 20),
          );
          if (picked != null) field.didChange(picked);
        },
      ),
    );
  }

  DateTime _clamp(DateTime value) {
    final first = firstDate;
    final last = lastDate;
    if (first != null && value.isBefore(first)) return first;
    if (last != null && value.isAfter(last)) return last;
    return value;
  }
}

/// A time of day, shown as HH:MM in 24-hour format.
class AppTimeField extends StatelessWidget {
  final String name;
  final String label;
  final TimeOfDay? initialValue;
  final FormFieldValidator<TimeOfDay>? validator;
  final ValueChanged<TimeOfDay?>? onChanged;
  final bool enabled;

  const AppTimeField({
    super.key,
    required this.name,
    required this.label,
    this.initialValue,
    this.validator,
    this.onChanged,
    this.enabled = true,
  });

  @override
  Widget build(BuildContext context) {
    return FormBuilderField<TimeOfDay>(
      name: name,
      initialValue: initialValue,
      enabled: enabled,
      validator: validator,
      onChanged: onChanged,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      builder: (field) => _PickerBox(
        field: field,
        label: label,
        hint: AppStrings.timeHint,
        icon: AppIcons.time,
        text: field.value == null ? null : AppFormat.timeOfDay(field.value!.hour, field.value!.minute),
        enabled: enabled,
        clearable: false,
        onClear: () => field.didChange(null),
        onTap: () async {
          final picked = await showTimePicker(
            context: context,
            helpText: AppStrings.pickTime,
            initialTime: field.value ?? TimeOfDay.now(),
            builder: (context, child) => MediaQuery(
              data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
              child: child!,
            ),
          );
          if (picked != null) field.didChange(picked);
        },
      ),
    );
  }
}

/// A date and a time in one control: the date picker opens first, then the time picker.
class AppDateTimeField extends StatelessWidget {
  final String name;
  final String label;
  final DateTime? initialValue;
  final DateTime? firstDate;
  final DateTime? lastDate;
  final FormFieldValidator<DateTime>? validator;
  final ValueChanged<DateTime?>? onChanged;
  final bool enabled;

  const AppDateTimeField({
    super.key,
    required this.name,
    required this.label,
    this.initialValue,
    this.firstDate,
    this.lastDate,
    this.validator,
    this.onChanged,
    this.enabled = true,
  });

  @override
  Widget build(BuildContext context) {
    return FormBuilderField<DateTime>(
      name: name,
      initialValue: initialValue,
      enabled: enabled,
      validator: validator,
      onChanged: onChanged,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      builder: (field) => _PickerBox(
        field: field,
        label: label,
        hint: '${AppStrings.dateHint} ${AppStrings.timeHint}',
        icon: AppIcons.date,
        text: field.value == null ? null : AppFormat.dateTime(field.value!),
        enabled: enabled,
        clearable: false,
        onClear: () => field.didChange(null),
        onTap: () async {
          final now = DateTime.now();
          final current = field.value ?? now;

          final date = await showDatePicker(
            context: context,
            helpText: AppStrings.pickDate,
            initialDate: current,
            firstDate: firstDate ?? DateTime(now.year - 1),
            lastDate: lastDate ?? DateTime(now.year + 5),
          );
          if (date == null || !context.mounted) return;

          final time = await showTimePicker(
            context: context,
            helpText: AppStrings.pickTime,
            initialTime: TimeOfDay.fromDateTime(current),
            builder: (context, child) => MediaQuery(
              data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
              child: child!,
            ),
          );
          if (time == null) return;

          field.didChange(DateTime(date.year, date.month, date.day, time.hour, time.minute));
        },
      ),
    );
  }
}
