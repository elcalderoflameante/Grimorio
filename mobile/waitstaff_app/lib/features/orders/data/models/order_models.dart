// ── Enums ──────────────────────────────────────────────────────────────────

enum OrderType {
  dineIn('DineIn', 'Mesa'),
  takeout('Takeout', 'Para llevar'),
  delivery('Delivery', 'Domicilio');

  final String apiValue;
  final String label;
  const OrderType(this.apiValue, this.label);

  static OrderType fromApi(dynamic raw) => OrderType.values.firstWhere(
    (e) => e.apiValue == raw.toString(),
    orElse: () => OrderType.dineIn,
  );
}

enum OrderStatus {
  draft('Draft', 'Borrador'),
  confirmed('Confirmed', 'Confirmado'),
  inPreparation('InPreparation', 'En preparación'),
  ready('Ready', 'Listo'),
  delivered('Delivered', 'Entregado'),
  cancelled('Cancelled', 'Cancelado');

  final String apiValue;
  final String label;
  const OrderStatus(this.apiValue, this.label);

  static OrderStatus fromApi(dynamic raw) => OrderStatus.values.firstWhere(
    (e) => e.apiValue == raw.toString(),
    orElse: () => OrderStatus.draft,
  );
}

enum OrderItemStatus {
  pending('Pending'),
  inPreparation('InPreparation'),
  ready('Ready'),
  cancelled('Cancelled');

  final String apiValue;
  const OrderItemStatus(this.apiValue);

  static OrderItemStatus fromApi(dynamic raw) =>
      OrderItemStatus.values.firstWhere(
        (e) => e.apiValue == raw.toString(),
        orElse: () => OrderItemStatus.pending,
      );
}

// ── DTOs del servidor ──────────────────────────────────────────────────────

class OrderItemDto {
  final String id;
  final String menuItemId;
  final String itemName;
  final String? itemCode;
  final String? stationId;
  final String? stationName;
  final int quantity;
  final double unitPrice;
  final double totalPrice;
  final String? notes;
  final bool isTakeout;
  final OrderItemStatus status;
  final List<ModifierSelectionDto> modifierSelections;

  const OrderItemDto({
    required this.id,
    required this.menuItemId,
    required this.itemName,
    this.itemCode,
    this.stationId,
    this.stationName,
    required this.quantity,
    required this.unitPrice,
    required this.totalPrice,
    this.notes,
    required this.isTakeout,
    required this.status,
    this.modifierSelections = const [],
  });

