import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/question.dart';

class QuestionProvider extends BaseProvider<Question> {
  QuestionProvider(AuthSession session) : super('questions', session);

  @override
  Question fromJson(Map<String, dynamic> json) => Question.fromJson(json);
}
