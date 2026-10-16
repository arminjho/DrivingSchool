/// Mirrors the backend's `UserDto`.
class User {
  final int id;
  final String fullName;
  final String firstName;
  final String lastName;
  final String email;
  final String phoneNumber;
  final String? address;
  final int? cityId;
  final String? cityName;
  final String role;
  final bool isActive;
  final String? profileImagePath;
  final int? vehicleCategoryId;

  const User({
    required this.id,
    required this.fullName,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.phoneNumber,
    this.address,
    this.cityId,
    this.cityName,
    required this.role,
    required this.isActive,
    this.profileImagePath,
    this.vehicleCategoryId,
  });

  factory User.fromJson(Map<String, dynamic> json) => User(
      id: json['id'] as int,
      fullName: json['fullName'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      email: json['email'] as String,
      phoneNumber: json['phoneNumber'] as String,
      address: json['address'] as String?,
      cityId: json['cityId'] as int?,
      cityName: json['cityName'] as String?,
      role: json['role'] as String,
      isActive: json['isActive'] as bool,
      profileImagePath: json['profileImagePath'] as String?,
      vehicleCategoryId: json['vehicleCategoryId'] as int?,
      );
}
