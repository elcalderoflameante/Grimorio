import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

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
    final activeRequestsCount = state.requests
        .where(
          (request) =>
              request.status != TableServiceRequestStatus.completed &&
              request.status != TableServiceRequestStatus.cancelled,
        )
        .length;

    return Scaffold(
      body: IndexedStack(
        index: _selectedIndex,
        children: _pages,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (index) {
          setState(() {
            _selectedIndex = index;
          });
        },
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            selectedIcon: Icon(Icons.receipt_long),
            label: 'Pedidos',
          ),
          NavigationDestination(
            icon: _RequestsIconBadge(
              count: activeRequestsCount,
              icon: Icons.notifications_outlined,
            ),
            selectedIcon: _RequestsIconBadge(
              count: activeRequestsCount,
              icon: Icons.notifications,
            ),
            label: 'Solicitudes',
          ),
        ],
      ),
    );
  }
}

class _RequestsIconBadge extends StatelessWidget {
  const _RequestsIconBadge({required this.count, required this.icon});

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
