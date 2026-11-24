import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

class InventoryContent extends StatelessWidget {
  const InventoryContent({
    super.key,
    required this.child,
    this.topSafeArea = false,
  });
  final Widget child;
  final bool topSafeArea;

  @override
  Widget build(BuildContext context) => SafeArea(
    top: topSafeArea,
    bottom: false,
    child: Align(
      alignment: Alignment.topCenter,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 760),
        child: child,
      ),
    ),
  );
}

class InventoryNotice extends StatelessWidget {
  const InventoryNotice({
    super.key,
    required this.text,
    this.icon = Icons.info_outline_rounded,
  });
  final String text;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(
      color: kGold.withAlpha(12),
      borderRadius: BorderRadius.circular(10),
      border: Border.all(color: kGoldDark.withAlpha(70)),
    ),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, color: kGold, size: 20),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            text,
            style: const TextStyle(
              color: kParchmentDim,
              fontSize: 13,
              height: 1.45,
            ),
          ),
        ),
      ],
    ),
  );
}
