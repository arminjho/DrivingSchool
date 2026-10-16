import '../core/json_helpers.dart';
import 'vehicle_service_report_category.dart';
import 'vehicle_service_report_vehicle.dart';

/// Mirrors the backend's `VehicleServiceReportData`.
class VehicleServiceReportData {
  final DateTime? dateFrom;
  final DateTime? dateTo;
  final int? vehicleCategoryId;
  final String? vehicleCategoryName;
  final DateTime generatedAtUtc;
  final List<VehicleServiceReportVehicle> vehicles;
  final int totalVehicles;
  final int totalServices;
  final double totalCost;
  final List<VehicleServiceReportCategory> categories;

  const VehicleServiceReportData({
    this.dateFrom,
    this.dateTo,
    this.vehicleCategoryId,
    this.vehicleCategoryName,
    required this.generatedAtUtc,
    required this.vehicles,
    required this.totalVehicles,
    required this.totalServices,
    required this.totalCost,
    required this.categories,
  });

  factory VehicleServiceReportData.fromJson(Map<String, dynamic> json) => VehicleServiceReportData(
      dateFrom: parseUtcDateTimeOrNull(json['dateFrom'] as String?),
      dateTo: parseUtcDateTimeOrNull(json['dateTo'] as String?),
      vehicleCategoryId: json['vehicleCategoryId'] as int?,
      vehicleCategoryName: json['vehicleCategoryName'] as String?,
      generatedAtUtc: parseUtcDateTime(json['generatedAtUtc'] as String),
      vehicles: (json['vehicles'] as List<dynamic>? ?? const []).map((item) => VehicleServiceReportVehicle.fromJson(item as Map<String, dynamic>)).toList(),
      totalVehicles: json['totalVehicles'] as int,
      totalServices: json['totalServices'] as int,
      totalCost: (json['totalCost'] as num).toDouble(),
      categories: (json['categories'] as List<dynamic>? ?? const []).map((item) => VehicleServiceReportCategory.fromJson(item as Map<String, dynamic>)).toList(),
      );
}
