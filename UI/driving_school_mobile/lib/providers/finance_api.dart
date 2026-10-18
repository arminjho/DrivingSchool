import 'dart:convert';

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/finance_summary.dart';

/// `GET /api/finance/my` - the Student's own cost breakdown, payment history and next
/// installment. Not a CRUD resource, so it sits beside [BaseProvider] rather than extending it.
class FinanceApi {
  FinanceApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<FinanceSummary> getMy() async {
    final response = await apiGet(ApiConfig.uri('api/finance/my'), headers: _authHeaders());
    BaseProvider.ensureOk(response, _session);
    return FinanceSummary.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
