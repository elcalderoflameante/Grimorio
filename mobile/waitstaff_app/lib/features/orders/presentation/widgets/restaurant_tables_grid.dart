import 'package:flutter/material.dart';

import '../../../../core/theme/app_theme.dart';
import '../../data/models/order_models.dart';

/// Rows grow with their contents instead of clipping balances at a fixed ratio.
class RestaurantTablesGrid extends StatelessWidget {
  const RestaurantTablesGrid({
    super.key,
    required this.tables,
    required this.now,
    required this.onTap,
  });

  final List<TableDto> tables;
  final DateTime now;
  final ValueChanged<TableDto> onTap;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) {
      const gap = 12.0;
      final textScale = MediaQuery.textScalerOf(context).scale(14) / 14;
      final minWidth = 140 * textScale.clamp(1.0, 3.0);
      final columns = ((constraints.maxWidth + gap) / (minWidth + gap))
          .floor()
          .clamp(1, 4)
          .toInt();
      return Column(
        children: [
          for (var start = 0; start < tables.length; start += columns) ...[
            if (start > 0) const SizedBox(height: gap),
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  for (var column = 0; column < columns; column++) ...[
                    if (column > 0) const SizedBox(width: gap),
                    if (start + column < tables.length)
                      Expanded(
                        child: _RestaurantTableCard(
                          key: ValueKey(tables[start + column].id),
                          table: tables[start + column],
                          now: now,
                          onTap: () => onTap(tables[start + column]),
                        ),
                      )
                    else
                      const Spacer(),
                  ],
                ],
              ),
            ),
          ],
        ],
      );
    },
  );
}

class _RestaurantTableCard extends StatelessWidget {
  const _RestaurantTableCard({
    super.key,
    required this.table,
    required this.now,
    required this.onTap,
  });

  final TableDto table;
  final DateTime now;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isDraft = table.currentStatus == 'Draft';
    final isOccupied = table.currentStatus == 'Occupied';
    final color = isOccupied
        ? const Color(0xFFFF6B6B)
        : isDraft
        ? const Color(0xFFFFAB00)
        : const Color(0xFF69F0AE);
    final status = isOccupied
        ? 'Ocupada'
        : isDraft
        ? 'Borrador'
        : 'Libre';
    final styles = Theme.of(context).textTheme;
    final elapsed = table.currentOrderStartedAt == null
        ? null
        : now.toUtc().difference(table.currentOrderStartedAt!.toUtc());
    final minutes = elapsed == null || elapsed.isNegative
        ? 0
        : elapsed.inMinutes;
    final duration = minutes < 60
        ? '$minutes min'
        : '${minutes ~/ 60}h ${(minutes % 60).toString().padLeft(2, '0')}m';

    return ConstrainedBox(
      constraints: const BoxConstraints(minHeight: 128),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Ink(
          decoration: BoxDecoration(
            color: color.withAlpha(18),
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: color.withAlpha(180), width: 1.5),
          ),
          padding: const EdgeInsets.all(12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Row(
                children: [
                  Icon(Icons.table_restaurant_rounded, color: color, size: 24),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Tooltip(
                      message: 'Mesa ${table.code}',
                      child: Text(
                        'Mesa ${table.code}',
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: styles.titleLarge?.copyWith(
                          color: kParchment,
                          fontSize: 17,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Wrap(
                alignment: WrapAlignment.center,
                crossAxisAlignment: WrapCrossAlignment.center,
                spacing: 8,
                runSpacing: 4,
                children: [
                  Text(
                    status,
                    style: styles.bodySmall?.copyWith(
                      color: color,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  if (!table.isFree && elapsed != null)
                    Tooltip(
                      message: 'Tiempo transcurrido: $duration',
                      child: Text(
                        duration,
                        style: styles.bodySmall?.copyWith(
                          color: kParchmentDim,
                          fontSize: 11,
                        ),
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 8),
              if (table.isFree)
                Text(
                  '${table.capacity} personas',
                  textAlign: TextAlign.center,
                  style: styles.bodySmall?.copyWith(
                    color: kParchmentDim,
                    fontSize: 12,
                  ),
                )
              else
                Wrap(
                  alignment: WrapAlignment.center,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  spacing: 5,
                  runSpacing: 2,
                  children: [
                    Text(
                      'Pendiente:',
                      style: styles.bodySmall?.copyWith(
                        color: kParchmentDim,
                        fontSize: 11,
                      ),
                    ),
                    Text(
                      '\$${table.pendingPaymentTotal.toStringAsFixed(2)}',
                      style: styles.bodyMedium?.copyWith(
                        color: kParchment,
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
            ],
          ),
        ),
      ),
    );
  }
}
