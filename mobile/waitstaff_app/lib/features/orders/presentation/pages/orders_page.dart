import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_theme.dart';
import '../../../auth/presentation/providers/auth_controller.dart';
import '../../data/models/order_models.dart';
import '../../data/services/order_api_service.dart';
import '../providers/order_controller.dart';
import 'new_order_page.dart';

class OrdersPage extends ConsumerStatefulWidget {
  const OrdersPage({super.key});

  @override
  ConsumerState<OrdersPage> createState() => _OrdersPageState();
}

class _OrdersPageState extends ConsumerState<OrdersPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(ordersControllerProvider.notifier).initialize();
    });
  }

  // ── color por estado de la orden ────────────────────────────────────────

  Color _statusColor(OrderStatus status) => switch (status) {
        OrderStatus.draft         => const Color(0xFF9E9E9E),
        OrderStatus.confirmed     => const Color(0xFF40C4FF),
        OrderStatus.inPreparation => const Color(0xFFFFAB00),
        OrderStatus.ready         => const Color(0xFF69F0AE),
        OrderStatus.delivered     => kGoldLight,
        OrderStatus.cancelled     => const Color(0xFFFF6B6B),
      };

  IconData _typeIcon(OrderType type) => switch (type) {
        OrderType.dineIn   => Icons.table_restaurant_rounded,
        OrderType.takeout  => Icons.shopping_bag_outlined,
        OrderType.delivery => Icons.delivery_dining_rounded,
      };

  // ── acciones ─────────────────────────────────────────────────────────────

  Future<void> _newOrder() async {
    final apiService = ref.read(orderApiServiceProvider);

    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: kBgMid,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => _NewOrderSheet(
        apiService: apiService,
        onConfirm: (type, table, client, address) {
          Navigator.of(ctx).pop();
          _openMenuForOrder(type, table, client, address);
        },
      ),
    );
  }

  void _openMenuForOrder(
    OrderType type,
    TableDto? table,
    String? client,
    String? address,
  ) {
    Navigator.of(context)
        .push<bool>(MaterialPageRoute(
          builder: (_) => NewOrderPage(
            type: type,
            table: table,
            clientName: client,
            deliveryAddress: address,
          ),
        ))
        .then((created) {
      if (created == true) {
        ref.read(ordersControllerProvider.notifier).load();
      }
    });
  }

  // ── build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(ordersControllerProvider);
    final active = state.active;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Pedidos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: () => ref.read(ordersControllerProvider.notifier).load(),
          ),
          IconButton(
            icon: const Icon(Icons.logout_rounded),
            onPressed: () async {
              await ref.read(authControllerProvider.notifier).logout();
              if (!context.mounted) return;
              context.go('/login');
            },
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _newOrder,
        icon: const Icon(Icons.add_rounded),
        label: const Text('Nuevo pedido'),
        backgroundColor: kGold,
        foregroundColor: kBrown,
      ),
      body: _buildBody(state, active),
    );
  }

  Widget _buildBody(OrdersState state, List<OrderDto> active) {
    if (state.isLoading && active.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.errorMessage != null && active.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.error_outline_rounded, size: 52, color: kGold.withAlpha(140)),
              const SizedBox(height: 16),
              Text(
                state.errorMessage!,
                textAlign: TextAlign.center,
                style: GoogleFonts.lato(color: kParchmentDim, fontSize: 14),
              ),
              const SizedBox(height: 20),
              OutlinedButton.icon(
                onPressed: () => ref.read(ordersControllerProvider.notifier).load(),
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Reintentar'),
              ),
            ],
          ),
        ),
      );
    }

    if (active.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(22),
              decoration: BoxDecoration(
                color: kBgCard,
                shape: BoxShape.circle,
                border: Border.all(color: kGoldDark.withAlpha(80)),
              ),
              child: const Icon(Icons.receipt_long_outlined, size: 44, color: kGold),
            ),
            const SizedBox(height: 20),
            Text(
              'Sin pedidos activos',
              style: GoogleFonts.cinzel(
                  color: kParchment, fontSize: 17, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            Text(
              'Toca el botón para tomar un pedido.',
              style: GoogleFonts.lato(color: kParchmentDim, fontSize: 13),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(ordersControllerProvider.notifier).load(),
      child: ListView.builder(
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 90),
        itemCount: active.length,
        itemBuilder: (_, i) => _buildCard(active[i]),
      ),
    );
  }

  Widget _buildCard(OrderDto order) {
    final color = _statusColor(order.status);
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(width: 4, color: color),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(_typeIcon(order.type), size: 18, color: kGoldLight),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              order.displayTitle,
                              style: GoogleFonts.cinzel(
                                color: kParchment,
                                fontSize: 16,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                          _StatusChip(label: order.status.label, color: color),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          _Meta(
                            icon: Icons.tag_rounded,
                            text: '#${order.number}',
                          ),
                          const SizedBox(width: 14),
                          _Meta(
                            icon: Icons.shopping_basket_outlined,
                            text: '${order.items.length} ítem${order.items.length != 1 ? 's' : ''}',
                          ),
                          const SizedBox(width: 14),
                          _Meta(
                            icon: Icons.attach_money_rounded,
                            text: '\$${order.total.toStringAsFixed(2)}',
                          ),
                        ],
                      ),
                      if (order.status == OrderStatus.draft) ...[
                        const SizedBox(height: 12),
                        SizedBox(
                          width: double.infinity,
                          child: FilledButton.icon(
                            onPressed: () => _openMenuForOrder(
                                order.type,
                                order.tableId != null
                                    ? TableDto(
                                        id: order.tableId!,
                                        code: order.tableCode ?? '',
                                        name: 'Mesa ${order.tableCode}',
                                        isActive: true,
                                        currentStatus: 'Occupied',
                                      )
                                    : null,
                                order.customerName,
                                order.deliveryAddress),
                            icon: const Icon(Icons.edit_rounded, size: 16),
                            label: const Text('Continuar pedido'),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Sheet para iniciar un nuevo pedido ────────────────────────────────────

class _NewOrderSheet extends StatefulWidget {
  const _NewOrderSheet({
    required this.apiService,
    required this.onConfirm,
  });

  final OrderApiService apiService;
  final void Function(OrderType, TableDto?, String?, String?) onConfirm;

  @override
  State<_NewOrderSheet> createState() => _NewOrderSheetState();
}

class _NewOrderSheetState extends State<_NewOrderSheet> {
  OrderType _type = OrderType.dineIn;
  List<TableDto>? _tables;
  bool _loadingTables = false;
  TableDto? _selectedTable;
  final _clientCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadTables();
  }

  Future<void> _loadTables() async {
    setState(() => _loadingTables = true);
    try {
      final tables = await widget.apiService.getTables();
      tables.sort((a, b) => a.code.compareTo(b.code));
      if (mounted) setState(() { _tables = tables; _loadingTables = false; });
    } catch (_) {
      if (mounted) setState(() { _tables = []; _loadingTables = false; });
    }
  }

  bool get _canConfirm {
    if (_type == OrderType.dineIn) return _selectedTable != null;
    return true;
  }

  @override
  void dispose() {
    _clientCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(20, 20, 20, 20 + bottomInset),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Handle visual
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: kGoldDark.withAlpha(120),
                borderRadius: BorderRadius.circular(4),
              ),
            ),
          ),
          const SizedBox(height: 20),
          Text(
            'Nuevo pedido',
            style: GoogleFonts.cinzel(
              color: kGold,
              fontSize: 18,
              fontWeight: FontWeight.w700,
              letterSpacing: 1,
            ),
          ),
          const SizedBox(height: 20),

          // Selector de tipo (domicilio solo desde caja web)
          Row(
            children: [OrderType.dineIn, OrderType.takeout].map((t) => _TypeButton(
              type: t,
              selected: _type == t,
              onTap: () => setState(() { _type = t; _selectedTable = null; }),
            )).toList(),
          ),
          const SizedBox(height: 20),

          if (_type == OrderType.dineIn) _buildTablePicker(),
          if (_type != OrderType.dineIn) _buildClientForm(),

          const SizedBox(height: 24),

          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: _canConfirm
                  ? () => widget.onConfirm(
                        _type,
                        _selectedTable,
                        _clientCtrl.text.trim().isEmpty ? null : _clientCtrl.text.trim(),
                        null,
                      )
                  : null,
              icon: const Icon(Icons.arrow_forward_rounded),
              label: const Text('Seleccionar ítems'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTablePicker() {
    if (_loadingTables) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(20),
          child: CircularProgressIndicator(),
        ),
      );
    }
    final tables = _tables ?? [];
    if (tables.isEmpty) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: Text(
          'No hay mesas disponibles',
          style: GoogleFonts.lato(color: kParchmentDim, fontSize: 14),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Selecciona la mesa',
          style: GoogleFonts.lato(
              color: kParchmentDim, fontSize: 13, fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 10),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: tables.map((t) {
            final selected = _selectedTable?.id == t.id;
            final free = t.isFree;
            return GestureDetector(
              onTap: free ? () => setState(() => _selectedTable = t) : null,
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 150),
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                decoration: BoxDecoration(
                  color: selected
                      ? kGold.withAlpha(30)
                      : free
                          ? kBgCard
                          : kBgMid,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: selected
                        ? kGold
                        : free
                            ? kGoldDark.withAlpha(80)
                            : kGoldDark.withAlpha(30),
                    width: selected ? 1.5 : 1,
                  ),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      t.code,
                      style: GoogleFonts.cinzel(
                        color: selected
                            ? kGold
                            : free
                                ? kParchment
                                : kParchmentDim.withAlpha(120),
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    if (t.area != null)
                      Text(
                        t.area!,
                        style: GoogleFonts.lato(
                          color: kParchmentDim.withAlpha(free ? 180 : 80),
                          fontSize: 11,
                        ),
                      ),
                    const SizedBox(height: 2),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: (free ? const Color(0xFF69F0AE) : const Color(0xFFFF6B6B))
                            .withAlpha(30),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        free ? 'Libre' : 'Ocupada',
                        style: GoogleFonts.lato(
                          color: free ? const Color(0xFF69F0AE) : const Color(0xFFFF6B6B),
                          fontSize: 10,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            );
          }).toList(),
        ),
      ],
    );
  }

  Widget _buildClientForm() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Nombre del cliente (opcional)',
          style: GoogleFonts.lato(
              color: kParchmentDim, fontSize: 13, fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 8),
        TextField(
          controller: _clientCtrl,
          decoration: InputDecoration(
            hintText: 'Ej: Juan Pérez',
            hintStyle: GoogleFonts.lato(color: kParchmentDim.withAlpha(100)),
            filled: true,
            fillColor: kBgCard,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide(color: kGoldDark.withAlpha(80)),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide(color: kGoldDark.withAlpha(80)),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: const BorderSide(color: kGold),
            ),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          ),
          style: GoogleFonts.lato(color: kParchment),
          textCapitalization: TextCapitalization.words,
        ),
      ],
    );
  }
}

