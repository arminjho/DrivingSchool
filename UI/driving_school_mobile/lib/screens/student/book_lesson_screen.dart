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
import '../../models/instructor.dart';
import '../../providers/driving_zone_provider.dart';
import '../../providers/instructor_provider.dart';
import '../../providers/lesson_provider.dart';
import '../../widgets/ui/api_dropdown_field.dart';
import '../../widgets/ui/app_action_button.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_date_fields.dart';
import '../../widgets/ui/app_states.dart';
import '../../widgets/ui/app_text_field.dart';

/// 4.2.2 - instructor, date, an available slot for that instructor/date, zone, and an optional
/// note. The slot list is server-side truth (availability/overlap is never trusted client-side).
class BookLessonScreen extends StatefulWidget {
  const BookLessonScreen({super.key});

  @override
  State<BookLessonScreen> createState() => _BookLessonScreenState();
}

class _BookLessonScreenState extends State<BookLessonScreen> {
  final _formKey = GlobalKey<FormBuilderState>();
  late final LessonProvider _lessonProvider;
  late final InstructorProvider _instructorProvider;
  late final DrivingZoneProvider _zoneProvider;

  int? _instructorId;
  DateTime? _date;
  bool _isSubmitting = false;
  String? _banner;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _lessonProvider = LessonProvider(session);
    _instructorProvider = InstructorProvider(session);
    _zoneProvider = DrivingZoneProvider(session);
  }

  Future<List<DateTime>> _loadSlots() async {
    final instructorId = _instructorId;
    final date = _date;
    if (instructorId == null || date == null) return const [];
    return _lessonProvider.getAvailableSlots(instructorId: instructorId, date: date);
  }

  Future<void> _submit() async {
    final form = _formKey.currentState;
    if (form == null || !form.saveAndValidate()) return;

    setState(() {
      _isSubmitting = true;
      _banner = null;
    });
    try {
      await _lessonProvider.book(
        instructorId: form.value['instructorId'] as int,
        startsAtUtc: form.value['slot'] as DateTime,
        drivingZoneId: form.value['drivingZoneId'] as int,
        note: (form.value['note'] as String?)?.trim().isEmpty ?? true ? null : form.value['note'] as String?,
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
      appBar: AppBar(title: const Text(AppStrings.bookLessonTitle)),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: AppCard(
          child: FormBuilder(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(AppStrings.bookLessonSubtitle, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                const SizedBox(height: AppSpacing.lg),
                if (_banner != null) ...[AppNotice(message: _banner!), const SizedBox(height: AppSpacing.md)],
                ApiDropdownField<Instructor>(
                  name: 'instructorId',
                  label: AppStrings.fieldInstructor,
                  icon: AppIcons.person,
                  loadItems: () async => (await _instructorProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (i) => i.id,
                  labelOf: (i) => i.fullName,
                  validator: AppValidators.requiredSelection<int>(),
                  onChanged: (id, _) {
                    setState(() => _instructorId = id);
                    _formKey.currentState?.fields['slot']?.didChange(null);
                  },
                ),
                const SizedBox(height: AppSpacing.md),
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
                  reloadKey: '$_instructorId-$_date',
                  loadItems: _loadSlots,
                  idOf: (slot) => slot.millisecondsSinceEpoch,
                  labelOf: (slot) => '${AppFormat.time(slot)} - ${AppFormat.time(slot.add(const Duration(hours: 1)))}',
                  validator: AppValidators.requiredSelection<int>(),
                  emptyMessage: _instructorId == null || _date == null ? AppStrings.selectInstructorFirst : AppStrings.noSlotsForDate,
                ),
                const SizedBox(height: AppSpacing.md),
                ApiDropdownField<DrivingZone>(
                  name: 'drivingZoneId',
                  label: AppStrings.fieldZone,
                  icon: AppIcons.place,
                  loadItems: () async => (await _zoneProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
                  idOf: (z) => z.id,
                  labelOf: (z) => z.name,
                  validator: AppValidators.requiredSelection<int>(),
                ),
                const SizedBox(height: AppSpacing.md),
                AppTextField(name: 'note', label: AppStrings.fieldNote, hint: AppStrings.noteHint, maxLines: 3),
                const SizedBox(height: AppSpacing.lg),
                AppActionButton(label: AppStrings.sendRequest, onPressed: _submit, isLoading: _isSubmitting),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
