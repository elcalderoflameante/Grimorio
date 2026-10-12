import 'order_models.dart';

class CartCatalogValidation {
  const CartCatalogValidation({required this.issues, required this.changed});

  final List<String> issues;
  final bool changed;
}

/// Reprices editable items; an ambiguous request must retain its original payload.
CartCatalogValidation validateCartCatalog({
  required List<CartItem> cart,
  required List<MenuItemDto> items,
  required List<PromotionDto> promotions,
  required Map<String, MenuItemAvailabilityDto> availability,
}) {
  final issues = <String>{};
  var changed = false;
  final itemsById = {for (final item in items) item.id: item};
  final usedItems = <String, int>{};
  final usedOptions = <String, int>{};
  for (final line in cart) {
    usedItems.update(
      line.menuItemId,
      (n) => n + line.quantity,
      ifAbsent: () => line.quantity,
    );
    for (final selection in line.modifierSelections) {
      usedOptions.update(
        selection.modifierOptionId,
        (n) => n + selection.quantity * line.quantity,
        ifAbsent: () => selection.quantity * line.quantity,
      );
    }
  }
  for (final line in cart) {
    final item = itemsById[line.menuItemId];
    if (item == null || !item.isActive) {
      issues.add(
        '${line.name} ya no está disponible en el menú. Retíralo del carrito.',
      );
      continue;
    }
    final validPromotion = promotions.any(
      (promotion) =>
          promotion.id == line.promotionId &&
          promotion.isActive &&
          promotion.isCurrentlyActive &&
          promotion.appliesTo(item),
    );
    if (line.promotionId != null && !validPromotion) {
      line.promotionId = null;
      line.promotionName = null;
      changed = true;
    }
    if (item.isPromotionsOnly && !validPromotion) {
      issues.add(
        '${line.name} requiere una promoción vigente. Retíralo y selecciónalo desde Promociones.',
      );
    }
    final options = {
      for (final group in item.modifierGroups)
        for (final option in group.options) option.id: option,
    };
    var price = item.price;
    for (var index = 0; index < line.modifierSelections.length; index++) {
      final selection = line.modifierSelections[index];
      final option = options[selection.modifierOptionId];
      if (option == null) {
        issues.add(
          '${line.name}: ${selection.optionName} ya no está disponible. Retira el plato y selecciónalo nuevamente.',
        );
        continue;
      }
      price += option.priceDelta * selection.quantity;
      line.modifierSelections[index] = CartModifierSelection(
        modifierOptionId: option.id,
        groupName: selection.groupName,
        optionName: option.name,
        quantity: selection.quantity,
        unitPriceDelta: option.priceDelta,
        isTracked: option.isTracked,
        availableQuantity: option.availableQuantity,
      );
      if (!option.isAvailable ||
          (option.isTracked &&
              (usedOptions[option.id] ?? 0) >
                  (option.availableQuantity ?? 0).floor())) {
        issues.add(
          'No hay stock suficiente de ${option.name}. Reduce la cantidad o retira el plato.',
        );
      }
    }
    for (final group in item.modifierGroups) {
      final ids = group.options.map((option) => option.id).toSet();
      final selections = line.modifierSelections.where(
        (s) => ids.contains(s.modifierOptionId),
      );
      final count = selections.fold(
        0,
        (n, selection) => n + selection.quantity,
      );
      if (count < group.effectiveMinimum ||
          count > group.maxSelections ||
          (!group.allowDuplicates && selections.any((s) => s.quantity > 1))) {
        issues.add(
          '${line.name}: revisa ${group.name}. Retira el plato y selecciónalo nuevamente.',
        );
      }
    }
    if ((line.price - price).abs() > 0.001) {
      changed = true;
      line.price = price;
    }
    final stock = availability[item.id];
    if (stock == null) {
      issues.add(
        'No se pudo verificar el inventario de ${line.name}. Actualiza el menú.',
      );
    } else if (stock.isTracked &&
        (!stock.isAvailable ||
            (usedItems[item.id] ?? 0) >
                (stock.availableQuantity ?? 0).floor())) {
      final names = stock.missingIngredientNames;
      issues.add(
        '${line.name}: stock insuficiente${names.isEmpty ? '' : '. Faltan: ${names.join(', ')}'}. Reduce la cantidad o retira el plato.',
      );
    }
  }
  return CartCatalogValidation(issues: issues.toList(), changed: changed);
}
