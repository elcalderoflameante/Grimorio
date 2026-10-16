import 'package:shared_preferences/shared_preferences.dart';
import '../models/display_size_mode.dart';

class DisplayPreferencesService {
  static const _key = 'kds_display_size';

  Future<DisplaySizeMode> load() async {
    final prefs = await SharedPreferences.getInstance();
    final value = prefs.getString(_key);
    return DisplaySizeMode.values.firstWhere(
      (mode) => mode.name == value,
      orElse: () => DisplaySizeMode.automatic,
    );
  }

  Future<void> save(DisplaySizeMode mode) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, mode.name);
  }
}
