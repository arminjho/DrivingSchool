import '../core/json_helpers.dart';

/// Mirrors the backend's `LessonDto`.
class Lesson {
  final int id;
  final int studentId;
  final String studentFullName;
  final String studentEmail;
  final String studentPhoneNumber;
  final String? studentProfileImagePath;
  final int instructorId;
  final String instructorFullName;
  final int? vehicleId;
  final String? vehicleRegistrationNumber;
  final String? vehicleMake;
  final String? vehicleModel;
  final int drivingZoneId;
  final String drivingZoneName;
  final DateTime startsAtUtc;
  final DateTime endsAtUtc;
  final int lessonNumber;
  final String? note;
  final String status;
  final String? rejectionReason;
  final int? rating;
  final String? instructorNotes;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final DateTime createdAt;
  final bool requiresPayment;
  final bool isPaid;

  const Lesson({
    required this.id,
    required this.studentId,
    required this.studentFullName,
    required this.studentEmail,
    required this.studentPhoneNumber,
    this.studentProfileImagePath,
    required this.instructorId,
    required this.instructorFullName,
    this.vehicleId,
    this.vehicleRegistrationNumber,
    this.vehicleMake,
    this.vehicleModel,
    required this.drivingZoneId,
    required this.drivingZoneName,
    required this.startsAtUtc,
    required this.endsAtUtc,
    required this.lessonNumber,
    this.note,
    required this.status,
    this.rejectionReason,
    this.rating,
    this.instructorNotes,
    this.startedAt,
    this.completedAt,
    required this.createdAt,
    required this.requiresPayment,
    required this.isPaid,
  });

  /// "VW Polo" - null while no vehicle is assigned yet.
  String? get vehicleName => vehicleMake == null ? null : '$vehicleMake $vehicleModel';

  /// "VW Polo (E22-A-123)" - how a detail screen names the vehicle.
  String? get vehicleLabel => vehicleName == null ? vehicleRegistrationNumber : '$vehicleName ($vehicleRegistrationNumber)';

  factory Lesson.fromJson(Map<String, dynamic> json) => Lesson(
      id: json['id'] as int,
      studentId: json['studentId'] as int,
      studentFullName: json['studentFullName'] as String,
      studentEmail: json['studentEmail'] as String,
      studentPhoneNumber: json['studentPhoneNumber'] as String,
      studentProfileImagePath: json['studentProfileImagePath'] as String?,
      instructorId: json['instructorId'] as int,
      instructorFullName: json['instructorFullName'] as String,
      vehicleId: json['vehicleId'] as int?,
      vehicleRegistrationNumber: json['vehicleRegistrationNumber'] as String?,
      vehicleMake: json['vehicleMake'] as String?,
      vehicleModel: json['vehicleModel'] as String?,
      drivingZoneId: json['drivingZoneId'] as int,
      drivingZoneName: json['drivingZoneName'] as String,
      startsAtUtc: parseUtcDateTime(json['startsAtUtc'] as String),
      endsAtUtc: parseUtcDateTime(json['endsAtUtc'] as String),
      lessonNumber: json['lessonNumber'] as int,
      note: json['note'] as String?,
      status: json['status'] as String,
      rejectionReason: json['rejectionReason'] as String?,
      rating: json['rating'] as int?,
      instructorNotes: json['instructorNotes'] as String?,
      startedAt: parseUtcDateTimeOrNull(json['startedAt'] as String?),
      completedAt: parseUtcDateTimeOrNull(json['completedAt'] as String?),
      createdAt: parseUtcDateTime(json['createdAt'] as String),
      requiresPayment: json['requiresPayment'] as bool,
      isPaid: json['isPaid'] as bool,
      );
}
