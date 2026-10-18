import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/question_area.dart';

class QuestionAreaProvider extends BaseProvider<QuestionArea> {
  QuestionAreaProvider(AuthSession session) : super('question-areas', session);

  @override
  QuestionArea fromJson(Map<String, dynamic> json) => QuestionArea.fromJson(json);
}
