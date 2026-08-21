import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../audits/audit_models.dart';

class ScanHistoryEntry {
  const ScanHistoryEntry({
    required this.assetId,
    required this.assetNumber,
    required this.assetName,
    required this.auditName,
    required this.branchName,
    required this.scannedAt,
    required this.wasAlreadyVerified,
  });

  factory ScanHistoryEntry.fromScan({
    required AssignedAsset asset,
    required AssignedAudit audit,
  }) => ScanHistoryEntry(
    assetId: asset.id,
    assetNumber: asset.assetNumber,
    assetName: asset.assetName,
    auditName: audit.auditName,
    branchName: audit.branchName,
    scannedAt: DateTime.now(),
    wasAlreadyVerified: asset.isVerified,
  );

  final int assetId;
  final String assetNumber;
  final String assetName;
  final String auditName;
  final String branchName;
  final DateTime scannedAt;
  final bool wasAlreadyVerified;
}

class ScanHistoryController extends Notifier<List<ScanHistoryEntry>> {
  @override
  List<ScanHistoryEntry> build() => const [];

  void record({required AssignedAsset asset, required AssignedAudit audit}) {
    state = [
      ScanHistoryEntry.fromScan(asset: asset, audit: audit),
      ...state,
    ].take(100).toList(growable: false);
  }

  void clear() => state = const [];
}

final scanHistoryProvider =
    NotifierProvider<ScanHistoryController, List<ScanHistoryEntry>>(
      ScanHistoryController.new,
    );
