import 'dart:typed_data';

import 'package:material_ui/material_ui.dart';
import 'package:printing/printing.dart';
import 'package:provider/provider.dart';

import '../../core/auth_session.dart';
import '../../core/base_provider.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/instructor.dart';
import '../../models/lesson_status_report_data.dart';
import '../../providers/instructor_provider.dart';
import '../../providers/report_api.dart';
import '../../widgets/charts/app_bar_chart.dart';
import '../../widgets/charts/chart_card.dart';
import '../../widgets/charts/chart_series.dart';
import '../../widgets/ui/api_filter_dropdown.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';

/// 4.1.4 left card - "Izvještaj o statusu časova vožnje": date range + instructor filter,
/// a chart of the same server-side aggregation, and the generated PDF (preview/print/download).
class LessonStatusReportTab extends StatefulWidget {
  const LessonStatusReportTab({super.key});

  @override
  State<LessonStatusReportTab> createState() => _LessonStatusReportTabState();
}

class _LessonStatusReportTabState extends State<LessonStatusReportTab> {
  late final ReportApi _api;
  late final InstructorProvider _instructorProvider;

  DateTime? _dateFrom;
  DateTime? _dateTo;
  int? _instructorId;

  Uint8List? _pdfBytes;
  LessonStatusReportData? _data;
  String? _error;
  bool _isGenerating = false;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _api = ReportApi(session);
    _instructorProvider = InstructorProvider(session);
  }

  Future<void> _generate() async {
    if (_dateFrom != null && _dateTo != null && _dateFrom!.isAfter(_dateTo!)) {
      setState(() => _error = AppStrings.reportDateRangeInvalid);
      return;
    }

    setState(() {
      _isGenerating = true;
      _error = null;
    });
    try {
      final pdfRequest = _api.getLessonStatusReportPdf(dateFrom: _dateFrom, dateTo: _dateTo, instructorId: _instructorId);
      final dataRequest = _api.getLessonStatusReport(dateFrom: _dateFrom, dateTo: _dateTo, instructorId: _instructorId);

      final pdfBytes = await pdfRequest;
      final data = await dataRequest;
      if (!mounted) return;
      setState(() {
        _pdfBytes = Uint8List.fromList(pdfBytes);
        _data = data;
      });
    } on Object catch (error) {
      if (mounted) setState(() => _error = '${AppStrings.reportGenerateFailed} ${failureCause(error)}');
    } finally {
      if (mounted) setState(() => _isGenerating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const Icon(Icons.description_outlined),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(AppStrings.lessonStatusReportTitle, style: context.text.titleMedium),
                      Text(AppStrings.lessonStatusReportSubtitle, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.lg),
            _DateField(label: AppStrings.fieldDateFrom, value: _dateFrom, onChanged: (v) => setState(() => _dateFrom = v)),
            const SizedBox(height: AppSpacing.md),
            _DateField(label: AppStrings.fieldDateTo, value: _dateTo, onChanged: (v) => setState(() => _dateTo = v)),
            const SizedBox(height: AppSpacing.md),
            ApiFilterDropdown<Instructor>(
              allLabel: AppStrings.allInstructors,
              width: double.infinity,
              loadItems: () async => (await _instructorProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
              idOf: (i) => i.id,
              labelOf: (i) => i.fullName,
              value: _instructorId,
              onChanged: (id) => setState(() => _instructorId = id),
            ),
            const SizedBox(height: AppSpacing.lg),
            if (_error != null) ...[AppNotice(message: _error!), const SizedBox(height: AppSpacing.md)],
            FilledButton.icon(
              onPressed: _isGenerating ? null : _generate,
              icon: _isGenerating
                  ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.picture_as_pdf_outlined, size: 18),
              label: const Text(AppStrings.downloadPdf),
              style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
            ),
            if (_pdfBytes != null) ...[
              const SizedBox(height: AppSpacing.lg),
              if (_data != null) ...[_chart(_data!), const SizedBox(height: AppSpacing.lg)],
              SizedBox(
                height: 600,
                child: PdfPreview(
                  build: (format) async => _pdfBytes!,
                  pdfFileName: 'izvjestaj-status-casova.pdf',
                  canChangePageFormat: false,
                  canChangeOrientation: false,
                  canDebug: false,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _chart(LessonStatusReportData data) {
    return ChartCard(
      title: AppStrings.navLessons,
      value: '${data.totalLessons}',
      subtitle: data.instructorName ?? AppStrings.allInstructors,
      child: data.rows.isEmpty
          ? const AppEmptyState(icon: Icons.bar_chart_rounded, title: AppStrings.emptyTitle, message: AppStrings.emptyMessage)
          : AppBarChart(
              labels: [for (final row in data.rows) row.instructorName],
              series: [
                AppChartSeries(name: AppStrings.reportCompleted, values: [for (final row in data.rows) row.completed.toDouble()]),
                AppChartSeries(name: AppStrings.reportScheduled, values: [for (final row in data.rows) row.scheduled.toDouble()]),
                AppChartSeries(name: AppStrings.reportCancelledCount, values: [for (final row in data.rows) row.cancelled.toDouble()]),
              ],
            ),
    );
  }
}

class _DateField extends StatelessWidget {
  final String label;
  final DateTime? value;
  final ValueChanged<DateTime?> onChanged;

  const _DateField({required this.label, required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: AppRadius.all(AppRadius.md),
      onTap: () async {
        final now = DateTime.now();
        final picked = await showDatePicker(context: context, initialDate: value ?? now, firstDate: DateTime(now.year - 5), lastDate: DateTime(now.year + 1));
        if (picked != null) onChanged(picked);
      },
      child: InputDecorator(
        decoration: InputDecoration(labelText: label, prefixIcon: const Icon(AppIcons.date, size: 18)),
        child: Text(value == null ? '' : AppFormat.date(value!)),
      ),
    );
  }
}
