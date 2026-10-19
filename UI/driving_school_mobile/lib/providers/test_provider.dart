import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/paged_result.dart';
import '../models/test_attempt_start.dart';
import '../models/test_summary.dart';

/// The student's side of the theory tests: the list with each test's last result and starting
/// a new attempt.
class TestProvider {
  TestProvider(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  /// Every test the student can take. The endpoint is paged (max 100 per page), so pages are
  /// read until the reported total is reached.
  Future<List<TestSummary>> getMy() async {
    const pageSize = 100;
    final tests = <TestSummary>[];
    for (var page = 1;; page++) {
      final response = await apiGet(
        ApiConfig.uri('api/tests/my', {'page': page, 'pageSize': pageSize}),
        headers: _authHeaders(),
      );
      final result = PagedResult<TestSummary>.fromJson(_decode(response) as Map<String, dynamic>, TestSummary.fromJson);
      tests.addAll(result.resultList);
      if (result.resultList.isEmpty || tests.length >= result.count) return tests;
    }
  }

  Future<TestAttemptStart> startAttempt(int testId) async {
    final response = await apiPost(ApiConfig.uri('api/tests/$testId/attempts'), headers: _authHeaders());
    return TestAttemptStart.fromJson(_decode(response) as Map<String, dynamic>);
  }

  dynamic _decode(http.Response response) {
    BaseProvider.ensureOk(response, _session);
    return jsonDecode(utf8.decode(response.bodyBytes));
  }
}
