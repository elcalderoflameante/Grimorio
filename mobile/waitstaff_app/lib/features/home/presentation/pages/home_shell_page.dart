import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../orders/presentation/pages/orders_page.dart';
import '../../../table_requests/data/models/table_service_models.dart';
import '../../../table_requests/presentation/pages/table_requests_page.dart';
import '../../../table_requests/presentation/providers/table_requests_controller.dart';

class HomeShellPage extends ConsumerStatefulWidget {
  const HomeShellPage({super.key});

  @override
  ConsumerState<HomeShellPage> createState() => _HomeShellPageState();
}

class _HomeShellPageState extends ConsumerState<HomeShellPage> {
  int _selectedIndex = 1;

  static const _pages = [
    OrdersPage(),
    TableRequestsPage(),
  ];

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(tableRequestsControllerProvider);
    final activeCount = state.requests
        .where((r) =>
            r.status != TableServiceRequestStatus.completed &&
            r.status != TableServiceRequestStatus.cancelled)
        .length;

    return Scaffold(
      body: IndexedStack(
        index: _selectedIndex,
        children: _pages,
      ),
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          border: Border(
            top: BorderSide(color: kGoldDark.withAlpha(80), width: 1),
          ),
        ),
        child: NavigationBar(
          selectedIndex: _selectedIndex,
          onDestinationSelected: (i) => setState(() => _selectedIndex = i),
          destinations: [
            const NavigationDestination(
              icon: Icon(Icons.receipt_long_outlined),
              selectedIcon: Icon(Icons.receipt_long),
              label: 'Pedidos',
            ),
            NavigationDestination(
              icon: _RequestsBadge(count: activeCount, icon: Icons.notifications_outlined),
              selectedIcon: _RequestsBadge(count: activeCount, icon: Icons.notifications),
              label: 'Solicitudes',
            ),
          ],
        ),
      ),
    );
  }
}

class _RequestsBadge extends StatelessWidget {
  const _RequestsBadge({required this.count, required this.icon});

  final int count;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Badge(
      isLabelVisible: count > 0,
      label: Text(count > 99 ? '99+' : '$count'),
      child: Icon(icon),
    );
  }
}
