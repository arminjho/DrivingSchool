/// Mirrors the backend's `VehicleServiceTypeDto`.
class VehicleServiceType {
  final int id;
  final String name;

  const VehicleServiceType({
    required this.id,
    required this.name,
  });

  factory VehicleServiceType.fromJson(Map<String, dynamic> json) => VehicleServiceType(
      id: json['id'] as int,
      name: json['name'] as String,
      );
}
