import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:wakelock_plus/wakelock_plus.dart';
import 'providers/station_provider.dart';
import 'screens/login_screen.dart';
import 'screens/station_picker_screen.dart';
import 'screens/kds_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Forzar orientación landscape en tablets/TV box de cocina
  SystemChrome.setPreferredOrientations([
    DeviceOrientation.landscapeLeft,
    DeviceOrientation.landscapeRight,
  ]);
  SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  // Mantener pantalla encendida mientras la app esté activa
  await WakelockPlus.enable();
  runApp(const StationApp());
}

class StationApp extends StatelessWidget {
  const StationApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => StationProvider()..init(),
      child: MaterialApp(
        title: 'Grimorio KDS',
        debugShowCheckedModeBanner: false,
        theme: ThemeData.dark().copyWith(
          colorScheme: const ColorScheme.dark(
            primary: Color(0xFFE94560),
            surface: Color(0xFF16213E),
          ),
          scaffoldBackgroundColor: const Color(0xFF1A1A2E),
        ),
        home: const _RootNavigator(),
      ),
    );
  }
}

class _RootNavigator extends StatelessWidget {
  const _RootNavigator();

  @override
  Widget build(BuildContext context) {
    final state = context.watch<StationProvider>().appState;

    return switch (state) {
      AppState.unauthenticated => const LoginScreen(),
      AppState.pickingStation => const StationPickerScreen(),
      AppState.ready => const KdsScreen(),
    };
  }
}
