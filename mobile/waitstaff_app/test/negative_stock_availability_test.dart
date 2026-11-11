import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';

void main() {
  test(
    'approximate ingredients do not appear as missing when a strict ingredient is absent',
    () {
      final item = MenuItemAvailabilityDto.fromJson({
        'menuItemId': 'dish',
        'isTracked': true,
        'isAvailable': false,
        'availableQuantity': 0,
        'components': [
          {
            'articleName': 'Lettuce',
            'availableServings': 0,
            'allowNegativeStock': true,
          },
          {
            'articleName': 'Meat',
            'availableServings': 0,
            'allowNegativeStock': false,
          },
        ],
      });
      expect(item.missingIngredientNames, ['Meat']);
    },
  );
  test('old payloads remain strict and unrestricted capacity remains null', () {
    final strict = MenuItemAvailabilityDto.fromJson({
      'menuItemId': 'drink',
      'isTracked': true,
      'isAvailable': false,
      'components': [
        {'articleName': 'Soda', 'availableServings': 0},
      ],
    });
    expect(strict.missingIngredientNames, ['Soda']);
    final approximate = MenuItemAvailabilityDto.fromJson({
      'menuItemId': 'salad',
      'isTracked': false,
      'isAvailable': true,
      'availableQuantity': null,
      'components': [
        {
          'articleName': 'Lettuce',
          'availableServings': 0,
          'allowNegativeStock': true,
        },
      ],
    });
    expect(approximate.availableQuantity, isNull);
    expect(approximate.missingIngredientNames, isEmpty);
  });
}
