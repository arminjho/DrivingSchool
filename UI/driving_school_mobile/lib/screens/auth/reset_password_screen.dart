import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_api.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../widgets/ui/app_action_button.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';

/// 3.2 "Resetovanje lozinke", step 2: the 6-digit code (valid 15 min) plus a new password.
class ResetPasswordScreen extends StatefulWidget {
  const ResetPasswordScreen({super.key, required this.email});

  final String email;

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormBuilderState>();
  bool _isSubmitting = false;
  String? _banner;

  Future<void> _submit() async {
    final form = _formKey.currentState;
    if (form == null || !form.saveAndValidate()) return;

    setState(() {
      _isSubmitting = true;
      _banner = null;
    });
    try {
      await context.read<AuthApi>().resetPassword(
            email: widget.email,
            code: form.value['code'] as String,
            newPassword: form.value['newPassword'] as String,
            confirmNewPassword: form.value['confirmNewPassword'] as String,
          );
      if (!mounted) return;
      showSuccessMessage(context, AppStrings.resetPasswordSuccess);
      Navigator.of(context).pop();
    } on Object catch (error) {
      if (!mounted) return;
      setState(() => _banner = applyApiErrors(_formKey, error));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.resetPasswordTitle)),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: AppCard(
              child: FormBuilder(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(AppStrings.resetPasswordSubtitle, style: context.text.bodyMedium),
                    const SizedBox(height: AppSpacing.lg),
                    if (_banner != null) ...[AppNotice(message: _banner!), const SizedBox(height: AppSpacing.md)],
                    AppTextField(
                      name: 'code',
                      label: AppStrings.fieldResetCode,
                      icon: Icons.pin_outlined,
                      keyboardType: TextInputType.number,
                      validator: AppValidators.required<String>(),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    AppTextField(name: 'newPassword', label: AppStrings.fieldNewPassword, obscureText: true, validator: AppValidators.password()),
                    const SizedBox(height: AppSpacing.md),
                    AppTextField(
                      name: 'confirmNewPassword',
                      label: AppStrings.fieldConfirmPassword,
                      obscureText: true,
                      validator: AppValidators.sameAs(() => _formKey.currentState?.fields['newPassword']?.value as String? ?? ''),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    AppActionButton(label: AppStrings.resetPasswordAction, onPressed: _submit, isLoading: _isSubmitting),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
