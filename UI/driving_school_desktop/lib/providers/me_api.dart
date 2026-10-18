import 'dart:convert';

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/user.dart';

/// `GET/PUT /api/me` + `POST /api/me/change-password` - the signed-in user's own profile, not a
/// CRUD resource over other users (that is [UserProvider], Admin-only).
class MeApi {
  MeApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<User> getMe() async {
    final response = await apiGet(ApiConfig.uri('api/me'), headers: _authHeaders());
    BaseProvider.ensureOk(response, _session);
    return User.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }

  Future<User> updateMe(Map<String, dynamic> request) async {
    final response = await apiPut(ApiConfig.uri('api/me'), headers: _authHeaders(), body: jsonEncode(request));
    BaseProvider.ensureOk(response, _session);
    return User.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }

  Future<void> changePassword({required String oldPassword, required String newPassword, required String confirmNewPassword}) async {
    final response = await apiPost(
      ApiConfig.uri('api/me/change-password'),
      headers: _authHeaders(),
      body: jsonEncode({'oldPassword': oldPassword, 'newPassword': newPassword, 'confirmNewPassword': confirmNewPassword}),
    );
    BaseProvider.ensureOk(response, _session);
  }
}
