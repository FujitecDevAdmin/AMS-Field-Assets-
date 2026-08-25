import 'package:flutter/material.dart';

class FujitecHeaderLogo extends StatelessWidget {
  const FujitecHeaderLogo({
    this.width = 108,
    this.height = 32,
    this.alignment = Alignment.centerLeft,
    super.key,
  });

  final double width;
  final double height;
  final AlignmentGeometry alignment;

  @override
  Widget build(BuildContext context) => Semantics(
    label: 'Fujitec — Moving freely. Elevating lives.',
    image: true,
    child: SizedBox(
      width: width,
      height: height,
      child: Image.asset(
        'assets/images/fujitec-logo-transparent.png',
        fit: BoxFit.contain,
        alignment: alignment,
        filterQuality: FilterQuality.high,
      ),
    ),
  );
}
