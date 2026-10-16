import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../core/auth_session.dart';
import '../core/design_tokens.dart';
import '../l10n/app_strings.dart';
import '../widgets/ui/app_badge.dart';
import '../widgets/ui/app_card.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final session = context.watch<AuthSession>();

    return Padding(
      padding: AppSpacing.page,
      child: Align(
        alignment: Alignment.topLeft,
        child: AppCard(
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              AppAvatar(name: session.fullName, size: 48),
              const SizedBox(width: AppSpacing.md),
              Flexible(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      AppStrings.welcome(session.fullName),
                      style: context.text.headlineSmall,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    AppStatusBadge(label: session.roleLabel, tone: AppTone.primary),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
