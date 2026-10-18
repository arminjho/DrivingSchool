/// Mirrors the backend's `InstructorDto`.
class Instructor {
  final int id;
  final String fullName;
  final int vehicleCategoryId;
  final String vehicleCategoryName;

  const Instructor({
    required this.id,
    required this.fullName,
    required this.vehicleCategoryId,
    required this.vehicleCategoryName,
  });

  factory Instructor.fromJson(Map<String, dynamic> json) => Instructor(
      id: json['id'] as int,
      fullName: json['fullName'] as String,
      vehicleCategoryId: json['vehicleCategoryId'] as int,
      vehicleCategoryName: json['vehicleCategoryName'] as String,
      );
}
