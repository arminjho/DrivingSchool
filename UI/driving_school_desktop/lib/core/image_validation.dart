import 'dart:typed_data';

import '../l10n/app_strings.dart';

/// A picked image waiting to be uploaded.
class PickedImage {
  final String name;
  final Uint8List bytes;

  const PickedImage({required this.name, required this.bytes});
}

/// Checks an image the way the API will (type by extension AND magic bytes, size), so the user
/// is told under the field instead of after a failed upload. A renamed file is rejected because
/// the bytes, not the name, decide.
abstract final class ImageValidator {
  static const int maxBytes = 5 * 1024 * 1024;
  static const Set<String> _extensions = {'jpg', 'jpeg', 'png', 'webp'};

  static String? validate(String fileName, Uint8List bytes) {
    final extension = fileName.contains('.') ? fileName.split('.').last.toLowerCase() : '';
    if (!_extensions.contains(extension) || !_hasKnownSignature(bytes)) {
      return AppStrings.imageTypeInvalid;
    }
    if (bytes.length > maxBytes) return AppStrings.imageTooLarge(maxBytes ~/ (1024 * 1024));
    return null;
  }

  static bool _hasKnownSignature(Uint8List b) {
    if (b.length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true; // JPEG
    if (b.length >= 8 && _matches(b, 0, const [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return true; // PNG
    // WEBP is a RIFF container: "RIFF" + size + "WEBP" - checking only "RIFF" would accept any WAV/AVI.
    return b.length >= 12 && _matches(b, 0, const [0x52, 0x49, 0x46, 0x46]) && _matches(b, 8, const [0x57, 0x45, 0x42, 0x50]);
  }

  static bool _matches(Uint8List bytes, int offset, List<int> expected) {
    for (var i = 0; i < expected.length; i++) {
      if (bytes[offset + i] != expected[i]) return false;
    }
    return true;
  }
}
