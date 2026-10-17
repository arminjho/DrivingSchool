import '../core/json_helpers.dart';

/// Mirrors the backend's `NotificationDto`. Named `NotificationItem` because Flutter already
/// has a `Notification` class.
class NotificationItem {
  final int id;
  final String title;
  final String message;
  final String type;
  final bool isRead;
  final DateTime createdAt;

  const NotificationItem({
    required this.id,
    required this.title,
    required this.message,
    required this.type,
    required this.isRead,
    required this.createdAt,
  });

  factory NotificationItem.fromJson(Map<String, dynamic> json) => NotificationItem(
        id: json['id'] as int,
        title: json['title'] as String,
        message: json['message'] as String,
        type: json['type'] as String,
        isRead: json['isRead'] as bool,
        createdAt: parseUtcDateTime(json['createdAt'] as String),
      );

  /// The optimistic copy shown between tapping a notification and the server confirming it.
  NotificationItem asRead() => NotificationItem(
        id: id,
        title: title,
        message: message,
        type: type,
        isRead: true,
        createdAt: createdAt,
      );
}
