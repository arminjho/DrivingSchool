import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/user.dart';

class UserProvider extends BaseProvider<User> {
  UserProvider(AuthSession session) : super('users', session);

  @override
  User fromJson(Map<String, dynamic> json) => User.fromJson(json);

  Future<void> activate(int id) async {
    final response = await apiPost(buildUri('api/users/$id/activate'), headers: authHeaders());
    decode(response, allowEmptyBody: true);
  }

  Future<void> deactivate(int id) async {
    final response = await apiPost(buildUri('api/users/$id/deactivate'), headers: authHeaders());
    decode(response, allowEmptyBody: true);
  }
}
