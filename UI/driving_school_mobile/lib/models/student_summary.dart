/// Mirrors the backend's `StudentSummaryDto` - "Moji učenici": a student who has at least one
/// lesson with the signed-in instructor.
class StudentSummary {
  final int id;
  final String fullName;
  final String email;
  final String phoneNumber;
  final String? profileImagePath;
  final int vehicleCategoryId;
  final String vehicleCategoryName;
  final int totalLessons;

  const StudentSummary({
    required this.id,
    required this.fullName,
    required this.email,
    required this.phoneNumber,
    this.profileImagePath,
    required this.vehicleCategoryId,
    required this.vehicleCategoryName,
    required this.totalLessons,
  });

  factory StudentSummary.fromJson(Map<String, dynamic> json) => StudentSummary(
      id: json['id'] as int,
      fullName: json['fullName'] as String,
      email: json['email'] as String,
      phoneNumber: json['phoneNumber'] as String,
      profileImagePath: json['profileImagePath'] as String?,
      vehicleCategoryId: json['vehicleCategoryId'] as int,
      vehicleCategoryName: json['vehicleCategoryName'] as String,
      totalLessons: json['totalLessons'] as int,
      );
}
