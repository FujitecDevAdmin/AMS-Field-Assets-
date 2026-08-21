import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../shared/widgets/fujitec_header_logo.dart';
import '../audits/audit_models.dart';
import '../audits/audits_api.dart';

class AuditorDashboardPage extends ConsumerStatefulWidget {
  const AuditorDashboardPage({required this.onOpenAudits, super.key});

  final VoidCallback onOpenAudits;

  @override
  ConsumerState<AuditorDashboardPage> createState() =>
      _AuditorDashboardPageState();
}

class _AuditorDashboardPageState extends ConsumerState<AuditorDashboardPage> {
  static const Color _themeDark = Color(0xFFD01126);
  static const Color _themeLight = Color(0xFFD01126);

  List<AssignedAudit> _audits = const [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final audits = await ref.read(auditsApiProvider).listMine();
      if (!mounted) return;
      setState(() {
        _audits = audits;
        _loading = false;
      });
    } on Exception catch (error) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _error = error.toString().replaceFirst('Exception: ', '');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authControllerProvider).session;
    final assets = _audits.expand((audit) => audit.assets).toList();
    final verified = assets.where((asset) => asset.isVerified).length;
    final pending = assets.length - verified;
    final activeAudits = _audits.where((audit) => audit.isActive).length;
    final branches = _audits.map((audit) => audit.branchId).toSet().length;
    final progress = assets.isEmpty ? 0.0 : verified / assets.length;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        scrolledUnderElevation: 0,
        elevation: 0,
        foregroundColor: _themeDark,
        flexibleSpace: const DecoratedBox(
          decoration: BoxDecoration(color: Colors.white),
        ),
        shape: const Border(bottom: BorderSide(color: Color(0xFFEBC8CD))),
        title: const FujitecHeaderLogo(),
        actions: [
          PopupMenuButton<String>(
            tooltip: 'Profile',
            icon: const Icon(Icons.account_circle_outlined),
            color: Colors.white,
            surfaceTintColor: Colors.white,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            onSelected: (value) {
              if (value == 'refresh') {
                unawaited(_load());
              } else if (value == 'signOut') {
                unawaited(ref.read(authControllerProvider.notifier).signOut());
              }
            },
            itemBuilder: (context) => [
              PopupMenuItem<String>(
                enabled: false,
                child: _ProfileName(
                  name: session?.displayName.trim().isNotEmpty == true
                      ? session!.displayName
                      : 'Auditor',
                  username: session?.username ?? '--',
                  userId: session?.userId,
                  expiresOnUtc: session?.expiresOnUtc,
                ),
              ),
              const PopupMenuDivider(),
              PopupMenuItem<String>(
                value: 'refresh',
                enabled: !_loading,
                child: const _MenuAction(
                  icon: Icons.refresh,
                  label: 'Refresh',
                  color: _themeDark,
                ),
              ),
              const PopupMenuItem<String>(
                value: 'signOut',
                child: _MenuAction(
                  icon: Icons.logout,
                  label: 'Sign Out',
                  color: _themeLight,
                ),
              ),
            ],
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 18, 16, 28),
          children: [
            const Text(
              'Dashboard',
              style: TextStyle(
                color: _themeDark,
                fontSize: 27,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              'Your assigned audit and verification overview.',
              style: TextStyle(color: Colors.grey.shade700),
            ),
            const SizedBox(height: 18),
            if (_loading)
              const Padding(
                padding: EdgeInsets.all(48),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_error != null)
              _ErrorPanel(message: _error!, onRetry: _load)
            else ...[
              LayoutBuilder(
                builder: (context, constraints) {
                  final width = constraints.maxWidth >= 700
                      ? (constraints.maxWidth - 36) / 4
                      : (constraints.maxWidth - 12) / 2;
                  return Wrap(
                    spacing: 12,
                    runSpacing: 12,
                    children: [
                      _MetricCard(
                        width: width,
                        label: 'Assigned Audits',
                        value: '${_audits.length}',
                        icon: Icons.assignment_outlined,
                      ),
                      _MetricCard(
                        width: width,
                        label: 'Active Audits',
                        value: '$activeAudits',
                        icon: Icons.pending_actions_outlined,
                      ),
                      _MetricCard(
                        width: width,
                        label: 'Verified Assets',
                        value: '$verified',
                        icon: Icons.verified_outlined,
                      ),
                      _MetricCard(
                        width: width,
                        label: 'Pending Assets',
                        value: '$pending',
                        icon: Icons.inventory_2_outlined,
                      ),
                    ],
                  );
                },
              ),
              const SizedBox(height: 16),
              _ProgressPanel(
                verified: verified,
                total: assets.length,
                branches: branches,
                progress: progress,
              ),
              const SizedBox(height: 16),
              _DashboardCharts(
                audits: _audits,
                verified: verified,
                pending: pending,
              ),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Audit Progress',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
                  ),
                  TextButton(
                    onPressed: widget.onOpenAudits,
                    child: const Text('View My Audits'),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              if (_audits.isEmpty)
                const _EmptyDashboard()
              else
                ..._audits.map((audit) => _AuditProgressTile(audit: audit)),
            ],
          ],
        ),
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.width,
    required this.label,
    required this.value,
    required this.icon,
  });

  final double width;
  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) => SizedBox(
    width: width,
    child: DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE8C5C9)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: const Color(0xFFD01126)),
            const SizedBox(height: 14),
            Text(
              value,
              style: const TextStyle(
                color: Color(0xFFD01126),
                fontSize: 25,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 2),
            Text(label, style: const TextStyle(fontSize: 12)),
          ],
        ),
      ),
    ),
  );
}

