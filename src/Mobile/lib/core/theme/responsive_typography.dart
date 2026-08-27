import 'package:flutter/material.dart';

/// Applies a modest logical-width adjustment without replacing Android's
/// nonlinear accessibility text scaling or reducing text on smaller phones.
class ResponsiveTypography extends StatelessWidget {
  const ResponsiveTypography({required this.child, super.key});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final media = MediaQuery.of(context);
    final width = media.size.shortestSide;
    final factor = (1 + (width - 360) / 2000).clamp(1.0, 1.12);
    return MediaQuery(
      data: media.copyWith(
        textScaler: ResponsiveTextScaler(media.textScaler, factor),
      ),
      child: child,
    );
  }
}

class ResponsiveTextScaler extends TextScaler {
  const ResponsiveTextScaler(this.systemScaler, this.widthFactor);

  final TextScaler systemScaler;
  final double widthFactor;

  @override
  double scale(double fontSize) => systemScaler.scale(fontSize) * widthFactor;

  @override
  double get textScaleFactor => scale(14) / 14;

  @override
  bool operator ==(Object other) =>
      other is ResponsiveTextScaler &&
      other.systemScaler == systemScaler &&
      other.widthFactor == widthFactor;

  @override
  int get hashCode => Object.hash(systemScaler, widthFactor);
}

/// Keeps paired controls aligned while allowing their text to grow.
double responsiveControlHeight(BuildContext context, {double base = 48}) {
  final textHeight = MediaQuery.textScalerOf(context).scale(16);
  return base + (textHeight - 16).clamp(0.0, double.infinity) * 1.5;
}
