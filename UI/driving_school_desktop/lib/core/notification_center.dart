import 'dart:async';

import 'package:flutter/widgets.dart';

import '../models/notification_item.dart';
import '../providers/notification_provider.dart';
import 'auth_session.dart';
import 'error_text.dart';
import 'notification_hub.dart';

/// The single source of truth for notifications; the bell badge and the notifications screen
/// both read from it.
///
/// **Real-time first, polling as the fallback.** While the SignalR hub is connected, pushes
/// arrive instantly and polling slows to an occasional safety net (a push can be missed across a
/// reconnect). Whenever the hub is down - it never connected, or the connection dropped - the
/// center polls every 20 seconds, backing off up to 5 minutes while the server is unreachable.
///
/// Two things keep polling cheap:
///  - the *list* is only fetched while a screen is watching it ([beginWatchingList]); the badge
///    only needs a count;
///  - polling pauses while the app is in the background and refreshes once on return.
class NotificationCenter extends ChangeNotifier with WidgetsBindingObserver {
  NotificationCenter({
    required this._session,
    required this._provider,
    required this._hub,
  }) {
    WidgetsBinding.instance.addObserver(this);
    _session.addListener(_onSessionChanged);
    _pushSubscription = _hub.notifications.listen(_onPushed);
    _connectionSubscription = _hub.connection.listen(_onConnectionChanged);
    _onSessionChanged();
  }

  static const Duration pollInterval = Duration(seconds: 20);
  static const Duration maxPollInterval = Duration(minutes: 5);
  static const Duration connectedPollInterval = Duration(minutes: 2);
  static const int latestCount = 50;

  final AuthSession _session;
  final NotificationProvider _provider;
  final NotificationHub _hub;

  late final StreamSubscription<NotificationItem> _pushSubscription;
  late final StreamSubscription<bool> _connectionSubscription;

  Timer? _timer;
  Duration _failureInterval = pollInterval;
  int _listWatchers = 0;
  bool _isForeground = true;
  bool _isRefreshing = false;
  bool _isRunning = false;
  bool _isDisposed = false;

  int _unreadCount = 0;
  List<NotificationItem>? _items;
  String? _listError;

  int get unreadCount => _unreadCount;

  /// Null until the list has loaded once - which is not the same as an empty list.
  List<NotificationItem>? get items => _items;

  /// Set when the latest list fetch failed. Non-null alongside non-null [items] means "what you
  /// see may be stale", not "nothing loaded".
  String? get listError => _listError;

  bool get isWatchingList => _listWatchers > 0;

  void beginWatchingList() {
    _listWatchers++;
    if (_listWatchers == 1) unawaited(refresh());
  }

  void endWatchingList() {
    if (_listWatchers > 0) _listWatchers--;
  }

  /// Fetches the unread count and, if a screen is watching, the list - in parallel.
  ///
  /// A failed *badge* poll stays silent (it repeats, and a banner that comes and goes teaches
  /// people to ignore banners) but feeds the backoff; a failed *list* fetch is recorded because a
  /// screen is showing that data to someone right now.
  Future<void> refresh() async {
    if (_isDisposed || !_session.isLoggedIn || _isRefreshing) return;
    _isRefreshing = true;

    final watching = isWatchingList;
    final (count, list) = await (
      _attempt(_provider.getUnreadCount),
      watching ? _attempt(() => _provider.getLatest(count: latestCount)) : Future.value(const _Attempt<List<NotificationItem>>.skipped()),
    ).wait;
    _isRefreshing = false;
    if (_isDisposed) return;

    if (count.error == null) {
      _unreadCount = count.value!;
      _failureInterval = pollInterval;
    } else {
      final doubled = _failureInterval * 2;
      _failureInterval = doubled > maxPollInterval ? maxPollInterval : doubled;
    }

    if (watching && !list.skipped) {
      if (list.error == null) {
        _items = list.value;
        _listError = null;
      } else {
        _listError = failureCause(list.error!);
      }
    }

    notifyListeners();
  }

