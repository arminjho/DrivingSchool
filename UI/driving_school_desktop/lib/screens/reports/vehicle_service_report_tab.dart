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
import '../../models/vehicle_category.dart';
import '../../models/vehicle_service_report_data.dart';
import '../../providers/report_api.dart';
import '../../providers/vehicle_category_provider.dart';
import '../../widgets/charts/app_bar_chart.dart';
import '../../widgets/charts/chart_card.dart';
import '../../widgets/charts/chart_series.dart';
import '../../widgets/ui/api_filter_dropdown.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_states.dart';

/// 4.1.4 right card - "Izvještaj o vozilima": vehicle category + date range, the same chart and
/// PDF pattern as the lesson-status report.
class VehicleServiceReportTab extends StatefulWidget {
  const VehicleServiceReportTab({super.key});

  @override
  State<VehicleServiceReportTab> createState() => _VehicleServiceReportTabState();
}

class _VehicleServiceReportTabState extends State<VehicleServiceReportTab> {
  late final ReportApi _api;
  late final VehicleCategoryProvider _categoryProvider;

  DateTime? _dateFrom;
  DateTime? _dateTo;
  int? _vehicleCategoryId;

  Uint8List? _pdfBytes;
  VehicleServiceReportData? _data;
  String? _error;
  bool _isGenerating = false;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _api = ReportApi(session);
    _categoryProvider = VehicleCategoryProvider(session);
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
      final pdfRequest = _api.getVehicleServiceReportPdf(dateFrom: _dateFrom, dateTo: _dateTo, vehicleCategoryId: _vehicleCategoryId);
      final dataRequest = _api.getVehicleServiceReport(dateFrom: _dateFrom, dateTo: _dateTo, vehicleCategoryId: _vehicleCategoryId);

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
                const Icon(AppIcons.vehicle),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(AppStrings.vehicleServiceReportTitle, style: context.text.titleMedium),
                      Text(AppStrings.vehicleServiceReportSubtitle, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.lg),
            ApiFilterDropdown<VehicleCategory>(
              allLabel: AppStrings.vehiclesAllCategories,
              width: double.infinity,
              loadItems: () async => (await _categoryProvider.getPaged(const PagedQuery(pageSize: 100))).resultList,
              idOf: (c) => c.id,
              labelOf: (c) => c.name,
              value: _vehicleCategoryId,
              onChanged: (id) => setState(() => _vehicleCategoryId = id),
            ),
            const SizedBox(height: AppSpacing.md),
            _DateField(label: AppStrings.fieldDateFrom, value: _dateFrom, onChanged: (v) => setState(() => _dateFrom = v)),
            const SizedBox(height: AppSpacing.md),
            _DateField(label: AppStrings.fieldDateTo, value: _dateTo, onChanged: (v) => setState(() => _dateTo = v)),
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
                  pdfFileName: 'izvjestaj-vozila.pdf',
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

  Widget _chart(VehicleServiceReportData data) {
    return ChartCard(
      title: AppStrings.fieldCost,
      value: AppFormat.money(data.totalCost),
      subtitle: data.vehicleCategoryName ?? AppStrings.vehiclesAllCategories,
      child: data.categories.isEmpty
          ? const AppEmptyState(icon: Icons.bar_chart_rounded, title: AppStrings.emptyTitle, message: AppStrings.emptyMessage)
          : AppBarChart(
              labels: [for (final category in data.categories) category.categoryCode],
              valueFormatter: (v) => '${v.toStringAsFixed(0)} KM',
              series: [AppChartSeries(name: AppStrings.fieldCost, values: [for (final category in data.categories) category.totalCost])],
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
