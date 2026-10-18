import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../core/api_exception.dart';
import '../../core/auth_api.dart';
import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/form_errors.dart';
import '../../core/image_validation.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/city.dart';
import '../../models/user.dart';
import '../../models/vehicle_category.dart';
import '../../providers/city_provider.dart';
import '../../providers/me_api.dart';
import '../../providers/vehicle_category_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_action_button.dart';
import '../../widgets/ui/app_badge.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';

/// 4.2.11 - shared by Student and Instructor; the Instructor variant is "the same, without
/// category" (CLAUDE.md §8), so [showCategory] is the only difference between the two.
class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key, this.showCategory = true});

  final bool showCategory;

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _profileFormKey = GlobalKey<FormBuilderState>();
  final _passwordFormKey = GlobalKey<FormBuilderState>();
  final _imagePicker = ImagePicker();

  late final MeApi _meApi;
  late final CityProvider _cityProvider;
  late final VehicleCategoryProvider _categoryProvider;

  User? _me;
  List<VehicleCategory> _categories = const [];
  String? _loadError;

  bool _isSavingProfile = false;
  bool _isUploadingImage = false;
  bool _showPasswordSection = false;
  bool _isSavingPassword = false;
  String? _profileBanner;
  String? _passwordBanner;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _meApi = MeApi(session);
    _cityProvider = CityProvider(session);
    _categoryProvider = VehicleCategoryProvider(session);
    _load();
  }

  Future<void> _load() async {
    setState(() => _loadError = null);
    try {
      final results = await (_meApi.getMe(), _categoryProvider.getPaged(const PagedQuery(pageSize: 100))).wait;
      if (!mounted) return;
      setState(() {
        _me = results.$1;
        _categories = results.$2.resultList;
      });
    } on Object catch (error) {
      if (mounted) setState(() => _loadError = failureCause(error));
    }
  }

  Future<void> _saveProfile() async {
    final form = _profileFormKey.currentState;
    final me = _me;
    if (form == null || me == null || !form.saveAndValidate()) return;

    setState(() {
      _isSavingProfile = true;
      _profileBanner = null;
    });
    final address = (form.value['address'] as String?)?.trim();
    try {
      final updated = await _meApi.updateMe({
        'firstName': form.value['firstName'],
        'lastName': form.value['lastName'],
        'email': form.value['email'],
        'phoneNumber': form.value['phoneNumber'],
        'address': address?.isEmpty ?? true ? null : address,
        'cityId': form.value['cityId'],
      });
      if (mounted) {
        setState(() => _me = updated);
        showSuccessMessage(context, AppStrings.profileUpdated);
      }
    } on Object catch (error) {
      setState(() => _profileBanner = applyApiErrors(_profileFormKey, error));
    } finally {
      if (mounted) setState(() => _isSavingProfile = false);
    }
  }

  Future<void> _pickAndUploadImage() async {
    final picked = await _imagePicker.pickImage(source: ImageSource.gallery, imageQuality: 85);
    if (picked == null || !mounted) return;

    final bytes = await picked.readAsBytes();
    if (!mounted) return;
    final validationError = ImageValidator.validate(picked.name, bytes);
    if (validationError != null) {
      showErrorMessage(context, ApiException(statusCode: 400, message: validationError));
      return;
    }

    setState(() => _isUploadingImage = true);
    try {
      final updated = await _meApi.uploadProfileImage(PickedImage(name: picked.name, bytes: bytes));
      if (mounted) setState(() => _me = updated);
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isUploadingImage = false);
    }
  }

  Future<void> _savePassword() async {
    final form = _passwordFormKey.currentState;
    if (form == null || !form.saveAndValidate()) return;

    setState(() {
      _isSavingPassword = true;
      _passwordBanner = null;
    });
    try {
      await _meApi.changePassword(
        oldPassword: form.value['oldPassword'] as String,
        newPassword: form.value['newPassword'] as String,
        confirmNewPassword: form.value['confirmNewPassword'] as String,
      );
      if (mounted) {
        showSuccessMessage(context, AppStrings.passwordChanged);
        setState(() => _showPasswordSection = false);
        form.reset();
      }
    } on Object catch (error) {
      setState(() => _passwordBanner = applyApiErrors(_passwordFormKey, error));
    } finally {
      if (mounted) setState(() => _isSavingPassword = false);
    }
  }

  Future<void> _logout() => context.read<AuthApi>().logout();

  @override
  Widget build(BuildContext context) {
    final me = _me;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.myProfile)),
      body: me == null
          ? Padding(
              padding: AppSpacing.page,
              child: _loadError != null ? AppErrorState(message: _loadError!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : ListView(
              padding: AppSpacing.page,
              children: [
                Center(child: _avatar(context, me)),
                const SizedBox(height: AppSpacing.lg),
                AppCard(
                  child: FormBuilder(
                    key: _profileFormKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (_profileBanner != null) ...[AppNotice(message: _profileBanner!), const SizedBox(height: AppSpacing.md)],
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
                        ApiDropdownField<City>(
                          name: 'cityId',
                          label: AppStrings.fieldCity,
                          initialValue: me.cityId,
                          loadItems: () async => (await _cityProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                          idOf: (c) => c.id,
                          labelOf: (c) => c.name,
                        ),
                        if (widget.showCategory && me.vehicleCategoryId != null) ...[
                          const SizedBox(height: AppSpacing.md),
                          InputDecorator(
                            decoration: const InputDecoration(labelText: AppStrings.fieldCategory, enabled: false),
                            child: Text(_categoryLabel(me.vehicleCategoryId!)),
                          ),
                        ],
                        const SizedBox(height: AppSpacing.lg),
                        AppActionButton(label: AppStrings.editProfileAction, onPressed: _saveProfile, isLoading: _isSavingProfile),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                AppCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      AppActionButton(
                        label: AppStrings.changePasswordAction,
                        kind: AppButtonKind.outlined,
                        onPressed: () => setState(() => _showPasswordSection = !_showPasswordSection),
                      ),
                      if (_showPasswordSection) ...[
                        const SizedBox(height: AppSpacing.md),
                        FormBuilder(
                          key: _passwordFormKey,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              if (_passwordBanner != null) ...[AppNotice(message: _passwordBanner!), const SizedBox(height: AppSpacing.md)],
                              AppTextField(name: 'oldPassword', label: AppStrings.fieldOldPassword, obscureText: true, validator: AppValidators.required<String>()),
                              const SizedBox(height: AppSpacing.md),
                              AppTextField(name: 'newPassword', label: AppStrings.fieldNewPassword, obscureText: true, validator: AppValidators.password()),
                              const SizedBox(height: AppSpacing.md),
                              AppTextField(
                                name: 'confirmNewPassword',
                                label: AppStrings.fieldConfirmPassword,
                                obscureText: true,
                                validator: AppValidators.sameAs(() => _passwordFormKey.currentState?.fields['newPassword']?.value as String? ?? ''),
                              ),
                              const SizedBox(height: AppSpacing.md),
                              AppActionButton(label: AppStrings.saveNewPassword, onPressed: _savePassword, isLoading: _isSavingPassword),
                            ],
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),
                AppActionButton(label: AppStrings.logoutAction, kind: AppButtonKind.text, destructive: true, onPressed: _logout),
              ],
            ),
    );
  }

  Widget _avatar(BuildContext context, User me) {
    final imagePath = me.profileImagePath;
    final session = context.read<AuthSession>();

    return Stack(
      children: [
        Container(
          height: 88,
          width: 88,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(shape: BoxShape.circle, color: context.colors.surfaceMuted, border: Border.all(color: context.colors.border)),
          child: imagePath == null
              ? Center(child: Text(AppAvatar.initials(me.fullName), style: context.text.headlineMedium))
              : Image.network(
                  MeApi.urlFor(imagePath).toString(),
                  headers: {if (session.token != null) 'Authorization': 'Bearer ${session.token}'},
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Center(child: Text(AppAvatar.initials(me.fullName), style: context.text.headlineMedium)),
                ),
        ),
        Positioned(
          right: 0,
          bottom: 0,
          child: InkWell(
            onTap: _isUploadingImage ? null : _pickAndUploadImage,
            borderRadius: AppRadius.all(16),
            child: Container(
              height: 32,
              width: 32,
              decoration: BoxDecoration(shape: BoxShape.circle, color: context.colors.primary, border: Border.all(color: context.colors.surface, width: 2)),
              child: _isUploadingImage
                  ? Padding(padding: const EdgeInsets.all(7), child: CircularProgressIndicator(strokeWidth: 2, color: context.colors.onPrimary))
                  : Icon(Icons.camera_alt_rounded, size: 16, color: context.colors.onPrimary),
            ),
          ),
        ),
      ],
    );
  }

  String _categoryLabel(int id) {
    final category = _categories.where((c) => c.id == id).firstOrNull;
    return category == null ? '' : '${category.code} — ${category.name}';
  }
}
