import 'package:driving_school_desktop/core/auth_session.dart';
import 'package:driving_school_desktop/l10n/app_strings.dart';
import 'package:driving_school_desktop/main.dart';
import 'package:driving_school_desktop/screens/reference_data/city_tab.dart';
import 'package:driving_school_desktop/screens/reference_data/reference_data_screen.dart';
import 'package:driving_school_desktop/widgets/ui/app_dialog.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../support/test_support.dart';

Future<void> _pumpScreen(WidgetTester tester, Widget screen, Future<http.Response> Function(http.Request request) api) async {
  tester.view.physicalSize = const Size(1400, 900);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  installClient(api);
  final session = signedInSession();
  onTestEnd(session.dispose);

  await tester.pumpWidget(themed(screen, wrap: (app) => ChangeNotifierProvider<AuthSession>.value(value: session, child: app)));
  await tester.pumpAndSettle();
}

Map<String, dynamic> _page(List<Map<String, dynamic>> rows) => {'resultList': rows, 'count': rows.length};

void main() {
  group('an add-form whose reference table is empty', () {
    testApp('is not opened: the message says what to add first', (tester) async {
      await _pumpScreen(tester, const CityTab(), (request) async => jsonResponse(_page([])));

      // The empty table offers the same action again in its empty state; either one is the add button.
      await tester.tap(find.text(AppStrings.addCity).first);
      await tester.pumpAndSettle();

      expect(find.byType(AppDialog), findsNothing);
      expect(find.text(AppStrings.noCountriesPrefix), findsOneWidget);
    });

    testApp('is opened as soon as the table has a record', (tester) async {
      await _pumpScreen(tester, const CityTab(), (request) async {
        if (request.url.path.endsWith('/countries')) return jsonResponse(_page([{'id': 1, 'code': 'BA', 'name': 'Bosna i Hercegovina'}]));
        return jsonResponse(_page([]));
      });

      await tester.tap(find.text(AppStrings.addCity).first);
      await tester.pumpAndSettle();

      expect(find.byType(AppDialog), findsOneWidget);
      expect(find.text(AppStrings.noCountriesPrefix), findsNothing);
    });
  });

  group('reference data', () {
    testApp('the vehicle service types have their own tab with its CRUD table', (tester) async {
      await _pumpScreen(tester, const ReferenceDataScreen(), (request) async {
        if (request.url.path.endsWith('/vehicle-service-types')) {
          return jsonResponse(_page([{'id': 1, 'name': 'Redovni servis'}, {'id': 2, 'name': 'Zamjena ulja'}]));
        }
        return jsonResponse(_page([]));
      });

      // The tab bar scrolls; the eighth tab starts off to the right.
      await tester.ensureVisible(find.text(AppStrings.tabVehicleServiceTypes));
      await tester.pumpAndSettle();
      await tester.tap(find.text(AppStrings.tabVehicleServiceTypes));
      await tester.pumpAndSettle();

      expect(find.text('Redovni servis'), findsOneWidget);
      expect(find.text('Zamjena ulja'), findsOneWidget);
      expect(find.text(AppStrings.addVehicleServiceType), findsOneWidget);
    });
  });

  group('the shell header', () {
    testApp('shows the breadcrumb, the title and the subtitle of the screen', (tester) async {
      installClient((request) async {
        final path = request.url.path;
        if (path.endsWith('unread-count')) return jsonResponse(0);
        return jsonResponse(_page([]));
      });
      final session = AuthSession(MemoryTokenStore(makeJwt()));
      await session.restore();
      onTestEnd(session.dispose);
      await tester.pumpWidget(DrivingSchoolDesktopApp(session: session, hub: FakeHub()));
      await tester.pump();

      await tester.tap(find.text(AppStrings.navReports));
      await tester.pumpAndSettle();

      expect(find.text('${AppStrings.breadcrumbRoot} > ${AppStrings.navReports}'), findsOneWidget);
      expect(find.text(AppStrings.reportsSubtitle), findsOneWidget);
    });
  });
}
