import 'dart:convert';

import 'api_exception.dart';
import 'api_http.dart';
import 'auth_session.dart';
import 'base_provider.dart';
import 'jwt_claims.dart';
import '../l10n/app_strings.dart';
import '../models/auth_response.dart';
import 'api_config.dart';

/// The backend's `api/auth` endpoints used by the client apps.
class AuthApi {
  AuthApi({
    required this._session,
    required this._allowedRoles,
    required this._roleNotAllowedMessage,
  });

  final AuthSession _session;
  final Set<String> _allowedRoles;
  final String _roleNotAllowedMessage;

  /// Signs in. Credentials travel in the POST body, never the query string. The role is read
  /// from the issued token; an account whose role this app is not built for is turned away
  /// and its freshly issued token revoked right away.
  Future<void> login({required String email, required String password}) async {
    final response = await apiPost(
      ApiConfig.uri('api/auth/login'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw BaseProvider.parseError(response);
    }

    final auth = AuthResponse.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
    final claims = JwtClaims.tryParse(auth.token);

    if (claims == null) {
      throw const ApiException(statusCode: 0, message: AppStrings.loginFailed);
    }
    if (!_allowedRoles.contains(claims.role)) {
      await _revoke(auth.token);
      throw ApiException(statusCode: 403, message: _roleNotAllowedMessage);
    }
    if (!await _session.signIn(auth.token)) {
      throw const ApiException(statusCode: 0, message: AppStrings.loginFailed);
    }
  }

  /// Invalidates the token on the server, then ends the local session. The local session is
  /// cleared even when the server cannot be reached - it must never trap the user signed in.
  Future<void> logout() async {
    final token = _session.token;
    if (token != null) await _revoke(token);
    _session.clear();
  }

  Future<void> _revoke(String token) async {
    try {
      await apiPost(ApiConfig.uri('api/auth/logout'), headers: {'Authorization': 'Bearer $token'});
    } on ApiException {
      // Best effort: the token expires on its own.
    }
  }
}
