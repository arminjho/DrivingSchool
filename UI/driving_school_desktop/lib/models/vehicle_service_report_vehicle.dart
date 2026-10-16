import 'vehicle_service_report_entry.dart';

/// Mirrors the backend's `VehicleServiceReportVehicle`.
class VehicleServiceReportVehicle {
  final int vehicleId;
  final String registrationNumber;
  final String make;
  final String model;
  final String categoryCode;
  final String categoryName;
  final int manufactureYear;
  final String status;
  final List<VehicleServiceReportEntry> services;
  final double totalCost;

  const VehicleServiceReportVehicle({
    required this.vehicleId,
    required this.registrationNumber,
    required this.make,
    required this.model,
    required this.categoryCode,
    required this.categoryName,
    required this.manufactureYear,
    required this.status,
    required this.services,
    required this.totalCost,
  });

  factory VehicleServiceReportVehicle.fromJson(Map<String, dynamic> json) => VehicleServiceReportVehicle(
      vehicleId: json['vehicleId'] as int,
      registrationNumber: json['registrationNumber'] as String,
      make: json['make'] as String,
      model: json['model'] as String,
      categoryCode: json['categoryCode'] as String,
      categoryName: json['categoryName'] as String,
      manufactureYear: json['manufactureYear'] as int,
      status: json['status'] as String,
      services: (json['services'] as List<dynamic>? ?? const []).map((item) => VehicleServiceReportEntry.fromJson(item as Map<String, dynamic>)).toList(),
      totalCost: (json['totalCost'] as num).toDouble(),
      );
}
