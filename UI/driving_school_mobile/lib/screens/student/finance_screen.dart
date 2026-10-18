import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/api_exception.dart';
import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';
import '../../core/error_text.dart';
import '../../core/formatters.dart';
import '../../l10n/app_strings.dart';
import '../../models/finance_summary.dart';
import '../../providers/finance_api.dart';
import '../../providers/payment_api.dart';
import '../../widgets/ui/app_card.dart';
import '../../widgets/ui/app_messages.dart';
import '../../widgets/ui/app_states.dart';
import '../payment/payment_webview_screen.dart';

/// 4.2.10 - cost breakdown, payment history, remaining balance, and "Plati sljedeću ratu"
/// triggering the in-app PayPal flow. The server owns the catalog/price throughout; this screen
/// never computes or sends an amount of its own.
class FinanceScreen extends StatefulWidget {
  const FinanceScreen({super.key});

  @override
  State<FinanceScreen> createState() => _FinanceScreenState();
}

class _FinanceScreenState extends State<FinanceScreen> {
  late final FinanceApi _financeApi;
  late final PaymentApi _paymentApi;

  FinanceSummary? _summary;
  String? _error;
  bool _isPaying = false;

  @override
  void initState() {
    super.initState();
    final session = context.read<AuthSession>();
    _financeApi = FinanceApi(session);
    _paymentApi = PaymentApi(session);
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final summary = await _financeApi.getMy();
      if (mounted) setState(() => _summary = summary);
    } on Object catch (error) {
      if (mounted) setState(() => _error = failureCause(error));
    }
  }

  Future<void> _pay() async {
    final installment = _summary?.nextInstallment;
    if (installment == null) return;

    setState(() => _isPaying = true);
    try {
      final order = await _paymentApi.createOrder(installment.installmentId);
      if (!mounted) return;
      final approved = await Navigator.of(context).push<bool>(
        MaterialPageRoute(builder: (_) => PaymentWebViewScreen(approvalUrl: order.approvalUrl)),
      );

      if (approved != true) {
        if (mounted) showErrorMessage(context, const ApiException(statusCode: 400, message: AppStrings.paymentCancelledMessage));
        return;
      }

      final capture = await _paymentApi.capture(order.orderId);
      if (!mounted) return;
      if (capture.status == 'Completed') {
        showSuccessMessage(context, AppStrings.paymentSuccess);
        await _load();
      } else {
        showErrorMessage(context, const ApiException(statusCode: 400, message: AppStrings.paymentFailed));
      }
    } on Object catch (error) {
      if (mounted) showErrorMessage(context, error);
    } finally {
      if (mounted) setState(() => _isPaying = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final summary = _summary;

    return Scaffold(
      appBar: AppBar(title: const Text(AppStrings.financeTitle)),
      body: summary == null
          ? Padding(
              padding: AppSpacing.page,
              child: _error != null ? AppErrorState(message: _error!, onRetry: _load) : const Center(child: CircularProgressIndicator()),
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: AppSpacing.page,
                children: [
                  Text(AppStrings.costOverviewTitle, style: context.text.labelLarge),
                  const SizedBox(height: AppSpacing.sm),
                  for (final item in summary.costBreakdown)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                      child: AppCard(
                        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
                        child: Row(
                          children: [
                            Expanded(child: Text(item.description)),
                            Text(AppFormat.money(item.amount), style: context.text.titleSmall?.copyWith(fontFeatures: AppTypography.tabular)),
                          ],
                        ),
                      ),
                    ),
                  const SizedBox(height: AppSpacing.lg),
                  Text(AppStrings.paymentHistoryTitle, style: context.text.labelLarge),
                  const SizedBox(height: AppSpacing.sm),
                  if (summary.paymentHistory.isEmpty)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                      child: Text(AppStrings.paymentHistoryEmpty, style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                    )
                  else
                    for (final item in summary.paymentHistory)
                      Padding(
                        padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                        child: AppCard(
                          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(item.type == 'Refund' ? AppStrings.refundLabel : item.label),
                                    Text(AppFormat.date(item.dateUtc), style: context.text.bodySmall?.copyWith(color: context.colors.textMuted)),
                                  ],
                                ),
                              ),
                              Text(
                                '${item.type == 'Refund' ? '+' : '-'}${AppFormat.money(item.amount)}',
                                style: context.text.titleSmall?.copyWith(color: context.colors.success, fontFeatures: AppTypography.tabular),
                              ),
                            ],
                          ),
                        ),
                      ),
                  const SizedBox(height: AppSpacing.lg),
                  if (summary.nextInstallment == null)
                    Container(
                      padding: const EdgeInsets.all(AppSpacing.md),
                      decoration: BoxDecoration(color: context.colors.successSoft, borderRadius: AppRadius.all(AppRadius.lg)),
                      child: Text(AppStrings.noRemainingBalance, style: context.text.bodyMedium),
                    )
                  else ...[
                    Container(
                      padding: const EdgeInsets.all(AppSpacing.md),
                      decoration: BoxDecoration(color: context.colors.warningSoft, borderRadius: AppRadius.all(AppRadius.lg), border: Border.all(color: context.colors.warning.withValues(alpha: 0.35))),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(AppStrings.remainingBalanceTitle, style: context.text.labelMedium),
                                Text(AppFormat.money(summary.remainingBalance), style: context.text.headlineSmall),
                                Text(AppStrings.nextDueDate(AppFormat.date(summary.nextInstallment!.dueDate)), style: context.text.bodySmall),
                              ],
                            ),
                          ),
                          Icon(Icons.account_balance_wallet_outlined, color: context.colors.warning, size: 28),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    FilledButton(
                      onPressed: _isPaying ? null : _pay,
                      style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(AppSizes.controlHeight)),
                      child: _isPaying
                          ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Text(AppStrings.payNextInstallment),
                    ),
                  ],
                ],
              ),
            ),
    );
  }
}
