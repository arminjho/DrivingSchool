import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/instructor_home.dart';
import '../models/instructor_statistics.dart';
import '../models/lesson.dart';
import '../models/lesson_request_counts.dart';
import '../models/paged_result.dart';
import '../models/student_summary.dart';

/// Everything under `api/instructors/...` that isn't the plain `InstructorDto` lookup
/// (4.3.1 home/statistics, 4.3.2 lesson requests, 4.3.4 schedule, "Moji učenici").
class InstructorApi {
  InstructorApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<InstructorHome> getHome() async {
    final response = await apiGet(ApiConfig.uri('api/instructors/home'), headers: _authHeaders());
    return InstructorHome.fromJson(_decodeMap(response));
  }

  Future<InstructorStatistics> getStatistics() async {
    final response = await apiGet(ApiConfig.uri('api/instructors/statistics'), headers: _authHeaders());
    return InstructorStatistics.fromJson(_decodeMap(response));
  }

  Future<LessonRequestCounts> getLessonRequestCounts() async {
    final response = await apiGet(ApiConfig.uri('api/instructors/lesson-requests/counts'), headers: _authHeaders());
    return LessonRequestCounts.fromJson(_decodeMap(response));
  }

  Future<PagedResult<Lesson>> getLessonRequests(PagedQuery query) async {
    final response = await apiGet(ApiConfig.uri('api/instructors/lesson-requests', query.toQuery()), headers: _authHeaders());
    return PagedResult<Lesson>.fromJson(_decodeMap(response), Lesson.fromJson);
  }

  Future<List<Lesson>> getSchedule(DateTime date) async {
    final isoDate = '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
    final response = await apiGet(ApiConfig.uri('api/instructors/schedule', {'date': isoDate}), headers: _authHeaders());
    if (response.statusCode == 401) _session.clear();
    if (response.statusCode < 200 || response.statusCode >= 300) throw BaseProvider.parseError(response);
    final decoded = jsonDecode(utf8.decode(response.bodyBytes)) as List<dynamic>;
    return decoded.map((item) => Lesson.fromJson(item as Map<String, dynamic>)).toList();
  }

  Future<PagedResult<StudentSummary>> getMyStudents(PagedQuery query) async {
    final response = await apiGet(ApiConfig.uri('api/instructors/me/students', query.toQuery()), headers: _authHeaders());
    return PagedResult<StudentSummary>.fromJson(_decodeMap(response), StudentSummary.fromJson);
  }

  Map<String, dynamic> _decodeMap(http.Response response) {
    if (response.statusCode == 401) _session.clear();
    if (response.statusCode < 200 || response.statusCode >= 300) throw BaseProvider.parseError(response);
    return jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>;
  }
}
