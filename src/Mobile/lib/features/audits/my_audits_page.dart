import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../core/theme/theme_mode_controller.dart';
import '../../core/theme/responsive_typography.dart';
import '../../shared/widgets/corporate_wave_background.dart';
import '../../shared/widgets/fujitec_header_logo.dart';
import '../../shared/widgets/techy_loader.dart';
import 'audit_models.dart';
import 'audits_api.dart';
import 'scanner_page.dart';
import '../scan_history/scan_history.dart';

class MyAuditsPage extends ConsumerStatefulWidget {
  const MyAuditsPage({
    this.initialAuditFilter = 'All',
    this.initialAuditId,
    this.navigationVersion = 0,
    super.key,
  });

  final String initialAuditFilter;
  final int? initialAuditId;
  final int navigationVersion;

  @override
  ConsumerState<MyAuditsPage> createState() => _MyAuditsPageState();
}

class _MyAuditsPageState extends ConsumerState<MyAuditsPage> {
  static const Color _themeDark = Color(0xFFD01126);
  static const Color _themeLight = Color(0xFFD01126);
  static const List<int> _pageSizes = [5, 10, 20, 50];
  final TextEditingController _searchController = TextEditingController();
  final FocusNode _searchFocusNode = FocusNode();
  int _pageSize = 10;
  List<AssignedAudit> _audits = const [];
  int? _selectedId;
  bool _loading = true;
  String? _error;
  String _query = '';
  String _auditFilter = 'All';
  String _assetFilter = 'All';
  String _assetTypeFilter = 'All';
  String _identifierFilter = 'All';
  Set<String> _locationFilters = const {};
  int _pageIndex = 0;
  bool _refreshInProgress = false;
  bool _showAssetCards = true;
  bool _showAuditDetails = false;
  bool _isAuditSummaryExpanded = true;
  bool _isSearchFocused = false;
  String? _selectedLocation;
  int? _pendingNavigationAuditId;

  AssignedAudit? get _selected =>
      _audits.where((a) => a.id == _selectedId).firstOrNull;

  List<String> get _availableLocations {
    final audit = _selected;
    if (audit == null) return const [];
    final locations = audit.assets
        .map((asset) => _locationFor(asset, audit))
        .toSet()
        .toList();
    locations.sort(
      (left, right) => left.toLowerCase().compareTo(right.toLowerCase()),
    );
    return locations;
  }

  String _locationFor(AssignedAsset asset, AssignedAudit audit) {
    final location = asset.location?.trim();
    return location == null || location.isEmpty ? audit.branchName : location;
  }

  void _openAssetListPage(AssignedAudit audit) {
    unawaited(
      Navigator.of(context).push<void>(
        MaterialPageRoute(builder: (_) => _AuditAssetListPage(audit: audit)),
      ),
    );
  }

  List<AssignedAudit> get _visibleAudits {
    final query = _query.trim().toUpperCase();
    return _audits.where((audit) {
      final statusMatches =
          _auditFilter == 'All' ||
          (_auditFilter == 'Active' ? audit.isActive : !audit.isActive);
      final queryMatches =
          query.isEmpty ||
          audit.auditName.toUpperCase().contains(query) ||
          audit.branchName.toUpperCase().contains(query) ||
          audit.assets.any((asset) => _assetMatchesQuery(asset, query));
      return statusMatches && queryMatches;
    }).toList();
  }

  List<AssignedAsset> get _visibleAssets {
    final audit = _selected;
    if (audit == null) return const [];
    final query = _query.trim().toUpperCase();
    return audit.assets.where((asset) {
      final statusMatches =
          _assetFilter == 'All' ||
          (_assetFilter == 'Verified' ? asset.isVerified : !asset.isVerified);
      final location = asset.location?.trim();
      final locationMatches =
          _locationFilters.isEmpty ||
          (location != null && _locationFilters.contains(location));
      final typeMatches =
          _assetTypeFilter == 'All' ||
          (_assetTypeFilter == 'Bulk' ? asset.isBulk : !asset.isBulk);
      final identifierMatches = switch (_identifierFilter) {
        'QR Code' => asset.qrCodeValue?.trim().isNotEmpty == true,
        'Barcode' => asset.barcodeValue?.trim().isNotEmpty == true,
        'Serial Number' => asset.serialNumber?.trim().isNotEmpty == true,
        'No Scan Code' =>
          asset.qrCodeValue?.trim().isNotEmpty != true &&
              asset.barcodeValue?.trim().isNotEmpty != true,
        _ => true,
      };
      return statusMatches &&
          locationMatches &&
          typeMatches &&
          identifierMatches &&
          (query.isEmpty ||
              audit.auditName.toUpperCase().contains(query) ||
              _assetMatchesQuery(asset, query));
    }).toList();
  }

  bool _assetMatchesQuery(AssignedAsset asset, String query) =>
      asset.assetNumber.toUpperCase().contains(query) ||
      asset.assetName.toUpperCase().contains(query) ||
      (asset.serialNumber?.toUpperCase().contains(query) ?? false) ||
      (asset.location?.toUpperCase().contains(query) ?? false);

  void _resetPage() => _pageIndex = 0;

  @override
  void initState() {
    super.initState();
    _searchFocusNode.addListener(_handleSearchFocusChanged);
    _auditFilter = widget.initialAuditFilter;
    _showAuditDetails = widget.initialAuditId != null;
    unawaited(_load(widget.initialAuditId));
  }

  @override
  void didUpdateWidget(covariant MyAuditsPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.navigationVersion == oldWidget.navigationVersion) return;

    final requestedAuditId = widget.initialAuditId;
    _pendingNavigationAuditId = requestedAuditId;
    setState(() {
      _auditFilter = widget.initialAuditFilter;
      _showAuditDetails = requestedAuditId != null;
      _pageIndex = 0;
      if (requestedAuditId != null &&
          _audits.any((audit) => audit.id == requestedAuditId)) {
        _selectedId = requestedAuditId;
      }
    });

