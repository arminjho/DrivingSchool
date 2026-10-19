import 'package:intl/intl.dart';

/// How dates, times and money are shown. The API speaks UTC; people read local time.
abstract final class AppFormat {
  static final DateFormat _date = DateFormat('dd.MM.yyyy.');
  static final DateFormat _time = DateFormat('HH:mm');
  static final DateFormat _dateTime = DateFormat('dd.MM.yyyy. HH:mm');

  static String date(DateTime value) => _date.format(value.toLocal());
  static String time(DateTime value) => _time.format(value.toLocal());
  static String dateTime(DateTime value) => _dateTime.format(value.toLocal());

  /// "10:00 - 11:00" - how a lesson's slot reads on a card.
  static String timeRange(DateTime start, DateTime end) => '${time(start)} - ${time(end)}';

  /// "15.05.2026. 10:00 - 11:00"
  static String dateTimeRange(DateTime start, DateTime end) => '${date(start)} ${timeRange(start, end)}';

  static String timeOfDay(int hour, int minute) =>
      '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}';

  /// Amounts are kept and shown in KM; PayPal's EUR conversion never reaches the UI.
  static String money(double amount) => '${amount.toStringAsFixed(2)} KM';
}
