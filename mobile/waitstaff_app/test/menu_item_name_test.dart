import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';

MenuItemDto parse(String name, [String? generalName]) => MenuItemDto.fromJson({
  'id': 'plate',
  'name': name,
  'generalName': generalName,
  'price': 5.5,
  'menuCategoryId': 'category',
});

void main() {
  test('combines thematic and real name without changing catalog identity', () {
    final item = parse(' Pluma mistica ', ' Filete de pollo ');
    expect(item.displayName, 'Pluma mistica - Filete de pollo');
    expect(item.name, ' Pluma mistica ');
    expect(item.id, 'plate');
    expect(item.price, 5.5);
  });

  test(
    'does not duplicate equal names or append a separator for legacy items',
    () {
      expect(parse('Limonada', ' limonada ').displayName, 'Limonada');
      expect(parse('Limonada').displayName, 'Limonada');
      expect(parse('Limonada', ' ').displayName, 'Limonada');
      expect(parse('', 'Filete de pollo').displayName, 'Filete de pollo');
    },
  );
}
