import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/instructor.dart';

/// `GET /api/instructors` - the public instructor lookup, used for the booking screen's
/// instructor dropdown.
class InstructorProvider extends BaseProvider<Instructor> {
  InstructorProvider(AuthSession session) : super('instructors', session);

  @override
  Instructor fromJson(Map<String, dynamic> json) => Instructor.fromJson(json);
}
