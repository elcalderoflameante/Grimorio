import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_theme.dart';
import '../../data/models/order_models.dart';
import '../../data/services/order_api_service.dart';

class NewOrderPage extends ConsumerStatefulWidget {
  const NewOrderPage({
    super.key,
    required this.type,
    this.table,
    this.clientName,
    this.deliveryAddress,
    this.orderId,
    this.orderIsDraft = false,
    this.draftOrder,
  });

  final OrderType type;
  final TableDto? table;
  final String? clientName;
  final String? deliveryAddress;
  final String? orderId;
  final bool orderIsDraft;
  final OrderDto? draftOrder;

  @override
  ConsumerState<NewOrderPage> createState() => _NewOrderPageState();
}

class _NewOrderPageState extends ConsumerState<NewOrderPage> {
  // ── Catálogo ──────────────────────────────────────────────────────────────
  List<MenuCategoryDto> _categories = [];
  List<MenuItemDto> _items = [];
  bool _loadingCatalog = true;
  String? _selectedCategory; // null = todas las categorías

  // ── Carrito ───────────────────────────────────────────────────────────────
  final List<CartItem> _cart = [];
  final TextEditingController _notesCtrl = TextEditingController();

  // ── UI state ──────────────────────────────────────────────────────────────
  bool _cartExpanded = false;
  bool _saving = false;

  double get _total => _cart.fold(0, (s, i) => s + i.subtotal);
  int get _totalItems => _cart.fold(0, (s, i) => s + i.quantity);

  @override
  void initState() {
    super.initState();
    _loadDraftIntoCart();
    _loadCatalog();
  }

  void _loadDraftIntoCart() {
    final draft = widget.draftOrder;
    if (!widget.orderIsDraft || draft == null) return;

    _notesCtrl.text = draft.notes ?? '';
    _cart.addAll(
      draft.items.map(
        (item) => CartItem(
          menuItemId: item.menuItemId,
          name: item.itemName,
          price: item.unitPrice,
          quantity: item.quantity,
          notes: item.notes,
          isTakeout: item.isTakeout,
          modifierSelections: item.modifierSelections
              .where((selection) => selection.modifierOptionId.isNotEmpty)
              .map(
                (selection) => CartModifierSelection(
                  modifierOptionId: selection.modifierOptionId,
                  groupName: selection.groupName,
                  optionName: selection.optionName,
                  quantity: selection.quantity,
                  unitPriceDelta: selection.unitPriceDelta,
                  isTracked: false,
                ),
              )
              .toList(),
        ),
      ),
    );
    _cartExpanded = _cart.isNotEmpty;
  }

  @override
  void dispose() {
    _notesCtrl.dispose();
    super.dispose();
  }

  // ── Carga de datos ────────────────────────────────────────────────────────

