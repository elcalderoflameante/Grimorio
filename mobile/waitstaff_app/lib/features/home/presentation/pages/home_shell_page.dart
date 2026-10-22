import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../orders/presentation/pages/orders_page.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../../responsibilities/presentation/responsibilities_page.dart';
import '../../../responsibilities/data/responsibility_api_service.dart';
import '../../../table_requests/data/models/table_service_models.dart';
import '../../../table_requests/presentation/pages/table_requests_page.dart';
import '../../../table_requests/presentation/providers/table_requests_controller.dart';

class HomeShellPage extends ConsumerStatefulWidget {
  const HomeShellPage({super.key});

  @override
  ConsumerState<HomeShellPage> createState() => _HomeShellPageState();
}

class _HomeShellPageState extends ConsumerState<HomeShellPage> {
  int _selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    final canExecute =
        ref
            .watch(authControllerProvider)
            .session
            ?.permissions
            .contains('Operations.Responsibilities.Execute') ==
        true;
    final pages = [
      const OrdersPage(),
      const TableRequestsPage(),
      if (canExecute) const ResponsibilitiesPage(),
    ];
    final pendingTasks = ref.watch(responsibilityPendingCountProvider);
    final state = ref.watch(tableRequestsControllerProvider);
    final activeCount = state.requests
        .where(
          (r) =>
              r.status != TableServiceRequestStatus.completed &&
              r.status != TableServiceRequestStatus.cancelled,
        )
        .length;

    return Scaffold(
      body: IndexedStack(
        index: _selectedIndex < pages.length ? _selectedIndex : 0,
        children: pages,
      ),
      bottomNavigationBar: SafeArea(
        top: false,
        child: Container(
          decoration: BoxDecoration(
            border: Border(
              top: BorderSide(color: kGoldDark.withAlpha(80), width: 1),
            ),
          ),
          child: NavigationBar(
            selectedIndex: _selectedIndex < pages.length ? _selectedIndex : 0,
            onDestinationSelected: (i) => setState(() => _selectedIndex = i),
            destinations: [
              const NavigationDestination(
                icon: Icon(Icons.receipt_long_outlined),
                selectedIcon: Icon(Icons.receipt_long),
                label: 'Mesas',
              ),
              NavigationDestination(
                icon: _RequestsBadge(
                  count: activeCount,
                  icon: Icons.notifications_outlined,
                ),
                selectedIcon: _RequestsBadge(
                  count: activeCount,
                  icon: Icons.notifications,
                ),
                label: 'Solicitudes',
              ),
              if (canExecute)
                NavigationDestination(
                  icon: _RequestsBadge(
                    count: pendingTasks,
                    icon: Icons.assignment_outlined,
                  ),
                  selectedIcon: _RequestsBadge(
                    count: pendingTasks,
                    icon: Icons.assignment,
                  ),
                  label: 'Mi jornada',
                ),
            ],
          ),
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
