import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../../core/network/api_error.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../data/models/order_models.dart';
import '../../data/services/order_api_service.dart';
import '../widgets/restaurant_tables_grid.dart';
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
  Timer? _refreshTimer;
  DateTime _now = DateTime.now();
  bool _refreshing = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadTables());
    _refreshTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      _now = DateTime.now();
      _loadTables(showLoading: false);
    });
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    super.dispose();
  }

  Future<void> _loadTables({bool showLoading = true}) async {
    if (_refreshing) return;
    _refreshing = true;
    if (mounted && showLoading) {
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
        _now = DateTime.now();
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        if (showLoading) {
          _errorMessage = readableApiError(
            error,
            fallback: 'No se pudieron cargar las mesas.',
          );
        }
      });
    } finally {
      _refreshing = false;
    }
  }

  Future<void> _openTable(TableDto table) async {
    var currentTable = table;
    try {
      final refreshedTables = await ref
          .read(orderApiServiceProvider)
          .getTables();
      currentTable = refreshedTables.firstWhere(
        (candidate) => candidate.id == table.id,
        orElse: () => table,
      );
    } catch (_) {
      // Si la actualización falla, se conserva el estado visible del mapa.
    }
    if (!mounted) return;

    if (currentTable.currentOrderId == null) {
      await Navigator.of(context).push<bool>(
        MaterialPageRoute(
          builder: (_) =>
              NewOrderPage(type: OrderType.dineIn, table: currentTable),
        ),
      );
    } else {
      await Navigator.of(context).push<bool>(
        MaterialPageRoute(
          builder: (_) => TableAccountPage(table: currentTable),
        ),
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
        toolbarHeight: 48,
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
        padding: const EdgeInsets.fromLTRB(14, 4, 14, 24),
        children: tablesByArea.entries.expand((entry) {
          return [
            Padding(
              padding: const EdgeInsets.fromLTRB(2, 8, 2, 8),
              child: Text(
                'ÁREA: ${entry.key.toUpperCase()}',
                style: GoogleFonts.cinzel(
                  color: kGoldLight,
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            RestaurantTablesGrid(
              tables: entry.value,
              now: _now,
              onTap: _openTable,
            ),
            const SizedBox(height: 16),
          ];
        }).toList(),
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
