enum TableServiceRequestStatus {
  pending(1),
  taken(2),
  inProgress(3),
  completed(4),
  cancelled(5);

  final int value;
  const TableServiceRequestStatus(this.value);

  static TableServiceRequestStatus fromValue(int value) {
    return TableServiceRequestStatus.values.firstWhere(
      (e) => e.value == value,
      orElse: () => TableServiceRequestStatus.pending,
    );
  }
}

enum TableServiceRequestType {
  napkins(1, 'Servilletas'),
  salt(2, 'Sal'),
  tomatoSauce(3, 'Salsa de tomate'),
  mayonnaise(4, 'Mayonesa'),
  chili(5, 'Ají'),
  container(6, 'Envase'),
  bill(7, 'Cuenta'),
  callWaiter(8, 'Llamar mesero'),
  custom(99, 'Mensaje personalizado');

  final int value;
  final String label;
  const TableServiceRequestType(this.value, this.label);

  static TableServiceRequestType fromValue(int value) {
    return TableServiceRequestType.values.firstWhere(
      (e) => e.value == value,
      orElse: () => TableServiceRequestType.custom,
    );
  }

  static TableServiceRequestType fromJson(dynamic raw) {
    if (raw is int) return fromValue(raw);
    if (raw is double) return fromValue(raw.toInt());
    if (raw is String) {
      final asInt = int.tryParse(raw);
      if (asInt != null) return fromValue(asInt);
      final lower = raw.toLowerCase();
      return TableServiceRequestType.values.firstWhere(
        (e) => e.name.toLowerCase() == lower,
        orElse: () => TableServiceRequestType.custom,
      );
    }
    return TableServiceRequestType.custom;
  }
}

class TableServiceRequest {
  final String id;
  final String branchId;
  final String restaurantTableId;
  final String tableCode;
  final String tableName;
  final String? tableArea;
  final TableServiceRequestType type;
  final String? customMessage;
  final TableServiceRequestStatus status;
  final DateTime requestedAt;
  final DateTime? takenAt;
  final DateTime? completedAt;
  final String? takenByUserId;
  final String? takenByName;

  const TableServiceRequest({
    required this.id,
    required this.branchId,
    required this.restaurantTableId,
    required this.tableCode,
    required this.tableName,
    this.tableArea,
    required this.type,
    this.customMessage,
    required this.status,
    required this.requestedAt,
    this.takenAt,
    this.completedAt,
    this.takenByUserId,
    this.takenByName,
  });

  String get displayDescription {
    if (type == TableServiceRequestType.custom) {
      final backendMessage = customMessage?.trim();
      if (backendMessage != null &&
          backendMessage.isNotEmpty &&
          backendMessage.contains(' ')) {
        return backendMessage;
      }
    }
    return type.label;
  }

  /// Texto que se usa para TTS: solo el customMessage para tipo custom,
  /// o el label fijo del enum para cualquier otro tipo.
  String get ttsDescription {
    if (type == TableServiceRequestType.custom) {
      final msg = customMessage?.trim();
      if (msg != null && msg.isNotEmpty) return msg;
    }
    return type.label;
  }

  TableServiceRequest copyWith({TableServiceRequestStatus? status}) {
    return TableServiceRequest(
      id: id,
      branchId: branchId,
      restaurantTableId: restaurantTableId,
      tableCode: tableCode,
      tableName: tableName,
      tableArea: tableArea,
      type: type,
      customMessage: customMessage,
      status: status ?? this.status,
      requestedAt: requestedAt,
      takenAt: takenAt,
      completedAt: completedAt,
      takenByUserId: takenByUserId,
      takenByName: takenByName,
    );
  }

  factory TableServiceRequest.fromJson(Map<String, dynamic> json) {
    return TableServiceRequest(
      id: json['id'] as String,
      branchId: json['branchId'] as String,
      restaurantTableId: json['restaurantTableId'] as String,
      tableCode: json['tableCode'] as String,
      tableName: json['tableName'] as String,
      tableArea: json['tableArea'] as String?,
      type: TableServiceRequestType.fromJson(json['type']),
      customMessage: json['customMessage'] as String?,
      status: TableServiceRequestStatus.fromValue(json['status'] as int),
      requestedAt: DateTime.parse(json['requestedAt'] as String),
      takenAt: json['takenAt'] != null ? DateTime.parse(json['takenAt'] as String) : null,
      completedAt: json['completedAt'] != null ? DateTime.parse(json['completedAt'] as String) : null,
      takenByUserId: json['takenByUserId'] as String?,
      takenByName: json['takenByName'] as String?,
    );
  }
}
