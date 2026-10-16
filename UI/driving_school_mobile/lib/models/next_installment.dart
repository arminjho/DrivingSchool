import '../core/json_helpers.dart';

/// Mirrors the backend's `NextInstallmentDto`.
class NextInstallment {
  final int installmentId;
  final int number;
  final String description;
  final double amount;
  final DateTime dueDate;

  const NextInstallment({
    required this.installmentId,
    required this.number,
    required this.description,
    required this.amount,
    required this.dueDate,
  });

  factory NextInstallment.fromJson(Map<String, dynamic> json) => NextInstallment(
      installmentId: json['installmentId'] as int,
      number: json['number'] as int,
      description: json['description'] as String,
      amount: (json['amount'] as num).toDouble(),
      dueDate: parseUtcDateTime(json['dueDate'] as String),
      );
}
