import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../../../core/theme/app_theme.dart';
import '../../data/models/auth_models.dart';
import '../providers/auth_controller.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _pinController = TextEditingController();
  List<PinBranch> _branches = const [];
  List<PinUser> _users = const [];
  PinBranch? _branch;
  PinUser? _user;
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
    _pinController.dispose();
    super.dispose();
  }

  Future<void> _loadBranches() async {
    setState(() {
      _loadingBranches = true;
      _localError = null;
    });
    try {
      final branches = await ref
          .read(authControllerProvider.notifier)
          .getWaitstaffBranches();
      if (!mounted) return;
      setState(() {
        _branches = branches;
        _branch = branches.length == 1 ? branches.first : null;
        _loadingBranches = false;
      });
      if (branches.length == 1) {
        await _loadUsers(branches.first);
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _loadingBranches = false;
          _localError = 'No se pudieron cargar las sucursales.';
        });
      }
    }
  }

  Future<void> _loadUsers(PinBranch branch) async {
    setState(() {
      _loadingUsers = true;
      _users = const [];
      _user = null;
      _pinController.clear();
      _localError = null;
    });
    try {
      final users = await ref
          .read(authControllerProvider.notifier)
          .getWaitstaffUsers(branch.id);
      if (!mounted) return;
      setState(() {
        _users = users;
        _user = users.length == 1 ? users.first : null;
        _loadingUsers = false;
      });
    } catch (_) {
      if (mounted) {
        setState(() {
          _loadingUsers = false;
          _localError = 'No se pudieron cargar los meseros.';
        });
      }
    }
  }

  Future<void> _submit() async {
    if (_branch == null) {
      setState(() => _localError = 'Selecciona una sucursal.');
      return;
    }
    if (_user == null) {
      setState(() => _localError = 'Selecciona tu usuario.');
      return;
    }
    if (!_user!.hasPin) {
      setState(
        () => _localError = 'Este usuario todavía no tiene un PIN configurado.',
      );
      return;
    }
    if (_pinController.text.length != 4) {
      setState(() => _localError = 'Ingresa un PIN de 4 dígitos.');
      return;
    }

    final success = await ref
        .read(authControllerProvider.notifier)
        .loginWithPin(
          branchId: _branch!.id,
          userId: _user!.id,
          pin: _pinController.text,
        );
    if (!mounted) return;
    if (success) {
      context.go('/home');
    } else {
      setState(
        () => _localError =
            ref.read(authControllerProvider).errorMessage ?? 'PIN incorrecto.',
      );
      _pinController.clear();
    }
  }

  @override
  Widget build(BuildContext context) {
    final loading = ref.watch(authControllerProvider).isLoading;
    return Scaffold(
      backgroundColor: kBgDeep,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(28),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                children: [
                  Image.asset(
                    'assets/images/ecf_logo.png',
                    width: 120,
                    height: 120,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'GRIMORIO MESEROS',
                    style: GoogleFonts.cinzel(
                      color: kGold,
                      fontSize: 23,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Acceso rápido con PIN',
                    style: GoogleFonts.lato(color: kParchmentDim, fontSize: 14),
                  ),
                  const SizedBox(height: 30),
                  _selector<PinBranch>(
                    label: _loadingBranches
                        ? 'Cargando sucursales...'
                        : 'Sucursal',
                    icon: Icons.storefront_outlined,
                    value: _branch,
                    items: _branches,
                    text: (item) => item.displayName,
                    enabled: !_loadingBranches,
                    onChanged: (value) {
                      if (value == null) return;
                      setState(() => _branch = value);
                      _loadUsers(value);
                    },
                  ),
                  const SizedBox(height: 14),
                  _selector<PinUser>(
                    label: _loadingUsers ? 'Cargando meseros...' : 'Mesero/a',
                    icon: Icons.person_outline_rounded,
                    value: _user,
                    items: _users,
                    text: (item) => item.displayName,
                    enabled: !_loadingUsers && _branch != null,
                    onChanged: (value) => setState(() {
                      _user = value;
                      _pinController.clear();
                      _localError = null;
                    }),
                  ),
                  const SizedBox(height: 14),
                  TextField(
                    controller: _pinController,
                    enabled: _user != null,
                    keyboardType: TextInputType.number,
                    obscureText: true,
                    maxLength: 4,
                    textAlign: TextAlign.center,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    onSubmitted: (_) => loading ? null : _submit(),
                    style: GoogleFonts.cinzel(
                      color: kParchment,
                      fontSize: 26,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 10,
                    ),
                    decoration: const InputDecoration(
                      counterText: '',
                      labelText: 'PIN',
                      prefixIcon: Icon(Icons.pin_outlined),
                    ),
                  ),
                  if (_localError != null) ...[
                    const SizedBox(height: 12),
                    Text(
                      _localError!,
                      textAlign: TextAlign.center,
                      style: GoogleFonts.lato(
                        color: const Color(0xFFFF6B6B),
                        fontSize: 13,
                      ),
                    ),
                  ],
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    height: 50,
                    child: FilledButton.icon(
                      onPressed: loading ? null : _submit,
                      icon: loading
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.login_rounded),
                      label: Text(loading ? 'Ingresando...' : 'Ingresar'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _selector<T>({
    required String label,
    required IconData icon,
    required T? value,
    required List<T> items,
    required String Function(T) text,
    required bool enabled,
    required ValueChanged<T?> onChanged,
  }) {
    return DropdownButtonFormField<T>(
      key: ValueKey(value),
      initialValue: value,
      isExpanded: true,
      dropdownColor: kBgMid,
      decoration: InputDecoration(labelText: label, prefixIcon: Icon(icon)),
      items: items
          .map(
            (item) => DropdownMenuItem(
              value: item,
              child: Text(text(item), overflow: TextOverflow.ellipsis),
            ),
          )
          .toList(),
      onChanged: enabled ? onChanged : null,
    );
  }
}