class _ProgressPanel extends StatelessWidget {
  const _ProgressPanel({
    required this.verified,
    required this.total,
    required this.branches,
    required this.progress,
  });

  final int verified;
  final int total;
  final int branches;
  final double progress;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(18),
    decoration: BoxDecoration(
      gradient: const LinearGradient(
        colors: [Color(0xFFD01126), Color(0xFFD01126)],
      ),
      borderRadius: BorderRadius.circular(16),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Overall Verification',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w700,
              ),
            ),
            Text(
              '${(progress * 100).round()}%',
              style: const TextStyle(
                color: Colors.white,
                fontSize: 20,
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        LinearProgressIndicator(
          value: progress,
          minHeight: 9,
          borderRadius: BorderRadius.circular(8),
          color: Colors.white,
          backgroundColor: Colors.white24,
        ),
        const SizedBox(height: 12),
        Text(
          '$verified of $total assets verified • $branches branches',
          style: const TextStyle(color: Colors.white),
        ),
      ],
    ),
  );
}

class _AuditProgressTile extends StatelessWidget {
  const _AuditProgressTile({required this.audit});

  final AssignedAudit audit;

  @override
  Widget build(BuildContext context) {
    final verified = audit.assets.where((asset) => asset.isVerified).length;
    final progress = audit.assets.isEmpty
        ? 0.0
        : verified / audit.assets.length;
    return Card(
      color: Colors.white,
      surfaceTintColor: Colors.white,
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(15),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    audit.auditName,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
                Text(
                  audit.isActive ? 'Active' : 'Closed',
                  style: const TextStyle(
                    color: Color(0xFFD01126),
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 3),
            Text(
              audit.branchName,
              style: TextStyle(color: Colors.grey.shade700),
            ),
            const SizedBox(height: 11),
            LinearProgressIndicator(
              value: progress,
              minHeight: 7,
              borderRadius: BorderRadius.circular(8),
              color: const Color(0xFFD01126),
              backgroundColor: const Color(0xFFFFE5E8),
            ),
            const SizedBox(height: 6),
            Text('$verified of ${audit.assets.length} assets verified'),
          ],
        ),
      ),
    );
  }
}

class _DashboardCharts extends StatelessWidget {
  const _DashboardCharts({
    required this.audits,
    required this.verified,
    required this.pending,
  });

  final List<AssignedAudit> audits;
  final int verified;
  final int pending;

