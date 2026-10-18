import 'dart:convert';

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/recommendations_result.dart';

/// `GET /api/recommendations?take=` - explainable, personalised study-lesson suggestions.
class RecommendationApi {
  RecommendationApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<RecommendationsResult> get({int take = 0}) async {
    final response = await apiGet(ApiConfig.uri('api/recommendations', {'take': take}), headers: _authHeaders());
    BaseProvider.ensureOk(response, _session);
    return RecommendationsResult.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
