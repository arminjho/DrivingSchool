/// Mirrors the backend's `VehicleCategoryDto`.
class VehicleCategory {
  final int id;
  final String code;
  final String name;
  final String? description;

  const VehicleCategory({
    required this.id,
    required this.code,
    required this.name,
    this.description,
  });

  factory VehicleCategory.fromJson(Map<String, dynamic> json) => VehicleCategory(
      id: json['id'] as int,
      code: json['code'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      );
}
