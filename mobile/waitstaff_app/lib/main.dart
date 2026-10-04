import 'package:flutter/material.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:grimorio_voice/grimorio_voice.dart';
import 'core/constants/api_config.dart';
import 'core/services/alert_service.dart';
import 'features/auth/data/services/auth_storage_service.dart';
import 'features/auth/presentation/providers/auth_controller.dart';

import 'core/theme/app_theme.dart';
import 'core/router/app_router.dart';
import 'core/services/push_notification_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  try {
    await Firebase.initializeApp();
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
  } catch (e) {
    debugPrint('[Firebase] Initialization failed: $e');
  }

  runApp(const ProviderScope(child: MyApp()));
}

class MyApp extends ConsumerWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(appRouterProvider);
    final auth = ref.watch(authControllerProvider);

    return MaterialApp.router(
      title: 'Grimorio - Meseros',
      theme: AppTheme.magicTheme(),
      themeMode: ThemeMode.dark,
      routerConfig: router,
      builder: (context, child) => auth.isAuthenticated
          ? VoiceShell(
              key: ValueKey(auth.session!.accessToken),
              apiBaseUrl: ApiConfig.baseUrl,
              readToken: ref.read(authStorageServiceProvider).readAccessToken,
              onChannelBusy: ref.read(alertServiceProvider).setVoiceBusy,
              child: child!,
            )
          : child!,
    );
  }
}
