import 'dart:async';

import 'package:signalr_netcore/signalr_client.dart';

import '../models/notification_item.dart';
import 'api_config.dart';

/// The real-time channel for notifications. Behind an interface so [NotificationCenter] can
/// fall back to polling whenever it is unavailable, and so tests can drive it by hand.
abstract class NotificationHub {
  /// Notifications pushed to the signed-in user.
  Stream<NotificationItem> get notifications;

  /// True while the channel is up. Changes are what tell the center to stop or resume polling.
  Stream<bool> get connection;

  bool get isConnected;

  /// Connects with [accessToken]. A failure is not thrown: the center keeps polling instead.
  Future<void> start(String accessToken);

  Future<void> stop();

  Future<void> dispose();
}

/// SignalR over WebSockets against the API's `/hubs/notifications` hub. The token travels in the
/// query string because browsers' and mobile WebSocket clients cannot set an Authorization header.
class SignalRNotificationHub implements NotificationHub {
  static const String _path = 'hubs/notifications';
  static const String _event = 'ReceiveNotification';

  final StreamController<NotificationItem> _notifications = StreamController.broadcast();
  final StreamController<bool> _connection = StreamController.broadcast();

  HubConnection? _hub;
  bool _isConnected = false;

  @override
  Stream<NotificationItem> get notifications => _notifications.stream;

  @override
  Stream<bool> get connection => _connection.stream;

  @override
  bool get isConnected => _isConnected;

  @override
  Future<void> start(String accessToken) async {
    await stop();

    final hub = HubConnectionBuilder()
        .withUrl(
          ApiConfig.uri(_path).toString(),
          options: HttpConnectionOptions(
            accessTokenFactory: () async => accessToken,
            transport: HttpTransportType.WebSockets,
            skipNegotiation: true,
          ),
        )
        .withAutomaticReconnect()
        .build();

    hub.on(_event, _onNotification);
    hub.onclose(({error}) => _setConnected(false));
    hub.onreconnecting(({error}) => _setConnected(false));
    hub.onreconnected(({connectionId}) => _setConnected(true));
    _hub = hub;

    try {
      await hub.start();
      _setConnected(true);
    } on Object {
      _setConnected(false);
    }
  }

  void _onNotification(List<Object?>? arguments) {
    final payload = arguments?.firstOrNull;
    if (payload is! Map) return;

    try {
      _notifications.add(NotificationItem.fromJson(Map<String, dynamic>.from(payload)));
    } on Object {
      // A payload this client doesn't understand is dropped; the next poll picks it up.
    }
  }

  void _setConnected(bool value) {
    if (_isConnected == value) return;
    _isConnected = value;
    _connection.add(value);
  }

  @override
  Future<void> stop() async {
    final hub = _hub;
    _hub = null;
    _setConnected(false);
    try {
      await hub?.stop();
    } on Object {
      // Already closed.
    }
  }

  @override
  Future<void> dispose() async {
    await stop();
    await _notifications.close();
    await _connection.close();
  }
}
