import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../data/models/order_models.dart';
import '../../data/services/order_api_service.dart';
import 'new_order_page.dart';
import 'table_account_page.dart';

class OrdersPage extends ConsumerStatefulWidget {
  const OrdersPage({super.key});

  @override
  ConsumerState<OrdersPage> createState() => _OrdersPageState();
}

class _OrdersPageState extends ConsumerState<OrdersPage> {
  List<TableDto> _tables = const [];
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadTables());
  }

  Future<void> _loadTables() async {
    if (mounted) {
      setState(() {
        _isLoading = true;
        _errorMessage = null;
      });
    }

    try {
      final tables = await ref.read(orderApiServiceProvider).getTables();
      tables.sort(_compareTables);
      if (!mounted) return;
      setState(() {
        _tables = tables;
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _errorMessage = 'No se pudieron cargar las mesas: $error';
      });
    }
  }

  Future<void> _openTable(TableDto table) async {
    if (table.isFree || table.currentOrderId == null) {
      await Navigator.of(context).push<bool>(
        MaterialPageRoute(
          builder: (_) => NewOrderPage(type: OrderType.dineIn, table: table),
        ),
      );
    } else {
      await Navigator.of(context).push<bool>(
        MaterialPageRoute(builder: (_) => TableAccountPage(table: table)),
      );
    }
    await _loadTables();
  }

  Future<void> _logout() async {
    await ref.read(authControllerProvider.notifier).logout();
    if (mounted) context.go('/login');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mesas'),
        actions: [
          IconButton(
            tooltip: 'Actualizar',
            onPressed: _loadTables,
            icon: const Icon(Icons.refresh_rounded),
          ),
          IconButton(
            tooltip: 'Cerrar sesión',
            onPressed: _logout,
            icon: const Icon(Icons.logout_rounded),
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(_errorMessage!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: _loadTables,
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Reintentar'),
              ),
            ],
          ),
        ),
      );
    }

    if (_tables.isEmpty) {
      return const Center(child: Text('No hay mesas configuradas'));
    }

    final tablesByArea = <String, List<TableDto>>{};
    for (final table in _tables) {
      final area = table.area?.trim();
      final areaName = area != null && area.isNotEmpty ? area : 'General';
      tablesByArea.putIfAbsent(areaName, () => []).add(table);
    }

    return RefreshIndicator(
      onRefresh: _loadTables,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(14, 14, 14, 30),
        children: tablesByArea.entries.expand((entry) {
          return [
            Padding(
              padding: const EdgeInsets.fromLTRB(2, 10, 2, 10),
              child: Text(
                'ÁREA: ${entry.key.toUpperCase()}',
                style: GoogleFonts.cinzel(
                  color: kGoldLight,
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                childAspectRatio: 1.25,
              ),
              itemCount: entry.value.length,
              itemBuilder: (_, index) {
                final table = entry.value[index];
                return _TableCard(table: table, onTap: () => _openTable(table));
              },
            ),
            const SizedBox(height: 16),
          ];
        }).toList(),
      ),
    );
  }
}

class _TableCard extends StatelessWidget {
  const _TableCard({required this.table, required this.onTap});

  final TableDto table;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isDraft = table.currentStatus == 'Draft';
    final isOccupied = table.currentStatus == 'Occupied';
    final color = isOccupied
        ? const Color(0xFFFF6B6B)
        : isDraft
        ? const Color(0xFFFFAB00)
        : const Color(0xFF69F0AE);
    final statusLabel = isOccupied
        ? 'Ocupada'
        : isDraft
        ? 'Borrador'
        : 'Libre';

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Ink(
        decoration: BoxDecoration(
          color: color.withAlpha(18),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: color.withAlpha(180), width: 1.5),
        ),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.table_restaurant_rounded, color: color, size: 28),
              const SizedBox(height: 6),
              Text(
                'Mesa ${table.code}',
                style: GoogleFonts.cinzel(
                  color: kParchment,
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 5),
              Text(
                statusLabel,
                style: GoogleFonts.lato(
                  color: color,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 5),
              Text(
                table.isFree
                    ? '${table.capacity} personas'
                    : 'Pendiente: \$${table.pendingPaymentTotal.toStringAsFixed(2)}',
                style: GoogleFonts.lato(color: kParchmentDim, fontSize: 11),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

int _tableNumber(String code) => int.tryParse(code.trim()) ?? 0x3fffffff;

int _compareTables(TableDto first, TableDto second) {
  final numberComparison = _tableNumber(
    first.code,
  ).compareTo(_tableNumber(second.code));
  if (numberComparison != 0) return numberComparison;

  final codeComparison = first.code.toLowerCase().compareTo(
    second.code.toLowerCase(),
  );
  if (codeComparison != 0) return codeComparison;

  return (first.area ?? '').toLowerCase().compareTo(
    (second.area ?? '').toLowerCase(),
  );
}
