import 'cost_breakdown_item.dart';
import 'next_installment.dart';
import 'payment_history_item.dart';

/// Mirrors the backend's `FinanceSummaryDto`.
class FinanceSummary {
  final List<CostBreakdownItem> costBreakdown;
  final List<PaymentHistoryItem> paymentHistory;
  final double remainingBalance;
  final NextInstallment? nextInstallment;

  const FinanceSummary({
    required this.costBreakdown,
    required this.paymentHistory,
    required this.remainingBalance,
    this.nextInstallment,
  });

  factory FinanceSummary.fromJson(Map<String, dynamic> json) => FinanceSummary(
      costBreakdown: (json['costBreakdown'] as List<dynamic>? ?? const []).map((item) => CostBreakdownItem.fromJson(item as Map<String, dynamic>)).toList(),
      paymentHistory: (json['paymentHistory'] as List<dynamic>? ?? const []).map((item) => PaymentHistoryItem.fromJson(item as Map<String, dynamic>)).toList(),
      remainingBalance: (json['remainingBalance'] as num).toDouble(),
      nextInstallment: json['nextInstallment'] == null ? null : NextInstallment.fromJson(json['nextInstallment'] as Map<String, dynamic>),
      );
}
