import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';

import '../../l10n/app_strings.dart';

/// A text control for `FormBuilder`. The validation message is shown under the field (never
/// inside it or as a dialog), and an optional leading [icon] makes dates, vehicles and prices
/// scannable at a glance.
class AppTextField extends StatefulWidget {
  final String name;
  final String label;
  final String? initialValue;
  final IconData? icon;
  final String? hint;
  final FormFieldValidator<String>? validator;
  final bool obscureText;
  final bool enabled;
  final int maxLines;
  final int? maxLength;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final ValueChanged<String?>? onChanged;
  final ValueChanged<String?>? onSubmitted;
  final Iterable<String>? autofillHints;

  const AppTextField({
    super.key,
    required this.name,
    required this.label,
    this.initialValue,
    this.icon,
    this.hint,
    this.validator,
    this.obscureText = false,
    this.enabled = true,
    this.maxLines = 1,
    this.maxLength,
    this.keyboardType,
    this.textInputAction,
    this.onChanged,
    this.onSubmitted,
    this.autofillHints,
  });

  @override
  State<AppTextField> createState() => _AppTextFieldState();
}

class _AppTextFieldState extends State<AppTextField> {
  late bool _hidden = widget.obscureText;

  @override
  Widget build(BuildContext context) {
    return FormBuilderTextField(
      name: widget.name,
      initialValue: widget.initialValue,
      enabled: widget.enabled,
      obscureText: _hidden,
      maxLines: widget.obscureText ? 1 : widget.maxLines,
      maxLength: widget.maxLength,
      keyboardType: widget.keyboardType,
      textInputAction: widget.textInputAction,
      autofillHints: widget.autofillHints,
      validator: widget.validator,
      onChanged: widget.onChanged,
      onSubmitted: widget.onSubmitted,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      decoration: InputDecoration(
        labelText: widget.label,
        hintText: widget.hint,
        counterText: '',
        prefixIcon: widget.icon == null ? null : Icon(widget.icon, size: 18),
        suffixIcon: widget.obscureText
            ? IconButton(
                tooltip: _hidden ? AppStrings.showPassword : AppStrings.hidePassword,
                icon: Icon(_hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined, size: 18),
                onPressed: () => setState(() => _hidden = !_hidden),
              )
            : null,
      ),
    );
  }
}
