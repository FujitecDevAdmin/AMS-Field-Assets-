import 'package:flutter/material.dart';
import '../../core/theme/responsive_typography.dart';

import '../audits/my_audits_page.dart';
import '../scan_history/scan_history_page.dart';
import 'auditor_dashboard_page.dart';

class AuditorShell extends StatefulWidget {
  const AuditorShell({super.key});

  @override
  State<AuditorShell> createState() => _AuditorShellState();
}

class _AuditorShellState extends State<AuditorShell> {
  int _selectedIndex = 0;
  int _auditNavigationVersion = 0;
  String _initialAuditFilter = 'All';
  int? _initialAuditId;

  void _openAudits({String auditFilter = 'All', int? auditId}) {
    setState(() {
      _selectedIndex = 1;
      _initialAuditFilter = auditFilter;
      _initialAuditId = auditId;
      _auditNavigationVersion++;
    });
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: IndexedStack(
      index: _selectedIndex,
      children: [
        AuditorDashboardPage(onOpenAudits: _openAudits),
        MyAuditsPage(
          navigationVersion: _auditNavigationVersion,
          initialAuditFilter: _initialAuditFilter,
          initialAuditId: _initialAuditId,
        ),
        const ScanHistoryPage(),
      ],
    ),
    bottomNavigationBar: _AuditorNavigationBar(
      selectedIndex: _selectedIndex,
      onSelected: (value) => setState(() => _selectedIndex = value),
    ),
  );
}

class _AuditorNavigationBar extends StatelessWidget {
  const _AuditorNavigationBar({
    required this.selectedIndex,
    required this.onSelected,
  });

  final int selectedIndex;
  final ValueChanged<int> onSelected;

  static const _items = [
    (Icons.dashboard_outlined, Icons.dashboard, 'Dashboard'),
    (Icons.assignment_outlined, Icons.assignment, 'My Audits'),
    (Icons.history_outlined, Icons.history, 'Scan History'),
  ];

  @override
  Widget build(BuildContext context) => ColoredBox(
    color: Theme.of(context).scaffoldBackgroundColor,
    child: SafeArea(
      top: false,
      minimum: const EdgeInsets.fromLTRB(12, 5, 12, 8),
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        elevation: 10,
        shadowColor: const Color(0x30000000),
        borderRadius: BorderRadius.circular(20),
        clipBehavior: Clip.antiAlias,
        child: Container(
          height: responsiveControlHeight(context, base: 68),
          decoration: BoxDecoration(
            border: Border.all(
              color: Theme.of(context).brightness == Brightness.dark
                  ? const Color(0xFF403A3C)
                  : const Color(0xFFE8D9DB),
            ),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Row(
            children: [
              for (var index = 0; index < _items.length; index++)
                Expanded(
                  child: _NavigationItem(
                    icon: _items[index].$1,
                    selectedIcon: _items[index].$2,
                    label: _items[index].$3,
                    selected: selectedIndex == index,
                    onTap: () => onSelected(index),
                  ),
                ),
            ],
          ),
        ),
      ),
    ),
  );
}

class _NavigationItem extends StatelessWidget {
  const _NavigationItem({
    required this.icon,
    required this.selectedIcon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final IconData icon;
  final IconData selectedIcon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Semantics(
    selected: selected,
    button: true,
    label: label,
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 220),
        curve: Curves.easeOutCubic,
        margin: const EdgeInsets.symmetric(horizontal: 3, vertical: 3),
        decoration: BoxDecoration(
          color: selected ? const Color(0x14D01126) : Colors.transparent,
          borderRadius: BorderRadius.circular(16),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            AnimatedScale(
              duration: const Duration(milliseconds: 220),
              curve: Curves.easeOutBack,
              scale: selected ? 1 : 0.92,
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 220),
                width: 29,
                height: 29,
                decoration: BoxDecoration(
                  color: selected
                      ? const Color(0xFFD01126)
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(10),
                  boxShadow: selected
                      ? const [
                          BoxShadow(
                            color: Color(0x35D01126),
                            blurRadius: 8,
                            offset: Offset(0, 3),
                          ),
                        ]
                      : null,
                ),
                child: Icon(
                  selected ? selectedIcon : icon,
                  size: 18,
                  color: selected
                      ? Colors.white
                      : Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            ),
            const SizedBox(height: 1),
            AnimatedDefaultTextStyle(
              duration: const Duration(milliseconds: 200),
              style: TextStyle(
                color: selected
                    ? const Color(0xFFD01126)
                    : Theme.of(context).colorScheme.onSurfaceVariant,
                fontSize: 12,
                fontWeight: selected ? FontWeight.w800 : FontWeight.w500,
              ),
              child: Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.center,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
