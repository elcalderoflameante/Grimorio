class RecipeIngredient {
  final String name;
  final double? quantity;
  final String unit;
  final String? notes;
  final List<RecipeIngredient> ingredients;

  const RecipeIngredient({
    required this.name,
    this.quantity,
    this.unit = '',
    this.notes,
    this.ingredients = const [],
  });

  factory RecipeIngredient.fromJson(Map<String, dynamic> json) =>
      RecipeIngredient(
        name: json['name'] as String? ?? '',
        quantity: (json['quantity'] as num?)?.toDouble(),
        unit: json['unit'] as String? ?? '',
        notes: json['notes'] as String?,
        ingredients: (json['ingredients'] as List? ?? [])
            .map((e) => RecipeIngredient.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  String get quantityLabel {
    if (quantity == null) return '';
    final value = quantity!
        .toStringAsFixed(4)
        .replaceFirst(RegExp(r'\.?0+$'), '')
        .replaceAll('.', ',');
    return '$value $unit'.trim();
  }
}

class RecipeStep {
  final int number;
  final String instructions;
  final int? estimatedMinutes;
  final String? temperature;
  final bool isCritical;

  const RecipeStep({
    required this.number,
    required this.instructions,
    this.estimatedMinutes,
    this.temperature,
    this.isCritical = false,
  });

  factory RecipeStep.fromJson(Map<String, dynamic> json) => RecipeStep(
    number: (json['stepNumber'] as num?)?.toInt() ?? 0,
    instructions: json['instructions'] as String? ?? '',
    estimatedMinutes: (json['estimatedMinutes'] as num?)?.toInt(),
    temperature: json['temperature'] as String?,
    isCritical: json['isCritical'] as bool? ?? false,
  );
}

class RecipePreparation {
  final int? estimatedMinutes;
  final String? yieldLabel;
  final String? temperature;
  final String? presentation;
  final String? notes;
  final List<RecipeStep> steps;

  const RecipePreparation({
    this.estimatedMinutes,
    this.yieldLabel,
    this.temperature,
    this.presentation,
    this.notes,
    this.steps = const [],
  });

  factory RecipePreparation.fromJson(Map<String, dynamic> json) =>
      RecipePreparation(
        estimatedMinutes: (json['estimatedMinutes'] as num?)?.toInt(),
        yieldLabel: json['yield'] as String?,
        temperature: json['temperature'] as String?,
        presentation: json['presentation'] as String?,
        notes: json['notes'] as String?,
        steps: (json['steps'] as List? ?? [])
            .map((e) => RecipeStep.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class KitchenRecipe {
  final String name;
  final String? description;
  final String? imageUrl;
  final List<RecipeIngredient> ingredients;
  final RecipePreparation? preparation;

  const KitchenRecipe({
    required this.name,
    this.description,
    this.imageUrl,
    this.ingredients = const [],
    this.preparation,
  });

  factory KitchenRecipe.fromJson(Map<String, dynamic> json) => KitchenRecipe(
    name: json['name'] as String? ?? '',
    description: json['description'] as String?,
    imageUrl: json['imageUrl'] as String?,
    ingredients: (json['ingredients'] as List? ?? [])
        .map((e) => RecipeIngredient.fromJson(e as Map<String, dynamic>))
        .toList(),
    preparation: json['preparation'] is Map<String, dynamic>
        ? RecipePreparation.fromJson(
            json['preparation'] as Map<String, dynamic>,
          )
        : null,
  );
}
