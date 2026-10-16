/// Mirrors the backend's `VehicleDto`.
class Vehicle {
  final int id;
  final String registrationNumber;
  final String make;
  final String model;
  final int vehicleCategoryId;
  final String vehicleCategoryName;
  final int manufactureYear;
  final String status;
  final String? imagePath;

  const Vehicle({
    required this.id,
    required this.registrationNumber,
    required this.make,
    required this.model,
    required this.vehicleCategoryId,
    required this.vehicleCategoryName,
    required this.manufactureYear,
    required this.status,
    this.imagePath,
  });

  factory Vehicle.fromJson(Map<String, dynamic> json) => Vehicle(
      id: json['id'] as int,
      registrationNumber: json['registrationNumber'] as String,
      make: json['make'] as String,
      model: json['model'] as String,
      vehicleCategoryId: json['vehicleCategoryId'] as int,
      vehicleCategoryName: json['vehicleCategoryName'] as String,
      manufactureYear: json['manufactureYear'] as int,
      status: json['status'] as String,
      imagePath: json['imagePath'] as String?,
      );
}
