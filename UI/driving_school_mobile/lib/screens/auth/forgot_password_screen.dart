import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_api.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../widgets/ui/app_action_button.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';
import 'reset_password_screen.dart';

/// 3.2 "Resetovanje lozinke", step 1: request a reset code by e-mail. The response never
/// reveals whether the e-mail exists, so success is shown the same way either way.
class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormBuilderState>();
  bool _isSubmitting = false;
  String? _error;

  Future<void> _submit() async {
    final form = _formKey.currentState;
    if (form == null || !form.saveAndValidate()) return;

    final email = (form.value['email'] as String).trim();
    setState(() {
      _isSubmitting = true;
      _error = null;
    });
    try {
      await context.read<AuthApi>().forgotPassword(email);
      if (!mounted) return;
      await Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => ResetPasswordScreen(email: email)));
      if (mounted) Navigator.of(context).pop();
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.forgotPasswordTitle)),
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
                    Text(AppStrings.forgotPasswordSubtitle, style: context.text.bodyMedium),
                    const SizedBox(height: AppSpacing.lg),
                    if (_error != null) ...[AppNotice(message: _error!), const SizedBox(height: AppSpacing.md)],
                    AppTextField(
                      name: 'email',
                      label: AppStrings.fieldEmail,
                      icon: Icons.mail_outline_rounded,
                      keyboardType: TextInputType.emailAddress,
                      validator: AppValidators.email(),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    AppActionButton(label: AppStrings.forgotPasswordAction, onPressed: _submit, isLoading: _isSubmitting),
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
