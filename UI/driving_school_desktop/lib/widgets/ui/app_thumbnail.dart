import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import '../../core/api_config.dart';
import '../../core/auth_session.dart';
import '../../core/design_tokens.dart';

/// A small rounded picture of a stored image (`GET /api/files?path=`, sent with the session's
/// token) for table rows - a vehicle photo, a question's sign. [fallbackIcon] is shown when the
/// record has no image or it cannot be loaded.
class AppThumbnail extends StatelessWidget {
  final String? imagePath;
  final IconData fallbackIcon;
  final double width;
  final double height;

  const AppThumbnail({super.key, required this.imagePath, required this.fallbackIcon, this.width = 52, this.height = 36});

  @override
  Widget build(BuildContext context) {
    final c = context.colors;
    final placeholder = Container(
      width: width,
      height: height,
      alignment: Alignment.center,
      decoration: BoxDecoration(color: c.surfaceMuted, borderRadius: AppRadius.all(6), border: Border.all(color: c.border)),
      child: Icon(fallbackIcon, size: height * 0.5, color: c.textMuted),
    );

    final path = imagePath;
    if (path == null) return placeholder;

    final token = context.read<AuthSession>().token;
    return ClipRRect(
      borderRadius: AppRadius.all(6),
      child: SizedBox(
        width: width,
        height: height,
        child: Image.network(
          ApiConfig.uri('api/files', {'path': path}).toString(),
          headers: {if (token != null) 'Authorization': 'Bearer $token'},
          fit: BoxFit.cover,
          cacheWidth: (width * 2).round(),
          errorBuilder: (context, error, stackTrace) => placeholder,
          loadingBuilder: (context, child, progress) => progress == null ? child : placeholder,
        ),
      ),
    );
  }
}
