import 'dart:convert';

/// The claims the app reads from the API's access token. The role comes from the token itself
/// (the same value the API authorizes against), never from a separate field.
class JwtClaims {
  static const String _roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  static const String _userIdClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
  static const String _emailClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
  static const String _firstNameClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname';
  static const String _lastNameClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname';

  final int userId;
  final String role;
  final String email;
  final String firstName;
  final String lastName;
  final DateTime expiresAtUtc;

  const JwtClaims({
    required this.userId,
    required this.role,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.expiresAtUtc,
  });

  /// Returns null for anything that is not a well-formed token carrying a role, a user id and
  /// an expiry - a token the app cannot reason about is treated as no token at all.
  static JwtClaims? tryParse(String token) {
    final parts = token.split('.');
    if (parts.length != 3) return null;

    final Map<String, dynamic> payload;
    try {
      final decoded = utf8.decode(base64Url.decode(base64Url.normalize(parts[1])));
      payload = jsonDecode(decoded) as Map<String, dynamic>;
    } on FormatException {
      return null;
    }

    final role = _first(payload[_roleClaim] ?? payload['role']);
    final userId = int.tryParse('${payload[_userIdClaim] ?? payload['nameid'] ?? payload['sub'] ?? ''}');
    final expiresAt = payload['exp'];
    if (role == null || userId == null || expiresAt is! num) return null;

    return JwtClaims(
      userId: userId,
      role: role,
      email: _first(payload[_emailClaim] ?? payload['email']) ?? '',
      firstName: _first(payload[_firstNameClaim] ?? payload['given_name']) ?? '',
      lastName: _first(payload[_lastNameClaim] ?? payload['family_name']) ?? '',
      expiresAtUtc: DateTime.fromMillisecondsSinceEpoch(expiresAt.toInt() * 1000, isUtc: true),
    );
  }

  // A claim with several values is serialised as a JSON array.
  static String? _first(Object? value) {
    if (value is List && value.isNotEmpty) return '${value.first}';
    return value is String && value.isNotEmpty ? value : null;
  }
}
