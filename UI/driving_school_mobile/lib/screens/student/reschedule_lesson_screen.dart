import 'package:material_ui/material_ui.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/form_errors.dart';
import '../../core/formatters.dart';
import '../../core/validators.dart';
import '../../l10n/app_strings.dart';
import '../../models/driving_zone.dart';
import '../../models/lesson.dart';
import '../../providers/driving_zone_provider.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_action_button.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_date_fields.dart';
import '../../widgets/ui/app_states.dart';

/// 4.2.4's "Izmijeni termin" - only reachable while the lesson is still Pending (same rule the
/// backend enforces server-side). The instructor stays fixed; only date/slot/zone change.
class RescheduleLessonScreen extends StatefulWidget {
  const RescheduleLessonScreen({super.key, required this.lesson});

  final Lesson lesson;

  @override
  State<RescheduleLessonScreen> createState() => _RescheduleLessonScreenState();
}

class _RescheduleLessonScreenState extends State<RescheduleLessonScreen> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final LessonProvider _lessonProvider;
  late final DrivingZoneProvider _zoneProvider;

  DateTime? _date;
  bool _isSubmitting = false;
  String? _banner;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _lessonProvider = LessonProvider(session);
    _zoneProvider = DrivingZoneProvider(session);
  }

  Future<List<DateTime>> _loadSlots() async {
    final date = _date;
    if (date == null) return const [];
    return _lessonProvider.getAvailableSlots(instructorId: widget.lesson.instructorId, date: date);
  }

  Future<void> _submit() async {
    final form = _formKey.currentState;
    if (form == null || !form.saveAndValidate()) return;

    setState(() {
      _isSubmitting = true;
      _banner = null;
    });
    try {
      await _lessonProvider.reschedule(
        widget.lesson.id,
        startsAtUtc: form.value['slot'] as DateTime,
        drivingZoneId: form.value['drivingZoneId'] as int,
      );
      if (mounted) Navigator.of(context).pop(true);
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
      appBar: AppBar(title: const Text(AppStrings.rescheduleAction)),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: AppCard(
          child: FormBuilder(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(widget.lesson.instructorFullName, style: context.text.titleMedium),
                const SizedBox(height: AppSpacing.lg),
                if (_banner != null) ...[AppNotice(message: _banner!), const SizedBox(height: AppSpacing.md)],
                AppDateField(
                  name: 'date',
                  label: AppStrings.fieldBookingDate,
                  firstDate: DateTime.now(),
                  lastDate: DateTime.now().add(const Duration(days: 60)),
                  validator: (value) => value == null ? AppStrings.dateRequired : null,
                  onChanged: (value) {
                    setState(() => _date = value);
                    _formKey.currentState?.fields['slot']?.didChange(null);
                  },
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<DateTime>(
                  name: 'slot',
                  label: AppStrings.fieldSlot,
                  icon: AppIcons.time,
                  reloadKey: _date,
                  loadItems: _loadSlots,
                  idOf: (slot) => slot.millisecondsSinceEpoch,
                  labelOf: (slot) => '${AppFormat.time(slot)} - ${AppFormat.time(slot.add(const Duration(hours: 1)))}',
                  validator: AppValidators.requiredSelection<int>(),
                  emptyMessage: _date == null ? AppStrings.dateRequired : AppStrings.noSlotsForDate,
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<DrivingZone>(
                  name: 'drivingZoneId',
                  label: AppStrings.fieldZone,
                  icon: AppIcons.place,
                  initialValue: widget.lesson.drivingZoneId,
                  loadItems: () async => (await _zoneProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (z) => z.id,
                  labelOf: (z) => z.name,
                  validator: AppValidators.requiredSelection<int>(),
                ),
                const SizedBox(height: AppSpacing.lg),
                AppActionButton(label: AppStrings.save, onPressed: _submit, isLoading: _isSubmitting),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
