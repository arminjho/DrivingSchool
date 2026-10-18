import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_api.dart';
import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/city.dart';
import '../../models/vehicle_category.dart';
import '../../providers/city_provider.dart';
import '../../providers/vehicle_category_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_action_button.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';

/// 3.2 "Kreiranje korisničkog računa" - always a Student account (no role field at all,
/// matching the backend's `RegisterRequest`), signs straight in on success.
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final CityProvider _cityProvider;
  late final VehicleCategoryProvider _categoryProvider;

  bool _isSubmitting = false;
  String? _banner;

  @override
  void initState() {
    super.initState();
    // This screen only exists while signed out, so the shared session has no token - fine,
    // since CityController/VehicleCategoryController allow anonymous reads specifically so the
    // registration form can populate its dropdowns.
    final session = context.read<AuthSession>();
    _cityProvider = CityProvider(session);
    _categoryProvider = VehicleCategoryProvider(session);
  }

  Future<void> _submit() async {
    final form = _formKey.currentState;
    if (form == null || !form.saveAndValidate()) return;

    setState(() {
      _isSubmitting = true;
      _banner = null;
    });

    final api = context.read<AuthApi>();
    try {
      await api.register(
        email: (form.value['email'] as String).trim(),
        password: form.value['password'] as String,
        confirmPassword: form.value['confirmPassword'] as String,
        firstName: form.value['firstName'] as String,
        lastName: form.value['lastName'] as String,
        phoneNumber: form.value['phoneNumber'] as String,
        cityId: form.value['cityId'] as int?,
        vehicleCategoryId: form.value['vehicleCategoryId'] as int,
      );
      if (mounted) Navigator.of(context).popUntil((route) => route.isFirst);
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
      appBar: AppBar(title: const Text(AppStrings.registerTitle)),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: AppCard(
          child: FormBuilder(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(AppStrings.registerSubtitle, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                const SizedBox(height: AppSpacing.lg),
                if (_banner != null) ...[AppNotice(message: _banner!), const SizedBox(height: AppSpacing.md)],
                Row(
                  children: [
                    Expanded(
                      child: AppTextField(name: 'firstName', label: AppStrings.fieldFirstName, validator: AppValidators.required<String>()),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: AppTextField(name: 'lastName', label: AppStrings.fieldLastName, validator: AppValidators.required<String>()),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'email',
                  label: AppStrings.fieldEmail,
                  icon: Icons.mail_outline_rounded,
                  keyboardType: TextInputType.emailAddress,
                  validator: AppValidators.email(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'phoneNumber',
                  label: AppStrings.fieldPhone,
                  icon: Icons.phone_outlined,
                  hint: '+387 6X XXX XXX',
                  validator: AppValidators.phone(),
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<City>(
                  name: 'cityId',
                  label: AppStrings.fieldCity,
                  icon: Icons.location_city_outlined,
                  loadItems: () async => (await _cityProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (c) => c.id,
                  labelOf: (c) => c.name,
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<VehicleCategory>(
                  name: 'vehicleCategoryId',
                  label: AppStrings.fieldCategory,
                  icon: AppIcons.vehicle,
                  loadItems: () async => (await _categoryProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (c) => c.id,
                  labelOf: (c) => '${c.code} — ${c.name}',
                  validator: AppValidators.requiredSelection<int>(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(name: 'password', label: AppStrings.passwordLabel, obscureText: true, validator: AppValidators.password()),
                const SizedBox(height: AppSpacing.md),
                AppTextField(
                  name: 'confirmPassword',
                  label: AppStrings.fieldConfirmPassword,
                  obscureText: true,
                  validator: AppValidators.sameAs(() => _formKey.currentState?.fields['password']?.value as String? ?? ''),
                ),
                const SizedBox(height: AppSpacing.lg),
                AppActionButton(label: AppStrings.registerAction, onPressed: _submit, isLoading: _isSubmitting),
                const SizedBox(height: AppSpacing.sm),
                TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text(AppStrings.haveAccount)),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
