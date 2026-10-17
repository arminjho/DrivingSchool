import 'dart:typed_data';

import 'package:driving_school_mobile/core/image_validation.dart';
import 'package:driving_school_mobile/core/validators.dart';
import 'package:driving_school_mobile/l10n/app_strings.dart';
import 'package:flutter_test/flutter_test.dart';

Uint8List _padded(List<int> header, [int total = 64]) =>
    Uint8List.fromList([...header, ...List.filled(total - header.length, 0)]);

final Uint8List _jpeg = _padded([0xFF, 0xD8, 0xFF, 0xE0]);
final Uint8List _png = _padded([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
final Uint8List _webp = _padded([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50]);
final Uint8List _wav = _padded([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x41, 0x56, 0x45]);

void main() {
  group('AppValidators', () {
    test('required rejects null, empty and blank text with the Bosnian message', () {
      final validate = AppValidators.required<String>();

      expect(validate(null), AppStrings.required);
      expect(validate(''), AppStrings.required);
      expect(validate('   '), AppStrings.required);
      expect(validate('x'), isNull);
    });

    test('email states the expected format, and may be optional', () {
      final required = AppValidators.email();

      expect(required('ime@domena.com'), isNull);
      expect(required('ime@domena'), AppStrings.invalidEmail);
      expect(required(''), AppStrings.invalidEmail);
      expect(AppValidators.email(required: false)(''), isNull);
      expect(AppStrings.invalidEmail, contains('ime@domena.com'));
    });

    test('phone uses the same pattern as the backend (+387 6X XXX XXX)', () {
      final validate = AppValidators.phone();

      expect(validate('+387 61 123 456'), isNull);
      expect(validate('+38761123456'), isNull);
      expect(validate('061 123 456'), AppStrings.invalidPhone);
      expect(validate('+387 71 123 456'), AppStrings.invalidPhone);
      expect(AppStrings.invalidPhone, contains('+387 6X XXX XXX'));
    });

    test('password needs at least 8 characters; confirmation must match', () {
      expect(AppValidators.password()('1234567'), AppStrings.passwordTooShort);
      expect(AppValidators.password()('12345678'), isNull);
      expect(AppValidators.sameAs(() => 'abc')('abd'), AppStrings.passwordsDoNotMatch);
      expect(AppValidators.sameAs(() => 'abc')('abc'), isNull);
    });

    test('decimal accepts a comma or a dot and enforces its range', () {
      final price = AppValidators.decimal(min: 0, max: 10000);

      expect(price('120.50'), isNull);
      expect(price('120,50'), isNull);
      expect(price('abc'), AppStrings.invalidNumber);
      expect(price('-1'), AppStrings.numberAtLeast(0));
      expect(price('10001'), AppStrings.numberAtMost(10000));
      expect(price(''), AppStrings.required);
    });

    test('wholeNumber rejects decimals and out-of-range values', () {
      final year = AppValidators.wholeNumber(min: 1980, max: 2100);

      expect(year('2021'), isNull);
      expect(year('20.5'), AppStrings.invalidWholeNumber);
      expect(year('1900'), AppStrings.numberAtLeast(1980));
    });

    test('maxLength reports the label and the limit', () {
      expect(AppValidators.maxLength(5, 'Naziv')('abcdef'), AppStrings.maxLength('Naziv', 5));
      expect(AppValidators.maxLength(5, 'Naziv')('abcde'), isNull);
    });

    test('compose reports only the first failure', () {
      final validate = AppValidators.compose<String>([AppValidators.required(), AppValidators.maxLength(3, 'Šifra')]);

      expect(validate(''), AppStrings.required);
      expect(validate('abcd'), AppStrings.maxLength('Šifra', 3));
      expect(validate('abc'), isNull);
    });

    test('a selection that is missing asks the user to pick, not to type', () {
      expect(AppValidators.requiredSelection<int>()(null), AppStrings.selectRequired);
      expect(AppValidators.requiredSelection<int>()(3), isNull);
    });
  });

  group('ImageValidator', () {
    test('accepts JPEG, PNG and WEBP whose bytes match their extension', () {
      expect(ImageValidator.validate('a.jpg', _jpeg), isNull);
      expect(ImageValidator.validate('a.JPEG', _jpeg), isNull);
      expect(ImageValidator.validate('a.png', _png), isNull);
      expect(ImageValidator.validate('a.webp', _webp), isNull);
    });

    test('judges by the bytes, not the name: a renamed file is rejected', () {
      final text = Uint8List.fromList('<script>alert(1)</script>'.codeUnits);

      expect(ImageValidator.validate('virus.jpg', text), AppStrings.imageTypeInvalid);
    });

    test('a WAV is not a WEBP just because both are RIFF containers', () {
      expect(ImageValidator.validate('sound.webp', _wav), AppStrings.imageTypeInvalid);
    });

    test('rejects a disallowed extension even with valid image bytes', () {
      expect(ImageValidator.validate('a.gif', _jpeg), AppStrings.imageTypeInvalid);
      expect(ImageValidator.validate('noextension', _jpeg), AppStrings.imageTypeInvalid);
    });

    test('rejects a file over 5 MB and says so', () {
      final big = Uint8List(ImageValidator.maxBytes + 1)..setRange(0, 4, [0xFF, 0xD8, 0xFF, 0xE0]);

      expect(ImageValidator.validate('a.jpg', big), AppStrings.imageTooLarge(5));
      expect(AppStrings.imageTooLarge(5), contains('5 MB'));
    });

    test('rejects an empty file', () {
      expect(ImageValidator.validate('a.jpg', Uint8List(0)), AppStrings.imageTypeInvalid);
    });
  });
}
