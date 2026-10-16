/// Mirrors the backend's `CreateOrderResultDto`.
class CreateOrderResult {
  final int paymentId;
  final String orderId;
  final String approvalUrl;
  final double amount;
  final double payPalAmount;
  final String payPalCurrency;

  const CreateOrderResult({
    required this.paymentId,
    required this.orderId,
    required this.approvalUrl,
    required this.amount,
    required this.payPalAmount,
    required this.payPalCurrency,
  });

  factory CreateOrderResult.fromJson(Map<String, dynamic> json) => CreateOrderResult(
      paymentId: json['paymentId'] as int,
      orderId: json['orderId'] as String,
      approvalUrl: json['approvalUrl'] as String,
      amount: (json['amount'] as num).toDouble(),
      payPalAmount: (json['payPalAmount'] as num).toDouble(),
      payPalCurrency: json['payPalCurrency'] as String,
      );
}
