import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

class ScannerPage extends StatefulWidget {
  const ScannerPage({super.key});

  @override
  State<ScannerPage> createState() => _ScannerPageState();
}

class _ScannerPageState extends State<ScannerPage>
    with SingleTickerProviderStateMixin {
  static const Color _themeDark = Color(0xFFD01126);
  static const Color _themeLight = Color(0xFFD01126);

  late final MobileScannerController _scannerController;
  late final AnimationController _scanAnimation;
  bool _returned = false;

  @override
  void initState() {
    super.initState();
    _scannerController = MobileScannerController(
      detectionSpeed: DetectionSpeed.noDuplicates,
      formats: const [BarcodeFormat.qrCode, BarcodeFormat.code128],
    );
    _scanAnimation = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 2),
    )..repeat(reverse: true);
  }

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_returned) return;
    final value = capture.barcodes
        .map((code) => code.rawValue)
        .whereType<String>()
        .firstOrNull;
    if (value == null || value.trim().isEmpty) return;
    _returned = true;
    await _scannerController.stop();
    if (mounted) Navigator.of(context).pop(value.trim());
  }

  Future<void> _toggleTorch() async {
    try {
      await _scannerController.toggleTorch();
    } on Exception {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Torch is not available on this camera.')),
      );
    }
  }

  @override
  void dispose() {
    _scanAnimation.dispose();
    unawaited(_scannerController.dispose());
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: const Color(0xFF100E0F),
    body: SafeArea(
      child: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.fromLTRB(8, 8, 12, 12),
            decoration: const BoxDecoration(
              gradient: LinearGradient(colors: [_themeDark, _themeLight]),
            ),
            child: Row(
              children: [
                IconButton(
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.close),
                  color: Colors.white,
                  tooltip: 'Close scanner',
                ),
                const SizedBox(width: 4),
                const Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'SCAN ASSET',
                        style: TextStyle(
                          color: Colors.white70,
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 1.4,
                        ),
                      ),
                      SizedBox(height: 2),
                      Text(
                        'QR & Barcode Scanner',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  onPressed: () => unawaited(_toggleTorch()),
                  icon: const Icon(Icons.flashlight_on_outlined),
                  color: Colors.white,
                  tooltip: 'Toggle torch',
                ),
              ],
            ),
          ),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final scannerSize = math.min(
                  math.min(constraints.maxWidth - 32, 440.0),
                  math.max(220.0, constraints.maxHeight - 190),
                );
                return SingleChildScrollView(
                  padding: const EdgeInsets.fromLTRB(16, 22, 16, 20),
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      minHeight: math.max(0, constraints.maxHeight - 42),
                    ),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(
                          Icons.qr_code_scanner,
                          color: _themeLight,
                          size: 30,
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Align the QR code within the frame',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 16,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 6),
                        const Text(
                          'Hold your device steady. Scanning happens automatically.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: Colors.white60, fontSize: 12),
                        ),
                        const SizedBox(height: 18),
                        Container(
                          width: scannerSize,
                          height: scannerSize,
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(24),
                            boxShadow: const [
                              BoxShadow(
                                color: Color(0x66000000),
                                blurRadius: 28,
                                offset: Offset(0, 14),
                              ),
                            ],
                          ),
                          clipBehavior: Clip.antiAlias,
                          child: Stack(
                            fit: StackFit.expand,
                            children: [
                              MobileScanner(
                                controller: _scannerController,
                                fit: BoxFit.cover,
                                onDetect: (capture) =>
                                    unawaited(_onDetect(capture)),
                                errorBuilder: (context, error) =>
                                    const _CameraError(),
                              ),
                              AnimatedBuilder(
                                animation: _scanAnimation,
                                builder: (context, child) => CustomPaint(
                                  painter: _ScannerOverlayPainter(
                                    progress: _scanAnimation.value,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),
                        OutlinedButton.icon(
                          onPressed: () => Navigator.of(context).pop(),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Colors.white,
                            side: const BorderSide(color: _themeLight),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 24,
                              vertical: 12,
                            ),
                          ),
                          icon: const Icon(Icons.close, size: 18),
                          label: const Text('Cancel scanning'),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    ),
  );
}

class _ScannerOverlayPainter extends CustomPainter {
  const _ScannerOverlayPainter({required this.progress});

  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final frameSize = size.shortestSide * 0.72;
    final frame = Rect.fromCenter(
      center: size.center(Offset.zero),
      width: frameSize,
      height: frameSize,
    );
    final roundedFrame = RRect.fromRectAndRadius(
      frame,
      const Radius.circular(18),
    );
    final outside = Path.combine(
      PathOperation.difference,
      Path()..addRect(Offset.zero & size),
      Path()..addRRect(roundedFrame),
    );
    canvas.drawPath(outside, Paint()..color = const Color(0x99000000));

    final cornerPaint = Paint()
      ..color = const Color(0xFFD01126)
      ..strokeWidth = 4
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke;
    const corner = 30.0;
    final path = Path()
      ..moveTo(frame.left, frame.top + corner)
      ..lineTo(frame.left, frame.top)
      ..lineTo(frame.left + corner, frame.top)
      ..moveTo(frame.right - corner, frame.top)
      ..lineTo(frame.right, frame.top)
      ..lineTo(frame.right, frame.top + corner)
      ..moveTo(frame.right, frame.bottom - corner)
      ..lineTo(frame.right, frame.bottom)
      ..lineTo(frame.right - corner, frame.bottom)
      ..moveTo(frame.left + corner, frame.bottom)
      ..lineTo(frame.left, frame.bottom)
      ..lineTo(frame.left, frame.bottom - corner);
    canvas.drawPath(path, cornerPaint);

    final lineY = frame.top + 16 + (frame.height - 32) * progress;
    final scanPaint = Paint()
      ..shader =
          const LinearGradient(
            colors: [Colors.transparent, Color(0xFFD01126), Colors.transparent],
          ).createShader(
            Rect.fromLTRB(frame.left, lineY - 2, frame.right, lineY + 2),
          )
      ..strokeWidth = 3;
    canvas.drawLine(
      Offset(frame.left + 12, lineY),
      Offset(frame.right - 12, lineY),
      scanPaint,
    );
  }

  @override
  bool shouldRepaint(covariant _ScannerOverlayPainter oldDelegate) =>
      oldDelegate.progress != progress;
}

class _CameraError extends StatelessWidget {
  const _CameraError();

  @override
  Widget build(BuildContext context) => const ColoredBox(
    color: Color(0xFF211D1F),
    child: Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.no_photography_outlined, color: Colors.white, size: 42),
            SizedBox(height: 12),
            Text(
              'Camera could not be opened. Check camera permission and try again.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.white),
            ),
          ],
        ),
      ),
    ),
  );
}
