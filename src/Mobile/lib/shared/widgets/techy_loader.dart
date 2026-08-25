import 'package:flutter/material.dart';

class TechyLoader extends StatelessWidget {
  const TechyLoader({this.size = 40, super.key});

  final double size;

  @override
  Widget build(BuildContext context) => Semantics(
    label: 'Loading',
    liveRegion: true,
    child: SizedBox.square(
      dimension: size,
      child: Image.asset(
        'assets/images/techy-loader.gif',
        fit: BoxFit.contain,
        filterQuality: FilterQuality.high,
        gaplessPlayback: true,
      ),
    ),
  );
}
