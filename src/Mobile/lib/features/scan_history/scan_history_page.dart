import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../shared/widgets/corporate_wave_background.dart';
import '../../shared/widgets/fujitec_header_logo.dart';
import 'scan_history.dart';

class ScanHistoryPage extends ConsumerWidget {
  const ScanHistoryPage({super.key});

  static const Color _themeDark = Color(0xFFD01126);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final history = ref.watch(scanHistoryProvider);
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.surface,
        surfaceTintColor: Colors.transparent,
        foregroundColor: _themeDark,
        elevation: 0,
        flexibleSpace: DecoratedBox(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
          ),
        ),
        shape: const Border(bottom: BorderSide(color: Color(0xFFEBC8CD))),
        title: const FujitecHeaderLogo(),
        actions: [
          if (history.isNotEmpty)
            IconButton(
              onPressed: () => _confirmClear(context, ref),
              icon: const Icon(Icons.delete_sweep_outlined),
              tooltip: 'Clear session history',
            ),
          const SizedBox(width: 8),
        ],
      ),
      body: CorporateWaveBackground(
        variant: CorporateWaveVariant.history,
        child: history.isEmpty
            ? const _EmptyHistory()
            : ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  const Text(
                    'Scan History',
                    style: TextStyle(
                      color: _themeDark,
                      fontSize: 26,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${history.length} scans in this app session',
                    style: TextStyle(color: Colors.grey.shade700),
                  ),
                  const SizedBox(height: 16),
                  ...history.map((entry) => _HistoryTile(entry: entry)),
                ],
              ),
      ),
    );
  }

  Future<void> _confirmClear(BuildContext context, WidgetRef ref) async {
    final clear = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Clear scan history?'),
        content: const Text(
          'This clears only the current in-memory session history. Audit records are not affected.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Clear'),
          ),
        ],
      ),
    );
    if (clear == true) ref.read(scanHistoryProvider.notifier).clear();
  }
}

class _HistoryTile extends StatelessWidget {
  const _HistoryTile({required this.entry});
  final ScanHistoryEntry entry;

  @override
  Widget build(BuildContext context) {
    final time = TimeOfDay.fromDateTime(entry.scannedAt).format(context);
    return Card(
      color: Theme.of(context).colorScheme.surface,
      surfaceTintColor: Colors.transparent,
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const CircleAvatar(
              backgroundColor: Color(0xFFFFE5E8),
              foregroundColor: Color(0xFFD01126),
              child: Icon(Icons.qr_code_scanner),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    entry.assetNumber,
                    style: const TextStyle(
                      color: Color(0xFFD01126),
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(entry.assetName),
                  const SizedBox(height: 6),
                  Text(
                    '${entry.auditName} • ${entry.branchName}',
                    style: TextStyle(color: Colors.grey.shade700, fontSize: 12),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(time, style: const TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 5),
                Text(
                  entry.wasAlreadyVerified ? 'Previously verified' : 'Scanned',
                  style: const TextStyle(
                    color: Color(0xFFD01126),
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyHistory extends StatelessWidget {
  const _EmptyHistory();

  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.qr_code_2, size: 64, color: Color(0xFFD01126)),
          SizedBox(height: 14),
          Text(
            'No assets scanned yet',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
          ),
          SizedBox(height: 6),
          Text(
            'Assets scanned from My Audits will appear here until the app is closed.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    ),
  );
}
