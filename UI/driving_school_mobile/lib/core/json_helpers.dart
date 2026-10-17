/// The API sends every date as UTC, but EF-materialised values arrive without a zone suffix
/// ("2026-10-16T12:29:14"), which Dart would read as *local* time. These always yield UTC.
DateTime parseUtcDateTime(String value) {
  if (value.length == 10) {
    final date = DateTime.parse(value);
    return DateTime.utc(date.year, date.month, date.day);
  }

  final hasZone = value.endsWith('Z') || RegExp(r'[+-]\d{2}:?\d{2}$').hasMatch(value);
  return DateTime.parse(hasZone ? value : '${value}Z').toUtc();
}

DateTime? parseUtcDateTimeOrNull(String? value) => value == null ? null : parseUtcDateTime(value);
