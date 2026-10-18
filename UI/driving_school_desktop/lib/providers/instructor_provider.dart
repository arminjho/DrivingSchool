import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/instructor.dart';

/// `GET /api/instructors` - the public instructor lookup, open to any authenticated role; used
/// here for the admin reports' instructor filter.
class InstructorProvider extends BaseProvider<Instructor> {
  InstructorProvider(AuthSession session) : super('instructors', session);

  @override
  Instructor fromJson(Map<String, dynamic> json) => Instructor.fromJson(json);
}
