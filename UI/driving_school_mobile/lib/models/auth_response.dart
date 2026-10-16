import '../core/json_helpers.dart';

/// Mirrors the backend's `AuthResponseDto`.
class AuthResponse {
  final String token;
  final DateTime expiresAtUtc;
  final int userId;
  final String email;
  final String firstName;
  final String lastName;
  final String role;

  const AuthResponse({
    required this.token,
    required this.expiresAtUtc,
    required this.userId,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.role,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
      token: json['token'] as String,
      expiresAtUtc: parseUtcDateTime(json['expiresAtUtc'] as String),
      userId: json['userId'] as int,
      email: json['email'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      role: json['role'] as String,
      );
}