  @override
  Widget build(BuildContext context) {
    final branches = <String, List<AssignedAsset>>{};
    for (final audit in audits) {
      branches.putIfAbsent(audit.branchName, () => []).addAll(audit.assets);
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Progress Analytics',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 10),
        _ChartCard(
          title: 'Verified vs Pending Assets',
          child: Row(
            children: [
              SizedBox(
                width: 116,
                height: 116,
                child: CustomPaint(
                  painter: _DonutPainter(verified: verified, pending: pending),
                  child: Center(
                    child: Text(
                      '${verified + pending}',
                      style: const TextStyle(
                        color: Color(0xFFD01126),
                        fontSize: 22,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 22),
              Expanded(
                child: Column(
                  children: [
                    _LegendRow(
                      color: const Color(0xFFD01126),
                      label: 'Verified',
                      value: verified,
                    ),
                    const SizedBox(height: 12),
                    _LegendRow(
                      color: const Color(0xFFD01126),
                      label: 'Pending',
                      value: pending,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _ChartCard(
          title: 'Audit Completion',
          child: audits.isEmpty
              ? const Text('No audit data available.')
              : Column(
                  children: audits
                      .map(
                        (audit) => _ProgressBar(
                          label: audit.auditName,
                          value: audit.assets.isEmpty
                              ? 0
                              : audit.assets
                                        .where((asset) => asset.isVerified)
                                        .length /
                                    audit.assets.length,
                        ),
                      )
                      .toList(),
                ),
        ),
        const SizedBox(height: 12),
        _ChartCard(
          title: 'Branch-wise Progress',
          child: branches.isEmpty
              ? const Text('No branch data available.')
              : Column(
                  children: branches.entries.map((entry) {
                    final completed = entry.value
                        .where((asset) => asset.isVerified)
                        .length;
                    return _ProgressBar(
                      label: entry.key,
                      value: entry.value.isEmpty
                          ? 0
                          : completed / entry.value.length,
                    );
                  }).toList(),
                ),
        ),
      ],
    );
  }
}

class _ChartCard extends StatelessWidget {
  const _ChartCard({required this.title, required this.child});
  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) => Container(
    width: double.infinity,
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: const Color(0xFFE8C5C9)),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(title, style: const TextStyle(fontWeight: FontWeight.w800)),
        const SizedBox(height: 16),
        child,
      ],
    ),
  );
}

class _LegendRow extends StatelessWidget {
  const _LegendRow({
    required this.color,
    required this.label,
    required this.value,
  });
  final Color color;
  final String label;
  final int value;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Container(
        width: 10,
        height: 10,
        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      ),
      const SizedBox(width: 8),
      Expanded(child: Text(label)),
      Text('$value', style: const TextStyle(fontWeight: FontWeight.w800)),
    ],
  );
}

class _ProgressBar extends StatelessWidget {
  const _ProgressBar({required this.label, required this.value});
  final String label;
  final double value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(child: Text(label, overflow: TextOverflow.ellipsis)),
            Text(
              '${(value * 100).round()}%',
              style: const TextStyle(
                color: Color(0xFFD01126),
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ),
        const SizedBox(height: 7),
        LinearProgressIndicator(
          value: value,
          minHeight: 8,
          borderRadius: BorderRadius.circular(8),
          color: const Color(0xFFD01126),
          backgroundColor: const Color(0xFFFFDDE1),
        ),
      ],
    ),
  );
}

class _DonutPainter extends CustomPainter {
  const _DonutPainter({required this.verified, required this.pending});
  final int verified;
  final int pending;

  @override
  void paint(Canvas canvas, Size size) {
    final total = verified + pending;
    final rect = Offset.zero & size;
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 15
      ..strokeCap = StrokeCap.round;
    paint.color = const Color(0xFFFFDDE1);
    canvas.drawArc(rect.deflate(10), 0, 6.28318, false, paint);
    if (total == 0) return;
    paint.color = const Color(0xFFD01126);
    canvas.drawArc(
      rect.deflate(10),
      -1.5708,
      6.28318 * verified / total,
      false,
      paint,
    );
    if (pending > 0) {
      paint.color = const Color(0xFFD01126);
      canvas.drawArc(
        rect.deflate(10),
        -1.5708 + 6.28318 * verified / total,
        6.28318 * pending / total,
        false,
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant _DonutPainter oldDelegate) =>
      oldDelegate.verified != verified || oldDelegate.pending != pending;
}

class _ProfileName extends StatelessWidget {
  const _ProfileName({
    required this.name,
    required this.username,
    required this.userId,
    required this.expiresOnUtc,
  });
  final String name;
  final String username;
  final int? userId;
  final DateTime? expiresOnUtc;

  @override
  Widget build(BuildContext context) {
    final expiry = expiresOnUtc?.toLocal();
    final expiryText = expiry == null
        ? '--'
        : '${expiry.day.toString().padLeft(2, '0')}/${expiry.month.toString().padLeft(2, '0')}/${expiry.year} '
              '${TimeOfDay.fromDateTime(expiry).format(context)}';
    return SizedBox(
      width: 260,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const CircleAvatar(
                backgroundColor: Color(0xFFD01126),
                foregroundColor: Colors.white,
                child: Icon(Icons.person_outline),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w800),
                    ),
                    Text('@$username', style: const TextStyle(fontSize: 12)),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const _ProfileField(label: 'Assigned role', value: 'Auditor'),
          _ProfileField(label: 'User ID', value: userId?.toString() ?? '--'),
          const _ProfileField(label: 'Session', value: 'Authenticated'),
          _ProfileField(label: 'Expires', value: expiryText),
        ],
      ),
    );
  }
}

class _ProfileField extends StatelessWidget {
  const _ProfileField({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(top: 5),
    child: Row(
      children: [
        SizedBox(
          width: 90,
          child: Text(
            label,
            style: const TextStyle(color: Colors.grey, fontSize: 11),
          ),
        ),
        Expanded(
          child: Text(
            value,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
          ),
        ),
      ],
    ),
  );
}

class _MenuAction extends StatelessWidget {
  const _MenuAction({
    required this.icon,
    required this.label,
    required this.color,
  });
  final IconData icon;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      Icon(icon, color: color),
      const SizedBox(width: 12),
      Text(
        label,
        style: TextStyle(color: color, fontWeight: FontWeight.w700),
      ),
    ],
  );
}

class _ErrorPanel extends StatelessWidget {
  const _ErrorPanel({required this.message, required this.onRetry});
  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Column(
      children: [
        const Icon(Icons.error_outline, size: 48, color: Color(0xFFD01126)),
        const SizedBox(height: 10),
        Text(message, textAlign: TextAlign.center),
        const SizedBox(height: 12),
        FilledButton(onPressed: onRetry, child: const Text('Try Again')),
      ],
    ),
  );
}

class _EmptyDashboard extends StatelessWidget {
  const _EmptyDashboard();

  @override
  Widget build(BuildContext context) => const Padding(
    padding: EdgeInsets.symmetric(vertical: 30),
    child: Text(
      'No audits are currently assigned to your account.',
      textAlign: TextAlign.center,
    ),
  );
}
