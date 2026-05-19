class WorkStation {
  final String id;
  final String name;
  final String type;
  final bool isActive;

  WorkStation({
    required this.id,
    required this.name,
    required this.type,
    required this.isActive,
  });

  factory WorkStation.fromJson(Map<String, dynamic> json) => WorkStation(
        id: json['id'] as String,
        name: json['name'] as String,
        type: json['type'] as String,
        isActive: json['isActive'] as bool,
      );
}
