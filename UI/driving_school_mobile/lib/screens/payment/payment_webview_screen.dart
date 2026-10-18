import 'package:flutter/foundation.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:webview_flutter/webview_flutter.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';

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
          ? _ExternalBrowserApproval(
              approvalUrl: widget.approvalUrl,
              onResult: (approved) {
                if (_resultHandled) return;
                _resultHandled = true;
                Navigator.of(context).pop(approved);
              },
            )
          : WebViewWidget(controller: _controller!),
    );
  }
}

/// Fallback for a host with no `webview_flutter` implementation: hand the approval URL to the
/// system browser and let the student self-report when they are done. Safe because it is not
/// trusted - [onResult] only triggers `POST /api/payments/{orderId}/capture`, and the backend
/// captures against PayPal itself.
class _ExternalBrowserApproval extends StatefulWidget {
  const _ExternalBrowserApproval({required this.approvalUrl, required this.onResult});

  final String approvalUrl;
  final ValueChanged<bool> onResult;

  @override
  State<_ExternalBrowserApproval> createState() => _ExternalBrowserApprovalState();
}

class _ExternalBrowserApprovalState extends State<_ExternalBrowserApproval> {
  bool _opened = false;
  String? _launchError;

  Future<void> _open() async {
    setState(() => _launchError = null);
    try {
      final launched = await launchUrl(Uri.parse(widget.approvalUrl), mode: LaunchMode.externalApplication);
      if (!mounted) return;
      setState(() => launched ? _opened = true : _launchError = AppStrings.cannotOpenPayPal);
    } on Object catch (error) {
      if (mounted) setState(() => _launchError = AppStrings.cannotOpenPayPalWithReason(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.xl),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.open_in_new_rounded, size: 44, color: c.textMuted),
            const SizedBox(height: AppSpacing.md),
            Text(AppStrings.payPalOpensInBrowser, textAlign: TextAlign.center, style: context.text.titleMedium),
            const SizedBox(height: AppSpacing.xl),
            FilledButton.icon(
              onPressed: _open,
              icon: const Icon(Icons.open_in_new_rounded, size: 18),
              label: Text(_opened ? AppStrings.reopenPayPal : AppStrings.payWithPayPal),
            ),
            if (_launchError != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Text(_launchError!, textAlign: TextAlign.center, style: TextStyle(color: c.danger)),
            ],
            if (_opened) ...[
              const SizedBox(height: AppSpacing.xl),
              const Divider(),
              const SizedBox(height: AppSpacing.md),
              FilledButton(onPressed: () => widget.onResult(true), child: const Text(AppStrings.confirm)),
              const SizedBox(height: AppSpacing.xs),
              TextButton(onPressed: () => widget.onResult(false), child: const Text(AppStrings.cancel)),
            ],
          ],
        ),
      ),
    );
  }
}
