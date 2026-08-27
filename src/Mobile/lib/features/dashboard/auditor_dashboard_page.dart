import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../core/theme/theme_mode_controller.dart';
import '../../shared/widgets/corporate_wave_background.dart';
import '../../shared/widgets/fujitec_header_logo.dart';
import '../../shared/widgets/techy_loader.dart';
import '../audits/audit_models.dart';
import '../audits/audits_api.dart';

typedef OpenAuditsCallback = void Function({String auditFilter, int? auditId});

class AuditorDashboardPage extends ConsumerStatefulWidget {
  const AuditorDashboardPage({required this.onOpenAudits, super.key});

  final OpenAuditsCallback onOpenAudits;

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

  void _openAssets(
    String title,
    Iterable<AssignedAudit> audits,
    bool verified,
  ) {
    final rows = [
      for (final audit in audits)
        for (final asset in audit.assets)
          if (asset.isVerified == verified) (audit: audit, asset: asset),
    ];
    unawaited(
      Navigator.of(context).push<void>(
        MaterialPageRoute(
          builder: (_) => _DashboardAssetsPage(title: title, rows: rows),
        ),
      ),
    );
  }

  void _openBranchAssets(String branchName, List<AssignedAsset> assets) {
    final audit = _audits.firstWhere((item) => item.branchName == branchName);
    final rows = assets.map((asset) => (audit: audit, asset: asset)).toList();
    unawaited(
      Navigator.of(context).push<void>(
        MaterialPageRoute(
          builder: (_) =>
              _DashboardAssetsPage(title: '$branchName Assets', rows: rows),
        ),
      ),
    );
  }

  Future<void> _showAuditPopup(BuildContext context) async {
    final auditId = await showDialog<int>(
      context: context,
      builder: (context) => _AllAuditsDialog(audits: _audits),
    );
    if (auditId != null) {
      widget.onOpenAudits(auditFilter: 'All', auditId: auditId);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authControllerProvider).session;
    final isDarkMode = ref.watch(themeModeProvider) == ThemeMode.dark;
    final assets = _audits.expand((audit) => audit.assets).toList();
    final verified = assets.where((asset) => asset.isVerified).length;
    final pending = assets.length - verified;
    final activeAudits = _audits.where((audit) => audit.isActive).length;
    final branches = _audits.map((audit) => audit.branchId).toSet().length;
    final progress = assets.isEmpty ? 0.0 : verified / assets.length;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.surface,
        surfaceTintColor: Colors.transparent,
        scrolledUnderElevation: 0,
        elevation: 0,
        foregroundColor: _themeDark,
        flexibleSpace: DecoratedBox(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
          ),
        ),
        shape: const Border(bottom: BorderSide(color: Color(0xFFEBC8CD))),
        title: const FujitecHeaderLogo(),
        actions: [
          PopupMenuButton<String>(
            tooltip: 'Profile',
            color: Theme.of(context).colorScheme.surface,
            surfaceTintColor: Colors.transparent,
            constraints: const BoxConstraints.tightFor(width: 210),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
            ),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 10),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.account_circle_outlined, size: 25),
                  const SizedBox(width: 5),
                  ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 82),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          session?.displayName.trim().isNotEmpty == true
                              ? session!.displayName
                              : 'Auditor',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Theme.of(context).textTheme.labelMedium
                              ?.copyWith(
                                color: Theme.of(context).colorScheme.onSurface,
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                        const Text(
                          'Auditor',
                          style: TextStyle(
                            color: Color(0xFF8A7477),
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            onSelected: (value) {
              if (value == 'theme') {
                ref.read(themeModeProvider.notifier).toggle();
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
                ),
              ),
              const PopupMenuDivider(),
              PopupMenuItem<String>(
                value: 'theme',
                height: 42,
                child: _MenuAction(
                  icon: isDarkMode
                      ? Icons.light_mode_outlined
                      : Icons.dark_mode_outlined,
                  label: isDarkMode ? 'Light theme' : 'Dark theme',
                  color: _themeDark,
                ),
              ),
              const PopupMenuItem<String>(
                value: 'signOut',
                height: 42,
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
      body: CorporateWaveBackground(
        variant: CorporateWaveVariant.dashboard,
        child: _loading
            ? const Center(child: TechyLoader(size: 40))
            : RefreshIndicator(
                onRefresh: _load,
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 18, 16, 28),
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 14,
                      ),
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.surface,
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(color: const Color(0xFFE4E7EC)),
                        boxShadow: const [
                          BoxShadow(
                            color: Color(0x0D0F172A),
                            blurRadius: 14,
                            offset: Offset(0, 5),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 4,
                            height: 46,
                            decoration: BoxDecoration(
                              color: _themeDark,
                              borderRadius: BorderRadius.circular(99),
                            ),
                          ),
                          const SizedBox(width: 13),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Dashboard',
                                  style: TextStyle(
                                    color: Theme.of(
                                      context,
                                    ).colorScheme.onSurface,
                                    fontSize: 28,
                                    fontWeight: FontWeight.w900,
                                    letterSpacing: -0.6,
                                    height: 1.05,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  'Your assigned audit and verification overview.',
                                  style: TextStyle(
                                    color: Theme.of(
                                      context,
                                    ).colorScheme.onSurfaceVariant,
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    if (_error != null)
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
                                onTap: () =>
                                    widget.onOpenAudits(auditFilter: 'All'),
                              ),
                              _MetricCard(
                                width: width,
                                label: 'Active Audits',
                                value: '$activeAudits',
                                icon: Icons.pending_actions_outlined,
                                onTap: () =>
                                    widget.onOpenAudits(auditFilter: 'Active'),
                              ),
                              _MetricCard(
                                width: width,
                                label: 'Verified Assets',
                                value: '$verified',
                                icon: Icons.verified_outlined,
                                onTap: () => _openAssets(
                                  'Verified Assets',
                                  _audits,
                                  true,
                                ),
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
                        onTap: () => widget.onOpenAudits(auditFilter: 'All'),
                      ),
                      const SizedBox(height: 16),
                      _DashboardCharts(
                        audits: _audits,
                        verified: verified,
                        pending: pending,
                        onOpenAudits: widget.onOpenAudits,
                        onOpenBranch: _openBranchAssets,
                        onOpenVerified: () =>
                            _openAssets('Verified Assets', _audits, true),
                      ),
                      const SizedBox(height: 16),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            'Audit Progress',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          TextButton(
                            onPressed: () =>
                                widget.onOpenAudits(auditFilter: 'All'),
                            child: const Text('View My Audits'),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      if (_audits.isEmpty)
                        const _EmptyDashboard()
                      else
                        ..._audits
                            .take(2)
                            .map(
                              (audit) => _AuditProgressTile(
                                audit: audit,
                                onTap: () => widget.onOpenAudits(
                                  auditFilter: 'All',
                                  auditId: audit.id,
                                ),
                              ),
                            ),
                      if (_audits.length > 2)
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton.icon(
                            onPressed: () => _showAuditPopup(context),
                            icon: const Icon(
                              Icons.open_in_new_rounded,
                              size: 17,
                            ),
                            label: Text('View More (${_audits.length})'),
                          ),
                        ),
                    ],
                  ],
                ),
              ),
      ),
    );
  }
}

