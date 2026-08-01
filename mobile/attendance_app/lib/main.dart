import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:wakelock_plus/wakelock_plus.dart';

import 'core/theme/app_theme.dart';
import 'features/setup/presentation/pages/setup_page.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await WakelockPlus.enable();
  runApp(const ProviderScope(child: GrimorioAttendanceApp()));
}

class GrimorioAttendanceApp extends StatelessWidget {
  const GrimorioAttendanceApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Grimorio Asistencia',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.dark,
      home: const SetupPage(),
    );
  }
}
