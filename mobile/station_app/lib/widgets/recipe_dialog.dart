import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/api_config.dart';
import '../models/kitchen_recipe.dart';
import '../models/station_item.dart';
import '../providers/station_provider.dart';

class RecipeDialog extends StatefulWidget {
  final StationItem item;
  const RecipeDialog({super.key, required this.item});

  @override
  State<RecipeDialog> createState() => _RecipeDialogState();
}

class _RecipeDialogState extends State<RecipeDialog> {
  late Future<KitchenRecipe> _recipe;

  @override
  void initState() {
    super.initState();
    _recipe = context.read<StationProvider>().loadItemRecipe(
      widget.item.orderItemId,
    );
  }

  void _retry() => setState(() {
    _recipe = context.read<StationProvider>().loadItemRecipe(
      widget.item.orderItemId,
    );
  });

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<StationProvider>();
    final matching = provider.items.where(
      (i) => i.orderItemId == widget.item.orderItemId,
    );
    final item = matching.isNotEmpty ? matching.first : widget.item;
    final size = MediaQuery.sizeOf(context);
    return Dialog(
      backgroundColor: const Color(0xFF1C1C30),
      insetPadding: const EdgeInsets.all(12),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      child: SizedBox(
        width: math.min(820, size.width - 24),
        height: math.min(640, size.height - 48),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 6, 8),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Padding(
                    padding: EdgeInsets.only(top: 8),
                    child: Icon(
                      Icons.menu_book_rounded,
                      size: 22,
                      color: Color(0xFFB9F3FF),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Padding(
                      padding: const EdgeInsets.only(top: 5),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            item.itemName,
                            style: const TextStyle(
                              fontSize: 17,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            'Pedido #${item.orderNumber} - ${item.orderLabel}',
                            style: const TextStyle(
                              fontSize: 12,
                              color: Colors.white60,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Cerrar receta',
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.close_rounded),
                  ),
                ],
              ),
            ),
            const Divider(height: 1, color: Colors.white12),
            Expanded(
              child: FutureBuilder<KitchenRecipe>(
                future: _recipe,
                builder: (context, snapshot) {
                  if (snapshot.connectionState != ConnectionState.done) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (snapshot.hasError || snapshot.data == null) {
                    return Center(
                      child: Padding(
                        padding: const EdgeInsets.all(20),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(
                              Icons.cloud_off_rounded,
                              color: Colors.white54,
                              size: 32,
                            ),
                            const SizedBox(height: 12),
                            const Text(
                              'No se pudo consultar la receta.',
                              textAlign: TextAlign.center,
                            ),
                            const SizedBox(height: 12),
                            FilledButton.icon(
                              onPressed: _retry,
                              icon: const Icon(Icons.refresh_rounded),
                              label: const Text('Reintentar'),
                            ),
                          ],
                        ),
                      ),
                    );
                  }
                  return _RecipeContent(recipe: snapshot.data!, item: item);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _RecipeContent extends StatelessWidget {
  final KitchenRecipe recipe;
  final StationItem item;
  const _RecipeContent({required this.recipe, required this.item});

  Uri? get _imageUri {
    final path = recipe.imageUrl?.trim();
    if (path == null || path.isEmpty) return null;
    final parsed = Uri.tryParse(path);
    if (parsed == null) return null;
    final uri = Uri.parse('${ApiConfig.hubBaseUrl}/').resolveUri(parsed);
    return uri.scheme == 'https' || uri.scheme == 'http' ? uri : null;
  }

  @override
  Widget build(BuildContext context) {
    final preparation = recipe.preparation;
    final imageUri = _imageUri;
    final modifiers = item.modifierSelections
        .map((m) => m.label)
        .where((s) => s.isNotEmpty)
        .toList();
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: DefaultTextStyle(
        style: Theme.of(context).textTheme.bodyMedium!.copyWith(
          color: Colors.white,
          fontSize: 14,
          height: 1.35,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (imageUri != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: SizedBox(
                  height: 150,
                  child: Image.network(
                    imageUri.toString(),
                    fit: BoxFit.contain,
                    cacheWidth: 800,
                    errorBuilder: (_, _, _) => const Center(
                      child: Icon(
                        Icons.broken_image_outlined,
                        color: Colors.white38,
                      ),
                    ),
                  ),
                ),
              ),
            Text(
              recipe.name,
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            if (recipe.description?.trim().isNotEmpty == true) ...[
              const SizedBox(height: 6),
              Text(recipe.description!),
            ],
            _heading('En esta comanda'),
            Text(
              'Cantidad: ${item.quantity}${item.isTakeout ? ' - Para llevar' : ''}',
            ),
            if (modifiers.isNotEmpty) ...[
              const SizedBox(height: 8),
              Wrap(
                spacing: 6,
                runSpacing: 6,
                children: modifiers
                    .map(
                      (label) => Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: const Color(0xFFB9F3FF),
                          borderRadius: BorderRadius.circular(3),
                        ),
                        child: Text(
                          label,
                          style: const TextStyle(
                            color: Color(0xFF082F38),
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                    )
                    .toList(),
              ),
            ],
            if (item.orderNotes?.trim().isNotEmpty == true)
              _note('Pedido: ${item.orderNotes}'),
            if (item.notes?.trim().isNotEmpty == true)
              _note('Plato: ${item.notes}'),
            _heading('Ingredientes por porcion'),
            if (recipe.ingredients.isEmpty)
              const Text(
                'No hay ingredientes registrados para este plato.',
                style: TextStyle(color: Colors.white60),
              ),
            ...recipe.ingredients.map(
              (ingredient) => _Ingredient(ingredient: ingredient),
            ),
            _heading('Preparacion'),
            if (preparation == null)
              const Text(
                'No hay instrucciones de preparacion registradas.',
                style: TextStyle(color: Colors.white60),
              )
            else ...[
              Wrap(
                spacing: 16,
                runSpacing: 6,
                children: [
                  if (preparation.estimatedMinutes != null)
                    Text('Tiempo: ${preparation.estimatedMinutes} min'),
                  if (preparation.yieldLabel?.trim().isNotEmpty == true)
                    Text('Rendimiento: ${preparation.yieldLabel}'),
                  if (preparation.temperature?.trim().isNotEmpty == true)
                    Text('Temperatura: ${preparation.temperature}'),
                ],
              ),
              ...preparation.steps.map(
                (step) => Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${step.number}. ${step.instructions}',
                        style: TextStyle(
                          color: step.isCritical
                              ? const Color(0xFFFFE89A)
                              : Colors.white,
                          fontWeight: step.isCritical
                              ? FontWeight.w700
                              : FontWeight.normal,
                        ),
                      ),
                      if (step.estimatedMinutes != null ||
                          step.temperature?.trim().isNotEmpty == true)
                        Text(
                          [
                            if (step.estimatedMinutes != null)
                              '${step.estimatedMinutes} min',
                            if (step.temperature?.trim().isNotEmpty == true)
                              step.temperature!,
                          ].join(' - '),
                          style: const TextStyle(
                            color: Colors.white60,
                            fontSize: 12,
                          ),
                        ),
                    ],
                  ),
                ),
              ),
              if (preparation.presentation?.trim().isNotEmpty == true) ...[
                _heading('Presentacion'),
                Text(preparation.presentation!),
              ],
              if (preparation.notes?.trim().isNotEmpty == true) ...[
                _heading('Notas de preparacion'),
                Text(preparation.notes!),
              ],
            ],
          ],
        ),
      ),
    );
  }

  Widget _heading(String label) => Padding(
    padding: const EdgeInsets.only(top: 18, bottom: 8),
    child: Text(
      label,
      style: const TextStyle(
        fontSize: 14,
        fontWeight: FontWeight.bold,
        color: Color(0xFFB9F3FF),
      ),
    ),
  );

  Widget _note(String text) => Container(
    margin: const EdgeInsets.only(top: 8),
    padding: const EdgeInsets.all(8),
    decoration: BoxDecoration(
      color: const Color(0xFFFFE89A),
      borderRadius: BorderRadius.circular(3),
    ),
    child: Text(
      text,
      style: const TextStyle(
        color: Color(0xFF332500),
        fontWeight: FontWeight.w700,
      ),
    ),
  );
}

class _Ingredient extends StatelessWidget {
  final RecipeIngredient ingredient;
  const _Ingredient({required this.ingredient});

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 8),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(child: Text(ingredient.name)),
            const SizedBox(width: 12),
            Text(
              ingredient.quantityLabel,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
          ],
        ),
        if (ingredient.notes?.trim().isNotEmpty == true)
          Text(
            ingredient.notes!,
            style: const TextStyle(color: Colors.white60, fontSize: 12),
          ),
        if (ingredient.ingredients.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(left: 16, top: 6),
            child: Column(
              children: ingredient.ingredients
                  .map((child) => _Ingredient(ingredient: child))
                  .toList(),
            ),
          ),
      ],
    ),
  );
}
