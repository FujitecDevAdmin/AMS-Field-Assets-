import 'package:flutter/material.dart';

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

  @override
  Widget build(BuildContext context) => Scaffold(
    body: IndexedStack(
      index: _selectedIndex,
      children: [
        AuditorDashboardPage(
          onOpenAudits: () => setState(() => _selectedIndex = 1),
        ),
        const MyAuditsPage(),
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
  Widget build(BuildContext context) => Material(
    color: Colors.white,
    elevation: 12,
    shadowColor: const Color(0x26000000),
    child: SafeArea(
      top: false,
      child: SizedBox(
        height: 62,
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
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 220),
        curve: Curves.easeOut,
        margin: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
        decoration: BoxDecoration(
          color: selected ? const Color(0xFFFFEEF0) : Colors.transparent,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            AnimatedContainer(
              duration: const Duration(milliseconds: 220),
              width: selected ? 26 : 0,
              height: 3,
              decoration: BoxDecoration(
                color: selected ? const Color(0xFFD01126) : Colors.transparent,
                borderRadius: BorderRadius.circular(3),
              ),
            ),
            const SizedBox(height: 4),
            Icon(
              selected ? selectedIcon : icon,
              size: 20,
              color: selected
                  ? const Color(0xFFD01126)
                  : const Color(0xFF74686A),
            ),
            const SizedBox(height: 2),
            AnimatedDefaultTextStyle(
              duration: const Duration(milliseconds: 200),
              style: TextStyle(
                color: selected
                    ? const Color(0xFFD01126)
                    : const Color(0xFF74686A),
                fontSize: 10.5,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
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
