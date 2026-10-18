import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../core/image_validation.dart';
import '../models/user.dart';

/// `GET/PUT /api/me` + `POST /api/me/change-password` + `POST /api/me/profile-image` - the
/// signed-in user's own profile, not a CRUD resource over other users.
class MeApi {
  MeApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<User> getMe() async {
    final response = await apiGet(ApiConfig.uri('api/me'), headers: _authHeaders());
    return _decodeUser(response);
  }

  Future<User> updateMe(Map<String, dynamic> request) async {
    final response = await apiPut(ApiConfig.uri('api/me'), headers: _authHeaders(), body: jsonEncode(request));
    return _decodeUser(response);
  }

  Future<void> changePassword({required String oldPassword, required String newPassword, required String confirmNewPassword}) async {
    final response = await apiPost(
      ApiConfig.uri('api/me/change-password'),
      headers: _authHeaders(),
      body: jsonEncode({'oldPassword': oldPassword, 'newPassword': newPassword, 'confirmNewPassword': confirmNewPassword}),
    );
    BaseProvider.ensureOk(response, _session);
  }

  Future<User> uploadProfileImage(PickedImage image) async {
    final request = http.MultipartRequest('POST', ApiConfig.uri('api/me/profile-image'))
      ..headers['Authorization'] = 'Bearer ${_session.token}'
      ..files.add(http.MultipartFile.fromBytes('file', image.bytes, filename: image.name));

    final streamed = await BaseProvider.send(() async {
      final response = await request.send();
      return http.Response.fromStream(response);
    });
    return _decodeUser(streamed);
  }

  /// The URL a signed-in client loads an uploaded image from (`FilesController`, `[Authorize]`).
  static Uri urlFor(String relativePath) => ApiConfig.uri('api/files', {'path': relativePath});

  User _decodeUser(http.Response response) {
    BaseProvider.ensureOk(response, _session);
    return User.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
