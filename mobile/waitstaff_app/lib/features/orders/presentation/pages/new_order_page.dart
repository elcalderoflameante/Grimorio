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
  });

  final OrderType type;
  final TableDto? table;
  final String? clientName;
  final String? deliveryAddress;
  final String? orderId;
  final bool orderIsDraft;

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
    _loadCatalog();
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

  void _addItem(MenuItemDto item) {
    if (item.hasVariableIngredients) {
      _showVariableChoiceDialog(item);
      return;
    }
    setState(() {
      final idx = _cart.indexWhere(
        (c) => c.menuItemId == item.id && c.ingredientChoices.isEmpty,
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

  Future<void> _showVariableChoiceDialog(MenuItemDto item) async {
    // Map: recipeIngredientId -> chosen articleId
    final Map<String, String> chosen = {
      for (final slot in item.variableIngredients)
        slot.recipeIngredientId: slot.defaultArticleId,
    };

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          backgroundColor: kBgCard,
          title: Text(
            item.name,
            style: GoogleFonts.cinzel(color: kGold, fontSize: 15),
          ),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: item.variableIngredients.map((slot) {
                final options = slot.allOptions;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${slot.quantity} ${slot.unitSymbol} de:',
                        style: GoogleFonts.lato(
                          color: kParchmentDim,
                          fontSize: 12,
                        ),
                      ),
                      const SizedBox(height: 6),
                      ...options.map((opt) {
                        final isSelected =
                            chosen[slot.recipeIngredientId] == opt['articleId'];
                        return GestureDetector(
                          onTap: () => setDialogState(() {
                            chosen[slot.recipeIngredientId] = opt['articleId']!;
                          }),
                          child: AnimatedContainer(
                            duration: const Duration(milliseconds: 120),
                            margin: const EdgeInsets.only(bottom: 6),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: isSelected ? kGold.withAlpha(30) : kBgMid,
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(
                                color: isSelected
                                    ? kGold
                                    : kGoldDark.withAlpha(60),
                                width: isSelected ? 1.5 : 1,
                              ),
                            ),
                            child: Row(
                              children: [
                                Icon(
                                  isSelected
                                      ? Icons.radio_button_checked_rounded
                                      : Icons.radio_button_off_rounded,
                                  size: 16,
                                  color: isSelected
                                      ? kGold
                                      : kParchmentDim.withAlpha(120),
                                ),
                                const SizedBox(width: 8),
                                Text(
                                  opt['articleName']!,
                                  style: GoogleFonts.lato(
                                    color: isSelected
                                        ? kParchment
                                        : kParchmentDim,
                                    fontSize: 13,
                                    fontWeight: isSelected
                                        ? FontWeight.w600
                                        : FontWeight.w400,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      }),
                    ],
                  ),
                );
              }).toList(),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: Text(
                'Cancelar',
                style: GoogleFonts.lato(color: kParchmentDim),
              ),
            ),
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(true),
              child: Text(
                'Agregar',
                style: GoogleFonts.lato(
                  color: kGold,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
      ),
    );

    if (confirmed == true && mounted) {
      final choices = item.variableIngredients.map((slot) {
        final chosenId =
            chosen[slot.recipeIngredientId] ?? slot.defaultArticleId;
        final allOpts = slot.allOptions;
        final name = allOpts.firstWhere(
          (o) => o['articleId'] == chosenId,
          orElse: () => {'articleName': chosenId},
        )['articleName']!;
        return CartItemChoice(
          recipeIngredientId: slot.recipeIngredientId,
          chosenArticleId: chosenId,
          chosenArticleName: name,
        );
      }).toList();

      setState(() {
        _cart.add(
          CartItem(
            menuItemId: item.id,
            name: item.name,
            price: item.price,
            ingredientChoices: choices,
          ),
        );
      });
    }
  }

  void _changeQuantity(int idx, int delta) {
    setState(() {
      _cart[idx].quantity += delta;
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
    final idx = _cart.indexWhere((c) => c.menuItemId == menuItemId);
    return idx >= 0 ? _cart[idx].quantity : 0;
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
          Positioned(left: 0, right: 0, bottom: 0, child: _buildCartPanel()),
        ],
      ),
    );
  }

  // ── Barra de categorías ───────────────────────────────────────────────────

  Widget _buildCategoryBar() {
    return Container(
      height: 48,
      color: kBgMid,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
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
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
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
            fontSize: 12,
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
            width: 28,
            alignment: Alignment.center,
            child: Text(
              '${item.quantity}',
              style: GoogleFonts.cinzel(
                color: kGold,
                fontSize: 14,
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
                if (item.choicesLabel != null)
                  Text(
                    item.choicesLabel!,
                    style: GoogleFonts.lato(
                      color: const Color(0xFFFFAB00),
                      fontSize: 11,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
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
                  onTap: onToggleTakeout,
                  child: Padding(
                    padding: const EdgeInsets.only(top: 3),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          item.isTakeout
                              ? Icons.check_box_rounded
                              : Icons.check_box_outline_blank_rounded,
                          size: 16,
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
                            fontSize: 11,
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
          GestureDetector(
            onTap: onNote,
            child: Icon(
              item.notes != null
                  ? Icons.comment_rounded
                  : Icons.comment_outlined,
              size: 18,
              color: item.notes != null ? kGold : kParchmentDim.withAlpha(120),
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
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 26,
        height: 26,
        decoration: BoxDecoration(
          color: kBgCard,
          shape: BoxShape.circle,
          border: Border.all(color: kGoldDark.withAlpha(80)),
        ),
        child: Icon(icon, size: 14, color: kGoldLight),
      ),
    );
  }
}
