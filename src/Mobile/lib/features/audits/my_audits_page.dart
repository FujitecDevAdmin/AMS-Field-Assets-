import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_controller.dart';
import '../../shared/widgets/fujitec_header_logo.dart';
import 'audit_models.dart';
import 'audits_api.dart';
import 'scanner_page.dart';
import '../scan_history/scan_history.dart';

class MyAuditsPage extends ConsumerStatefulWidget {
  const MyAuditsPage({super.key});
  @override
  ConsumerState<MyAuditsPage> createState() => _MyAuditsPageState();
}

class _MyAuditsPageState extends ConsumerState<MyAuditsPage> {
  static const Color _themeDark = Color(0xFFD01126);
  static const Color _themeLight = Color(0xFFD01126);
  static const List<int> _pageSizes = [5, 10, 20, 50];
  final TextEditingController _searchController = TextEditingController();
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

  AssignedAudit? get _selected =>
      _audits.where((a) => a.id == _selectedId).firstOrNull;

  List<String> get _availableLocations {
    final branchId = _selected?.branchId;
    if (branchId == null) return const [];
    final locations = _audits
        .where((audit) => audit.branchId == branchId)
        .expand((audit) => audit.assets)
        .map((asset) => asset.location?.trim())
        .whereType<String>()
        .where((location) => location.isNotEmpty)
        .toSet()
        .toList();
    locations.sort(
      (left, right) => left.toLowerCase().compareTo(right.toLowerCase()),
    );
    return locations;
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
    unawaited(_load());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
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
        _audits = audits;
        _selectedId = audits.any((a) => a.id == (preferred ?? _selectedId))
            ? (preferred ?? _selectedId)
            : audits.firstOrNull?.id;
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
        builder: (_) => const ScannerPage(),
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
              const SizedBox(height: 18),
              DropdownButtonFormField<String>(
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
                  color: Colors.white,
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
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton(
                              onPressed: () => setSheetState(() {
                                auditFilter = 'All';
                                assetFilter = 'All';
                                assetTypeFilter = 'All';
                                identifierFilter = 'All';
                                locationFilters = {};
                              }),
                              child: const Text('Reset'),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton(
                              onPressed: () => Navigator.pop(context),
                              child: const Text('Cancel'),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: FilledButton(
                              style: FilledButton.styleFrom(
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
                            ),
                          ),
                        ],
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
                  userId: session?.userId,
                  expiresOnUtc: session?.expiresOnUtc,
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
      body: _loading
          ? const Center(child: CircularProgressIndicator())
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
                            fontSize: 11,
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
                  const SizedBox(height: 16),
                  TextField(
                    controller: _searchController,
                    decoration: InputDecoration(
                      hintText: 'Search audit, asset, serial or location',
                      prefixIcon: const Icon(Icons.search),
                      suffixIcon: _query.isEmpty
                          ? null
                          : IconButton(
                              onPressed: () => setState(() {
                                _searchController.clear();
                                _query = '';
                                _resetPage();
                              }),
                              icon: const Icon(Icons.close),
                            ),
                    ),
                    onChanged: (value) => setState(() {
                      _query = value;
                      _resetPage();
                    }),
                  ),
                  const SizedBox(height: 12),
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
      color: Colors.white,
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
            dataTextStyle: const TextStyle(
              fontSize: 13,
              color: Color(0xFF292124),
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
  const _AuditorProfileSummary({
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
          const SizedBox(height: 10),
          const Text('Role: Auditor', style: TextStyle(fontSize: 11)),
          Text(
            'User ID: ${userId ?? '--'}',
            style: const TextStyle(fontSize: 11),
          ),
          const Text('Session: Authenticated', style: TextStyle(fontSize: 11)),
          Text('Expires: $expiryText', style: const TextStyle(fontSize: 11)),
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
