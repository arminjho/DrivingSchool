import 'dart:convert';

import 'package:http/http.dart' as http;

import '../core/api_config.dart';
import '../core/api_http.dart';
import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/capture_payment_result.dart';
import '../models/create_order_result.dart';

/// `POST /api/payments/installments/{id}/create-order` + `POST /api/payments/{orderId}/capture`
/// - PayPal order creation and server-side capture. The Flutter app never records a payment
/// itself; capture is the only thing that marks an installment paid.
class PaymentApi {
  PaymentApi(this._session);

  final AuthSession _session;

  Map<String, String> _authHeaders() => {
        'Content-Type': 'application/json',
        if (_session.token != null) 'Authorization': 'Bearer ${_session.token}',
      };

  Future<CreateOrderResult> createOrder(int installmentId) async {
    final response = await apiPost(ApiConfig.uri('api/payments/installments/$installmentId/create-order'), headers: _authHeaders());
    return _decode(response, CreateOrderResult.fromJson);
  }

  Future<CapturePaymentResult> capture(String orderId) async {
    final response = await apiPost(ApiConfig.uri('api/payments/$orderId/capture'), headers: _authHeaders());
    return _decode(response, CapturePaymentResult.fromJson);
  }

  T _decode<T>(http.Response response, T Function(Map<String, dynamic>) fromJson) {
    if (response.statusCode == 401) _session.clear();
    if (response.statusCode < 200 || response.statusCode >= 300) throw BaseProvider.parseError(response);
    return fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as Map<String, dynamic>);
  }
}