class _AllAuditsDialog extends StatelessWidget {
  const _AllAuditsDialog({required this.audits});

  final List<AssignedAudit> audits;

  @override
  Widget build(BuildContext context) {
    final screen = MediaQuery.sizeOf(context);
    return Dialog(
      insetPadding: const EdgeInsets.symmetric(horizontal: 18, vertical: 28),
      backgroundColor: Colors.transparent,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 430,
          maxHeight: (screen.height * 0.78).clamp(380, 650),
        ),
        child: Material(
          color: Theme.of(context).colorScheme.surfaceContainerLow,
          borderRadius: BorderRadius.circular(22),
          clipBehavior: Clip.antiAlias,
          elevation: 18,
          shadowColor: const Color(0x66000000),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: double.infinity,
                padding: const EdgeInsets.fromLTRB(18, 15, 8, 15),
                decoration: const BoxDecoration(color: Color(0xFFD01126)),
                child: Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.16),
                        borderRadius: BorderRadius.circular(11),
                      ),
                      child: const Icon(
                        Icons.assignment_outlined,
                        color: Colors.white,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Assigned Audits',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          Text(
                            '${audits.length} audits available',
                            style: const TextStyle(
                              color: Colors.white70,
                              fontSize: 12,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: 'Close',
                      onPressed: () => Navigator.pop(context),
                      icon: const Icon(Icons.close_rounded),
                      color: Colors.white,
                    ),
                  ],
                ),
              ),
              Flexible(
                child: ListView.separated(
                  padding: const EdgeInsets.all(14),
                  itemCount: audits.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 10),
                  itemBuilder: (context, index) {
                    final audit = audits[index];
                    final verified = audit.assets
                        .where((asset) => asset.isVerified)
                        .length;
                    final total = audit.assets.length;
                    final progress = total == 0 ? 0.0 : verified / total;
                    return Material(
                      color: Theme.of(context).colorScheme.surface,
                      borderRadius: BorderRadius.circular(14),
                      clipBehavior: Clip.antiAlias,
                      child: InkWell(
                        onTap: () => Navigator.pop(context, audit.id),
                        child: Ink(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            border: Border.all(color: const Color(0xFFE7D5D8)),
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: Text(
                                      audit.auditName,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        color: Color(0xFF201A1C),
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 8),
                                  Container(
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 8,
                                      vertical: 4,
                                    ),
                                    decoration: BoxDecoration(
                                      color: audit.isActive
                                          ? const Color(0xFFFFEEF0)
                                          : const Color(0xFFF1F3F5),
                                      borderRadius: BorderRadius.circular(99),
                                    ),
                                    child: Text(
                                      audit.isActive
                                          ? 'IN PROGRESS'
                                          : 'COMPLETED',
                                      style: TextStyle(
                                        color: audit.isActive
                                            ? const Color(0xFFD01126)
                                            : const Color(0xFF667085),
                                        fontSize: 12,
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 5),
                              Row(
                                children: [
                                  const Icon(
                                    Icons.location_on_outlined,
                                    size: 15,
                                    color: Color(0xFFD01126),
                                  ),
                                  const SizedBox(width: 4),
                                  Expanded(
                                    child: Text(
                                      audit.branchName,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        color: Color(0xFF667085),
                                        fontSize: 12,
                                        fontWeight: FontWeight.w500,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 11),
                              Row(
                                children: [
                                  Expanded(
                                    child: ClipRRect(
                                      borderRadius: BorderRadius.circular(99),
                                      child: LinearProgressIndicator(
                                        value: progress,
                                        minHeight: 6,
                                        color: const Color(0xFFD01126),
                                        backgroundColor: const Color(
                                          0xFFF1DADD,
                                        ),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 10),
                                  Text(
                                    '$verified / $total',
                                    style: const TextStyle(
                                      color: Color(0xFF344054),
                                      fontSize: 12,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  const SizedBox(width: 2),
                                  const Icon(
                                    Icons.chevron_right_rounded,
                                    color: Color(0xFFD01126),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
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
    this.onTap,
  });

  final double width;
  final String label;
  final String value;
  final IconData icon;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => SizedBox(
    width: width,
    child: Material(
      color: Theme.of(context).colorScheme.surface,
      borderRadius: BorderRadius.circular(14),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Ink(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(
              color: Theme.of(context).colorScheme.outlineVariant,
            ),
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
    required this.onTap,
  });

  final int verified;
  final int total;
  final int branches;
  final double progress;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: Colors.transparent,
    borderRadius: BorderRadius.circular(16),
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Ink(
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
      ),
    ),
  );
}

class _AuditProgressTile extends StatelessWidget {
  const _AuditProgressTile({required this.audit, required this.onTap});

  final AssignedAudit audit;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final verified = audit.assets.where((asset) => asset.isVerified).length;
    final progress = audit.assets.isEmpty
        ? 0.0
        : verified / audit.assets.length;
    return Card(
      color: Theme.of(context).colorScheme.surface,
      surfaceTintColor: Colors.transparent,
      margin: const EdgeInsets.only(bottom: 10),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
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
      ),
    );
  }
}

class _DashboardCharts extends StatelessWidget {
  const _DashboardCharts({
    required this.audits,
    required this.verified,
    required this.pending,
    required this.onOpenAudits,
    required this.onOpenBranch,
    required this.onOpenVerified,
  });

  final List<AssignedAudit> audits;
  final int verified;
  final int pending;
  final OpenAuditsCallback onOpenAudits;
  final void Function(String branchName, List<AssignedAsset> assets)
  onOpenBranch;
  final VoidCallback onOpenVerified;

  Future<void> _showAudits(BuildContext context) async {
    final auditId = await showDialog<int>(
      context: context,
      builder: (context) => _ProgressListDialog(
        title: 'Audit Completion',
        rows: [
          for (final audit in audits)
            (
              id: audit.id,
              label: audit.auditName,
              value: audit.assets.isEmpty
                  ? 0.0
                  : audit.assets.where((asset) => asset.isVerified).length /
                        audit.assets.length,
            ),
        ],
      ),
    );
    if (auditId != null) {
      onOpenAudits(auditFilter: 'All', auditId: auditId);
    }
  }

  Future<void> _showBranches(
    BuildContext context,
    Map<String, List<AssignedAsset>> branches,
  ) async {
    final branchName = await showDialog<String>(
      context: context,
      builder: (context) => _BranchProgressDialog(branches: branches),
    );
    if (branchName != null) onOpenBranch(branchName, branches[branchName]!);
  }

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
                      onTap: onOpenVerified,
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
          onTap: () => _showAudits(context),
          child: audits.isEmpty
              ? const Text('No audit data available.')
              : Column(
                  children: [
                    ...audits
                        .take(2)
                        .map(
                          (audit) => _ProgressBar(
                            label: audit.auditName,
                            value: audit.assets.isEmpty
                                ? 0
                                : audit.assets
                                          .where((asset) => asset.isVerified)
                                          .length /
                                      audit.assets.length,
                            onTap: () => onOpenAudits(
                              auditFilter: 'All',
                              auditId: audit.id,
                            ),
                          ),
                        ),
                    if (audits.length > 2)
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton(
                          onPressed: () => _showAudits(context),
                          child: Text('View More (${audits.length})'),
                        ),
                      ),
                  ],
                ),
        ),
        const SizedBox(height: 12),
        _ChartCard(
          title: 'Branch-wise Progress',
          onTap: () => _showBranches(context, branches),
          child: branches.isEmpty
              ? const Text('No branch data available.')
              : Column(
                  children: [
                    ...branches.entries.take(2).map((entry) {
                      final completed = entry.value
                          .where((asset) => asset.isVerified)
                          .length;
                      return _ProgressBar(
                        label: entry.key,
                        value: entry.value.isEmpty
                            ? 0
                            : completed / entry.value.length,
                        onTap: () => onOpenBranch(entry.key, entry.value),
                      );
                    }),
                    if (branches.length > 2)
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton(
                          onPressed: () => _showBranches(context, branches),
                          child: Text('View More (${branches.length})'),
                        ),
                      ),
                  ],
                ),
        ),
      ],
    );
  }
}

class _ChartCard extends StatelessWidget {
  const _ChartCard({required this.title, required this.child, this.onTap});
  final String title;
  final Widget child;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: Colors.transparent,
    borderRadius: BorderRadius.circular(14),
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Ink(
        width: double.infinity,
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: Theme.of(context).colorScheme.outlineVariant,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: const TextStyle(fontWeight: FontWeight.w800)),
            const SizedBox(height: 16),
            child,
          ],
        ),
      ),
    ),
  );
}

