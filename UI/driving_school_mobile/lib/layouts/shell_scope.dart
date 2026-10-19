import 'package:material_ui/material_ui.dart';

/// Wraps the body of the app shell. A screen that is both a bottom-navigation destination and a
/// route pushed from the home screen asks [isInside] whether the shell's app bar is already above
/// it: inside the shell it must not draw a second one, while the pushed copy keeps its own (with
/// the back button).
class ShellScope extends InheritedWidget {
  const ShellScope({super.key, required super.child});

  static bool isInside(BuildContext context) => context.getInheritedWidgetOfExactType<ShellScope>() != null;

  @override
  bool updateShouldNotify(ShellScope oldWidget) => false;
}
