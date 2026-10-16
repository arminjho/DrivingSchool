import '../core/json_helpers.dart';

/// Mirrors the backend's `VehicleServiceReportEntry`.
class VehicleServiceReportEntry {
  final DateTime serviceDate;
  final String serviceTypeName;
  final String description;
  final String status;
  final double cost;

  const VehicleServiceReportEntry({
    required this.serviceDate,
    required this.serviceTypeName,
    required this.description,
    required this.status,
    required this.cost,
  });

  factory VehicleServiceReportEntry.fromJson(Map<String, dynamic> json) => VehicleServiceReportEntry(
      serviceDate: parseUtcDateTime(json['serviceDate'] as String),
      serviceTypeName: json['serviceTypeName'] as String,
      description: json['description'] as String,
      status: json['status'] as String,
      cost: (json['cost'] as num).toDouble(),
      );
}