class _LegendRow extends StatelessWidget {
  const _LegendRow({
    required this.color,
    required this.label,
    required this.value,
    this.onTap,
  });
  final Color color;
  final String label;
  final int value;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(8),
    child: Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
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
      ),
    ),
  );
}

class _ProgressBar extends StatelessWidget {
  const _ProgressBar({required this.label, required this.value, this.onTap});
  final String label;
  final double value;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(8),
    child: Padding(
      padding: const EdgeInsets.only(bottom: 14, top: 4),
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
    ),
  );
}

class _ProgressListDialog extends StatelessWidget {
  const _ProgressListDialog({required this.title, required this.rows});

  final String title;
  final List<({int id, String label, double value})> rows;

  @override
  Widget build(BuildContext context) => _ProgressDialogFrame(
    title: title,
    subtitle: '${rows.length} audits',
    icon: Icons.insights_rounded,
    children: [
      for (final row in rows)
        _PopupProgressCard(
          label: row.label,
          meta: '${(row.value * 100).round()}% completed',
          value: row.value,
          onTap: () => Navigator.pop(context, row.id),
        ),
    ],
  );
}

class _BranchProgressDialog extends StatelessWidget {
  const _BranchProgressDialog({required this.branches});

  final Map<String, List<AssignedAsset>> branches;

