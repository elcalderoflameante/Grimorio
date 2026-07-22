class ModifierSelection {
  final String optionName;
  final int quantity;

  const ModifierSelection({
    required this.optionName,
    required this.quantity,
  });

  factory ModifierSelection.fromJson(Map<String, dynamic> json) =>
      ModifierSelection(
        optionName: json['optionName'] as String,
        quantity: (json['quantity'] as num?)?.toInt() ?? 1,
      );

  String get label => quantity > 1 ? '$optionName x$quantity' : optionName;
}

class StationItem {
  final String orderItemId;
  final String orderId;
  final int orderNumber;
  final String orderType;
  final String? tableCode;
  final String? customerName;
  final String? orderNotes;
  final String itemName;
  final int quantity;
  String? notes;
  final bool isTakeout;
  String status;
  final DateTime confirmedAt;
  final DateTime? updatedAt;
  final List<ModifierSelection> modifierSelections;

  StationItem({
    required this.orderItemId,
    required this.orderId,
    required this.orderNumber,
    required this.orderType,
    this.tableCode,
    this.customerName,
    this.orderNotes,
    required this.itemName,
    required this.quantity,
    this.notes,
    this.isTakeout = false,
    required this.status,
    required this.confirmedAt,
    this.updatedAt,
    this.modifierSelections = const [],
  });

  factory StationItem.fromJson(Map<String, dynamic> json) => StationItem(
        orderItemId: json['orderItemId'] as String,
        orderId: json['orderId'] as String,
        orderNumber: json['orderNumber'] as int,
        orderType: json['orderType'] as String,
        tableCode: json['tableCode'] as String?,
        customerName: json['customerName'] as String?,
        orderNotes: json['orderNotes'] as String?,
        itemName: json['itemName'] as String,
        quantity: json['quantity'] as int,
        notes: json['notes'] as String?,
        isTakeout: json['isTakeout'] as bool? ?? false,
        status: json['status'] as String,
        confirmedAt: DateTime.parse(json['confirmedAt'] as String),
        updatedAt: json['updatedAt'] != null
            ? DateTime.parse(json['updatedAt'] as String)
            : null,
        modifierSelections: (json['modifierSelections'] as List<dynamic>? ?? [])
            .map((e) => ModifierSelection.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  String get orderLabel {
    if (tableCode != null) return 'Mesa $tableCode';
    if (customerName != null) return customerName!;
    return '#$orderNumber';
  }
}
