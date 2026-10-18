import 'dart:convert';

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/dashboard.dart';

/// `GET /api/dashboard` - a single aggregate, not a CRUD resource, so it sits beside
/// [BaseProvider] rather than extending it.
class DashboardApi {
  DashboardApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<Dashboard> getDashboard() async {
    final response = await apiGet(ApiConfig.uri('api/dashboard'), headers: _authHeaders());
    BaseProvider.ensureOk(response, _session);
    return Dashboard.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
