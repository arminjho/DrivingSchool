import 'package:flutter/foundation.dart';
import 'package:material_ui/material_ui.dart';
import 'package:webview_flutter/webview_flutter.dart';

import '../../l10n/app_strings.dart';
import '../../widgets/ui/app_states.dart';

/// 4.2.10's in-app PayPal approval - never an external-browser handoff with no return. PayPal's
/// `PAYPAL_RETURN_URL`/`PAYPAL_CANCEL_URL` are deliberately non-resolving sentinel addresses
/// (`eAutoskola.Infrastructure.Configuration.PayPalOptions`); this screen's `NavigationDelegate`
/// intercepts the attempt to navigate to either one and closes with a result *before* the
/// WebView ever tries to actually load them.
class PaymentWebViewScreen extends StatefulWidget {
  const PaymentWebViewScreen({super.key, required this.approvalUrl});

  final String approvalUrl;

  @override
  State<PaymentWebViewScreen> createState() => _PaymentWebViewScreenState();
}

/// `webview_flutter` only ships platform implementations for Android and iOS. On any other
/// target, merely constructing a `WebViewController` trips a framework assertion - checked up
/// front so an unsupported host (a desktop test run, say) degrades to an explanation instead of
/// a crash.
bool get _webViewSupported =>
    !kIsWeb && (defaultTargetPlatform == TargetPlatform.android || defaultTargetPlatform == TargetPlatform.iOS);

class _PaymentWebViewScreenState extends State<PaymentWebViewScreen> {
  static const _returnPathSegment = '/api/payment/paypal/return';
  static const _cancelPathSegment = '/api/payment/paypal/cancel';

  WebViewController? _controller;

  /// PayPal's redirect chain can trigger `onNavigationRequest` more than once for the same
  /// sentinel URL; without this guard a second call would pop an already-popped route.
  bool _resultHandled = false;

  @override
  void initState() {
    super.initState();
    if (!_webViewSupported) return;
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(NavigationDelegate(
        onNavigationRequest: (request) {
          if (request.url.contains(_returnPathSegment)) return _finish(true);
          if (request.url.contains(_cancelPathSegment)) return _finish(false);
          return NavigationDecision.navigate;
        },
      ))
      ..loadRequest(Uri.parse(widget.approvalUrl));
  }

  NavigationDecision _finish(bool approved) {
    if (!_resultHandled && mounted) {
      _resultHandled = true;
      Navigator.of(context).pop(approved);
    }
    return NavigationDecision.prevent;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(AppStrings.payWithPayPal),
        leading: IconButton(
          icon: const Icon(Icons.close_rounded),
          onPressed: () {
            if (_resultHandled) return;
            _resultHandled = true;
            Navigator.of(context).pop(false);
          },
        ),
      ),
      body: _controller == null
          ? const AppEmptyState(
              icon: Icons.phone_android_rounded,
              title: AppStrings.paymentUnavailableTitle,
              message: AppStrings.paymentUnavailableMessage,
            )
          : WebViewWidget(controller: _controller!),
    );
  }
}
