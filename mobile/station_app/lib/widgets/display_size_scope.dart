import 'package:flutter/material.dart';
import '../models/display_size_mode.dart';

class DisplaySizeScope extends InheritedWidget {
  const DisplaySizeScope({
    super.key,
    required this.scale,
    required super.child,
  });

  final double scale;

  static double scaleOf(BuildContext context) {
    return context
            .dependOnInheritedWidgetOfExactType<DisplaySizeScope>()
            ?.scale ??
        1;
  }

  static double resolve(DisplaySizeMode mode, Size screenSize) {
    if (mode != DisplaySizeMode.automatic) {
      return switch (mode) {
        DisplaySizeMode.compact => 1,
        DisplaySizeMode.normal => 1.25,
        DisplaySizeMode.large => 1.5,
        DisplaySizeMode.automatic => 1,
      };
    }

    if (screenSize.width >= 1600 && screenSize.height >= 850) return 1.5;
    if (screenSize.width >= 1200 && screenSize.height >= 700) return 1.25;
    return 1;
  }

  @override
  bool updateShouldNotify(DisplaySizeScope oldWidget) =>
      scale != oldWidget.scale;
}

extension DisplaySizeContext on BuildContext {
  double displaySize(double value) => value * DisplaySizeScope.scaleOf(this);
}
