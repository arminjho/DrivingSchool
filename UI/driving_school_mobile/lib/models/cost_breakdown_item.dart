/// Mirrors the backend's `CostBreakdownItemDto`.
class CostBreakdownItem {
  final int chargeId;
  final int? lessonId;
  final String description;
  final double amount;
  final bool isPaid;

  const CostBreakdownItem({
    required this.chargeId,
    this.lessonId,
    required this.description,
    required this.amount,
    required this.isPaid,
  });

  factory CostBreakdownItem.fromJson(Map<String, dynamic> json) => CostBreakdownItem(
      chargeId: json['chargeId'] as int,
      lessonId: json['lessonId'] as int?,
      description: json['description'] as String,
      amount: (json['amount'] as num).toDouble(),
      isPaid: json['isPaid'] as bool,
      );
}
