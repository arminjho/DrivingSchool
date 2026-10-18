import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/test_attempt_start.dart';
import '../models/test_summary.dart';

/// The backend's `TestController` has no `[Route]` attribute, so it inherits the bare
/// `api/[controller]` template - the literal controller name `Test`, not the conventional
/// pluralised `tests`.
class TestProvider {
  TestProvider(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<List<TestSummary>> getMy() async {
    final response = await apiGet(ApiConfig.uri('api/Test/my'), headers: _authHeaders());
    final decoded = _decode(response) as List<dynamic>;
    return decoded.map((item) => TestSummary.fromJson(item as Map<String, dynamic>)).toList();
  }

  Future<TestAttemptStart> startAttempt(int testId) async {
    final response = await apiPost(ApiConfig.uri('api/Test/$testId/attempts'), headers: _authHeaders());
    return TestAttemptStart.fromJson(_decode(response) as Map<String, dynamic>);
  }

  dynamic _decode(http.Response response) {
    if (response.statusCode == 401) _session.clear();
    if (response.statusCode < 200 || response.statusCode >= 300) throw BaseProvider.parseError(response);
    return jsonDecode(utf8.decode(response.bodyBytes));
  }
}
