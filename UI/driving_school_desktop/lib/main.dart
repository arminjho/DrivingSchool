import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import 'core/app_theme.dart';
import 'core/auth_api.dart';
import 'core/auth_session.dart';
import 'core/notification_center.dart';
import 'core/notification_hub.dart';
import 'core/roles.dart';
import 'core/token_store.dart';
import 'l10n/app_strings.dart';
import 'layouts/app_shell.dart';
import 'providers/notification_provider.dart';
import 'screens/login_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // A token saved by a previous run is picked up before the first frame, so a returning user
  // never sees the login screen flash.
  final session = AuthSession(const SecureTokenStore());
  await session.restore();

  runApp(DrivingSchoolDesktopApp(session: session));
}

class DrivingSchoolDesktopApp extends StatelessWidget {
  const DrivingSchoolDesktopApp({super.key, required this.session, this.hub});

  final AuthSession session;

  /// Replaceable so tests can run the app without opening a real connection.
  final NotificationHub? hub;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: session),
        Provider(
          create: (context) => AuthApi(
            session: session,
            allowedRoles: const {Roles.admin},
            roleNotAllowedMessage: AppStrings.roleNotAllowed,
          ),
        ),
        // Above MaterialApp so the bell keeps counting whichever screen is open. It starts and
        // stops itself off the session, so there is nothing to call on login.
        ChangeNotifierProvider(
          create: (context) => NotificationCenter(
            session: session,
            provider: NotificationProvider(session),
            hub: hub ?? SignalRNotificationHub(),
          ),
        ),
      ],
      child: MaterialApp(
        title: AppStrings.appName,
        debugShowCheckedModeBanner: false,
        theme: AppTheme.dark(),
        themeMode: ThemeMode.dark,
        locale: const Locale('bs'),
        supportedLocales: const [Locale('bs')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        // The screen follows the session alone: logging in, logging out, a 401 or token expiry
        // all swap it without any screen calling Navigator.
        home: Consumer<AuthSession>(
          builder: (context, session, child) => session.isLoggedIn ? const AppShell() : const LoginScreen(),
        ),
      ),
    );
  }
}
