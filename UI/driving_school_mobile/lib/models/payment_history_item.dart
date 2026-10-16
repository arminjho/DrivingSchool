import '../core/json_helpers.dart';

/// Mirrors the backend's `PaymentHistoryItemDto`.
class PaymentHistoryItem {
  final String type;
  final String label;
  final double amount;
  final DateTime dateUtc;

  const PaymentHistoryItem({
    required this.type,
    required this.label,
    required this.amount,
    required this.dateUtc,
  });

  factory PaymentHistoryItem.fromJson(Map<String, dynamic> json) => PaymentHistoryItem(
      type: json['type'] as String,
      label: json['label'] as String,
      amount: (json['amount'] as num).toDouble(),
      dateUtc: parseUtcDateTime(json['dateUtc'] as String),
      );
}
