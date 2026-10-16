import 'package:intl/intl.dart';

/// How dates, times and money are shown. The API speaks UTC; people read local time.
abstract final class AppFormat {
  static final DateFormat _date = DateFormat('dd.MM.yyyy.');
  static final DateFormat _time = DateFormat('HH:mm');
  static final DateFormat _dateTime = DateFormat('dd.MM.yyyy. HH:mm');

  static String date(DateTime value) => _date.format(value.toLocal());
  static String time(DateTime value) => _time.format(value.toLocal());
  static String dateTime(DateTime value) => _dateTime.format(value.toLocal());

  static String timeOfDay(int hour, int minute) =>
      '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}';

  /// Amounts are kept and shown in KM; PayPal's EUR conversion never reaches the UI.
  static String money(double amount) => '${amount.toStringAsFixed(2)} KM';
}
