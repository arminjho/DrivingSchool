import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/test.dart';

/// The backend's `TestController` has no `[Route]` attribute, so it inherits the bare
/// `api/[controller]` template - the literal controller name `Test`, not the conventional
/// pluralised `tests`.
class TestProvider extends BaseProvider<Test> {
  TestProvider(AuthSession session) : super('Test', session);

  @override
  Test fromJson(Map<String, dynamic> json) => Test.fromJson(json);
}
