import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/test_attempt_review.dart';
import '../models/test_result.dart';

/// `POST /api/test-attempts/{id}/submit` + `GET /api/test-attempts/{id}/review`.
class TestAttemptApi {
  TestAttemptApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  /// [answers] maps questionId to the selected answer option id, or null for an unanswered
  /// question.
  Future<TestResult> submit(int attemptId, Map<int, int?> answers) async {
    final response = await apiPost(
      ApiConfig.uri('api/test-attempts/$attemptId/submit'),
      headers: _authHeaders(),
      body: jsonEncode({
        'answers': [
          for (final entry in answers.entries) {'questionId': entry.key, 'selectedAnswerOptionId': entry.value},
        ],
      }),
    );
    return _decode(response, TestResult.fromJson);
  }

  Future<TestAttemptReview> getReview(int attemptId) async {
    final response = await apiGet(ApiConfig.uri('api/test-attempts/$attemptId/review'), headers: _authHeaders());
    return _decode(response, TestAttemptReview.fromJson);
  }

  T _decode<T>(http.Response response, T Function(Map<String, dynamic>) fromJson) {
    BaseProvider.ensureOk(response, _session);
    return fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