    if (_audits.isEmpty && !_refreshInProgress) {
      unawaited(_load(requestedAuditId));
    }
  }

  @override
  void dispose() {
    _searchFocusNode.removeListener(_handleSearchFocusChanged);
    _searchFocusNode.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _handleSearchFocusChanged() {
    if (!mounted || _isSearchFocused == _searchFocusNode.hasFocus) return;
    setState(() {
      _isSearchFocused = _searchFocusNode.hasFocus;
      if (_showAuditDetails && _isSearchFocused) {
        _isAuditSummaryExpanded = false;
      }
    });
  }

  Future<void> _load([int? preferred]) async {
    if (_refreshInProgress) return;
    _refreshInProgress = true;
    final isInitialLoad = _audits.isEmpty;
    setState(() {
      _loading = isInitialLoad;
      _error = null;
    });
    try {
      final audits = await ref.read(auditsApiProvider).listMine();
      if (!mounted) return;
      setState(() {
        final requestedId = _pendingNavigationAuditId ?? preferred;
        _audits = audits;
        _selectedId = audits.any((a) => a.id == (requestedId ?? _selectedId))
            ? (requestedId ?? _selectedId)
            : audits.firstOrNull?.id;
        _pendingNavigationAuditId = null;
        final availableLocations = _availableLocations.toSet();
        _locationFilters = _locationFilters
            .where(availableLocations.contains)
            .toSet();
        _loading = false;
        _pageIndex = 0;
      });
    } on Exception catch (error) {
      if (!mounted) return;
      final message = error.toString().replaceFirst('Exception: ', '');
      if (isInitialLoad) {
        setState(() {
          _loading = false;
          _error = message;
        });
      } else {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(content: Text(message), backgroundColor: _themeDark),
          );
      }
    } finally {
      _refreshInProgress = false;
      if (mounted && _loading) setState(() => _loading = false);
    }
  }

  Future<void> _scan() async {
    final audit = _selected;
    if (audit == null) return;
    await _scanForAudit(audit);
  }

  Future<void> _scanForAudit(
    AssignedAudit audit, {
    AssignedAsset? expectedAsset,
  }) async {
    final value = await Navigator.of(context).push<String>(
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (_) => ScannerPage(expectedAsset: expectedAsset),
      ),
    );
    if (!mounted || value == null) return;
    try {
      final asset = await ref
          .read(auditsApiProvider)
          .resolveScan(audit: audit, scannedValue: value);
      if (!mounted) return;
      ref.read(scanHistoryProvider.notifier).record(asset: asset, audit: audit);
      if (expectedAsset != null && asset.id != expectedAsset.id) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'The scanned code belongs to ${asset.assetNumber}. Please scan ${expectedAsset.assetNumber}.',
            ),
            backgroundColor: _themeDark,
          ),
        );
        return;
      }
      if (asset.isVerified) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('This asset is already verified.')),
        );
        return;
      }
      await _verify(audit, asset);
    } on Exception catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(error.toString().replaceFirst('Exception: ', '')),
          backgroundColor: const Color(0xFFD01126),
        ),
      );
    }
  }

  Future<void> _verify(AssignedAudit audit, AssignedAsset asset) async {
    var condition = 'Good';
    var serialVerified = false;
    final remarks = TextEditingController();
    final save = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      builder: (context) => StatefulBuilder(
        builder: (context, setSheetState) => Padding(
          padding: EdgeInsets.fromLTRB(
            20,
            20,
            20,
            20 + MediaQuery.viewInsetsOf(context).bottom,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                asset.assetNumber,
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
              ),
              Text(asset.assetName),
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFFE8F5EE),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      Icons.verified_user_outlined,
                      color: Color(0xFF18794E),
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'Audit and branch assignment confirmed.\n'
                        'GPS validation is disabled for this test.',
                        style: TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 18),
              DropdownButtonFormField<String>(
                isExpanded: true,
                initialValue: condition,
                decoration: const InputDecoration(
                  labelText: 'Working condition',
                ),
                items:
                    const [
                          'Good',
                          'MinorDamage',
                          'Damaged',
                          'NotWorking',
                          'Missing',
                        ]
                        .map(
                          (value) => DropdownMenuItem(
                            value: value,
                            child: Text(value),
                          ),
                        )
                        .toList(),
                onChanged: (value) => condition = value ?? 'Good',
              ),
              CheckboxListTile(
                contentPadding: EdgeInsets.zero,
                value: serialVerified,
                title: const Text('Serial number physically verified'),
                onChanged: (value) =>
                    setSheetState(() => serialVerified = value ?? false),
              ),
              TextField(
                controller: remarks,
                maxLines: 2,
                decoration: const InputDecoration(
                  labelText: 'Remarks (optional)',
                ),
              ),
              const SizedBox(height: 16),
              FilledButton.icon(
                onPressed: () => Navigator.pop(context, true),
                icon: const Icon(Icons.check),
                label: const Text('Save verification'),
              ),
            ],
          ),
        ),
      ),
    );
    if (save != true || !mounted) {
      remarks.dispose();
      return;
    }
    try {
      await ref
          .read(auditsApiProvider)
          .verify(
            audit: audit,
            asset: asset,
            workingCondition: condition,
            serialVerified: serialVerified,
            remarks: remarks.text.trim().isEmpty ? null : remarks.text.trim(),
          );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Asset verified successfully.')),
        );
        await _load(audit.id);
      }
    } on Exception catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(error.toString().replaceFirst('Exception: ', '')),
            backgroundColor: const Color(0xFFD01126),
          ),
        );
      }
    } finally {
      remarks.dispose();
    }
  }

  Future<void> _showAuditAssetFilters() async {
    var assetFilter = _assetFilter;
    var assetTypeFilter = _assetTypeFilter;
    var identifierFilter = _identifierFilter;

    final result =
        await showDialog<
          ({
            String assetFilter,
            String assetTypeFilter,
            String identifierFilter,
          })
        >(
          context: context,
          builder: (context) => StatefulBuilder(
            builder: (context, setDialogState) => AlertDialog(
              backgroundColor: Theme.of(context).colorScheme.surface,
              surfaceTintColor: Colors.transparent,
              insetPadding: const EdgeInsets.symmetric(horizontal: 22),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(20),
              ),
              titlePadding: const EdgeInsets.fromLTRB(20, 20, 12, 10),
              title: Row(
                children: [
                  const CircleAvatar(
                    radius: 18,
                    backgroundColor: Color(0xFFFFE8EB),
                    foregroundColor: _themeDark,
                    child: Icon(Icons.tune_rounded, size: 20),
                  ),
                  const SizedBox(width: 11),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Filter Assets',
                          style: TextStyle(
                            color: Color(0xFF1D2939),
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        SizedBox(height: 2),
                        Text(
                          'Refine assets in the selected location.',
                          style: TextStyle(
                            color: Color(0xFF667085),
                            fontSize: 12,
                            fontWeight: FontWeight.w400,
                          ),
                        ),
                      ],
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close filters',
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.close_rounded),
                  ),
                ],
              ),
              content: SizedBox(
                width: 330,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    DropdownButtonFormField<String>(
                      isExpanded: true,
                      key: ValueKey('detail-status-$assetFilter'),
                      initialValue: assetFilter,
                      decoration: const InputDecoration(
                        labelText: 'Verification Status',
                        prefixIcon: Icon(Icons.verified_outlined, size: 18),
                        prefixIconConstraints: BoxConstraints.tightFor(
                          width: 40,
                          height: 42,
                        ),
                        contentPadding: EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 10,
                        ),
                        isDense: true,
                      ),
                      items: const ['All', 'Pending', 'Verified']
                          .map(
                            (value) => DropdownMenuItem(
                              value: value,
                              child: Text(value),
                            ),
                          )
                          .toList(),
                      onChanged: (value) =>
                          setDialogState(() => assetFilter = value ?? 'All'),
                    ),
                    const SizedBox(height: 10),
                    DropdownButtonFormField<String>(
                      isExpanded: true,
                      key: ValueKey('detail-type-$assetTypeFilter'),
                      initialValue: assetTypeFilter,
                      decoration: const InputDecoration(
                        labelText: 'Asset Type',
                        prefixIcon: Icon(Icons.category_outlined, size: 18),
                        prefixIconConstraints: BoxConstraints.tightFor(
                          width: 40,
                          height: 42,
                        ),
                        contentPadding: EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 10,
                        ),
                        isDense: true,
                      ),
                      items: const ['All', 'Individual', 'Bulk']
                          .map(
                            (value) => DropdownMenuItem(
                              value: value,
                              child: Text(value),
                            ),
                          )
                          .toList(),
                      onChanged: (value) => setDialogState(
                        () => assetTypeFilter = value ?? 'All',
                      ),
                    ),
                    const SizedBox(height: 10),
                    DropdownButtonFormField<String>(
                      isExpanded: true,
                      key: ValueKey('detail-identifier-$identifierFilter'),
                      initialValue: identifierFilter,
                      decoration: const InputDecoration(
                        labelText: 'Identifier Type',
                        prefixIcon: Icon(Icons.qr_code_2_outlined, size: 18),
                        prefixIconConstraints: BoxConstraints.tightFor(
                          width: 40,
                          height: 42,
                        ),
                        contentPadding: EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 10,
                        ),
                        isDense: true,
                      ),
                      items:
                          const [
                                'All',
                                'QR Code',
                                'Barcode',
                                'Serial Number',
                                'No Scan Code',
                              ]
                              .map(
                                (value) => DropdownMenuItem(
                                  value: value,
                                  child: Text(
                                    value,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              )
                              .toList(),
                      onChanged: (value) => setDialogState(
                        () => identifierFilter = value ?? 'All',
                      ),
                    ),
                  ],
                ),
              ),
              actionsPadding: const EdgeInsets.fromLTRB(20, 8, 20, 18),
              actions: [
                TextButton(
                  style: TextButton.styleFrom(
                    minimumSize: const Size(0, 38),
                    visualDensity: VisualDensity.compact,
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                  ),
                  onPressed: () => setDialogState(() {
                    assetFilter = 'All';
                    assetTypeFilter = 'All';
                    identifierFilter = 'All';
                  }),
                  child: const Text('Reset'),
                ),
                FilledButton(
                  style: FilledButton.styleFrom(
                    backgroundColor: _themeDark,
                    foregroundColor: Colors.white,
                    minimumSize: const Size(0, 38),
                    visualDensity: VisualDensity.compact,
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                  ),
                  onPressed: () => Navigator.pop(context, (
                    assetFilter: assetFilter,
                    assetTypeFilter: assetTypeFilter,
                    identifierFilter: identifierFilter,
                  )),
                  child: const Text('Apply'),
                ),
              ],
            ),
          ),
        );

    if (result == null || !mounted) return;
    setState(() {
      _assetFilter = result.assetFilter;
      _assetTypeFilter = result.assetTypeFilter;
      _identifierFilter = result.identifierFilter;
      _resetPage();
    });
  }

  Future<void> _showFilters() async {
    var auditFilter = _auditFilter;
    var assetFilter = _assetFilter;
    var assetTypeFilter = _assetTypeFilter;
    var identifierFilter = _identifierFilter;
    var locationFilters = {..._locationFilters};
    final availableLocations = _availableLocations;
    final result =
        await showDialog<
          ({
            String auditFilter,
            String assetFilter,
            String assetTypeFilter,
            String identifierFilter,
            Set<String> locationFilters,
          })
        >(
          context: context,
          builder: (context) => StatefulBuilder(
            builder: (context, setSheetState) => Dialog(
              backgroundColor: Colors.transparent,
              insetPadding: const EdgeInsets.symmetric(
                horizontal: 18,
                vertical: 24,
              ),
              child: Container(
                width: 520,
                constraints: BoxConstraints(
                  maxHeight: MediaQuery.sizeOf(context).height * 0.86,
                ),
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surface,
                  borderRadius: BorderRadius.circular(22),
                  boxShadow: const [
                    BoxShadow(
                      color: Color(0x33000000),
                      blurRadius: 28,
                      offset: Offset(0, 12),
                    ),
                  ],
                ),
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Row(
                        children: [
                          CircleAvatar(
                            backgroundColor: Color(0xFFFFE5E8),
                            foregroundColor: _themeDark,
                            child: Icon(Icons.tune),
                          ),
                          SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Filter Records',
                                  style: TextStyle(
                                    color: _themeDark,
                                    fontSize: 20,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                Text(
                                  'Refine the audits and assets shown below.',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: Colors.grey,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 22),
                      DropdownButtonFormField<String>(
                        isExpanded: true,
                        key: ValueKey('audit-$auditFilter'),
                        initialValue: auditFilter,
                        decoration: const InputDecoration(
                          labelText: 'Audit Status',
                          prefixIcon: Icon(Icons.assignment_outlined),
                        ),
                        items: const ['All', 'Active', 'Closed']
                            .map(
                              (value) => DropdownMenuItem(
                                value: value,
                                child: Text(value),
                              ),
                            )
                            .toList(),
                        onChanged: (value) =>
                            setSheetState(() => auditFilter = value ?? 'All'),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<String>(
                        isExpanded: true,
                        key: ValueKey('asset-$assetFilter'),
                        initialValue: assetFilter,
                        decoration: const InputDecoration(
                          labelText: 'Asset Status',
                          prefixIcon: Icon(Icons.inventory_2_outlined),
                        ),
                        items: const ['All', 'Pending', 'Verified']
                            .map(
                              (value) => DropdownMenuItem(
                                value: value,
                                child: Text(value),
                              ),
                            )
                            .toList(),
                        onChanged: (value) =>
                            setSheetState(() => assetFilter = value ?? 'All'),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<String>(
                        isExpanded: true,
                        key: ValueKey('asset-type-$assetTypeFilter'),
                        initialValue: assetTypeFilter,
                        decoration: const InputDecoration(
                          labelText: 'Asset Type',
                          prefixIcon: Icon(Icons.category_outlined),
                        ),
                        items: const ['All', 'Individual', 'Bulk']
                            .map(
                              (value) => DropdownMenuItem(
                                value: value,
                                child: Text(value),
                              ),
                            )
                            .toList(),
                        onChanged: (value) => setSheetState(
                          () => assetTypeFilter = value ?? 'All',
                        ),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<String>(
                        isExpanded: true,
                        key: ValueKey('identifier-$identifierFilter'),
                        initialValue: identifierFilter,
                        decoration: const InputDecoration(
                          labelText: 'Identifier Type',
                          prefixIcon: Icon(Icons.qr_code_2_outlined),
                        ),
                        items:
                            const [
                                  'All',
                                  'QR Code',
                                  'Barcode',
                                  'Serial Number',
                                  'No Scan Code',
                                ]
                                .map(
                                  (value) => DropdownMenuItem(
                                    value: value,
                                    child: Text(value),
                                  ),
                                )
                                .toList(),
                        onChanged: (value) => setSheetState(
                          () => identifierFilter = value ?? 'All',
                        ),
                      ),
                      const SizedBox(height: 14),
                      _LocationMultiSelect(
                        branchName: _selected?.branchName ?? 'Selected branch',
                        locations: availableLocations,
                        selected: locationFilters,
                        onChanged: (locations) =>
                            setSheetState(() => locationFilters = locations),
                      ),
                      const SizedBox(height: 22),
                      LayoutBuilder(
                        builder: (context, constraints) {
                          final resetButton = OutlinedButton(
                            style: OutlinedButton.styleFrom(
                              minimumSize: const Size(0, 44),
                              padding: const EdgeInsets.symmetric(
                                horizontal: 10,
                              ),
                            ),
                            onPressed: () => setSheetState(() {
                              auditFilter = 'All';
                              assetFilter = 'All';
                              assetTypeFilter = 'All';
                              identifierFilter = 'All';
                              locationFilters = {};
                            }),
                            child: const Text('Reset'),
                          );
                          final cancelButton = OutlinedButton(
                            style: OutlinedButton.styleFrom(
                              minimumSize: const Size(0, 44),
                              padding: const EdgeInsets.symmetric(
                                horizontal: 10,
                              ),
                            ),
                            onPressed: () => Navigator.pop(context),
                            child: const Text('Cancel'),
                          );
                          final applyButton = FilledButton(
                            style: FilledButton.styleFrom(
                              minimumSize: const Size(0, 44),
                              padding: const EdgeInsets.symmetric(
                                horizontal: 10,
                              ),
                              backgroundColor: _themeDark,
                              foregroundColor: Colors.white,
                            ),
                            onPressed: () => Navigator.pop(context, (
                              auditFilter: auditFilter,
                              assetFilter: assetFilter,
                              assetTypeFilter: assetTypeFilter,
                              identifierFilter: identifierFilter,
                              locationFilters: locationFilters,
                            )),
                            child: const Text('Apply'),
                          );

                          if (constraints.maxWidth < 280) {
                            return Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                resetButton,
                                const SizedBox(height: 8),
                                cancelButton,
                                const SizedBox(height: 8),
                                applyButton,
                              ],
                            );
                          }

                          return Row(
                            children: [
                              Expanded(child: resetButton),
                              const SizedBox(width: 8),
                              Expanded(child: cancelButton),
                              const SizedBox(width: 8),
                              Expanded(child: applyButton),
                            ],
                          );
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        );
    if (result == null || !mounted) return;
    setState(() {
      _auditFilter = result.auditFilter;
      _assetFilter = result.assetFilter;
      _assetTypeFilter = result.assetTypeFilter;
      _identifierFilter = result.identifierFilter;
      _locationFilters = Set.unmodifiable(result.locationFilters);
      _resetPage();
    });
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authControllerProvider).session;
    final isDarkMode = ref.watch(themeModeProvider) == ThemeMode.dark;
    final audit = _selected;
    return PopScope<void>(
      canPop: !_showAuditDetails,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && _showAuditDetails) _closeAuditDetails();
      },
      child: Scaffold(
        resizeToAvoidBottomInset: true,
        appBar: AppBar(
          backgroundColor: Theme.of(context).colorScheme.surface,
          surfaceTintColor: Colors.transparent,
          foregroundColor: _themeDark,
          elevation: 0,
          scrolledUnderElevation: 0,
          centerTitle: false,
          titleSpacing: _showAuditDetails ? 0 : 12,
          shape: const Border(bottom: BorderSide(color: Color(0xFFE4E7EC))),
          leading: _showAuditDetails
              ? IconButton(
                  tooltip: 'Back to assigned audits',
                  onPressed: _closeAuditDetails,
                  icon: const Icon(Icons.arrow_back),
                )
              : null,
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
                padding: const EdgeInsets.symmetric(
                  horizontal: 8,
                  vertical: 10,
                ),
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
                                  color: Theme.of(
                                    context,
                                  ).colorScheme.onSurface,
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
                  unawaited(
                    ref.read(authControllerProvider.notifier).signOut(),
                  );
                }
              },
              itemBuilder: (context) => [
                PopupMenuItem<String>(
                  enabled: false,
                  child: _AuditorProfileSummary(
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
                  child: Row(
                    children: [
                      Icon(
                        isDarkMode
                            ? Icons.light_mode_outlined
                            : Icons.dark_mode_outlined,
                        color: _themeDark,
                        size: 20,
                      ),
                      const SizedBox(width: 10),
                      Text(isDarkMode ? 'Light theme' : 'Dark theme'),
                    ],
                  ),
                ),
                const PopupMenuItem<String>(
                  value: 'signOut',
                  height: 42,
                  child: Row(
                    children: [
                      Icon(Icons.logout, color: _themeDark, size: 20),
                      SizedBox(width: 10),
                      Text('Sign Out'),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(width: 6),
          ],
        ),
        floatingActionButton:
            !_showAuditDetails || audit == null || !audit.isActive
            ? null
            : FloatingActionButton(
                onPressed: _scan,
                tooltip: 'Scan asset',
                backgroundColor: _themeDark,
                foregroundColor: Colors.white,
                child: const Icon(Icons.qr_code_scanner),
              ),
        body: CorporateWaveBackground(
          variant: CorporateWaveVariant.audits,
          child: _loading
              ? const Center(child: TechyLoader(size: 40))
              : _error != null
              ? _Message(icon: Icons.error_outline, text: _error!)
              : _audits.isEmpty
              ? const _Message(
                  icon: Icons.assignment_outlined,
                  text: 'No audit is assigned to your account.',
                )
              : _showAuditDetails && audit != null
              ? _buildAuditAssetsView(audit)
              : _buildAssignedAuditsView(),
        ),
      ),
    );
  }

  Widget _buildAssignedAuditsView() {
    final audits = _visibleAudits;
    return RefreshIndicator(
      onRefresh: () => _load(),
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(16, 18, 16, 92),
        children: [
          Text(
            'Assigned audits',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w800,
              color: const Color(0xFF201A1C),
            ),
          ),
          const SizedBox(height: 5),
          Text(
            'Select an audit to view its authorized locations and assets.',
            style: Theme.of(
              context,
            ).textTheme.bodyMedium?.copyWith(color: const Color(0xFF667085)),
          ),
          const SizedBox(height: 16),
          _buildSearchBar('Search assigned audits'),
          const SizedBox(height: 14),
          LayoutBuilder(
            builder: (context, constraints) => SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: ConstrainedBox(
                constraints: BoxConstraints(minWidth: constraints.maxWidth),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    _AuditStatusChip(
                      label: 'In Progress',
                      selected: _auditFilter == 'Active',
                      onSelected: () => setState(() => _auditFilter = 'Active'),
                    ),
                    const SizedBox(width: 8),
                    _AuditStatusChip(
                      label: 'Completed',
                      selected: _auditFilter == 'Closed',
                      onSelected: () => setState(() => _auditFilter = 'Closed'),
                    ),
                    const SizedBox(width: 8),
                    _AuditStatusChip(
                      label: 'All',
                      selected: _auditFilter == 'All',
                      onSelected: () => setState(() => _auditFilter = 'All'),
                    ),
                  ],
                ),
              ),
            ),
          ),
          const SizedBox(height: 16),
          if (audits.isEmpty)
            const _InlineEmptyMessage(
              icon: Icons.search_off_outlined,
              message: 'No assigned audits match your search.',
            )
          else
            ...audits.map(
              (audit) => _AssignedAuditCard(
                audit: audit,
                onTap: () => _openAuditDetails(audit),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildAuditAssetsView(AssignedAudit audit) {
    // Read from the root Flutter view. Scaffold removes the inset from the
    // body's MediaQuery after resizing, which otherwise makes an open keyboard
    // look closed here and leaves the fixed header controls overflowing.
    final keyboardRequiresCompactLayout =
        _isSearchFocused || View.of(context).viewInsets.bottom > 0;
    final locations = _availableLocations;
    final selectedLocation = locations.contains(_selectedLocation)
        ? _selectedLocation
        : locations.firstOrNull;
    final query = _query.trim().toUpperCase();
    final assets = audit.assets.where((asset) {
      final locationMatches =
          selectedLocation == null ||
          _locationFor(asset, audit) == selectedLocation;
      final statusMatches =
          _assetFilter == 'All' ||
          (_assetFilter == 'Verified' ? asset.isVerified : !asset.isVerified);
      final typeMatches =
          _assetTypeFilter == 'All' ||
          (_assetTypeFilter == 'Bulk' ? asset.isBulk : !asset.isBulk);
      final identifierMatches = switch (_identifierFilter) {
        'QR Code' => asset.qrCodeValue?.trim().isNotEmpty == true,
        'Barcode' => asset.barcodeValue?.trim().isNotEmpty == true,
        'Serial Number' => asset.serialNumber?.trim().isNotEmpty == true,
        'No Scan Code' =>
          asset.qrCodeValue?.trim().isNotEmpty != true &&
              asset.barcodeValue?.trim().isNotEmpty != true,
        _ => true,
      };
      return locationMatches &&
          statusMatches &&
          typeMatches &&
          identifierMatches &&
          (query.isEmpty || _assetMatchesQuery(asset, query));
    }).toList();
    final verified = audit.assets.where((asset) => asset.isVerified).length;
    final activeAssetFilterCount =
        (_assetFilter == 'All' ? 0 : 1) +
        (_assetTypeFilter == 'All' ? 0 : 1) +
        (_identifierFilter == 'All' ? 0 : 1);

    return LayoutBuilder(
      builder: (context, constraints) {
        // The height guard also covers intermediate keyboard animation frames
        // where the viewport has already shrunk but the inset notification has
        // not reached this widget yet.
        final compactForSearch =
            keyboardRequiresCompactLayout || constraints.maxHeight < 520;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 10),
              child: _SelectedAuditSummary(
                audit: audit,
                verified: verified,
                // Any keyboard can resize this underlying page, including the
                // remarks field in the verification sheet. Always render the
                // compact summary while that reduced viewport is active.
                isExpanded: compactForSearch ? false : _isAuditSummaryExpanded,
                onToggle: () => setState(
                  () => _isAuditSummaryExpanded = !_isAuditSummaryExpanded,
                ),
                onViewAssets: () => _openAssetListPage(audit),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
              child: _buildSearchBar('Search assets in this audit'),
            ),
            if (!compactForSearch)
              Container(
                color: Theme.of(
                  context,
                ).colorScheme.surface.withValues(alpha: 0.96),
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          flex: 4,
                          child: _AssignedLocationDropdown(
                            locations: locations,
                            selectedLocation: selectedLocation,
                            onSelected: (location) => setState(() {
                              _selectedLocation = location;
                              _resetPage();
                            }),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          flex: 1,
                          child: Tooltip(
                            message: 'Filter assets',
                            child: DecoratedBox(
                              decoration: BoxDecoration(
                                color: Theme.of(context).colorScheme.surface,
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                  color: Theme.of(
                                    context,
                                  ).colorScheme.outlineVariant,
                                ),
                                boxShadow: const [
                                  BoxShadow(
                                    color: Color(0x0F101828),
                                    blurRadius: 8,
                                    offset: Offset(0, 3),
                                  ),
                                ],
                              ),
                              child: Material(
                                color: Colors.transparent,
                                borderRadius: BorderRadius.circular(12),
                                clipBehavior: Clip.antiAlias,
                                child: InkWell(
                                  onTap: _showAuditAssetFilters,
                                  child: SizedBox(
                                    height: responsiveControlHeight(context),
                                    child: Center(
                                      child: Icon(
                                        activeAssetFilterCount == 0
                                            ? Icons.filter_alt_outlined
                                            : Icons.filter_alt_rounded,
                                        size: 21,
                                        color: _themeDark,
                                      ),
                                    ),
                                  ),
                                ),
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            const Divider(height: 1),
            Expanded(
              child: NotificationListener<ScrollUpdateNotification>(
                onNotification: (notification) {
                  final isUserScrollingUpThePage =
                      notification.dragDetails != null &&
                      (notification.scrollDelta ?? 0) > 3;
                  if (isUserScrollingUpThePage && _isAuditSummaryExpanded) {
                    setState(() => _isAuditSummaryExpanded = false);
                  }
                  return false;
                },
                child: RefreshIndicator(
                  onRefresh: () => _load(audit.id),
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 14, 16, 100),
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              selectedLocation ?? audit.branchName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: Theme.of(context).textTheme.titleMedium
                                  ?.copyWith(fontWeight: FontWeight.w700),
                            ),
                          ),
                          Text(
                            '${assets.length} assets',
                            style: Theme.of(context).textTheme.labelMedium
                                ?.copyWith(color: _themeDark),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      if (assets.isEmpty)
                        const _InlineEmptyMessage(
                          icon: Icons.inventory_2_outlined,
                          message: 'No assets match this location and search.',
                        )
                      else
                        _AssetCards(
                          assets: assets,
                          onAssetTap: (asset) => unawaited(
                            _scanForAudit(audit, expectedAsset: asset),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _buildSearchBar(String hintText) => TextField(
    controller: _searchController,
    focusNode: _searchFocusNode,
    onTap: () {
      if (_showAuditDetails && _isAuditSummaryExpanded) {
        setState(() => _isAuditSummaryExpanded = false);
      }
    },
    decoration: InputDecoration(
      hintText: hintText,
      constraints: const BoxConstraints(minHeight: 48),
      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      prefixIconConstraints: const BoxConstraints.tightFor(
        width: 40,
        height: 40,
      ),
      suffixIconConstraints: const BoxConstraints.tightFor(
        width: 38,
        height: 38,
      ),
      prefixIcon: const Icon(Icons.search, size: 20),
      suffixIcon: _query.isEmpty
          ? null
          : IconButton(
              constraints: const BoxConstraints.tightFor(width: 38, height: 38),
              padding: EdgeInsets.zero,
              visualDensity: VisualDensity.compact,
              onPressed: () => setState(() {
                _searchController.clear();
                _query = '';
              }),
              icon: const Icon(Icons.close, size: 19),
            ),
    ),
    onChanged: (value) => setState(() => _query = value),
  );

  void _openAuditDetails(AssignedAudit audit) {
    setState(() {
      _selectedId = audit.id;
      _showAuditDetails = true;
      _isAuditSummaryExpanded = true;
      _selectedLocation = audit.assets
          .map((asset) => _locationFor(asset, audit))
          .firstOrNull;
      _assetFilter = 'All';
      _assetTypeFilter = 'All';
      _identifierFilter = 'All';
      _searchController.clear();
      _query = '';
    });
  }

  void _closeAuditDetails() {
    setState(() {
      _showAuditDetails = false;
      _selectedLocation = null;
      _searchController.clear();
      _query = '';
    });
  }

  // Retained temporarily while the redesigned flow is validated in the field.
  // ignore: unused_element
  Widget _buildLegacy(BuildContext context) {
    final session = ref.watch(authControllerProvider).session;
    final activeFilterCount =
        (_auditFilter == 'All' ? 0 : 1) +
        (_assetFilter == 'All' ? 0 : 1) +
        (_assetTypeFilter == 'All' ? 0 : 1) +
        (_identifierFilter == 'All' ? 0 : 1) +
        (_locationFilters.isEmpty ? 0 : 1);
    final audit = _selected;
    final visibleAudits = _visibleAudits;
    final visibleAssets = _visibleAssets;
    final pageCount = visibleAssets.isEmpty
        ? 1
        : (visibleAssets.length / _pageSize).ceil();
    final safePage = _pageIndex.clamp(0, pageCount - 1);
    final pageStart = safePage * _pageSize;
    final pageAssets = visibleAssets.skip(pageStart).take(_pageSize).toList();
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        shadowColor: Colors.transparent,
        scrolledUnderElevation: 0,
        flexibleSpace: const DecoratedBox(
          decoration: BoxDecoration(color: Colors.white),
        ),
        foregroundColor: _themeDark,
        elevation: 0,
        shape: const Border(bottom: BorderSide(color: Color(0xFFEBC8CD))),
        titleSpacing: 18,
        title: const FujitecHeaderLogo(),
        actions: [
          PopupMenuButton<String>(
            tooltip: 'Profile',
            icon: const Icon(Icons.account_circle_outlined),
            color: Colors.white,
            surfaceTintColor: Colors.white,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
              side: const BorderSide(color: Color(0xFFE8C5C9)),
            ),
            onSelected: (value) {
              if (value == 'refresh') {
                unawaited(_load(_selectedId));
              } else if (value == 'signOut') {
                unawaited(ref.read(authControllerProvider.notifier).signOut());
              }
            },
            itemBuilder: (context) => [
              PopupMenuItem<String>(
                enabled: false,
                child: _AuditorProfileSummary(
                  name: session?.displayName.trim().isNotEmpty == true
                      ? session!.displayName
                      : 'Auditor',
                  username: session?.username ?? '--',
                ),
              ),
              const PopupMenuDivider(),
              PopupMenuItem<String>(
                value: 'refresh',
                enabled: !_loading,
                child: const Row(
                  children: [
                    Icon(Icons.refresh, color: _themeDark),
                    SizedBox(width: 12),
                    Text(
                      'Refresh',
                      style: TextStyle(
                        color: _themeDark,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
              const PopupMenuItem<String>(
                value: 'signOut',
                child: Row(
                  children: [
                    Icon(Icons.logout, color: _themeLight),
                    SizedBox(width: 12),
                    Text(
                      'Sign Out',
                      style: TextStyle(
                        color: _themeLight,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(width: 8),
        ],
      ),
      floatingActionButton: audit == null || !audit.isActive
          ? null
          : FloatingActionButton(
              onPressed: _scan,
              tooltip: 'Scan asset',
              backgroundColor: _themeDark,
              foregroundColor: Colors.white,
              child: const Icon(Icons.qr_code_scanner),
            ),
      body: CorporateWaveBackground(
        variant: CorporateWaveVariant.audits,
        child: _loading
            ? const Center(child: TechyLoader(size: 40))
            : _error != null
            ? _Message(icon: Icons.error_outline, text: _error!)
            : _audits.isEmpty
            ? const _Message(
                icon: Icons.assignment_outlined,
                text: 'No audit is assigned to your account.',
              )
            : RefreshIndicator(
                onRefresh: () => _load(_selectedId),
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 18, 16, 92),
                  children: [
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 22,
                        vertical: 20,
                      ),
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(
                          colors: [_themeDark, _themeLight],
                        ),
                        borderRadius: BorderRadius.circular(18),
                        boxShadow: const [
                          BoxShadow(
                            color: Color(0x26D01126),
                            blurRadius: 18,
                            offset: Offset(0, 8),
                          ),
                        ],
                      ),
                      child: const Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'AUDITOR WORKSPACE',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 1.4,
                            ),
                          ),
                          SizedBox(height: 8),
                          Text(
                            'My Audits',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 28,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          SizedBox(height: 5),
                          Text(
                            'Review assigned assets and record physical verification.',
                            style: TextStyle(color: Colors.white, fontSize: 13),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 14),
                    TextField(
                      controller: _searchController,
                      decoration: InputDecoration(
                        hintText: 'Search audit, asset, serial or location',
                        constraints: const BoxConstraints(minHeight: 48),
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 8,
                        ),
                        prefixIconConstraints: const BoxConstraints.tightFor(
                          width: 40,
                          height: 40,
                        ),
                        suffixIconConstraints: const BoxConstraints.tightFor(
                          width: 38,
                          height: 38,
                        ),
                        prefixIcon: const Icon(Icons.search, size: 20),
                        suffixIcon: _query.isEmpty
                            ? null
                            : IconButton(
                                constraints: const BoxConstraints.tightFor(
                                  width: 38,
                                  height: 38,
                                ),
                                padding: EdgeInsets.zero,
                                visualDensity: VisualDensity.compact,
                                onPressed: () => setState(() {
                                  _searchController.clear();
                                  _query = '';
                                  _resetPage();
                                }),
                                icon: const Icon(Icons.close, size: 19),
                              ),
                      ),
                      onChanged: (value) => setState(() {
                        _query = value;
                        _resetPage();
                      }),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            activeFilterCount == 0
                                ? 'Showing all records'
                                : '$activeFilterCount active filter${activeFilterCount == 1 ? '' : 's'} applied',
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              color: Colors.grey.shade700,
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        FilledButton.icon(
                          onPressed: _showFilters,
                          style: FilledButton.styleFrom(
                            minimumSize: const Size(0, 36),
                            backgroundColor: _themeDark,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(horizontal: 11),
                            visualDensity: VisualDensity.compact,
                            tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                          ),
                          icon: const Icon(Icons.tune, size: 16),
                          label: Text(
                            activeFilterCount == 0
                                ? 'Filters'
                                : 'Filters ($activeFilterCount)',
                            style: const TextStyle(fontSize: 12),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 14),
                    DropdownButtonFormField<int>(
                      isExpanded: true,
                      initialValue:
                          visibleAudits.any((item) => item.id == _selectedId)
                          ? _selectedId
                          : null,
                      decoration: const InputDecoration(
                        labelText: 'Assigned audit',
                        prefixIcon: Icon(Icons.assignment_outlined),
                      ),
                      hint: const Text('No audit matches the filters'),
                      items: visibleAudits
                          .map(
                            (item) => DropdownMenuItem(
                              value: item.id,
                              child: Text(
                                item.auditName,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          )
                          .toList(),
                      onChanged: (value) => setState(() {
                        _selectedId = value;
                        _locationFilters = const {};
                        _resetPage();
                      }),
                    ),
                    if (audit != null) ...[
                      const SizedBox(height: 14),
                      Card(
                        color: Colors.white,
                        surfaceTintColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                          side: const BorderSide(color: Color(0xFFF0C4C8)),
                        ),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            children: [
                              const Icon(
                                Icons.location_on_outlined,
                                color: Color(0xFFD01126),
                              ),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Text('Assigned branch'),
                                    Text(
                                      audit.branchName,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              Text(
                                '${audit.assets.where((a) => !a.isVerified).length} pending',
                                style: const TextStyle(
                                  color: Color(0xFFD01126),
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 14),
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              'Asset register',
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                          ),
                          SegmentedButton<bool>(
                            segments: const [
                              ButtonSegment(
                                value: false,
                                icon: Icon(Icons.table_rows_outlined, size: 18),
                                tooltip: 'Table view',
                              ),
                              ButtonSegment(
                                value: true,
                                icon: Icon(
                                  Icons.view_agenda_outlined,
                                  size: 18,
                                ),
                                tooltip: 'Card view',
                              ),
                            ],
                            selected: {_showAssetCards},
                            showSelectedIcon: false,
                            style: const ButtonStyle(
                              visualDensity: VisualDensity.compact,
                              tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            ),
                            onSelectionChanged: (selection) => setState(
                              () => _showAssetCards = selection.first,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      _TableControls(
                        assetCount: visibleAssets.length,
                        pageSize: _pageSize,
                        pageSizes: _pageSizes,
                        currentPage: safePage,
                        pageCount: pageCount,
                        onPageSizeChanged: (value) => setState(() {
                          _pageSize = value;
                          _resetPage();
                        }),
                        onPrevious: safePage == 0
                            ? null
                            : () => setState(() => _pageIndex = safePage - 1),
                        onNext: safePage >= pageCount - 1
                            ? null
                            : () => setState(() => _pageIndex = safePage + 1),
                      ),
                      const SizedBox(height: 10),
                      if (pageAssets.isEmpty)
                        const Padding(
                          padding: EdgeInsets.symmetric(vertical: 28),
                          child: Text(
                            'No assets match the selected search and filters.',
                            textAlign: TextAlign.center,
                          ),
                        ),
                      if (pageAssets.isNotEmpty)
                        if (_showAssetCards)
                          _AssetCards(
                            assets: pageAssets,
                            onAssetTap: (asset) => unawaited(
                              _scanForAudit(audit, expectedAsset: asset),
                            ),
                          )
                        else
                          _AssetTable(
                            assets: pageAssets,
                            onAssetTap: (asset) => unawaited(
                              _scanForAudit(audit, expectedAsset: asset),
                            ),
                          ),
                      const SizedBox(height: 10),
                      _TableControls(
                        assetCount: visibleAssets.length,
                        pageSize: _pageSize,
                        pageSizes: _pageSizes,
                        currentPage: safePage,
                        pageCount: pageCount,
                        onPageSizeChanged: (value) => setState(() {
                          _pageSize = value;
                          _resetPage();
                        }),
                        onPrevious: safePage == 0
                            ? null
                            : () => setState(() => _pageIndex = safePage - 1),
                        onNext: safePage >= pageCount - 1
                            ? null
                            : () => setState(() => _pageIndex = safePage + 1),
                      ),
                    ],
                  ],
                ),
              ),
      ),
    );
  }
}

class _AuditStatusChip extends StatelessWidget {
  const _AuditStatusChip({
    required this.label,
    required this.selected,
    required this.onSelected,
  });

  final String label;
  final bool selected;
  final VoidCallback onSelected;

  @override
  Widget build(BuildContext context) => ChoiceChip(
    label: Text(label),
    selected: selected,
    showCheckmark: false,
    selectedColor: const Color(0xFFD01126),
    backgroundColor: Theme.of(context).colorScheme.surface,
    side: BorderSide(
      color: selected
          ? const Color(0xFFD01126)
          : Theme.of(context).colorScheme.outlineVariant,
    ),
    labelStyle: Theme.of(context).textTheme.labelMedium?.copyWith(
      color: selected
          ? Colors.white
          : Theme.of(context).colorScheme.onSurfaceVariant,
      fontWeight: FontWeight.w700,
    ),
    onSelected: (_) => onSelected(),
  );
}

class _AssignedAuditCard extends StatelessWidget {
  const _AssignedAuditCard({required this.audit, required this.onTap});

  final AssignedAudit audit;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final total = audit.assets.length;
    final verified = audit.assets.where((asset) => asset.isVerified).length;
    final progress = total == 0 ? 0.0 : verified / total;
    final percentage = (progress * 100).round();
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: const BorderSide(color: Color(0xFFE4E7EC)),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(15),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        audit.auditName,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                          color: Theme.of(context).colorScheme.onSurface,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          const Icon(
                            Icons.location_on_outlined,
                            size: 16,
                            color: Color(0xFF667085),
                          ),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              audit.branchName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(color: const Color(0xFF667085)),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 7),
                      Row(
                        children: [
                          _AuditStateLabel(isActive: audit.isActive),
                          const Spacer(),
                          Text(
                            '$verified / $total',
                            style: Theme.of(context).textTheme.labelMedium
                                ?.copyWith(color: const Color(0xFF475467)),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 14),
                SizedBox(
                  width: 48,
                  height: 48,
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      CircularProgressIndicator(
                        value: progress,
                        strokeWidth: 3,
                        backgroundColor: const Color(0xFFE4E7EC),
                        color: audit.isActive
                            ? const Color(0xFFD01126)
                            : const Color(0xFF18794E),
                      ),
                      Center(
                        child: Text(
                          '$percentage%',
                          style: Theme.of(context).textTheme.labelSmall
                              ?.copyWith(fontWeight: FontWeight.w800),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                const Icon(
                  Icons.chevron_right,
                  color: Color(0xFF98A2B3),
                  size: 20,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _AuditStateLabel extends StatelessWidget {
  const _AuditStateLabel({required this.isActive});

  final bool isActive;

  @override
  Widget build(BuildContext context) => Text(
    isActive ? 'In Progress' : 'Completed',
    style: Theme.of(context).textTheme.labelSmall?.copyWith(
      color: isActive ? const Color(0xFFD01126) : const Color(0xFF18794E),
      fontWeight: FontWeight.w700,
    ),
  );
}

class _SelectedAuditSummary extends StatelessWidget {
  const _SelectedAuditSummary({
    required this.audit,
    required this.verified,
    required this.isExpanded,
    required this.onToggle,
    required this.onViewAssets,
  });

  final AssignedAudit audit;
  final int verified;
  final bool isExpanded;
  final VoidCallback onToggle;
  final VoidCallback onViewAssets;

  @override
  Widget build(BuildContext context) {
    final total = audit.assets.length;
    final pending = total - verified;
    final notFound = total - verified - pending;
    final progress = total == 0 ? 0.0 : verified / total;
    final percentage = (progress * 100).round();

    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Theme.of(context).colorScheme.outlineVariant),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0F101828),
            blurRadius: 12,
            offset: Offset(0, 4),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 15, 16, 14),
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
                        style: Theme.of(context).textTheme.titleSmall?.copyWith(
                          color: Theme.of(context).colorScheme.onSurface,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 9,
                        vertical: 5,
                      ),
                      decoration: BoxDecoration(
                        color: audit.isActive
                            ? const Color(0xFFFFECEE)
                            : const Color(0xFFE9F7EF),
                        borderRadius: BorderRadius.circular(7),
                      ),
                      child: Text(
                        audit.isActive ? 'In Progress' : 'Completed',
                        style: TextStyle(
                          color: audit.isActive
                              ? const Color(0xFFD01126)
                              : const Color(0xFF16803A),
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    const SizedBox(width: 4),
                    IconButton(
                      tooltip: isExpanded
                          ? 'Hide audit summary'
                          : 'Show audit summary',
                      onPressed: onToggle,
                      visualDensity: VisualDensity.compact,
                      constraints: const BoxConstraints.tightFor(
                        width: 34,
                        height: 34,
                      ),
                      padding: EdgeInsets.zero,
                      alignment: Alignment.center,
                      icon: AnimatedSwitcher(
                        duration: const Duration(milliseconds: 220),
                        transitionBuilder: (child, animation) =>
                            FadeTransition(opacity: animation, child: child),
                        child: Icon(
                          isExpanded
                              ? Icons.keyboard_arrow_up_rounded
                              : Icons.keyboard_arrow_down_rounded,
                          key: ValueKey(isExpanded),
                          color: const Color(0xFFD01126),
                          size: 24,
                        ),
                      ),
                    ),
                  ],
                ),
                if (isExpanded) ...[
                  const SizedBox(height: 5),
                  Row(
                    children: [
                      const Icon(
                        Icons.location_on_outlined,
                        size: 16,
                        color: Color(0xFF667085),
                      ),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          audit.branchName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(
                                color: Theme.of(
                                  context,
                                ).colorScheme.onSurfaceVariant,
                                fontWeight: FontWeight.w500,
                              ),
                        ),
                      ),
                    ],
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 12),
                    child: Divider(height: 1),
                  ),
                  Text(
                    'Overall Progress',
                    style: Theme.of(context).textTheme.labelMedium?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          '$verified / $total Assets',
                          style: Theme.of(context).textTheme.bodyMedium
                              ?.copyWith(
                                color: Theme.of(
                                  context,
                                ).colorScheme.onSurfaceVariant,
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                      ),
                      Text(
                        '$percentage%',
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(
                              color: Theme.of(context).colorScheme.onSurface,
                              fontWeight: FontWeight.w900,
                            ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 9),
                  LinearProgressIndicator(
                    value: progress,
                    minHeight: 7,
                    borderRadius: BorderRadius.circular(99),
                    color: const Color(0xFFD01126),
                    backgroundColor: const Color(0xFFFFE5E8),
                  ),
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 14),
                    child: Divider(height: 1),
                  ),
                  IntrinsicHeight(
                    child: Row(
                      children: [
                        Expanded(
                          child: _AuditMetric(
                            label: 'Verified',
                            value: verified,
                            color: const Color(0xFF16803A),
                          ),
                        ),
                        const VerticalDivider(width: 1),
                        Expanded(
                          child: _AuditMetric(
                            label: 'Pending',
                            value: pending,
                            color: const Color(0xFFC77700),
                          ),
                        ),
                        const VerticalDivider(width: 1),
                        Expanded(
                          child: _AuditMetric(
                            label: 'Not Found',
                            value: notFound,
                            color: const Color(0xFFD01126),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ],
            ),
          ),
          if (isExpanded) ...[
            const Divider(height: 1),
            Material(
              color: Theme.of(context).colorScheme.surface,
              child: InkWell(
                onTap: onViewAssets,
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 13,
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.format_list_bulleted_rounded, size: 19),
                      const SizedBox(width: 9),
                      Expanded(
                        child: Text(
                          'View Asset List',
                          style: TextStyle(
                            color: Theme.of(context).colorScheme.onSurface,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      const Icon(
                        Icons.chevron_right_rounded,
                        color: Color(0xFF667085),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _AuditAssetListPage extends StatelessWidget {
  const _AuditAssetListPage({required this.audit});

  final AssignedAudit audit;

  String _locationFor(AssignedAsset asset) {
    final location = asset.location?.trim();
    return location == null || location.isEmpty ? audit.branchName : location;
  }

  @override
  Widget build(BuildContext context) {
    final verified = audit.assets.where((asset) => asset.isVerified).length;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.surface,
        surfaceTintColor: Colors.transparent,
        foregroundColor: const Color(0xFFD01126),
        elevation: 0,
        scrolledUnderElevation: 0,
        shape: const Border(bottom: BorderSide(color: Color(0xFFE4E7EC))),
        titleSpacing: 0,
        title: Row(
          children: [
            const FujitecHeaderLogo(),
            const SizedBox(width: 9),
            Expanded(
              child: Text(
                'Asset List',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: Theme.of(context).colorScheme.onSurface,
                  fontWeight: FontWeight.w800,
                  fontSize: 17,
                ),
              ),
            ),
          ],
        ),
      ),
      body: CorporateWaveBackground(
        variant: CorporateWaveVariant.audits,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              color: const Color(0xF7FFFFFF),
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          audit.auditName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: Color(0xFF1D2939),
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          '${audit.assets.length} assets • $verified verified',
                          style: const TextStyle(
                            color: Color(0xFF667085),
                            fontSize: 12,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 9,
                      vertical: 5,
                    ),
                    decoration: BoxDecoration(
                      color: audit.isActive
                          ? const Color(0xFFFFECEE)
                          : const Color(0xFFE9F7EF),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      audit.isActive ? 'In Progress' : 'Completed',
                      style: TextStyle(
                        color: audit.isActive
                            ? const Color(0xFFD01126)
                            : const Color(0xFF16803A),
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: audit.assets.isEmpty
                  ? const _InlineEmptyMessage(
                      icon: Icons.inventory_2_outlined,
                      message: 'No assets are assigned to this audit.',
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
                      itemCount: audit.assets.length,
                      separatorBuilder: (_, _) => const SizedBox(height: 9),
                      itemBuilder: (context, index) {
                        final asset = audit.assets[index];
                        return Container(
                          padding: const EdgeInsets.all(13),
                          decoration: BoxDecoration(
                            color: Theme.of(context).colorScheme.surface,
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(
                              color: Theme.of(
                                context,
                              ).colorScheme.outlineVariant,
                            ),
                            boxShadow: const [
                              BoxShadow(
                                color: Color(0x0A101828),
                                blurRadius: 8,
                                offset: Offset(0, 3),
                              ),
                            ],
                          ),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                width: 38,
                                height: 38,
                                decoration: BoxDecoration(
                                  color: asset.isVerified
                                      ? const Color(0xFFE9F7EF)
                                      : const Color(0xFFFFECEE),
                                  borderRadius: BorderRadius.circular(10),
                                ),
                                child: Icon(
                                  asset.isVerified
                                      ? Icons.check_circle_outline_rounded
                                      : Icons.cancel_outlined,
                                  color: asset.isVerified
                                      ? const Color(0xFF16803A)
                                      : const Color(0xFFD01126),
                                  size: 23,
                                ),
                              ),
                              const SizedBox(width: 11),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      asset.assetName,
                                      maxLines: 2,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        color: Color(0xFF1D2939),
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      asset.assetNumber,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(
                                        color: Color(0xFFD01126),
                                        fontSize: 12,
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                    const SizedBox(height: 7),
                                    Row(
                                      children: [
                                        const Icon(
                                          Icons.location_on_outlined,
                                          size: 15,
                                          color: Color(0xFF667085),
                                        ),
                                        const SizedBox(width: 4),
                                        Expanded(
                                          child: Text(
                                            _locationFor(asset),
                                            maxLines: 1,
                                            overflow: TextOverflow.ellipsis,
                                            style: const TextStyle(
                                              color: Color(0xFF667085),
                                              fontSize: 12,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ),
                              const SizedBox(width: 8),
                              Text(
                                asset.isVerified ? 'Verified' : 'Pending',
                                style: TextStyle(
                                  color: asset.isVerified
                                      ? const Color(0xFF16803A)
                                      : const Color(0xFFD01126),
                                  fontSize: 12,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ],
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
}

class _AuditMetric extends StatelessWidget {
  const _AuditMetric({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final int value;
  final Color color;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
          color: const Color(0xFF475467),
          fontWeight: FontWeight.w600,
        ),
      ),
      const SizedBox(height: 5),
      Text(
        '$value',
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
          color: color,
          fontWeight: FontWeight.w900,
        ),
      ),
    ],
  );
}

class _InlineEmptyMessage extends StatelessWidget {
  const _InlineEmptyMessage({required this.icon, required this.message});

  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 34, horizontal: 16),
    child: Column(
      children: [
        Icon(icon, color: const Color(0xFF98A2B3), size: 34),
        const SizedBox(height: 10),
        Text(
          message,
          textAlign: TextAlign.center,
          style: Theme.of(
            context,
          ).textTheme.bodyMedium?.copyWith(color: const Color(0xFF667085)),
        ),
      ],
    ),
  );
}

class _AssignedLocationDropdown extends StatefulWidget {
  const _AssignedLocationDropdown({
    required this.locations,
    required this.selectedLocation,
    required this.onSelected,
  });

  final List<String> locations;
  final String? selectedLocation;
  final ValueChanged<String> onSelected;

  @override
  State<_AssignedLocationDropdown> createState() =>
      _AssignedLocationDropdownState();
}

class _AssignedLocationDropdownState extends State<_AssignedLocationDropdown> {
  bool _isOpen = false;

  Future<void> _openSearchablePicker(String selected) async {
    final searchController = TextEditingController();
    var query = '';
    setState(() => _isOpen = true);

    try {
      final location = await showDialog<String>(
        context: context,
        builder: (dialogContext) => StatefulBuilder(
          builder: (context, setDialogState) {
            final mediaQuery = MediaQuery.of(context);
            final availableHeight =
                mediaQuery.size.height - mediaQuery.viewInsets.bottom;
            final contentHeight = (availableHeight - 150).clamp(96.0, 390.0);
            final filteredLocations = widget.locations
                .where(
                  (location) => location.toLowerCase().contains(
                    query.trim().toLowerCase(),
                  ),
                )
                .toList();

            return AlertDialog(
              backgroundColor: Theme.of(context).colorScheme.surface,
              surfaceTintColor: Colors.transparent,
              insetPadding: const EdgeInsets.symmetric(
                horizontal: 22,
                vertical: 12,
              ),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(18),
              ),
              titlePadding: const EdgeInsets.fromLTRB(20, 18, 12, 8),
              title: Row(
                children: [
                  const Icon(
                    Icons.location_on_outlined,
                    color: Color(0xFFD01126),
                    size: 22,
                  ),
                  const SizedBox(width: 8),
                  const Expanded(
                    child: Text(
                      'Select Location',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close',
                    onPressed: () => Navigator.pop(dialogContext),
                    icon: const Icon(Icons.close_rounded),
                  ),
                ],
              ),
              contentPadding: const EdgeInsets.fromLTRB(18, 6, 18, 18),
              content: SizedBox(
                width: 340,
                height: contentHeight,
                child: Column(
                  children: [
                    TextField(
                      controller: searchController,
                      autofocus: false,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.onSurface,
                        fontWeight: FontWeight.w500,
                      ),
                      textInputAction: TextInputAction.search,
                      onChanged: (value) => setDialogState(() => query = value),
                      decoration: InputDecoration(
                        hintText: 'Search assigned locations',
                        prefixIcon: const Icon(Icons.search_rounded, size: 20),
                        suffixIcon: query.isEmpty
                            ? null
                            : IconButton(
                                tooltip: 'Clear search',
                                onPressed: () {
                                  searchController.clear();
                                  setDialogState(() => query = '');
                                },
                                icon: const Icon(Icons.close_rounded, size: 18),
                              ),
                        isDense: true,
                        filled: true,
                        fillColor: Theme.of(
                          context,
                        ).colorScheme.surfaceContainerHighest,
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 12,
                          vertical: 12,
                        ),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(12),
                          borderSide: const BorderSide(
                            color: Color(0xFFD0D5DD),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    Expanded(
                      child: filteredLocations.isEmpty
                          ? const Center(
                              child: Text(
                                'No matching locations found.',
                                style: TextStyle(color: Color(0xFF667085)),
                              ),
                            )
                          : ListView.separated(
                              itemCount: filteredLocations.length,
                              separatorBuilder: (_, _) =>
                                  const Divider(height: 1),
                              itemBuilder: (context, index) {
                                final location = filteredLocations[index];
                                final isSelected = location == selected;
                                return ListTile(
                                  dense: true,
                                  contentPadding: const EdgeInsets.symmetric(
                                    horizontal: 10,
                                    vertical: 2,
                                  ),
                                  tileColor: isSelected
                                      ? const Color(0xFFFFF1F3)
                                      : Colors.transparent,
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(10),
                                  ),
                                  leading: Icon(
                                    isSelected
                                        ? Icons.location_on
                                        : Icons.location_on_outlined,
                                    color: isSelected
                                        ? const Color(0xFFD01126)
                                        : const Color(0xFF667085),
                                    size: 20,
                                  ),
                                  title: Text(
                                    location,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: TextStyle(
                                      color: Theme.of(
                                        context,
                                      ).colorScheme.onSurface,
                                      fontWeight: isSelected
                                          ? FontWeight.w700
                                          : FontWeight.w500,
                                    ),
                                  ),
                                  trailing: isSelected
                                      ? const Icon(
                                          Icons.check_rounded,
                                          color: Color(0xFFD01126),
                                          size: 20,
                                        )
                                      : null,
                                  onTap: () =>
                                      Navigator.pop(dialogContext, location),
                                );
                              },
                            ),
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      );

      if (location != null) widget.onSelected(location);
    } finally {
      // The dialog remains in the tree during its closing animation. Dispose
      // after that animation so the TextField cannot reference a dead controller.
      unawaited(
        Future<void>.delayed(
          const Duration(milliseconds: 400),
          searchController.dispose,
        ),
      );
      if (mounted) setState(() => _isOpen = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final hasLocations = widget.locations.isNotEmpty;
    final selected = hasLocations
        ? (widget.locations.contains(widget.selectedLocation)
              ? widget.selectedLocation!
              : widget.locations.first)
        : null;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: hasLocations ? () => _openSearchablePicker(selected!) : null,
        borderRadius: BorderRadius.circular(12),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          width: double.infinity,
          height: responsiveControlHeight(context),
          padding: const EdgeInsets.symmetric(horizontal: 11),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: hasLocations
                  ? const Color(0xFFD0D5DD)
                  : const Color(0xFFE4E7EC),
            ),
            boxShadow: const [
              BoxShadow(
                color: Color(0x0F101828),
                blurRadius: 8,
                offset: Offset(0, 3),
              ),
            ],
          ),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  hasLocations ? '📍 Select Location' : 'No assigned locations',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: hasLocations
                        ? Theme.of(context).colorScheme.onSurface
                        : const Color(0xFF98A2B3),
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.1,
                  ),
                ),
              ),
              const SizedBox(width: 6),
              AnimatedRotation(
                turns: _isOpen ? 0.5 : 0,
                duration: const Duration(milliseconds: 200),
                curve: Curves.easeOutCubic,
                child: Icon(
                  Icons.keyboard_arrow_down_rounded,
                  size: 21,
                  color: hasLocations
                      ? const Color(0xFF667085)
                      : const Color(0xFFB9C0CA),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LocationMultiSelect extends StatelessWidget {
  const _LocationMultiSelect({
    required this.branchName,
    required this.locations,
    required this.selected,
    required this.onChanged,
  });

  final String branchName;
  final List<String> locations;
  final Set<String> selected;
  final ValueChanged<Set<String>> onChanged;

  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: const Color(0xFFF1F3F5),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(
        color: selected.isEmpty ? Colors.transparent : const Color(0xFFD01126),
      ),
    ),
    clipBehavior: Clip.antiAlias,
    child: ExpansionTile(
      leading: const Icon(Icons.location_on_outlined),
      title: const Text('Locations'),
      subtitle: Text(
        selected.isEmpty
            ? 'All locations in $branchName'
            : '${selected.length} location${selected.length == 1 ? '' : 's'} selected',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
      childrenPadding: const EdgeInsets.only(bottom: 8),
      children: [
        if (locations.isEmpty)
          const Padding(
            padding: EdgeInsets.all(16),
            child: Text(
              'No asset locations are available from the assigned audits.',
              textAlign: TextAlign.center,
            ),
          )
        else ...[
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => onChanged(locations.toSet()),
                  child: const Text('Select all'),
                ),
                TextButton(
                  onPressed: () => onChanged({}),
                  child: const Text('Clear'),
                ),
              ],
            ),
          ),
          ConstrainedBox(
            constraints: const BoxConstraints(maxHeight: 190),
            child: ListView.builder(
              shrinkWrap: true,
              itemCount: locations.length,
              itemBuilder: (context, index) {
                final location = locations[index];
                final checked = selected.contains(location);
                return CheckboxListTile(
                  dense: true,
                  value: checked,
                  activeColor: const Color(0xFFD01126),
                  title: Text(
                    location,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  onChanged: (_) {
                    final next = {...selected};
                    checked ? next.remove(location) : next.add(location);
                    onChanged(next);
                  },
                );
              },
            ),
          ),
        ],
      ],
    ),
  );
}

class _AssetTable extends StatelessWidget {
  const _AssetTable({required this.assets, required this.onAssetTap});

  final List<AssignedAsset> assets;
  final ValueChanged<AssignedAsset> onAssetTap;

  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: Theme.of(context).colorScheme.surface,
      border: Border.all(color: const Color(0xFFE8C5C9)),
      borderRadius: BorderRadius.circular(12),
    ),
    clipBehavior: Clip.antiAlias,
    child: SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Stack(
        children: [
          const Positioned(
            top: 0,
            left: 0,
            right: 0,
            height: 56,
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [Color(0xFFD01126), Color(0xFFD01126)],
                ),
              ),
            ),
          ),
          DataTable(
            headingRowColor: const WidgetStatePropertyAll(Colors.transparent),
            headingTextStyle: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w700,
            ),
            dataTextStyle: TextStyle(
              fontSize: 13,
              color: Theme.of(context).colorScheme.onSurface,
            ),
            columnSpacing: 28,
            horizontalMargin: 18,
            dividerThickness: 0.8,
            columns: const [
              DataColumn(label: Text('Asset Number')),
              DataColumn(label: Text('Asset Name')),
              DataColumn(label: Text('Serial Number')),
              DataColumn(label: Text('Location')),
              DataColumn(label: Text('Status')),
            ],
            rows: assets.indexed.map((entry) {
              final index = entry.$1;
              final asset = entry.$2;
              return DataRow(
                onSelectChanged: asset.isVerified
                    ? null
                    : (_) => onAssetTap(asset),
                mouseCursor: WidgetStatePropertyAll(
                  asset.isVerified
                      ? SystemMouseCursors.basic
                      : SystemMouseCursors.click,
                ),
                color: WidgetStateProperty.resolveWith((states) {
                  if (states.contains(WidgetState.hovered)) {
                    return const Color(0xFFFFE9EC);
                  }
                  return index.isEven ? Colors.white : const Color(0xFFFFF7F8);
                }),
                cells: [
                  DataCell(
                    Text(
                      asset.assetNumber,
                      style: const TextStyle(
                        color: Color(0xFFD01126),
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  DataCell(Text(asset.assetName)),
                  DataCell(Text(asset.serialNumber ?? '--')),
                  DataCell(Text(asset.location ?? '--')),
                  DataCell(
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 5,
                      ),
                      decoration: BoxDecoration(
                        color: asset.isVerified
                            ? const Color(0xFFFFE6E9)
                            : const Color(0xFFF4F0F1),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        asset.isVerified ? 'Verified' : 'Pending',
                        style: const TextStyle(
                          color: Color(0xFFD01126),
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ),
                ],
              );
            }).toList(),
          ),
        ],
      ),
    ),
  );
}

class _AssetCards extends StatelessWidget {
  const _AssetCards({required this.assets, required this.onAssetTap});

  final List<AssignedAsset> assets;
  final ValueChanged<AssignedAsset> onAssetTap;

  @override
  Widget build(BuildContext context) => Column(
    children: assets
        .map(
          (asset) => Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Material(
              color: Theme.of(context).colorScheme.surface,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
                side: BorderSide(
                  color: Theme.of(context).colorScheme.outlineVariant,
                ),
              ),
              clipBehavior: Clip.antiAlias,
              child: InkWell(
                onTap: asset.isVerified ? null : () => onAssetTap(asset),
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 40,
                            height: 40,
                            decoration: BoxDecoration(
                              color: const Color(0xFFFFECEE),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: const Icon(
                              Icons.precision_manufacturing_outlined,
                              color: Color(0xFFD01126),
                              size: 21,
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  asset.assetName,
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  style: Theme.of(context).textTheme.titleSmall,
                                ),
                                const SizedBox(height: 3),
                                Text(
                                  asset.assetNumber,
                                  style: Theme.of(context).textTheme.labelMedium
                                      ?.copyWith(
                                        color: const Color(0xFFD01126),
                                      ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 8),
                          _AssetStatusBadge(isVerified: asset.isVerified),
                        ],
                      ),
                      const SizedBox(height: 13),
                      const Divider(height: 1),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: _AssetCardDetail(
                              label: 'Serial number',
                              value: asset.serialNumber ?? '--',
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: _AssetCardDetail(
                              label: 'Location',
                              value: asset.location ?? '--',
                            ),
                          ),
                        ],
                      ),
                      if (!asset.isVerified) ...[
                        const SizedBox(height: 12),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            Text(
                              'Tap to scan and verify',
                              style: Theme.of(context).textTheme.labelSmall
                                  ?.copyWith(
                                    color: Theme.of(
                                      context,
                                    ).colorScheme.onSurfaceVariant,
                                  ),
                            ),
                            const SizedBox(width: 5),
                            const Icon(
                              Icons.qr_code_scanner,
                              size: 17,
                              color: Color(0xFFD01126),
                            ),
                          ],
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ),
          ),
        )
        .toList(),
  );
}

class _AssetCardDetail extends StatelessWidget {
  const _AssetCardDetail({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        ),
      ),
      const SizedBox(height: 3),
      Text(
        value,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
        style: Theme.of(context).textTheme.bodySmall?.copyWith(
          color: Theme.of(context).colorScheme.onSurface,
          fontWeight: FontWeight.w600,
        ),
      ),
    ],
  );
}

class _AssetStatusBadge extends StatelessWidget {
  const _AssetStatusBadge({required this.isVerified});

  final bool isVerified;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
    decoration: BoxDecoration(
      color: isVerified ? const Color(0xFFE9F7EF) : const Color(0xFFFFF1D9),
      borderRadius: BorderRadius.circular(20),
    ),
    child: Text(
      isVerified ? 'Verified' : 'Pending',
      style: Theme.of(context).textTheme.labelSmall?.copyWith(
        color: isVerified ? const Color(0xFF18794E) : const Color(0xFF9A6700),
        fontWeight: FontWeight.w700,
      ),
    ),
  );
}

class _TableControls extends StatelessWidget {
  const _TableControls({
    required this.assetCount,
    required this.pageSize,
    required this.pageSizes,
    required this.currentPage,
    required this.pageCount,
    required this.onPageSizeChanged,
    required this.onPrevious,
    required this.onNext,
  });

  final int assetCount;
  final int pageSize;
  final List<int> pageSizes;
  final int currentPage;
  final int pageCount;
  final ValueChanged<int> onPageSizeChanged;
  final VoidCallback? onPrevious;
  final VoidCallback? onNext;

  @override
  Widget build(BuildContext context) => Wrap(
    spacing: 12,
    runSpacing: 8,
    crossAxisAlignment: WrapCrossAlignment.center,
    alignment: WrapAlignment.spaceBetween,
    children: [
      Text(
        '$assetCount assets',
        style: const TextStyle(
          color: Color(0xFFD01126),
          fontWeight: FontWeight.w700,
        ),
      ),
      Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text('Page size'),
          const SizedBox(width: 8),
          DropdownButton<int>(
            value: pageSize,
            underline: const SizedBox.shrink(),
            items: pageSizes
                .map(
                  (size) => DropdownMenuItem(value: size, child: Text('$size')),
                )
                .toList(),
            onChanged: (value) {
              if (value != null) onPageSizeChanged(value);
            },
          ),
          const SizedBox(width: 12),
          IconButton.outlined(
            onPressed: onPrevious,
            icon: const Icon(Icons.chevron_left),
            tooltip: 'Previous page',
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10),
            child: Text(
              '${currentPage + 1} / $pageCount',
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
          ),
          IconButton.outlined(
            onPressed: onNext,
            icon: const Icon(Icons.chevron_right),
            tooltip: 'Next page',
          ),
        ],
      ),
    ],
  );
}

class _AuditorProfileSummary extends StatelessWidget {
  const _AuditorProfileSummary({required this.name, required this.username});

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
          const SizedBox(height: 10),
          const Text('Role: Auditor', style: TextStyle(fontSize: 11)),
        ],
      ),
    );
  }
}

class _Message extends StatelessWidget {
  const _Message({required this.icon, required this.text});
  final IconData icon;
  final String text;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 52),
          const SizedBox(height: 12),
          Text(text, textAlign: TextAlign.center),
        ],
      ),
    ),
  );
}