  Future<_Attempt<T>> _attempt<T>(Future<T> Function() call) async {
    try {
      return _Attempt(value: await call());
    } on Object catch (error) {
      return _Attempt(error: error);
    }
  }

  void _onPushed(NotificationItem item) {
    if (_isDisposed) return;
    if (_items?.any((existing) => existing.id == item.id) ?? false) return;

    if (!item.isRead) _unreadCount++;
    _items = _items == null ? null : [item, ...(_items!)];
    notifyListeners();
  }

  void _onConnectionChanged(bool connected) {
    if (_isDisposed || !_isRunning) return;

    // Catch up on anything missed while disconnected, then settle on the right cadence.
    unawaited(refresh().then((_) => _scheduleNext()));
  }

  /// Marked locally first so the row and badge react at once, then reconciled with the server.
  /// A failed call is put right by the refresh that follows - and rethrown for the screen.
  Future<void> markAsRead(NotificationItem item) async {
    if (item.isRead) return;

    _applyLocallyRead({item.id});
    try {
      await _provider.markAsRead(item.id);
    } finally {
      await refresh();
    }
  }

  Future<void> markAllAsRead() async {
    final unreadIds = _items?.where((n) => !n.isRead).map((n) => n.id).toSet() ?? const <int>{};
    if (unreadIds.isEmpty && _unreadCount == 0) return;

    _applyLocallyRead(unreadIds, all: true);
    try {
      await _provider.markAllAsRead();
    } finally {
      await refresh();
    }
  }

  void _applyLocallyRead(Set<int> ids, {bool all = false}) {
    _items = _items?.map((n) => ids.contains(n.id) ? n.asRead() : n).toList();
    _unreadCount = all ? 0 : (_unreadCount - ids.length).clamp(0, _unreadCount);
    notifyListeners();
  }

  void _onSessionChanged() {
    if (_session.isLoggedIn) {
      _start();
    } else {
      _stop();
      // Never leave one account's notifications on screen for the next person to sign in.
      _items = null;
      _listError = null;
      _unreadCount = 0;
      _listWatchers = 0;
      if (!_isDisposed) notifyListeners();
    }
  }

  void _start() {
    if (_isRunning) return;
    _isRunning = true;
    _failureInterval = pollInterval;

    // The first poll doesn't wait for the hub: polling covers the gap while it connects, and the
    // hub's own connect callback triggers a catch-up refresh.
    unawaited(_hub.start(_session.token!));
    unawaited(refresh().then((_) => _scheduleNext()));
  }

  void _stop() {
    _isRunning = false;
    _timer?.cancel();
    _timer = null;
    unawaited(_hub.stop());
  }

  /// A one-shot timer that re-arms itself, because the delay has to be re-read after every
  /// attempt - the backoff and the connected/disconnected cadence change it. A periodic timer
  /// fixes its interval at creation.
  void _scheduleNext() {
    if (_isDisposed || !_isRunning || !_isForeground) return;

    _timer?.cancel();
    final delay = _hub.isConnected ? connectedPollInterval : _failureInterval;
    _timer = Timer(delay, () async {
      await refresh();
      _scheduleNext();
    });
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    final isForeground = state == AppLifecycleState.resumed;
    if (isForeground == _isForeground) return;
    _isForeground = isForeground;

    if (!_isRunning) return;
    if (isForeground) {
      unawaited(refresh().then((_) => _scheduleNext()));
    } else {
      _timer?.cancel();
      _timer = null;
    }
  }

  @override
  void dispose() {
    _isDisposed = true;
    _timer?.cancel();
    _session.removeListener(_onSessionChanged);
    WidgetsBinding.instance.removeObserver(this);
    unawaited(_pushSubscription.cancel());
    unawaited(_connectionSubscription.cancel());
    unawaited(_hub.dispose());
    super.dispose();
  }
}

class _Attempt<T> {
  final T? value;
  final Object? error;
  final bool skipped;

  const _Attempt({this.value, this.error}) : skipped = false;
  const _Attempt.skipped()
      : value = null,
        error = null,
        skipped = true;
}
