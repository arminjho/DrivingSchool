import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/form_errors.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/city.dart';
import '../../models/user.dart';
import '../../providers/city_provider.dart';
import '../../providers/me_api.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_dialog.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';

/// "Moj profil" from the avatar menu - view/edit the signed-in admin's own contact details.
Future<void> showMyProfileDialog(BuildContext context) => showAppDialog<void>(
      context: context,
      builder: (dialogContext) => const _MyProfileDialog(),
    );

class _MyProfileDialog extends StatefulWidget {
  const _MyProfileDialog();

  @override
  State<_MyProfileDialog> createState() => _MyProfileDialogState();
}

class _MyProfileDialogState extends State<_MyProfileDialog> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final MeApi _meApi;
  late final CityProvider _cityProvider;

  bool _isSubmitting = false;
  User? _me;
  String? _loadError;
  String? _banner;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _meApi = MeApi(session);
    _cityProvider = CityProvider(session);
    _load();
  }

  Future<void> _load() async {
    try {
      final me = await _meApi.getMe();
      if (mounted) setState(() => _me = me);
    } on Object catch (error) {
      if (mounted) setState(() => _loadError = failureCause(error));
    }
  }

  Future<void> _submit() async {
    final me = _me;
    final form = _formKey.currentState;
    if (me == null || form == null || !form.saveAndValidate()) return;

    setState(() {
      _isSubmitting = true;
      _banner = null;
    });
    final address = (form.value['address'] as String?)?.trim();
    try {
      await _meApi.updateMe({
        'firstName': form.value['firstName'],
        'lastName': form.value['lastName'],
        'email': form.value['email'],
        'phoneNumber': form.value['phoneNumber'],
        'address': address?.isEmpty ?? true ? null : address,
        'cityId': form.value['cityId'],
      });
      if (mounted) Navigator.of(context).pop();
      if (mounted) showSuccessMessage(context, AppStrings.userUpdated(me.fullName));
    } on Object catch (error) {
      setState(() {
        _banner = applyApiErrors(_formKey, error);
        _isSubmitting = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final me = _me;

    return AppDialog(
      title: AppStrings.myProfile,
      icon: AppIcons.person,
      width: 480,
      actions: me == null
          ? const []
          : [
              OutlinedButton(onPressed: _isSubmitting ? null : () => Navigator.of(context).pop(), child: const Text(AppStrings.cancel)),
              FilledButton(
                onPressed: _isSubmitting ? null : _submit,
                child: _isSubmitting
                    ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text(AppStrings.save),
              ),
            ],
      child: me == null
          ? SizedBox(
              height: 160,
              child: Center(
                child: _loadError != null ? AppErrorState(message: _loadError!, onRetry: _load) : const CircularProgressIndicator(),
              ),
            )
          : FormBuilder(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (_banner != null) ...[AppNotice(message: _banner!), const SizedBox(height: AppSpacing.md)],
                  Row(
                    children: [
                      Expanded(
                        child: AppTextField(name: 'firstName', label: AppStrings.fieldFirstName, initialValue: me.firstName, validator: AppValidators.required<String>()),
                      ),
                      const SizedBox(width: AppSpacing.md),
                      Expanded(
                        child: AppTextField(name: 'lastName', label: AppStrings.fieldLastName, initialValue: me.lastName, validator: AppValidators.required<String>()),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.md),
                  AppTextField(name: 'email', label: AppStrings.fieldEmail, initialValue: me.email, validator: AppValidators.email()),
                  const SizedBox(height: AppSpacing.md),
                  AppTextField(name: 'phoneNumber', label: AppStrings.fieldPhone, initialValue: me.phoneNumber, validator: AppValidators.phone()),
                  const SizedBox(height: AppSpacing.md),
                  AppTextField(name: 'address', label: AppStrings.fieldAddress, initialValue: me.address),
                  const SizedBox(height: AppSpacing.md),
                  ApiDropdownField<City>(
                    name: 'cityId',
                    label: AppStrings.fieldCity,
                    initialValue: me.cityId,
                    loadItems: () async => (await _cityProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                    idOf: (c) => c.id,
                    labelOf: (c) => c.name,
                  ),
                ],
              ),
            ),
    );
  }
}

/// "Promijeni lozinku" from the avatar menu.
Future<void> showChangePasswordDialog(BuildContext context) async {
  final meApi = MeApi(context.read<AuthSession>());
  final formKey = GlobalKey<FormBuilderState>();
  var isSubmitting = false;
  String? banner;

  await showAppDialog<void>(
    context: context,
    builder: (dialogContext) => StatefulBuilder(
      builder: (dialogContext, setDialogState) => AppDialog(
        title: AppStrings.changePassword,
        icon: Icons.lock_outline_rounded,
        width: 440,
        actions: [
          OutlinedButton(onPressed: isSubmitting ? null : () => Navigator.of(dialogContext).pop(), child: const Text(AppStrings.cancel)),
          FilledButton(
            onPressed: isSubmitting
                ? null
                : () async {
                    final form = formKey.currentState;
                    if (form == null || !form.saveAndValidate()) return;
                    setDialogState(() {
                      isSubmitting = true;
                      banner = null;
                    });
                    try {
                      await meApi.changePassword(
                        oldPassword: form.value['oldPassword'] as String,
                        newPassword: form.value['newPassword'] as String,
                        confirmNewPassword: form.value['confirmNewPassword'] as String,
                      );
                      if (dialogContext.mounted) Navigator.of(dialogContext).pop();
                      if (context.mounted) showSuccessMessage(context, AppStrings.changePassword);
                    } on Object catch (error) {
                      setDialogState(() {
                        banner = applyApiErrors(formKey, error);
                        isSubmitting = false;
                      });
                    }
                  },
            child: isSubmitting
                ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : const Text(AppStrings.save),
          ),
        ],
        child: FormBuilder(
          key: formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (banner != null) ...[AppNotice(message: banner!), const SizedBox(height: AppSpacing.md)],
              AppTextField(name: 'oldPassword', label: AppStrings.fieldPassword, obscureText: true, validator: AppValidators.required<String>()),
              const SizedBox(height: AppSpacing.md),
              AppTextField(name: 'newPassword', label: AppStrings.fieldNewPassword, obscureText: true, validator: AppValidators.password()),
              const SizedBox(height: AppSpacing.md),
              AppTextField(
                name: 'confirmNewPassword',
                label: AppStrings.fieldConfirmPassword,
                obscureText: true,
                validator: AppValidators.sameAs(() => formKey.currentState?.fields['newPassword']?.value as String? ?? ''),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
