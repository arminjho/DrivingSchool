/// Mirrors `eAutoskola.Domain.Security.Role`. Only used to choose what to show - the API
/// re-checks the role on every endpoint regardless.
abstract final class Roles {
  static const String admin = 'Admin';
  static const String instructor = 'Instructor';
  static const String student = 'Student';
}
