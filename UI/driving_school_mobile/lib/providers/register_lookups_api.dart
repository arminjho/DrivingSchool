import 'dart:convert';

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/base_provider.dart';
import '../models/register_lookups.dart';

/// `GET /api/auth/register-lookups` - anonymous on purpose: the registration screen has no
/// token yet. A dedicated endpoint for exactly this, rather than the Student app reaching into
/// the general (token-protected) City/VehicleCategory CRUD endpoints.
class RegisterLookupsApi {
  Future<RegisterLookups> get() async {
    final response = await apiGet(ApiConfig.uri('api/auth/register-lookups'));
    if (response.statusCode < 200 || response.statusCode >= 300) throw BaseProvider.parseError(response);
    return RegisterLookups.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
