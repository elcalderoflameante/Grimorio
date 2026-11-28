import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:waitstaff_app/features/orders/data/models/order_models.dart';
import 'package:waitstaff_app/features/orders/data/services/order_api_service.dart';
import 'package:waitstaff_app/features/orders/presentation/pages/new_order_page.dart';

const _dish = MenuItemDto(
  id: 'dish',
  name: 'Plato con opciones',
  price: 5,
  menuCategoryId: 'food',
  categoryName: 'Comida',
  isActive: true,
  hasModifiers: true,
  modifierGroups: [
    MenuItemModifierGroupDto(
      id: 'extras',
      name: 'Extras',
      minSelections: 0,
      maxSelections: 1,
      isRequired: false,
      allowDuplicates: false,
      options: [
        MenuItemModifierOptionDto(
          id: 'sauce',
          name: 'Salsa',
          priceDelta: 0,
          isTracked: false,
          isAvailable: true,
        ),
      ],
    ),
  ],
);

class _ModifierApi extends OrderApiService {
  _ModifierApi(super.ref);
  var pending = Completer<MenuItemDto>();
  var detailRequests = 0;
  @override
  Future<List<MenuCategoryDto>> getCategories() async => [
    const MenuCategoryDto(id: 'food', name: 'Comida'),
  ];
  @override
  Future<List<MenuItemDto>> getMenuItems({String? categoryId}) async => [
    _dish,
    const MenuItemDto(
      id: 'other',
      name: 'Otro plato',
      price: 7,
      menuCategoryId: 'food',
      categoryName: 'Comida',
      isActive: true,
      hasModifiers: true,
    ),
  ];
  @override
  Future<List<MenuItemAvailabilityDto>> getMenuAvailability() async => [];
  @override
  Future<List<PromotionDto>> getActivePromotions() async => [];
  @override
  Future<MenuItemDto> getMenuItem(String id) {
    detailRequests++;
    return pending.future;
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final messenger =
      TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;

  setUpAll(() async {
    // Supply a valid bundled test font for font loading, never make HTTP calls.
    final font = await rootBundle.load('fonts/MaterialIcons-Regular.otf');
    const codec = StandardMessageCodec();
    final manifest = Map<Object?, Object?>.from(
      codec.decodeMessage(await rootBundle.load('AssetManifest.bin')) as Map,
    );
    for (final family in ['Cinzel', 'Lato']) {
      for (final weight in [
        'Thin',
        'ExtraLight',
        'Light',
        'Regular',
        'Medium',
        'SemiBold',
        'Bold',
        'ExtraBold',
        'Black',
      ]) {
        final path = 'test-fonts/$family-$weight.ttf';
        manifest[path] = [
          {'asset': path},
        ];
      }
    }
    final encodedManifest = codec.encodeMessage(manifest);
    messenger.setMockMessageHandler('flutter/assets', (message) async {
      final path = utf8.decode(
        message!.buffer.asUint8List(
          message.offsetInBytes,
          message.lengthInBytes,
        ),
      );
      if (path == 'AssetManifest.bin') return encodedManifest;
      if (path.startsWith('test-fonts/') ||
          path == 'fonts/MaterialIcons-Regular.otf') {
        return font;
      }
      return null;
    });
    GoogleFonts.config.allowRuntimeFetching = false;
  });
  tearDownAll(() {
    messenger.setMockMessageHandler('flutter/assets', null);
    GoogleFonts.config.allowRuntimeFetching = true;
  });

  Future<_ModifierApi> openEditor(WidgetTester tester) async {
    tester.view.devicePixelRatio = 1;
    tester.view.physicalSize = const Size(600, 1000);
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    late _ModifierApi api;
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          orderApiServiceProvider.overrideWith(
            (ref) => api = _ModifierApi(ref),
          ),
        ],
        child: const MaterialApp(home: NewOrderPage(type: OrderType.dineIn)),
      ),
    );
    await tester.pumpAndSettle();
    return api;
  }

  VoidCallback productTap(
    WidgetTester tester, {
    String name = 'Plato con opciones',
  }) => tester
      .widget<GestureDetector>(
        find
            .ancestor(
              of: find.text(name),
              matching: find.byType(GestureDetector),
            )
            .first,
      )
      .onTap!;

  testWidgets(
    'double tap before the next frame opens one dialog and adds once',
    (tester) async {
      final api = await openEditor(tester);
      final add = productTap(tester);
      add();
      add();
      productTap(tester, name: 'Otro plato')();
      expect(api.detailRequests, 1);
      await tester.pump();
      expect(find.text('Cargando opciones…'), findsOneWidget);
      api.pending.complete(_dish);
      await tester.pumpAndSettle();
      expect(find.byType(AlertDialog), findsOneWidget);
      add(); // Even an already queued callback cannot open a second dialog.
      expect(api.detailRequests, 1);
      final confirm = tester
          .widget<FilledButton>(find.widgetWithText(FilledButton, 'Agregar'))
          .onPressed!;
      confirm();
      confirm();
      await tester.pumpAndSettle();
      expect(find.byType(AlertDialog), findsNothing);
      expect(find.byType(NewOrderPage), findsOneWidget);
      expect(find.text('1 ítem en el carrito'), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.pumpWidget(const SizedBox.shrink());
    },
  );

  testWidgets('cancel and request failure release the lock for retry', (
    tester,
  ) async {
    final api = await openEditor(tester);
    final add = productTap(tester);
    add();
    api.pending.complete(_dish);
    await tester.pumpAndSettle();
    final cancel = tester
        .widget<TextButton>(find.widgetWithText(TextButton, 'Cancelar'))
        .onPressed!;
    cancel();
    cancel();
    await tester.pumpAndSettle();
    expect(find.byType(NewOrderPage), findsOneWidget);
    expect(find.text('1 ítem en el carrito'), findsNothing);
    api.pending = Completer<MenuItemDto>();
    add();
    api.pending.completeError(StateError('Simulated unavailable options'));
    await tester.pumpAndSettle();
    expect(find.text('Cargando opciones…'), findsNothing);
    api.pending = Completer<MenuItemDto>();
    add();
    api.pending.complete(_dish);
    await tester.pumpAndSettle();
    expect(api.detailRequests, 3);
    expect(find.byType(AlertDialog), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('leaving the editor during loading never opens a late dialog', (
    tester,
  ) async {
    final api = await openEditor(tester);
    productTap(tester)();
    await tester.pumpWidget(const SizedBox.shrink());
    api.pending.complete(_dish);
    await tester.pumpAndSettle();
    expect(find.byType(AlertDialog), findsNothing);
    expect(tester.takeException(), isNull);
  });
}