  @override
  Widget build(BuildContext context) => _ProgressDialogFrame(
    title: 'Branch-wise Progress',
    subtitle: '${branches.length} branches',
    icon: Icons.account_tree_outlined,
    children: [
      for (final entry in branches.entries)
        _PopupProgressCard(
          label: entry.key,
          meta:
              '${entry.value.where((asset) => asset.isVerified).length} of ${entry.value.length} assets verified',
          value: entry.value.isEmpty
              ? 0
              : entry.value.where((asset) => asset.isVerified).length /
                    entry.value.length,
          onTap: () => Navigator.pop(context, entry.key),
        ),
    ],
  );
}

class _ProgressDialogFrame extends StatelessWidget {
  const _ProgressDialogFrame({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.children,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final screenHeight = MediaQuery.sizeOf(context).height;
    return Dialog(
      insetPadding: const EdgeInsets.symmetric(horizontal: 18, vertical: 28),
      backgroundColor: Colors.transparent,
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 430,
          maxHeight: (screenHeight * 0.76).clamp(360, 620),
        ),
        child: Material(
          color: Theme.of(context).colorScheme.surfaceContainerLow,
          elevation: 18,
          shadowColor: const Color(0x66000000),
          borderRadius: BorderRadius.circular(22),
          clipBehavior: Clip.antiAlias,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: double.infinity,
                padding: const EdgeInsets.fromLTRB(18, 15, 8, 15),
                color: const Color(0xFFD01126),
                child: Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.16),
                        borderRadius: BorderRadius.circular(11),
                      ),
                      child: Icon(icon, color: Colors.white),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            title,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 17,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          Text(
                            subtitle,
                            style: const TextStyle(
                              color: Colors.white70,
                              fontSize: 12,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: 'Close',
                      onPressed: () => Navigator.pop(context),
                      icon: const Icon(Icons.close_rounded),
                      color: Colors.white,
                    ),
                  ],
                ),
              ),
              Flexible(
                child: ListView.separated(
                  padding: const EdgeInsets.all(14),
                  itemCount: children.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 10),
                  itemBuilder: (context, index) => children[index],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PopupProgressCard extends StatelessWidget {
  const _PopupProgressCard({
    required this.label,
    required this.meta,
    required this.value,
    required this.onTap,
  });

  final String label;
  final String meta;
  final double value;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: Theme.of(context).colorScheme.surface,
    borderRadius: BorderRadius.circular(14),
    clipBehavior: Clip.antiAlias,
    child: InkWell(
      onTap: onTap,
      child: Ink(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          border: Border.all(color: const Color(0xFFE7D5D8)),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.onSurface,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  '${(value * 100).round()}%',
                  style: const TextStyle(
                    color: Color(0xFFD01126),
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const Icon(
                  Icons.chevron_right_rounded,
                  color: Color(0xFFD01126),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              meta,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: Color(0xFF667085),
                fontSize: 12,
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 10),
            ClipRRect(
              borderRadius: BorderRadius.circular(99),
              child: LinearProgressIndicator(
                value: value,
                minHeight: 6,
                color: const Color(0xFFD01126),
                backgroundColor: const Color(0xFFF1DADD),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

class _DashboardAssetsPage extends StatelessWidget {
  const _DashboardAssetsPage({required this.title, required this.rows});

  final String title;
  final List<({AssignedAudit audit, AssignedAsset asset})> rows;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      backgroundColor: Theme.of(context).colorScheme.surface,
      surfaceTintColor: Colors.transparent,
      foregroundColor: const Color(0xFFD01126),
      title: Text(
        title,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: Theme.of(context).colorScheme.onSurface,
          fontWeight: FontWeight.w800,
        ),
      ),
    ),
    body: CorporateWaveBackground(
      variant: CorporateWaveVariant.dashboard,
      child: rows.isEmpty
          ? const Center(child: Text('No matching assets found.'))
          : ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: rows.length,
              separatorBuilder: (_, _) => const SizedBox(height: 9),
              itemBuilder: (context, index) {
                final row = rows[index];
                final location = row.asset.location?.trim();
                return Card(
                  color: Theme.of(context).colorScheme.surface,
                  surfaceTintColor: Colors.transparent,
                  child: ListTile(
                    leading: CircleAvatar(
                      backgroundColor: row.asset.isVerified
                          ? const Color(0xFFE9F7EF)
                          : const Color(0xFFFFECEE),
                      child: Icon(
                        row.asset.isVerified
                            ? Icons.check_circle_outline_rounded
                            : Icons.cancel_outlined,
                        color: row.asset.isVerified
                            ? const Color(0xFF16803A)
                            : const Color(0xFFD01126),
                      ),
                    ),
                    title: Text(
                      row.asset.assetName,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Color(0xFF1D2939),
                        fontSize: 14,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    subtitle: Text(
                      'Asset No: ${row.asset.assetNumber}\n'
                      'Location: ${location?.isNotEmpty == true ? location : row.audit.branchName}',
                      maxLines: 3,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: Color(0xFF667085),
                        fontSize: 12,
                        height: 1.45,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    trailing: Text(
                      row.asset.isVerified ? 'Verified' : 'Pending',
                      style: TextStyle(
                        color: row.asset.isVerified
                            ? const Color(0xFF16803A)
                            : const Color(0xFFD01126),
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                );
              },
            ),
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
  const _ProfileName({required this.name, required this.username});
  final String name;
  final String username;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 176,
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
          const SizedBox(height: 7),
          const Text(
            'AUDITOR',
            style: TextStyle(
              color: Color(0xFFD01126),
              fontSize: 12,
              fontWeight: FontWeight.w800,
              letterSpacing: 0.8,
            ),
          ),
        ],
      ),
    );
  }
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
