/// Mirrors the backend's `CapturePaymentResultDto`.
class CapturePaymentResult {
  final int paymentId;
  final int installmentId;
  final String status;
  final double amount;
  final bool alreadyProcessed;

  const CapturePaymentResult({
    required this.paymentId,
    required this.installmentId,
    required this.status,
    required this.amount,
    required this.alreadyProcessed,
  });

  factory CapturePaymentResult.fromJson(Map<String, dynamic> json) => CapturePaymentResult(
      paymentId: json['paymentId'] as int,
      installmentId: json['installmentId'] as int,
      status: json['status'] as String,
      amount: (json['amount'] as num).toDouble(),
      alreadyProcessed: json['alreadyProcessed'] as bool,
      );
}