// ── Botón selector de tipo de pedido ──────────────────────────────────────

class _TypeButton extends StatelessWidget {
  const _TypeButton({
    required this.type,
    required this.selected,
    required this.onTap,
  });

  final OrderType type;
  final bool selected;
  final VoidCallback onTap;

  IconData get _icon => switch (type) {
        OrderType.dineIn   => Icons.table_restaurant_rounded,
        OrderType.takeout  => Icons.shopping_bag_outlined,
        OrderType.delivery => Icons.delivery_dining_rounded,
      };

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          margin: const EdgeInsets.symmetric(horizontal: 4),
          padding: const EdgeInsets.symmetric(vertical: 14),
          decoration: BoxDecoration(
            color: selected ? kGold.withAlpha(30) : kBgCard,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: selected ? kGold : kGoldDark.withAlpha(80),
              width: selected ? 1.5 : 1,
            ),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(_icon, color: selected ? kGold : kParchmentDim, size: 26),
              const SizedBox(height: 6),
              Text(
                type.label,
                textAlign: TextAlign.center,
                style: GoogleFonts.lato(
                  color: selected ? kGold : kParchmentDim,
                  fontSize: 11,
                  fontWeight: selected ? FontWeight.w700 : FontWeight.w400,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Widgets auxiliares ─────────────────────────────────────────────────────

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withAlpha(35),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withAlpha(80)),
      ),
      child: Text(
        label,
        style: GoogleFonts.lato(
            color: color, fontSize: 11, fontWeight: FontWeight.w700),
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: kParchmentDim.withAlpha(160)),
        const SizedBox(width: 3),
        Text(
          text,
          style: GoogleFonts.lato(fontSize: 12, color: kParchmentDim.withAlpha(160)),
        ),
      ],
    );
  }
}
