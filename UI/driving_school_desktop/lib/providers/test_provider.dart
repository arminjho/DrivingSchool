import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/test.dart';

class TestProvider extends BaseProvider<Test> {
  TestProvider(AuthSession session) : super('tests', session);

  @override
  Test fromJson(Map<String, dynamic> json) => Test.fromJson(json);
}