  factory OrderItemDto.fromJson(Map<String, dynamic> j) => OrderItemDto(
    id: j['id'] as String,
    menuItemId: j['menuItemId'] as String,
    itemName: j['itemName'] as String? ?? '',
    itemCode: j['itemCode'] as String?,
    stationId: j['stationId'] as String?,
    stationName: j['stationName'] as String?,
    quantity: (j['quantity'] as num).toInt(),
    unitPrice: (j['unitPrice'] as num).toDouble(),
    totalPrice: (j['totalPrice'] as num).toDouble(),
    notes: j['notes'] as String?,
    isTakeout: j['isTakeout'] as bool? ?? false,
    status: OrderItemStatus.fromApi(j['status']),
    modifierSelections: (j['modifierSelections'] as List<dynamic>? ?? [])
        .map((e) => ModifierSelectionDto.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
}

class ModifierSelectionDto {
  final String groupName;
  final String optionName;
  final int quantity;
  final double totalPriceDelta;

  const ModifierSelectionDto({
    required this.groupName,
    required this.optionName,
    required this.quantity,
    required this.totalPriceDelta,
  });

  String get label => quantity > 1 ? '$optionName ×$quantity' : optionName;

  factory ModifierSelectionDto.fromJson(Map<String, dynamic> j) =>
      ModifierSelectionDto(
        groupName: j['groupName'] as String? ?? '',
        optionName: j['optionName'] as String? ?? '',
        quantity: (j['quantity'] as num?)?.toInt() ?? 1,
        totalPriceDelta: (j['totalPriceDelta'] as num?)?.toDouble() ?? 0,
      );
}

class OrderDto {
  final String id;
  final int number;
  final OrderType type;
  final OrderStatus status;
  final String? tableId;
  final String? tableCode;
  final String? tableName;
  final String? customerName;
  final String? deliveryAddress;
  final String? notes;
  final double subtotal;
  final double total;
  final DateTime? confirmedAt;
  final List<OrderItemDto> items;

  const OrderDto({
    required this.id,
    required this.number,
    required this.type,
    required this.status,
    this.tableId,
    this.tableCode,
    this.tableName,
    this.customerName,
    this.deliveryAddress,
    this.notes,
    required this.subtotal,
    required this.total,
    this.confirmedAt,
    required this.items,
  });

  String get displayTitle {
    if (type == OrderType.dineIn && tableCode != null) return 'Mesa $tableCode';
    if (customerName != null && customerName!.isNotEmpty) return customerName!;
    return type.label;
  }

  factory OrderDto.fromJson(Map<String, dynamic> j) => OrderDto(
    id: j['id'] as String,
    number: (j['number'] as num).toInt(),
    type: OrderType.fromApi(j['type']),
    status: OrderStatus.fromApi(j['status']),
    tableId: j['tableId'] as String?,
    tableCode: j['tableCode'] as String?,
    tableName: j['tableName'] as String?,
    customerName: j['customerName'] as String?,
    deliveryAddress: j['deliveryAddress'] as String?,
    notes: j['notes'] as String?,
    subtotal: (j['subtotal'] as num).toDouble(),
    total: (j['total'] as num).toDouble(),
    confirmedAt: j['confirmedAt'] != null
        ? DateTime.parse(j['confirmedAt'] as String)
        : null,
    items: (j['items'] as List<dynamic>? ?? [])
        .map((e) => OrderItemDto.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
}

class MenuCategoryDto {
  final String id;
  final String name;
  final String? color;

  const MenuCategoryDto({required this.id, required this.name, this.color});

  factory MenuCategoryDto.fromJson(Map<String, dynamic> j) => MenuCategoryDto(
    id: j['id'] as String,
    name: j['name'] as String,
    color: j['color'] as String?,
  );
}

class MenuItemModifierOptionDto {
  final String id;
  final String name;
  final double priceDelta;
  final bool isTracked;
  final bool isAvailable;
  final double? availableQuantity;

  const MenuItemModifierOptionDto({
    required this.id,
    required this.name,
    required this.priceDelta,
    required this.isTracked,
    required this.isAvailable,
    this.availableQuantity,
  });

  factory MenuItemModifierOptionDto.fromJson(Map<String, dynamic> j) =>
      MenuItemModifierOptionDto(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        priceDelta: (j['priceDelta'] as num?)?.toDouble() ?? 0,
        isTracked: j['isTracked'] as bool? ?? false,
        isAvailable: j['isAvailable'] as bool? ?? true,
        availableQuantity: (j['availableQuantity'] as num?)?.toDouble(),
      );
}

class MenuItemModifierGroupDto {
  final String id;
  final String name;
  final int minSelections;
  final int maxSelections;
  final bool isRequired;
  final bool allowDuplicates;
  final List<MenuItemModifierOptionDto> options;

  const MenuItemModifierGroupDto({
    required this.id,
    required this.name,
    required this.minSelections,
    required this.maxSelections,
    required this.isRequired,
    required this.allowDuplicates,
    required this.options,
  });

  int get effectiveMinimum =>
      isRequired && minSelections == 0 ? 1 : minSelections;

  factory MenuItemModifierGroupDto.fromJson(Map<String, dynamic> j) =>
      MenuItemModifierGroupDto(
        id: j['id'] as String,
        name: j['name'] as String? ?? '',
        minSelections: (j['minSelections'] as num?)?.toInt() ?? 0,
        maxSelections: (j['maxSelections'] as num?)?.toInt() ?? 1,
        isRequired: j['isRequired'] as bool? ?? false,
        allowDuplicates: j['allowDuplicates'] as bool? ?? false,
        options: (j['options'] as List<dynamic>? ?? [])
            .map(
              (e) =>
                  MenuItemModifierOptionDto.fromJson(e as Map<String, dynamic>),
            )
            .toList(),
      );
}

class MenuItemDto {
  final String id;
  final String name;
  final String? description;
  final double price;
  final String menuCategoryId;
  final String categoryName;
  final String? categoryColor;
  final bool isActive;
  final bool hasModifiers;
  final List<MenuItemModifierGroupDto> modifierGroups;

  const MenuItemDto({
    required this.id,
    required this.name,
    this.description,
    required this.price,
    required this.menuCategoryId,
    required this.categoryName,
    this.categoryColor,
    required this.isActive,
    this.hasModifiers = false,
    this.modifierGroups = const [],
  });

  factory MenuItemDto.fromJson(Map<String, dynamic> j) => MenuItemDto(
    id: j['id'] as String,
    name: j['name'] as String,
    description: j['description'] as String?,
    price: (j['price'] as num).toDouble(),
    menuCategoryId: j['menuCategoryId'] as String,
    categoryName: j['categoryName'] as String? ?? '',
    categoryColor: j['categoryColor'] as String?,
    isActive: j['isActive'] as bool? ?? true,
    hasModifiers: j['hasModifiers'] as bool? ?? false,
    modifierGroups: (j['modifierGroups'] as List<dynamic>? ?? [])
        .map(
          (e) => MenuItemModifierGroupDto.fromJson(e as Map<String, dynamic>),
        )
        .toList(),
  );
}

class TableDto {
  final String id;
  final String code;
  final String name;
  final String? area;
  final bool isActive;
  final String currentStatus;
  final int capacity;
  final String? currentOrderId;
  final double currentOrderTotal;
  final double pendingPaymentTotal;

  const TableDto({
    required this.id,
    required this.code,
    required this.name,
    this.area,
    required this.isActive,
    required this.currentStatus,
    this.capacity = 0,
    this.currentOrderId,
    this.currentOrderTotal = 0,
    this.pendingPaymentTotal = 0,
  });

  bool get isFree => currentStatus == 'Free';

  factory TableDto.fromJson(Map<String, dynamic> j) => TableDto(
    id: j['id'] as String,
    code: j['code'] as String,
    name: j['name'] as String? ?? 'Mesa ${j['code'] ?? ''}',
    area: j['area'] as String?,
    isActive: j['isActive'] as bool? ?? true,
    currentStatus: j['currentStatus'] as String? ?? 'Free',
    capacity: (j['capacity'] as num?)?.toInt() ?? 0,
    currentOrderId: j['currentOrderId'] as String?,
    currentOrderTotal: (j['currentOrderTotal'] as num?)?.toDouble() ?? 0,
    pendingPaymentTotal: (j['pendingPaymentTotal'] as num?)?.toDouble() ?? 0,
  );
}

// ── Carrito local (no viene del servidor) ─────────────────────────────────

class CartModifierSelection {
  final String modifierOptionId;
  final String groupName;
  final String optionName;
  final int quantity;
  final double unitPriceDelta;
  final bool isTracked;
  final double? availableQuantity;

  const CartModifierSelection({
    required this.modifierOptionId,
    required this.groupName,
    required this.optionName,
    required this.quantity,
    required this.unitPriceDelta,
    required this.isTracked,
    this.availableQuantity,
  });

  String get label => quantity > 1 ? '$optionName ×$quantity' : optionName;
}

class CartItem {
  final String menuItemId;
  final String name;
  final double price;
  int quantity;
  String? notes;
  bool isTakeout;
  final List<CartModifierSelection> modifierSelections;

  CartItem({
    required this.menuItemId,
    required this.name,
    required this.price,
    this.quantity = 1,
    this.notes,
    this.isTakeout = false,
    this.modifierSelections = const [],
  });

  double get subtotal => price * quantity;

  String? get modifiersLabel => modifierSelections.isEmpty
      ? null
      : modifierSelections.map((selection) => selection.label).join(', ');

  int? get maximumQuantityFromModifiers {
    final limits = modifierSelections
        .where(
          (selection) =>
              selection.isTracked && selection.availableQuantity != null,
        )
        .map(
          (selection) =>
              (selection.availableQuantity! / selection.quantity).floor(),
        )
        .toList();
    if (limits.isEmpty) return null;
    return limits.reduce((current, next) => current < next ? current : next);
  }
}
