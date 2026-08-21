class AssignedAudit {
  const AssignedAudit({
    required this.id,
    required this.auditName,
    required this.branchId,
    required this.branchName,
    required this.isActive,
    required this.assets,
  });

  factory AssignedAudit.fromJson(Map<String, dynamic> json) => AssignedAudit(
    id: json['id'] as int,
    auditName: json['auditName'] as String,
    branchId: json['branchId'] as int,
    branchName: json['branchName'] as String,
    isActive: json['isActive'] as bool,
    assets: (json['assets'] as List<dynamic>)
        .map((item) => AssignedAsset.fromJson(item as Map<String, dynamic>))
        .toList(),
  );

  final int id;
  final String auditName;
  final int branchId;
  final String branchName;
  final bool isActive;
  final List<AssignedAsset> assets;
}

class AssignedAsset {
  const AssignedAsset({
    required this.id,
    required this.assetNumber,
    required this.assetName,
    required this.quantity,
    required this.isBulk,
    required this.isVerified,
    this.serialNumber,
    this.qrCodeValue,
    this.barcodeValue,
    this.location,
  });

  factory AssignedAsset.fromJson(Map<String, dynamic> json) => AssignedAsset(
    id: json['id'] as int,
    assetNumber: json['assetNumber'] as String,
    assetName: json['assetName'] as String,
    serialNumber: json['serialNumber'] as String?,
    qrCodeValue: json['qrCodeValue'] as String?,
    barcodeValue: json['barcodeValue'] as String?,
    location: json['location'] as String?,
    quantity: (json['quantity'] as num).toDouble(),
    isBulk: json['isBulk'] as bool,
    isVerified: json['isVerified'] as bool,
  );

  final int id;
  final String assetNumber;
  final String assetName;
  final String? serialNumber;
  final String? qrCodeValue;
  final String? barcodeValue;
  final String? location;
  final double quantity;
  final bool isBulk;
  final bool isVerified;

  bool matchesScan(String value) {
    final scan = _normalise(value);
    return scan.isNotEmpty &&
        [
          assetNumber,
          qrCodeValue,
          barcodeValue,
        ].whereType<String>().any((candidate) => _normalise(candidate) == scan);
  }

  static String _normalise(String value) => value.trim().toUpperCase();
}
