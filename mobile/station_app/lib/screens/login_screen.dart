import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../providers/station_provider.dart';
import '../services/auth_service.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _pinCtrl = TextEditingController();
  List<KdsBranch> _branches = [];
  List<KdsUser> _users = [];
  KdsBranch? _selectedBranch;
  KdsUser? _selectedUser;
  bool _loadingBranches = true;
  bool _loadingUsers = false;
  String? _localError;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadBranches());
  }

  @override
  void dispose() {
    _pinCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadBranches() async {
    setState(() {
      _loadingBranches = true;
      _localError = null;
    });

    try {
      final branches = await context.read<StationProvider>().loadKdsBranches();
      if (!mounted) return;
      setState(() {
        _branches = branches;
        _selectedBranch = branches.length == 1 ? branches.first : null;
      });
      if (branches.length == 1) await _loadUsers(branches.first);
    } catch (e) {
      if (!mounted) return;
      setState(() => _localError = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loadingBranches = false);
    }
  }

  Future<void> _loadUsers(KdsBranch branch) async {
    setState(() {
      _loadingUsers = true;
      _users = [];
      _selectedUser = null;
      _pinCtrl.clear();
      _localError = null;
    });

    try {
      final users = await context.read<StationProvider>().loadKdsUsers(branch.id);
      if (!mounted) return;
      setState(() {
        _users = users;
        _selectedUser = users.length == 1 ? users.first : null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _localError = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => _loadingUsers = false);
    }
  }

  Future<void> _submit() async {
    final branch = _selectedBranch;
    final user = _selectedUser;
    final pin = _pinCtrl.text.trim();

    if (branch == null) {
      setState(() => _localError = 'Selecciona una sucursal.');
      return;
    }
    if (user == null) {
      setState(() => _localError = 'Selecciona un usuario.');
      return;
    }
    if (pin.length != 4) {
      setState(() => _localError = 'Ingresa un PIN de 4 dígitos.');
      return;
    }

    await context.read<StationProvider>().login(branch.id, user.id, pin);
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<StationProvider>();
    final error = provider.errorMessage ?? _localError;

    return Scaffold(
      backgroundColor: const Color(0xFF1A1A2E),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(32),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 460),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.restaurant_menu, size: 72, color: Color(0xFFE94560)),
                const SizedBox(height: 16),
                const Text(
                  'Grimorio KDS',
                  style: TextStyle(
                    fontSize: 32,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
                const Text(
                  'Estaciones de cocina y barra',
                  style: TextStyle(color: Colors.white54, fontSize: 14),
                ),
                const SizedBox(height: 8),
                Text(
                  provider.serverUrl,
                  style: const TextStyle(color: Colors.white24, fontSize: 11),
                ),
                const SizedBox(height: 36),
                _SelectBox<KdsBranch>(
                  label: 'Sucursal',
                  icon: Icons.storefront_outlined,
                  value: _selectedBranch,
                  loading: _loadingBranches,
                  items: _branches,
                  itemLabel: (b) => b.code.isEmpty ? b.name : '${b.name} (${b.code})',
                  onChanged: (branch) {
                    if (branch == null) return;
                    setState(() => _selectedBranch = branch);
                    _loadUsers(branch);
                  },
                ),
                const SizedBox(height: 16),
                _SelectBox<KdsUser>(
                  label: 'Usuario',
                  icon: Icons.person_outline,
                  value: _selectedUser,
                  loading: _loadingUsers,
                  items: _users,
                  itemLabel: (u) => u.displayName.isEmpty ? 'Usuario KDS' : u.displayName,
                  onChanged: (user) => setState(() {
                    _selectedUser = user;
                    _pinCtrl.clear();
                  }),
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: _pinCtrl,
                  keyboardType: TextInputType.number,
                  obscureText: true,
                  maxLength: 4,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 8,
                  ),
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  onSubmitted: (_) => provider.isLoading ? null : _submit(),
                  decoration: InputDecoration(
                    counterText: '',
                    labelText: 'PIN',
                    labelStyle: const TextStyle(color: Colors.white60),
                    prefixIcon: const Icon(Icons.pin_outlined, color: Colors.white54),
                    filled: true,
                    fillColor: const Color(0xFF16213E),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: BorderSide.none,
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                      borderSide: const BorderSide(color: Color(0xFFE94560)),
                    ),
                  ),
                ),
                if (error != null) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.red.shade900.withValues(alpha: 0.4),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.error_outline, color: Colors.redAccent, size: 18),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            error,
                            style: const TextStyle(color: Colors.redAccent),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 28),
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: FilledButton(
                    onPressed: provider.isLoading ? null : _submit,
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFFE94560),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                    child: provider.isLoading
                        ? const SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Text(
                            'Entrar',
                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _SelectBox<T> extends StatelessWidget {
  final String label;
  final IconData icon;
  final T? value;
  final bool loading;
  final List<T> items;
  final String Function(T item) itemLabel;
  final ValueChanged<T?> onChanged;

  const _SelectBox({
    required this.label,
    required this.icon,
    required this.value,
    required this.loading,
    required this.items,
    required this.itemLabel,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<T>(
      key: ValueKey(value),
      initialValue: value,
      isExpanded: true,
      dropdownColor: const Color(0xFF16213E),
      iconEnabledColor: Colors.white70,
      style: const TextStyle(color: Colors.white, fontSize: 16),
      decoration: InputDecoration(
        labelText: loading ? 'Cargando $label...' : label,
        labelStyle: const TextStyle(color: Colors.white60),
        prefixIcon: Icon(icon, color: Colors.white54),
        filled: true,
        fillColor: const Color(0xFF16213E),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: const BorderSide(color: Color(0xFFE94560)),
        ),
      ),
      items: items
          .map(
            (item) => DropdownMenuItem<T>(
              value: item,
              child: Text(itemLabel(item), overflow: TextOverflow.ellipsis),
            ),
          )
          .toList(),
      onChanged: loading ? null : onChanged,
    );
  }
}
