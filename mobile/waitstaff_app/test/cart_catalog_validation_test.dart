import 'package:flutter_test/flutter_test.dart';
import 'package:waitstaff_app/features/orders/data/models/cart_catalog_validation.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';

MenuItemDto menuItem({
  String visibility = 'MenuAndPromotions',
  double price = 10,
  List<MenuItemModifierGroupDto> groups = const [],
}) => MenuItemDto(
  id: 'plate',
  name: 'Plato',
  price: price,
  menuCategoryId: 'category',
  categoryName: 'Categoría',
  isActive: true,
  saleVisibility: visibility,
  modifierGroups: groups,
);

PromotionDto promotion({bool current = true, bool applies = true}) =>
    PromotionDto(
      id: 'promo',
      name: 'Promo',
      type: 'Percentage',
      discountPercent: 20,
      isActive: true,
      isCurrentlyActive: current,
      menuItemIds: applies ? ['plate'] : [],
    );

CartCatalogValidation validate(
  List<CartItem> cart,
  MenuItemDto item, {
  List<PromotionDto> promotions = const [],
  double stock = 10,
}) => validateCartCatalog(
  cart: cart,
  items: [item],
  promotions: promotions,
  availability: {
    'plate': MenuItemAvailabilityDto(
      menuItemId: 'plate',
      isTracked: true,
      isAvailable: stock > 0,
      availableQuantity: stock,
    ),
  },
);

void main() {
  test('promotion-only items require a current applicable promotion', () {
    final item = menuItem(visibility: 'PromotionsOnly');
    final line = CartItem(
      menuItemId: 'plate',
      name: 'Plato',
      price: 10,
      promotionId: 'promo',
    );
    expect(item.isPromotionsOnly, isTrue);
    expect(validate([line], item, promotions: [promotion()]).issues, isEmpty);
    final expired = validate(
      [line],
      item,
      promotions: [promotion(current: false)],
    );
    expect(expired.issues, isNotEmpty);
    expect(expired.changed, isTrue);
    expect(line.promotionId, isNull);
  });

  test('promotion no longer associated with the dish is removed', () {
    final line = CartItem(
      menuItemId: 'plate',
      name: 'Plato',
      price: 10,
      promotionId: 'promo',
      promotionName: 'Promo',
    );
    final result = validate(
      [line],
      menuItem(),
      promotions: [promotion(applies: false)],
    );
    expect(result.issues, isEmpty);
    expect(result.changed, isTrue);
    expect(line.promotionName, isNull);
  });

  test('draft prices include current modifier surcharges and shared stock', () {
    final item = menuItem(
      price: 12,
      groups: [
        const MenuItemModifierGroupDto(
          id: 'group',
          name: 'Extra',
          minSelections: 0,
          maxSelections: 2,
          isRequired: false,
          allowDuplicates: true,
          options: [
            MenuItemModifierOptionDto(
              id: 'cheese',
              name: 'Queso',
              priceDelta: 3,
              isTracked: true,
              isAvailable: true,
              availableQuantity: 3,
            ),
          ],
        ),
      ],
    );
    CartItem line() => CartItem(
      menuItemId: 'plate',
      name: 'Plato',
      price: 11,
      quantity: 2,
      modifierSelections: [
        const CartModifierSelection(
          modifierOptionId: 'cheese',
          groupName: 'Extra',
          optionName: 'Queso',
          quantity: 1,
          unitPriceDelta: 1,
          isTracked: false,
        ),
      ],
    );
    final cart = [line(), line()];
    final result = validate(cart, item);
    expect(result.changed, isTrue);
    expect(cart.first.price, 15);
    expect(cart.first.modifierSelections.first.unitPriceDelta, 3);
    expect(result.issues.single, contains('Queso'));
  });

  test('base stock is counted across normal and promotional lines', () {
    final cart = [
      CartItem(menuItemId: 'plate', name: 'Plato', price: 10, quantity: 2),
      CartItem(
        menuItemId: 'plate',
        name: 'Plato',
        price: 10,
        quantity: 2,
        promotionId: 'promo',
      ),
    ];
    expect(
      validate(cart, menuItem(), stock: 3, promotions: [promotion()]).issues,
      isNotEmpty,
    );
  });

  test(
    'removed dish or modifier blocks saving and permits removing the line',
    () {
      final cart = [
        CartItem(
          menuItemId: 'plate',
          name: 'Plato',
          price: 10,
          modifierSelections: [
            const CartModifierSelection(
              modifierOptionId: 'removed',
              groupName: 'Extra',
              optionName: 'Anterior',
              quantity: 1,
              unitPriceDelta: 0,
              isTracked: false,
            ),
          ],
        ),
      ];
      expect(validate(cart, menuItem()).issues.single, contains('Anterior'));
      expect(
        validateCartCatalog(
          cart: cart,
          items: [],
          promotions: [],
          availability: {},
        ).issues.single,
        contains('menú'),
      );
      cart.clear();
      expect(validate(cart, menuItem()).issues, isEmpty);
    },
  );

  test('missing ingredients show all shortages without duplicates', () {
    final stock = MenuItemAvailabilityDto.fromJson({
      'menuItemId': 'plate',
      'components': [
        {'articleName': 'Carne', 'availableServings': 0},
        {'articleName': 'Salsa', 'availableServings': 0},
        {'articleName': 'Carne', 'availableServings': 0},
        {'articleName': 'Papas', 'availableServings': 5},
      ],
    });
    expect(stock.missingIngredientNames, ['Carne', 'Salsa']);
    expect(
      MenuItemAvailabilityDto.fromJson({
        'menuItemId': 'plate',
        'limitingArticleName': 'Carne',
      }).missingIngredientNames,
      ['Carne'],
    );
  });
}