  Future<void> _loadCatalog() async {
    setState(() => _loadingCatalog = true);
    try {
      final api = ref.read(orderApiServiceProvider);
      final (cats, items) = await (
        api.getCategories(),
        api.getMenuItems(),
      ).wait;
      if (mounted) {
        setState(() {
          _categories = cats;
          _items = items;
          _loadingCatalog = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _loadingCatalog = false);
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('Error al cargar menú: $e')));
      }
    }
  }

  List<MenuItemDto> get _filteredItems => _selectedCategory == null
      ? _items
      : _items.where((i) => i.menuCategoryId == _selectedCategory).toList();

  // ── Carrito ───────────────────────────────────────────────────────────────

  Future<void> _addItem(MenuItemDto item) async {
    if (item.hasModifiers || item.modifierGroups.isNotEmpty) {
      try {
        final detail = await ref
            .read(orderApiServiceProvider)
            .getMenuItem(item.id);
        if (!mounted) return;
        await _showModifierDialog(detail);
      } catch (e) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('No se pudieron cargar los modificadores: $e'),
          ),
        );
      }
      return;
    }

    setState(() {
      final idx = _cart.indexWhere(
        (c) => c.menuItemId == item.id && c.modifierSelections.isEmpty,
      );
      if (idx >= 0) {
        _cart[idx].quantity++;
      } else {
        _cart.add(
          CartItem(menuItemId: item.id, name: item.name, price: item.price),
        );
      }
    });
  }

  Future<void> _showModifierDialog(MenuItemDto item) async {
    final selected = <String, int>{};
    var showValidationError = false;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          backgroundColor: kBgCard,
          title: Text(
            item.name,
            style: GoogleFonts.cinzel(color: kGold, fontSize: 17),
          ),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (showValidationError) ...[
                    Text(
                      'Revisa las opciones requeridas y sus cantidades.',
                      style: GoogleFonts.lato(
                        color: Theme.of(context).colorScheme.error,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 12),
                  ],
                  ...item.modifierGroups.map((group) {
                    final selectedCount = group.options.fold<int>(
                      0,
                      (sum, option) => sum + (selected[option.id] ?? 0),
                    );
                    final invalid =
                        showValidationError &&
                        (selectedCount < group.effectiveMinimum ||
                            selectedCount > group.maxSelections);

                    return Padding(
                      padding: const EdgeInsets.only(bottom: 18),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  group.name,
                                  style: GoogleFonts.cinzel(
                                    color: invalid ? Colors.redAccent : kGold,
                                    fontSize: 14,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ),
                              Text(
                                '$selectedCount/${group.maxSelections}',
                                style: GoogleFonts.lato(
                                  color: invalid
                                      ? Colors.redAccent
                                      : kParchmentDim,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          ...group.options.map((option) {
                            final quantity = selected[option.id] ?? 0;
                            final used = _usedModifierQuantity(option.id);
                            final stockLimit = option.isTracked
                                ? (option.availableQuantity ?? 0).floor()
                                : null;
                            final remainingStock = stockLimit == null
                                ? null
                                : (stockLimit - used)
                                      .clamp(0, stockLimit)
                                      .toInt();
                            final outOfStock =
                                !option.isAvailable || remainingStock == 0;
                            final canAdd =
                                !outOfStock &&
                                selectedCount < group.maxSelections &&
                                (group.allowDuplicates || quantity == 0) &&
                                (remainingStock == null ||
                                    quantity < remainingStock);

                            return Container(
                              margin: const EdgeInsets.only(bottom: 8),
                              padding: const EdgeInsets.symmetric(
                                horizontal: 12,
                                vertical: 10,
                              ),
                              decoration: BoxDecoration(
                                color: quantity > 0
                                    ? kGold.withAlpha(25)
                                    : kBgMid,
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: outOfStock
                                      ? Colors.redAccent.withAlpha(150)
                                      : quantity > 0
                                      ? kGold
                                      : kGoldDark.withAlpha(70),
                                ),
                              ),
                              child: Row(
                                children: [
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          option.name,
                                          style: GoogleFonts.lato(
                                            color: outOfStock
                                                ? kParchmentDim
                                                : kParchment,
                                            fontSize: 15,
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                        if (option.priceDelta != 0 ||
                                            option.isTracked)
                                          Text(
                                            [
                                              if (option.priceDelta != 0)
                                                '${option.priceDelta > 0 ? '+' : ''}\$${option.priceDelta.toStringAsFixed(2)}',
                                              if (option.isTracked)
                                                outOfStock
                                                    ? 'Sin stock'
                                                    : 'Disponibles: $remainingStock',
                                            ].join(' · '),
                                            style: GoogleFonts.lato(
                                              color: outOfStock
                                                  ? Colors.redAccent
                                                  : kParchmentDim,
                                              fontSize: 12,
                                            ),
                                          ),
                                      ],
                                    ),
                                  ),
                                  _QtyButton(
                                    icon: Icons.remove_rounded,
                                    onTap: quantity > 0
                                        ? () => setDialogState(() {
                                            selected[option.id] = quantity - 1;
                                            showValidationError = false;
                                          })
                                        : null,
                                  ),
                                  SizedBox(
                                    width: 36,
                                    child: Text(
                                      '$quantity',
                                      textAlign: TextAlign.center,
                                      style: GoogleFonts.cinzel(
                                        color: kGold,
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                  ),
                                  _QtyButton(
                                    icon: Icons.add_rounded,
                                    onTap: canAdd
                                        ? () => setDialogState(() {
                                            selected[option.id] = quantity + 1;
                                            showValidationError = false;
                                          })
                                        : null,
                                  ),
                                ],
                              ),
                            );
                          }),
                        ],
                      ),
                    );
                  }),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                final invalid = item.modifierGroups.any((group) {
                  final total = group.options.fold<int>(
                    0,
                    (sum, option) => sum + (selected[option.id] ?? 0),
                  );
                  return total < group.effectiveMinimum ||
                      total > group.maxSelections;
                });
                if (invalid) {
                  setDialogState(() => showValidationError = true);
                  return;
                }
                Navigator.of(dialogContext).pop(true);
              },
              child: const Text('Agregar'),
            ),
          ],
        ),
      ),
    );

    if (confirmed != true || !mounted) return;

    final selections = <CartModifierSelection>[];
    for (final group in item.modifierGroups) {
      for (final option in group.options) {
        final quantity = selected[option.id] ?? 0;
        if (quantity <= 0) continue;
        selections.add(
          CartModifierSelection(
            modifierOptionId: option.id,
            groupName: group.name,
            optionName: option.name,
            quantity: quantity,
            unitPriceDelta: option.priceDelta,
            isTracked: option.isTracked,
            availableQuantity: option.availableQuantity,
          ),
        );
      }
    }
    final modifierTotal = selections.fold<double>(
      0,
      (sum, selection) => sum + selection.unitPriceDelta * selection.quantity,
    );
    setState(() {
      _cart.add(
        CartItem(
          menuItemId: item.id,
          name: item.name,
          price: item.price + modifierTotal,
          modifierSelections: selections,
        ),
      );
    });
  }

  int _usedModifierQuantity(String modifierOptionId, {int? excludingIndex}) {
    var used = 0;
    for (var index = 0; index < _cart.length; index++) {
      if (index == excludingIndex) continue;
      final item = _cart[index];
      for (final selection in item.modifierSelections) {
        if (selection.modifierOptionId == modifierOptionId) {
          used += selection.quantity * item.quantity;
        }
      }
    }
    return used;
  }

  void _changeQuantity(int idx, int delta) {
    final item = _cart[idx];
    final nextQuantity = item.quantity + delta;
    if (delta > 0) {
      for (final selection in item.modifierSelections) {
        if (!selection.isTracked || selection.availableQuantity == null) {
          continue;
        }
        final usedByOthers = _usedModifierQuantity(
          selection.modifierOptionId,
          excludingIndex: idx,
        );
        final required = selection.quantity * nextQuantity;
        if (usedByOthers + required > selection.availableQuantity!.floor()) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'No hay stock suficiente de ${selection.optionName}.',
              ),
            ),
          );
          return;
        }
      }
    }
    setState(() {
      _cart[idx].quantity = nextQuantity;
      if (_cart[idx].quantity <= 0) _cart.removeAt(idx);
    });
  }

  void _toggleTakeout(int idx) {
    setState(() => _cart[idx].isTakeout = !_cart[idx].isTakeout);
  }

  void _editNote(int idx) {
    final ctrl = TextEditingController(text: _cart[idx].notes ?? '');
    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: kBgCard,
        title: Text(
          _cart[idx].name,
          style: GoogleFonts.cinzel(color: kGold, fontSize: 15),
        ),
        content: TextField(
          controller: ctrl,
          decoration: InputDecoration(
            hintText: 'Sin cebolla, bien cocido...',
            hintStyle: GoogleFonts.lato(color: kParchmentDim.withAlpha(100)),
            filled: true,
            fillColor: kBgMid,
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: BorderSide(color: kGoldDark.withAlpha(80)),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10),
              borderSide: const BorderSide(color: kGold),
            ),
          ),
          style: GoogleFonts.lato(color: kParchment),
          maxLines: 3,
          autofocus: true,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: Text(
              'Cancelar',
              style: GoogleFonts.lato(color: kParchmentDim),
            ),
          ),
          TextButton(
            onPressed: () {
              setState(() {
                _cart[idx].notes = ctrl.text.trim().isEmpty
                    ? null
                    : ctrl.text.trim();
              });
              Navigator.of(ctx).pop();
            },
            child: Text('Guardar', style: GoogleFonts.lato(color: kGold)),
          ),
        ],
      ),
    );
  }

  int _quantityInCart(String menuItemId) {
    return _cart
        .where((item) => item.menuItemId == menuItemId)
        .fold(0, (sum, item) => sum + item.quantity);
  }

  // ── Enviar a cocina ───────────────────────────────────────────────────────

  Future<void> _sendToKitchen() async {
    if (_cart.isEmpty) return;
    setState(() => _saving = true);
    try {
      final api = ref.read(orderApiServiceProvider);
      final OrderDto order;
      if (widget.orderId != null) {
        order = await api.addOrderItems(widget.orderId!, _cart);
        if (widget.orderIsDraft) await api.confirmOrder(order.id);
      } else {
        order = await api.createOrder(
          type: OrderType.dineIn,
          tableId: widget.table?.id,
          notes: _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
          items: _cart,
        );
        await api.confirmOrder(order.id);
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Pedido #${order.number} enviado a cocina'),
            backgroundColor: const Color(0xFF69F0AE).withAlpha(200),
          ),
        );
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _saving = false);
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('Error al enviar pedido: $e')));
      }
    }
  }

  // ── Build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final title = switch (widget.type) {
      OrderType.dineIn => 'Mesa ${widget.table?.code ?? '?'}',
      OrderType.takeout =>
        widget.clientName?.isNotEmpty == true
            ? 'Llevar · ${widget.clientName}'
            : 'Para llevar',
      OrderType.delivery =>
        widget.clientName?.isNotEmpty == true
            ? 'Domicilio · ${widget.clientName}'
            : 'Domicilio',
    };

    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => Navigator.of(context).pop(false),
        ),
        actions: [
          if (_totalItems > 0)
            TextButton.icon(
              onPressed: () => setState(() => _cartExpanded = !_cartExpanded),
              icon: Badge(
                label: Text('$_totalItems'),
                isLabelVisible: _totalItems > 0,
                child: const Icon(Icons.shopping_cart_outlined, color: kGold),
              ),
              label: Text(
                '\$${_total.toStringAsFixed(2)}',
                style: GoogleFonts.cinzel(color: kGold, fontSize: 13),
              ),
            ),
        ],
      ),
      body: Stack(
        children: [
          Column(
            children: [
              _buildCategoryBar(),
              Expanded(
                child: _loadingCatalog
                    ? const Center(child: CircularProgressIndicator())
                    : _buildItemGrid(),
              ),
              if (_totalItems > 0) const SizedBox(height: 80),
            ],
          ),
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: SafeArea(top: false, child: _buildCartPanel()),
          ),
        ],
      ),
    );
  }

  // ── Barra de categorías ───────────────────────────────────────────────────

  Widget _buildCategoryBar() {
    return Container(
      height: 58,
      color: kBgMid,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
        children: [
          _CategoryChip(
            label: 'Todos',
            selected: _selectedCategory == null,
            onTap: () => setState(() => _selectedCategory = null),
          ),
          ..._categories.map(
            (c) => _CategoryChip(
              label: c.name,
              selected: _selectedCategory == c.id,
              color: c.color,
              onTap: () => setState(() => _selectedCategory = c.id),
            ),
          ),
        ],
      ),
    );
  }

  // ── Grid de ítems del menú ────────────────────────────────────────────────

  Widget _buildItemGrid() {
    final items = _filteredItems;
    if (items.isEmpty) {
      return Center(
        child: Text(
          'Sin ítems en esta categoría',
          style: GoogleFonts.lato(color: kParchmentDim, fontSize: 14),
        ),
      );
    }
    return GridView.builder(
      padding: const EdgeInsets.all(12),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 10,
        crossAxisSpacing: 10,
        childAspectRatio: 1.1,
      ),
      itemCount: items.length,
      itemBuilder: (_, i) => _ItemCard(
        item: items[i],
        quantity: _quantityInCart(items[i].id),
        onTap: () => _addItem(items[i]),
      ),
    );
  }

  // ── Panel del carrito (expandible desde abajo) ────────────────────────────

  Widget _buildCartPanel() {
    if (_totalItems == 0) return const SizedBox.shrink();

    return AnimatedContainer(
      duration: const Duration(milliseconds: 250),
      curve: Curves.easeInOut,
      decoration: BoxDecoration(
        color: kBgMid,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        border: Border(
          top: BorderSide(color: kGoldDark.withAlpha(100), width: 1),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(80),
            blurRadius: 20,
            offset: const Offset(0, -4),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Resumen — tap para expandir/colapsar
          GestureDetector(
            onTap: () => setState(() => _cartExpanded = !_cartExpanded),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
              child: Row(
                children: [
                  Icon(
                    _cartExpanded
                        ? Icons.keyboard_arrow_down_rounded
                        : Icons.keyboard_arrow_up_rounded,
                    color: kGoldLight,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '$_totalItems ítem${_totalItems != 1 ? 's' : ''} en el carrito',
                      style: GoogleFonts.cinzel(
                        color: kParchment,
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                  Text(
                    'Total: \$${_total.toStringAsFixed(2)}',
                    style: GoogleFonts.cinzel(
                      color: kGold,
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ),

          if (_cartExpanded) ...[
            Container(height: 1, color: kGoldDark.withAlpha(50)),
            ConstrainedBox(
              constraints: BoxConstraints(
                maxHeight: MediaQuery.of(context).size.height * 0.4,
              ),
              child: ListView.builder(
                shrinkWrap: true,
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 8,
                ),
                itemCount: _cart.length,
                itemBuilder: (_, i) => _CartRow(
                  item: _cart[i],
                  onIncrease: () => _changeQuantity(i, 1),
                  onDecrease: () => _changeQuantity(i, -1),
                  onNote: () => _editNote(i),
                  onToggleTakeout: () => _toggleTakeout(i),
                ),
              ),
            ),
            // Observación general del pedido
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: TextField(
                controller: _notesCtrl,
                decoration: InputDecoration(
                  hintText: 'Observación general del pedido (opcional)',
                  hintStyle: GoogleFonts.lato(
                    color: kParchmentDim.withAlpha(100),
                    fontSize: 12,
                  ),
                  isDense: true,
                  filled: true,
                  fillColor: kBgCard,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: BorderSide(color: kGoldDark.withAlpha(60)),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: BorderSide(color: kGoldDark.withAlpha(60)),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: const BorderSide(color: kGold),
                  ),
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 10,
                  ),
                ),
                style: GoogleFonts.lato(color: kParchment, fontSize: 13),
              ),
            ),
          ],

          // Botón enviar a cocina
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
            child: SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _saving ? null : _sendToKitchen,
                icon: _saving
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: kBrown,
                        ),
                      )
                    : const Icon(Icons.send_rounded, size: 18),
                label: Text(_saving ? 'Enviando...' : 'Enviar a cocina'),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Widgets auxiliares ─────────────────────────────────────────────────────

class _CategoryChip extends StatelessWidget {
  const _CategoryChip({
    required this.label,
    required this.selected,
    required this.onTap,
    this.color,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final String? color;

  Color _parseColor() {
    if (color == null) return kGold;
    final hex = color!.replaceFirst('#', '');
    if (hex.length == 6) {
      return Color(int.parse('FF$hex', radix: 16));
    }
    return kGold;
  }

  @override
  Widget build(BuildContext context) {
    final c = _parseColor();
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        margin: const EdgeInsets.only(right: 8),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: selected ? c.withAlpha(40) : kBgCard,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: selected ? c : kGoldDark.withAlpha(60),
            width: selected ? 1.5 : 1,
          ),
        ),
        child: Text(
          label,
          style: GoogleFonts.lato(
            color: selected ? c : kParchmentDim,
            fontSize: 14,
            fontWeight: selected ? FontWeight.w700 : FontWeight.w400,
          ),
        ),
      ),
    );
  }
}

class _ItemCard extends StatelessWidget {
  const _ItemCard({
    required this.item,
    required this.quantity,
    required this.onTap,
  });

  final MenuItemDto item;
  final int quantity;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final inCart = quantity > 0;
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        decoration: BoxDecoration(
          color: inCart ? kGold.withAlpha(18) : kBgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: inCart ? kGold.withAlpha(120) : kGoldDark.withAlpha(60),
            width: inCart ? 1.5 : 1,
          ),
        ),
        child: Stack(
          children: [
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.name,
                    style: GoogleFonts.lato(
                      color: kParchment,
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      height: 1.3,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const Spacer(),
                  Text(
                    '\$${item.price.toStringAsFixed(2)}',
                    style: GoogleFonts.cinzel(
                      color: kGoldLight,
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
            if (inCart)
              Positioned(
                top: 8,
                right: 8,
                child: Container(
                  width: 22,
                  height: 22,
                  decoration: const BoxDecoration(
                    color: kGold,
                    shape: BoxShape.circle,
                  ),
                  alignment: Alignment.center,
                  child: Text(
                    '$quantity',
                    style: GoogleFonts.lato(
                      color: kBrown,
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ),
            if (!inCart)
              Positioned(
                top: 8,
                right: 8,
                child: Container(
                  width: 22,
                  height: 22,
                  decoration: BoxDecoration(
                    color: kBgMid,
                    shape: BoxShape.circle,
                    border: Border.all(color: kGoldDark.withAlpha(80)),
                  ),
                  child: const Icon(
                    Icons.add_rounded,
                    size: 14,
                    color: kGoldLight,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _CartRow extends StatelessWidget {
  const _CartRow({
    required this.item,
    required this.onIncrease,
    required this.onDecrease,
    required this.onNote,
    required this.onToggleTakeout,
  });

  final CartItem item;
  final VoidCallback onIncrease;
  final VoidCallback onDecrease;
  final VoidCallback onNote;
  final VoidCallback onToggleTakeout;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          _QtyButton(icon: Icons.remove_rounded, onTap: onDecrease),
          Container(
            width: 36,
            alignment: Alignment.center,
            child: Text(
              '${item.quantity}',
              style: GoogleFonts.cinzel(
                color: kGold,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          _QtyButton(icon: Icons.add_rounded, onTap: onIncrease),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.name,
                  style: GoogleFonts.lato(color: kParchment, fontSize: 13),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (item.modifiersLabel != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 3),
                    child: Text(
                      item.modifiersLabel!,
                      style: GoogleFonts.lato(
                        color: kGoldLight.withAlpha(190),
                        fontSize: 11,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                if (item.notes != null)
                  Text(
                    item.notes!,
                    style: GoogleFonts.lato(
                      color: kParchmentDim.withAlpha(160),
                      fontSize: 11,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                GestureDetector(
                  behavior: HitTestBehavior.opaque,
                  onTap: onToggleTakeout,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          item.isTakeout
                              ? Icons.check_box_rounded
                              : Icons.check_box_outline_blank_rounded,
                          size: 24,
                          color: item.isTakeout
                              ? const Color(0xFFFFAB00)
                              : kParchmentDim,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          'Preparar para llevar',
                          style: GoogleFonts.lato(
                            color: item.isTakeout
                                ? const Color(0xFFFFAB00)
                                : kParchmentDim,
                            fontSize: 14,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Text(
            '\$${item.subtotal.toStringAsFixed(2)}',
            style: GoogleFonts.lato(
              color: kGoldLight,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(width: 4),
          SizedBox(
            width: 44,
            height: 44,
            child: IconButton(
              padding: EdgeInsets.zero,
              onPressed: onNote,
              icon: Icon(
                item.notes != null
                    ? Icons.comment_rounded
                    : Icons.comment_outlined,
                size: 24,
                color: item.notes != null
                    ? kGold
                    : kParchmentDim.withAlpha(120),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _QtyButton extends StatelessWidget {
  const _QtyButton({required this.icon, required this.onTap});

  final IconData icon;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return InkResponse(
      onTap: onTap,
      radius: 24,
      child: Container(
        width: 38,
        height: 38,
        decoration: BoxDecoration(
          color: onTap == null ? kBgMid : kBgCard,
          shape: BoxShape.circle,
          border: Border.all(color: kGoldDark.withAlpha(80)),
        ),
        child: Icon(
          icon,
          size: 20,
          color: onTap == null ? kParchmentDim.withAlpha(80) : kGoldLight,
        ),
      ),
    );
  }
}
