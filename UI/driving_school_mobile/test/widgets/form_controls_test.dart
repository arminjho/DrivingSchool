import 'dart:async';

import 'package:driving_school_mobile/core/api_exception.dart';
import 'package:driving_school_mobile/core/form_errors.dart';
import 'package:driving_school_mobile/core/validators.dart';
import 'package:driving_school_mobile/l10n/app_strings.dart';
import 'package:driving_school_mobile/widgets/ui/api_dropdown_field.dart';
import 'package:driving_school_mobile/widgets/ui/app_action_button.dart';
import 'package:driving_school_mobile/widgets/ui/app_date_fields.dart';
import 'package:driving_school_mobile/widgets/ui/app_dialog.dart';
import 'package:driving_school_mobile/widgets/ui/app_text_field.dart';
import 'package:driving_school_mobile/widgets/ui/lesson_status_badge.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

import '../support/test_support.dart';

Widget _form(GlobalKey<FormBuilderState> key, List<Widget> children) =>
    themed(FormBuilder(key: key, child: SingleChildScrollView(child: Column(children: children))));

void main() {
  group('LessonStatusBadge', () {
    test('uses the Bosnian labels and the agreed colour for each status', () {
      expect(LessonStatusBadge.labelFor('Pending'), 'Na čekanju');
      expect(LessonStatusBadge.labelFor('Confirmed'), 'Potvrđen');
      expect(LessonStatusBadge.labelFor('Completed'), 'Završen');
      expect(LessonStatusBadge.labelFor('Cancelled'), 'Otkazan');
      expect(LessonStatusBadge.toneFor('Pending').name, 'warning'); // orange
      expect(LessonStatusBadge.toneFor('Confirmed').name, 'info'); // blue
      expect(LessonStatusBadge.toneFor('Completed').name, 'success'); // green
      expect(LessonStatusBadge.toneFor('Cancelled').name, 'danger'); // red
    });

    testWidgets('renders the label', (tester) async {
      await tester.pumpWidget(themed(const LessonStatusBadge(status: 'Confirmed')));

      expect(find.text('Potvrđen'), findsOneWidget);
    });
  });

  group('AppActionButton', () {
    testWidgets('an available button runs its action', (tester) async {
      var taps = 0;
      await tester.pumpWidget(themed(AppActionButton(label: 'Sačuvaj', onPressed: () => taps++)));

      await tester.tap(find.text('Sačuvaj'));

      expect(taps, 1);
    });

    testWidgets('a disabled button stays visible, does nothing, and explains why on tap', (tester) async {
      var taps = 0;
      await tester.pumpWidget(themed(AppActionButton(
        label: 'Otkaži čas',
        onPressed: () => taps++,
        disabledReason: 'Čas se može otkazati najkasnije 24h prije termina.',
      )));

      await tester.tap(find.text('Otkaži čas'));
      await tester.pump(const Duration(milliseconds: 50));

      expect(taps, 0);
      expect(find.text('Čas se može otkazati najkasnije 24h prije termina.'), findsOneWidget);
      expect(tester.widget<FilledButton>(find.byType(FilledButton)).onPressed, isNull);
    });

    testWidgets('while loading it is disabled and shows a spinner', (tester) async {
      await tester.pumpWidget(themed(AppActionButton(label: 'Prijavi se', onPressed: () {}, isLoading: true)));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(tester.widget<FilledButton>(find.byType(FilledButton)).onPressed, isNull);
    });
  });

  group('showConfirmDialog', () {
    final results = <bool>[];

    Future<void> open(WidgetTester tester, {bool destructive = false}) async {
      results.clear();
      await tester.pumpWidget(themed(Builder(
        builder: (context) => TextButton(
          onPressed: () async => results.add(await showConfirmDialog(
            context: context,
            title: 'Potvrda brisanja',
            message: 'Obrisati vozilo?',
            confirmLabel: 'Obriši',
            destructive: destructive,
          )),
          child: const Text('otvori'),
        ),
      )));
      await tester.tap(find.text('otvori'));
      await tester.pumpAndSettle();
    }

    testWidgets('names the action by its verb and returns true when confirmed', (tester) async {
      await open(tester, destructive: true);

      expect(find.text('Obrisati vozilo?'), findsOneWidget);
      await tester.tap(find.text('Obriši'));
      await tester.pumpAndSettle();

      expect(results, [true]);
      expect(find.text('Obrisati vozilo?'), findsNothing);
    });

    testWidgets('cancel means no', (tester) async {
      await open(tester);

      await tester.tap(find.text(AppStrings.cancel));
      await tester.pumpAndSettle();

      expect(results, [false]);
    });

    testWidgets('dismissing it by tapping outside also means no', (tester) async {
      await open(tester);

      await tester.tapAt(const Offset(5, 5));
      await tester.pumpAndSettle();

      expect(results, [false]);
    });
  });

  group('ApiDropdownField', () {
    final cities = [(1, 'Sarajevo'), (2, 'Mostar')];

    Widget field(Future<List<(int, String)>> Function() load, {int? initial, String? empty}) => ApiDropdownField<(int, String)>(
          name: 'cityId',
          label: 'Grad',
          loadItems: load,
          idOf: (c) => c.$1,
          labelOf: (c) => c.$2,
          initialValue: initial,
          emptyMessage: empty,
          validator: AppValidators.requiredSelection<int>(),
        );

    testWidgets('loads its options from the API and the form value is the chosen id', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [field(() async => cities)]));
      await tester.pumpAndSettle();

      await tester.tap(find.byType(DropdownButton<int>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Mostar').last);
      await tester.pumpAndSettle();

      expect(key.currentState!.saveAndValidate(), isTrue);
      expect(key.currentState!.value['cityId'], 2);
    });

    testWidgets('shows a loading state, disabled, until the call completes', (tester) async {
      final completer = Completer<List<(int, String)>>();
      await tester.pumpWidget(_form(GlobalKey<FormBuilderState>(), [field(() => completer.future)]));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text(AppStrings.dropdownLoading), findsOneWidget);

      completer.complete(cities);
      await tester.pumpAndSettle();
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('a failed load says so under the control and offers a retry that works', (tester) async {
      var attempts = 0;
      await tester.pumpWidget(_form(GlobalKey<FormBuilderState>(), [
        field(() async {
          attempts++;
          if (attempts == 1) throw const ApiException(statusCode: 500, message: 'x');
          return cities;
        }),
      ]));
      await tester.pumpAndSettle();

      expect(find.textContaining(AppStrings.dropdownLoadFailed), findsOneWidget);

      await tester.tap(find.byTooltip(AppStrings.retry));
      await tester.pumpAndSettle();

      expect(find.textContaining(AppStrings.dropdownLoadFailed), findsNothing);
      expect(attempts, 2);
    });

    testWidgets('when there is nothing to choose it says what is missing instead of an empty list', (tester) async {
      await tester.pumpWidget(_form(GlobalKey<FormBuilderState>(), [field(() async => [], empty: 'Prvo dodajte državu.')]));
      await tester.pumpAndSettle();

      expect(find.text('Prvo dodajte državu.'), findsOneWidget);
    });

    testWidgets('the pick-one message appears under the control when submitted empty', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [field(() async => cities)]));
      await tester.pumpAndSettle();

      expect(key.currentState!.saveAndValidate(), isFalse);
      await tester.pump();

      expect(find.text(AppStrings.selectRequired), findsOneWidget);
    });
  });

  group('AppDateField / AppTimeField', () {
    testWidgets('picking a date shows it as DD.MM.GGGG. and puts it in the form value', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [AppDateField(name: 'date', label: 'Datum', initialValue: DateTime(2026, 5, 15), firstDate: DateTime(2026), lastDate: DateTime(2026, 12, 31))]));

      expect(find.text('15.05.2026.'), findsOneWidget);

      await tester.tap(find.text('15.05.2026.'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('20'));
      await tester.pump();
      await tester.tap(find.byType(TextButton).last);
      await tester.pumpAndSettle();

      expect(find.text('20.05.2026.'), findsOneWidget);
      expect(key.currentState!.fields['date']!.value, DateTime(2026, 5, 20));
    });

    testWidgets('a missing required date is reported under the field with the expected format', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [
        AppDateField(name: 'date', label: 'Datum', validator: (value) => value == null ? AppStrings.dateRequired : null),
      ]));

      expect(key.currentState!.saveAndValidate(), isFalse);
      await tester.pump();

      expect(find.text(AppStrings.dateRequired), findsOneWidget);
      expect(AppStrings.dateRequired, contains('DD.MM.GGGG.'));
    });

    testWidgets('the time field displays HH:MM', (tester) async {
      await tester.pumpWidget(_form(GlobalKey<FormBuilderState>(), [
        const AppTimeField(name: 'time', label: 'Vrijeme', initialValue: TimeOfDay(hour: 8, minute: 5)),
      ]));

      expect(find.text('08:05'), findsOneWidget);
    });
  });

  group('applyApiErrors (backend validation under the matching field)', () {
    testWidgets('puts each message under its own control and returns no banner', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [
        const AppTextField(name: 'email', label: 'E-mail'),
        const AppTextField(name: 'phoneNumber', label: 'Telefon'),
      ]));

      final banner = applyApiErrors(key, const ApiException(statusCode: 400, message: 'x', fieldErrors: {
        'email': ['Korisnik sa ovom e-mail adresom već postoji.'],
        'phoneNumber': ['Broj telefona mora biti u formatu +387 6X XXX XXX.'],
      }));
      await tester.pump();

      expect(banner, isNull);
      expect(find.text('Korisnik sa ovom e-mail adresom već postoji.'), findsOneWidget);
      expect(find.text('Broj telefona mora biti u formatu +387 6X XXX XXX.'), findsOneWidget);
    });

    testWidgets('a rule that belongs to no field is returned for a banner, not lost', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [const AppTextField(name: 'email', label: 'E-mail')]));

      final banner = applyApiErrors(key, const ApiException(statusCode: 400, message: 'x', fieldErrors: {
        'email': ['Neispravan e-mail.'],
        'instructorId': ['Odabrani instruktor nije dostupan.'],
      }));

      expect(banner, 'Odabrani instruktor nije dostupan.');
    });

    testWidgets('a business-rule failure with no field errors is turned into an actionable message', (tester) async {
      final key = GlobalKey<FormBuilderState>();
      await tester.pumpWidget(_form(key, [const AppTextField(name: 'email', label: 'E-mail')]));

      final banner = applyApiErrors(key, const ApiException(statusCode: 409, message: 'Termin je zauzet.'));

      expect(banner, 'Termin je zauzet.');
    });
  });
}
