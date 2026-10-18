import 'dart:convert';

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/lesson_status_report_data.dart';
import '../models/vehicle_service_report_data.dart';

/// `GET /api/reports/...` - both the JSON data (for the on-screen chart) and the PDF bytes (for
/// preview/print/download) come from the same server-side aggregation and the same filters, so
/// what is on screen and what prints can never disagree.
class ReportApi {
  ReportApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Map<String, dynamic> _dateQuery(DateTime? dateFrom, DateTime? dateTo) => {
        if (dateFrom != null) 'DateFrom': _isoDate(dateFrom),
        if (dateTo != null) 'DateTo': _isoDate(dateTo),
      };

  String _isoDate(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Future<T> _getJson<T>(Uri uri, T Function(Map<String, dynamic>) fromJson) async {
    final response = await apiGet(uri, headers: _authHeaders());
    BaseProvider.ensureOk(response, _session);
    return fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }

  Future<List<int>> _getPdf(Uri uri) async {
    final response = await apiGet(uri, headers: _authHeaders(), timeout: const Duration(seconds: 60));
    BaseProvider.ensureOk(response, _session);
    return response.bodyBytes;
  }

  Future<LessonStatusReportData> getLessonStatusReport({DateTime? dateFrom, DateTime? dateTo, int? instructorId}) =>
      _getJson(
        ApiConfig.uri('api/reports/lesson-status', {..._dateQuery(dateFrom, dateTo), 'InstructorId': ?instructorId}),
        LessonStatusReportData.fromJson,
      );

  Future<List<int>> getLessonStatusReportPdf({DateTime? dateFrom, DateTime? dateTo, int? instructorId}) => _getPdf(
        ApiConfig.uri('api/reports/lesson-status/pdf', {..._dateQuery(dateFrom, dateTo), 'InstructorId': ?instructorId}),
      );

  Future<VehicleServiceReportData> getVehicleServiceReport({DateTime? dateFrom, DateTime? dateTo, int? vehicleCategoryId}) =>
      _getJson(
        ApiConfig.uri('api/reports/vehicle-services', {..._dateQuery(dateFrom, dateTo), 'VehicleCategoryId': ?vehicleCategoryId}),
        VehicleServiceReportData.fromJson,
      );

  Future<List<int>> getVehicleServiceReportPdf({DateTime? dateFrom, DateTime? dateTo, int? vehicleCategoryId}) => _getPdf(
        ApiConfig.uri('api/reports/vehicle-services/pdf', {..._dateQuery(dateFrom, dateTo), 'VehicleCategoryId': ?vehicleCategoryId}),
      );
}
