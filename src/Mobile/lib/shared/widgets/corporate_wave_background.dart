import 'package:flutter/material.dart';

enum CorporateWaveVariant { login, dashboard, audits, history, form, scanner }

/// Shared authenticated-page background.
///
/// The supplied Fujitec artwork remains fixed behind each page while the page
/// content scrolls independently. Login and splash use their own dedicated
/// artwork and do not use this widget.
class CorporateWaveBackground extends StatelessWidget {
  const CorporateWaveBackground({
    required this.child,
    required this.variant,
    this.backgroundColor,
    super.key,
  });

  final Widget child;
  final CorporateWaveVariant variant;
  final Color? backgroundColor;

  @override
  Widget build(BuildContext context) {
    final isScanner = variant == CorporateWaveVariant.scanner;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    if (isScanner) {
      return ColoredBox(
        color: backgroundColor ?? const Color(0xFF100E0F),
        child: child,
      );
    }

    return Stack(
      fit: StackFit.expand,
      children: [
        Image.asset(
          'assets/images/fujitec-app-background.png',
          fit: BoxFit.cover,
          alignment: Alignment.topCenter,
          filterQuality: FilterQuality.high,
          semanticLabel: 'Fujitec corporate application background',
        ),
        ColoredBox(
          color: isDark
              ? const Color(0xD9141213)
              : (backgroundColor ?? Colors.white).withValues(alpha: 0.08),
        ),
        child,
      ],
    );
  }
}
