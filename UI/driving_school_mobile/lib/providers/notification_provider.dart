import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/notification_item.dart';

/// Notifications are read-only apart from the two mark-read actions, so this reuses
/// [BaseProvider]'s URI/header/decode plumbing without its CRUD methods.
class NotificationProvider extends BaseProvider<NotificationItem> {
  NotificationProvider(AuthSession session) : super('notifications', session);

  @override
  NotificationItem fromJson(Map<String, dynamic> json) => NotificationItem.fromJson(json);

  Future<List<NotificationItem>> getLatest({int count = 50}) async =>
      (await getPaged(PagedQuery(pageSize: count))).resultList;

  Future<int> getUnreadCount() async {
    final response = await apiGet(buildUri('api/notifications/unread-count'), headers: authHeaders());
    final decoded = decode(response);
    return decoded is int ? decoded : int.tryParse('$decoded') ?? 0;
  }

  Future<void> markAsRead(int id) async {
    final response = await apiPost(buildUri('api/notifications/$id/mark-read'), headers: authHeaders());
    decode(response, allowEmptyBody: true);
  }

  Future<void> markAllAsRead() async {
    final response = await apiPost(buildUri('api/notifications/mark-all-read'), headers: authHeaders());
    decode(response, allowEmptyBody: true);
  }
}
