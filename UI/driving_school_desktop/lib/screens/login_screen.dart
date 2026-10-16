import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../core/auth_api.dart';
import '../core/design_tokens.dart';
import '../core/form_errors.dart';
import '../core/validators.dart';
import '../l10n/app_strings.dart';
import '../widgets/ui/app_action_button.dart';
import '../widgets/ui/app_card.dart';
import '../widgets/ui/app_states.dart';
import '../widgets/ui/app_text_field.dart';

/// Credentials go in the POST body; a successful sign-in changes the `AuthSession`, which is what
/// moves the app to the shell - this screen never navigates by hand.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  static const String _usernameField = 'email';
  static const String _passwordField = 'password';

  final _formKey = GlobalKey<FormBuilderState>();
  bool _isSubmitting = false;
  String? _banner;

  Future<void> _submit() async {
    final form = _formKey.currentState!;
    if (!form.saveAndValidate()) return;

    setState(() {
      _isSubmitting = true;
      _banner = null;
    });

    final api = context.read<AuthApi>();
    try {
      await api.login(
        email: (form.value[_usernameField] as String).trim(),
        password: form.value[_passwordField] as String,
      );
    } on Object catch (error) {
      if (!mounted) return;
      setState(() => _banner = applyApiErrors(_formKey, error, isSignIn: true));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Scaffold(
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: AppCard(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: FormBuilder(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Icon(AppIcons.vehicle, size: 40, color: c.primary),
                    const SizedBox(height: AppSpacing.sm),
                    Text(AppStrings.appName, textAlign: TextAlign.center, style: context.text.headlineMedium),
                    const SizedBox(height: AppSpacing.xxs),
                    Text(AppStrings.loginSubtitle, textAlign: TextAlign.center, style: context.text.bodySmall),
                    const SizedBox(height: AppSpacing.lg),
                    if (_banner != null) ...[AppNotice(message: _banner!), const SizedBox(height: AppSpacing.md)],
                    AppTextField(
                      name: _usernameField,
                      label: AppStrings.usernameLabel,
                      icon: AppIcons.person,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.username],
                      validator: AppValidators.required<String>(),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    AppTextField(
                      name: _passwordField,
                      label: AppStrings.passwordLabel,
                      icon: Icons.lock_outline_rounded,
                      obscureText: true,
                      textInputAction: TextInputAction.done,
                      autofillHints: const [AutofillHints.password],
                      validator: AppValidators.required<String>(),
                      onSubmitted: (_) => _submit(),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    AppActionButton(label: AppStrings.loginAction, onPressed: _submit, isLoading: _isSubmitting),
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
