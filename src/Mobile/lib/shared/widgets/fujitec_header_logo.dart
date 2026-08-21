import 'package:flutter/material.dart';

class FujitecHeaderLogo extends StatelessWidget {
  const FujitecHeaderLogo({super.key});

  @override
  Widget build(BuildContext context) => Semantics(
    label: 'Fujitec — Moving freely. Elevating lives.',
    image: true,
    child: SizedBox(
      width: 132,
      height: 40,
      child: Image.asset(
        'assets/images/fujitec-header-logo.png',
        fit: BoxFit.contain,
        alignment: Alignment.centerLeft,
        filterQuality: FilterQuality.high,
      ),
    ),
  );
}
