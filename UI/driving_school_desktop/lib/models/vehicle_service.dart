import '../core/json_helpers.dart';

/// Mirrors the backend's `VehicleServiceDto`.
class VehicleService {
  final int id;
  final int vehicleId;
  final String vehicleRegistrationNumber;
  final DateTime serviceDate;
  final int vehicleServiceTypeId;
  final String vehicleServiceTypeName;
  final String description;
  final double cost;
  final String status;

  const VehicleService({
    required this.id,
    required this.vehicleId,
    required this.vehicleRegistrationNumber,
    required this.serviceDate,
    required this.vehicleServiceTypeId,
    required this.vehicleServiceTypeName,
    required this.description,
    required this.cost,
    required this.status,
  });

  factory VehicleService.fromJson(Map<String, dynamic> json) => VehicleService(
      id: json['id'] as int,
      vehicleId: json['vehicleId'] as int,
      vehicleRegistrationNumber: json['vehicleRegistrationNumber'] as String,
      serviceDate: parseUtcDateTime(json['serviceDate'] as String),
      vehicleServiceTypeId: json['vehicleServiceTypeId'] as int,
      vehicleServiceTypeName: json['vehicleServiceTypeName'] as String,
      description: json['description'] as String,
      cost: (json['cost'] as num).toDouble(),
      status: json['status'] as String,
      );
}
