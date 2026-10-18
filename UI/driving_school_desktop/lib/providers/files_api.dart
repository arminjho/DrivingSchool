import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../core/image_validation.dart';

/// `POST /api/files/upload` (Admin-only) - the generic image upload used by admin forms whose
/// entity has no self-service upload route of its own (e.g. a Question's "Slika znaka").
class FilesApi {
  FilesApi(this._session);

  final AuthSession _session;

  Future<String> upload(PickedImage image) async {
    final request = http.MultipartRequest('POST', ApiConfig.uri('api/files/upload'))
      ..headers['Authorization'] = 'Bearer ${_session.token}'
      ..files.add(http.MultipartFile.fromBytes('file', image.bytes, filename: image.name));

    final streamed = await BaseProvider.send(() async {
      final response = await request.send();
      return http.Response.fromStream(response);
    });

    BaseProvider.ensureOk(streamed, _session);

    final body = jsonDecode(utf8.decode(streamed.bodyBytes)) as Map<String, dynamic>;
    return body['path'] as String;
  }

  /// The URL a signed-in client loads an uploaded image from (`FilesController`, `[Authorize]`).
  static Uri urlFor(String relativePath) => ApiConfig.uri('api/files', {'path': relativePath});
}
