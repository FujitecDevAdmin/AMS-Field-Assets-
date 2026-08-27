import 'package:ams_audit/core/theme/responsive_typography.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

class _NonlinearScaler extends TextScaler {
  const _NonlinearScaler();

  @override
  double scale(double fontSize) => fontSize * (fontSize < 20 ? 1.8 : 1.3);

  @override
  double get textScaleFactor => 1.8;
}

void main() {
  test('preserves nonlinear accessibility scaling at each font size', () {
    const scaler = ResponsiveTextScaler(_NonlinearScaler(), 1.1);
    expect(scaler.scale(14), closeTo(14 * 1.8 * 1.1, 0.001));
    expect(scaler.scale(28), closeTo(28 * 1.3 * 1.1, 0.001));
  });

  for (final width in [320.0, 360.0, 412.0, 600.0]) {
    for (final systemScale in [1.0, 1.5, 2.0]) {
      testWidgets('readable controls at width $width, scale $systemScale', (
        tester,
      ) async {
        tester.view.physicalSize = Size(width, 850);
        tester.view.devicePixelRatio = 1;
        addTearDown(tester.view.resetPhysicalSize);
        addTearDown(tester.view.resetDevicePixelRatio);
        late TextScaler effectiveScaler;
        await tester.pumpWidget(
          MaterialApp(
            builder: (context, child) => MediaQuery(
              data: MediaQuery.of(
                context,
              ).copyWith(textScaler: TextScaler.linear(systemScale)),
              child: ResponsiveTypography(child: child!),
            ),
            home: Scaffold(
              body: Builder(
                builder: (context) {
                  effectiveScaler = MediaQuery.textScalerOf(context);
                  return ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      const Text(
                        'Asset details',
                        style: TextStyle(fontSize: 20),
                      ),
                      const TextField(
                        decoration: InputDecoration(
                          hintText: 'Search assets',
                          constraints: BoxConstraints(minHeight: 48),
                        ),
                      ),
                      SizedBox(
                        height: responsiveControlHeight(context),
                        child: const Center(child: Text('Select location')),
                      ),
                    ],
                  );
                },
              ),
            ),
          ),
        );
        final widthFactor = (1 + (width - 360) / 2000).clamp(1.0, 1.12);
        expect(
          effectiveScaler.scale(14),
          closeTo(14 * systemScale * widthFactor, 0.001),
        );
        expect(tester.takeException(), isNull);
        await tester.showKeyboard(find.byType(TextField));
        await tester.pump();
        expect(tester.takeException(), isNull);
      });
    }
  }
}
