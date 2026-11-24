import 'dart:convert';
import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:inventory_app/api.dart';
import 'package:inventory_app/core/theme/app_theme.dart';
import 'package:inventory_app/main.dart';

const articleName = 'Pulpa de maracuyá para bebidas y preparaciones';
Json previewCount() => {
  'id': 'preview-count',
  'areaName': 'Cocina y preparaciones',
  'businessDate': '2026-11-23',
  'status': 'Draft',
  'lines': <Json>[
    {
      'id': 'a',
      'articleName': articleName,
      'warehouseName': 'Bodega principal',
      'unitSymbol': 'kg',
      'quantity': null,
      'notes': null,
      'countedAt': null,
    },
    {
      'id': 'b',
      'articleName': 'Carne de hamburguesa',
      'warehouseName': 'Cámara fría',
      'unitSymbol': 'kg',
      'quantity': 4.5,
      'notes': null,
      'countedAt': '2026-11-23T22:00:00Z',
    },
  ],
};

Future<void> capture(WidgetTester tester, GlobalKey key, String name) async {
  if (!const bool.fromEnvironment('GENERATE_INVENTORY_PREVIEWS')) return;
  await tester.runAsync(() async {
    final boundary =
        key.currentContext!.findRenderObject()! as RenderRepaintBoundary;
    final image = await boundary.toImage();
    final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
    final file = File('build/design-preview/$name.png');
    await file.parent.create(recursive: true);
    await file.writeAsBytes(bytes!.buffer.asUint8List());
    image.dispose();
  });
}

Future<void> mount(
  WidgetTester tester,
  Widget page,
  GlobalKey key, {
  double width = 390,
  double textScale = 1,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = Size(width, 900);
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    RepaintBoundary(
      key: key,
      child: MaterialApp(
        debugShowCheckedModeBanner: false,
        theme: AppTheme.magicTheme(),
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(textScale)),
          child: child!,
        ),
        home: page,
      ),
    ),
  );
  await tester.pumpAndSettle();
  await tester.runAsync(
    () => precacheImage(
      const AssetImage('assets/images/ecf_logo.png'),
      tester.element(find.byType(Scaffold).first),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() async {
    final icons = FontLoader('MaterialIcons')
      ..addFont(rootBundle.load('fonts/MaterialIcons-Regular.otf'));
    final title = FontLoader('Cinzel')
      ..addFont(rootBundle.load('assets/fonts/Cinzel-Variable.ttf'));
    final body = FontLoader('Lato')
      ..addFont(rootBundle.load('assets/fonts/Lato-Regular.ttf'))
      ..addFont(rootBundle.load('assets/fonts/Lato-Bold.ttf'));
    await Future.wait([title.load(), body.load(), icons.load()]);
  });

  testWidgets('PIN screen stays usable on a narrow phone with larger text', (
    tester,
  ) async {
    final api = InventoryApi(
      client: MockClient(
        (request) async => http.Response(
          jsonEncode(
            request.url.path.endsWith('/branches')
                ? [
                    {'id': 'branch', 'name': 'El Caldero Flameante'},
                  ]
                : [
                    {
                      'id': 'user',
                      'firstName': 'María Fernanda',
                      'lastName': 'Ramírez',
                      'hasKdsPin': true,
                    },
                  ],
          ),
          200,
        ),
      ),
    );
    addTearDown(api.close);
    final key = GlobalKey();
    await mount(
      tester,
      LoginPage(api: api, onLogin: (_) {}),
      key,
      width: 320,
      textScale: 1.3,
    );
    expect(tester.takeException(), isNull);
    await capture(tester, key, 'login-small');
  });

  testWidgets('shared areas fit the branded phone layout', (tester) async {
    final api = InventoryApi(
      client: MockClient(
        (_) async => http.Response(
          jsonEncode([
            {
              'areaId': 'kitchen',
              'areaName': 'Cocina',
              'pendingArticles': 12,
              'draftId': null,
              'counterName': null,
              'recount': false,
            },
            {
              'areaId': 'bar',
              'areaName': 'Bar y bebidas',
              'pendingArticles': 5,
              'draftId': 'draft',
              'counterName': 'María Fernanda',
              'recount': false,
            },
            {
              'areaId': 'tables',
              'areaName': 'Mesas',
              'pendingArticles': 0,
              'draftId': null,
              'counterName': null,
              'recount': false,
            },
          ]),
          200,
        ),
      ),
    );
    addTearDown(api.close);
    final key = GlobalKey();
    await mount(
      tester,
      AreasPage(api: api, name: 'María Fernanda Ramírez', onLogout: () {}),
      key,
    );
    expect(tester.takeException(), isNull);
    expect(find.text('Continuar conteo'), findsOneWidget);
    await capture(tester, key, 'areas');
  });

  testWidgets(
    'long article names and quantity entry fit at increased text scale',
    (tester) async {
      final api = InventoryApi();
      addTearDown(api.close);
      final key = GlobalKey();
      await mount(
        tester,
        CountPage(api: api, initial: previewCount()),
        key,
        width: 320,
        textScale: 1.3,
      );
      expect(tester.takeException(), isNull);
      await capture(tester, key, 'count-small');
      await tester.tap(find.text(articleName));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).at(1), '2,5');
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      await capture(tester, key, 'quantity-small');
    },
  );
}
