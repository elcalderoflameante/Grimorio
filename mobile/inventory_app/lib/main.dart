import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'api.dart';
import 'core/widgets/app_update_gate.dart';

void main() => runApp(const InventoryApp());

class InventoryApp extends StatefulWidget {
  const InventoryApp({super.key});
  @override
  State<InventoryApp> createState() => _InventoryAppState();
}

class _InventoryAppState extends State<InventoryApp> {
  final api = InventoryApi();
  Json? session;
  final navigatorKey = GlobalKey<NavigatorState>();
  @override
  void initState() {
    super.initState();
    api.onExpired = logout;
  }

  void logout() {
    api.token = null;
    navigatorKey.currentState?.popUntil((route) => route.isFirst);
    if (mounted) setState(() => session = null);
  }

  @override
  void dispose() {
    api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => MaterialApp(
    navigatorKey: navigatorKey,
    title: 'Grimorio Inventario',
    debugShowCheckedModeBanner: false,
    builder: (context, child) => AppUpdateGate(child: child!),
    theme: ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF8B4513)),
      useMaterial3: true,
      inputDecorationTheme: const InputDecorationTheme(
        border: OutlineInputBorder(),
      ),
    ),
    home: session == null
        ? LoginPage(
            api: api,
            onLogin: (value) {
              api.token = value['accessToken'] as String;
              setState(() => session = value);
            },
          )
        : AreasPage(
            api: api,
            name: '${session!['firstName']} ${session!['lastName']}',
            onLogout: logout,
          ),
  );
}

class LoginPage extends StatefulWidget {
  const LoginPage({super.key, required this.api, required this.onLogin});
  final InventoryApi api;
  final void Function(Json) onLogin;
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  List<Json> branches = [], users = [];
  String? branch, user, error;
  bool busy = true;
  final pin = TextEditingController();
  @override
  void initState() {
    super.initState();
    loadBranches();
  }

  @override
  void dispose() {
    pin.dispose();
    super.dispose();
  }

  Future<void> loadBranches() async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final result = rows(
        await widget.api.request('GET', '/auth/inventory/branches'),
      );
      if (!mounted) return;
      setState(() => branches = result);
      if (result.length == 1) await loadUsers(result.first['id'] as String);
    } catch (e) {
      if (mounted) setState(() => error = errorText(e));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> loadUsers(String id) async {
    setState(() {
      branch = id;
      user = null;
      users = [];
      pin.clear();
      busy = true;
      error = null;
    });
    try {
      final result = rows(
        await widget.api.request('GET', '/auth/inventory/users?branchId=$id'),
      );
      if (mounted) setState(() => users = result);
    } catch (e) {
      if (mounted) setState(() => error = errorText(e));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> login() async {
    if (branch == null || user == null || pin.text.length != 4) return;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final result = await widget.api.request('POST', '/auth/inventory/login', {
        'branchId': branch,
        'userId': user,
        'pin': pin.text,
      });
      if (mounted) widget.onLogin(result as Json);
    } catch (e) {
      if (mounted) setState(() => error = errorText(e));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Icon(Icons.inventory_2_outlined, size: 64),
              const SizedBox(height: 16),
              Text(
                'Grimorio Inventario',
                style: Theme.of(context).textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              const Text(
                'Cuenta tu área antes del cierre.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 28),
              DropdownButtonFormField<String>(
                key: ValueKey(branch),
                initialValue: branch,
                decoration: const InputDecoration(labelText: 'Sucursal'),
                isExpanded: true,
                items: branches
                    .map(
                      (b) => DropdownMenuItem(
                        value: b['id'] as String,
                        child: Text(b['name'] as String),
                      ),
                    )
                    .toList(),
                onChanged: busy
                    ? null
                    : (id) {
                        if (id != null) loadUsers(id);
                      },
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                key: ValueKey('$branch:$user'),
                initialValue: user,
                decoration: const InputDecoration(labelText: 'Tu nombre'),
                isExpanded: true,
                items: users
                    .map(
                      (u) => DropdownMenuItem(
                        value: u['id'] as String,
                        enabled: u['hasKdsPin'] == true,
                        child: Text(
                          '${u['firstName']} ${u['lastName']}${u['hasKdsPin'] == true ? '' : ' · Sin PIN'}',
                        ),
                      ),
                    )
                    .toList(),
                onChanged: busy
                    ? null
                    : (id) => setState(() {
                        user = id;
                        pin.clear();
                      }),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: pin,
                enabled: !busy,
                obscureText: true,
                maxLength: 4,
                keyboardType: TextInputType.number,
                inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                decoration: const InputDecoration(
                  labelText: 'PIN de 4 dígitos',
                ),
                onChanged: (_) => setState(() {}),
                onSubmitted: (_) => login(),
              ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(
                    error!,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                ),
              if (branch != null && users.isEmpty && !busy)
                const Padding(
                  padding: EdgeInsets.only(bottom: 12),
                  child: Text(
                    'No hay usuarios autorizados. Solicita al administrador el permiso de conteos y la asignación de tu área.',
                  ),
                ),
              FilledButton(
                onPressed: !busy && user != null && pin.text.length == 4
                    ? login
                    : null,
                child: Text(busy ? 'Cargando…' : 'Ingresar'),
              ),
              TextButton(
                onPressed: busy ? null : loadBranches,
                child: const Text('Actualizar sucursales y usuarios'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class AreasPage extends StatefulWidget {
  const AreasPage({
    super.key,
    required this.api,
    required this.name,
    required this.onLogout,
  });
  final InventoryApi api;
  final String name;
  final VoidCallback onLogout;
  @override
  State<AreasPage> createState() => _AreasPageState();
}

class _AreasPageState extends State<AreasPage> {
  List<Json> areas = [];
  bool busy = true;
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final result = rows(
        await widget.api.request('GET', '/inventory/conteos/mis-pendientes'),
      );
      if (mounted) setState(() => areas = result);
    } catch (e) {
      if (mounted) setState(() => error = errorText(e));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> start(Json area) async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final count =
          await widget.api.request(
                'POST',
                '/inventory/conteos/areas/${area['areaId']}/iniciar',
              )
              as Json;
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => CountPage(api: widget.api, initial: count),
        ),
      );
      if (mounted) await load();
    } catch (e) {
      if (mounted) setState(() => error = errorText(e));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Mis áreas'),
      actions: [
        IconButton(
          onPressed: widget.onLogout,
          icon: const Icon(Icons.logout),
          tooltip: 'Cerrar sesión',
        ),
      ],
    ),
    body: RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(widget.name, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          const Text(
            'Tus conteos se guardan para revisión. No modifican el inventario.',
          ),
          const SizedBox(height: 16),
          if (busy) const LinearProgressIndicator(),
          if (error != null)
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    Text(error!),
                    TextButton(
                      onPressed: busy ? null : load,
                      child: const Text('Reintentar'),
                    ),
                  ],
                ),
              ),
            ),
          if (!busy && areas.isEmpty && error == null)
            const Text(
              'No tienes áreas asignadas. Solicita la asignación al administrador.',
            ),
          ...areas.map((area) {
            final pending = area['pendingArticles'] as int;
            final draft = area['draftId'] != null;
            final recount = area['recount'] == true;
            return Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      area['areaName'] as String,
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                    Text(
                      recount
                          ? 'Nuevo conteo solicitado por administración'
                          : '$pending artículos pendientes',
                    ),
                    if (draft)
                      Text(
                        'Conteo iniciado por ${area['counterName']}. Puedes continuarlo.',
                      ),
                    const SizedBox(height: 12),
                    FilledButton(
                      onPressed: busy || (!draft && !recount && pending == 0)
                          ? null
                          : () => start(area),
                      child: Text(
                        draft
                            ? 'Continuar conteo'
                            : recount
                            ? 'Realizar reconteo'
                            : pending == 0
                            ? 'Al día'
                            : 'Comenzar conteo',
                      ),
                    ),
                  ],
                ),
              ),
            );
          }),
          const SizedBox(height: 16),
          OutlinedButton.icon(
            onPressed: busy ? null : load,
            icon: const Icon(Icons.refresh),
            label: const Text('Actualizar pendientes'),
          ),
        ],
      ),
    ),
  );
}

class CountPage extends StatefulWidget {
  const CountPage({super.key, required this.api, required this.initial});
  final InventoryApi api;
  final Json initial;
  @override
  State<CountPage> createState() => _CountPageState();
}

class _CountPageState extends State<CountPage> {
  VoidCallback? _releaseUpdate;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _releaseUpdate ??= AppUpdateGate.defer(context);
  }

  @override
  void dispose() {
    _releaseUpdate?.call();
    super.dispose();
  }

  late Json count = widget.initial;
  bool busy = false;
  String search = '';
  List<Json> get lines => rows(count['lines']);
  Future<void> refresh() async {
    setState(() => busy = true);
    try {
      final result =
          await widget.api.request(
                'GET',
                '/inventory/conteos/mis-conteos/${count['id']}',
              )
              as Json;
      if (mounted) {
        if (result['status'] != 'Draft') {
          Navigator.pop(context);
          return;
        }
        setState(() => count = result);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(errorText(e))));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> edit(Json line) async {
    final result = await showDialog<Json>(
      context: context,
      barrierDismissible: false,
      builder: (_) => LineDialog(
        api: widget.api,
        countId: count['id'] as String,
        line: line,
      ),
    );
    if (result != null && mounted) setState(() => count = result);
  }

  Future<void> submit() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('¿Enviar conteo?'),
        content: const Text(
          'Se enviará para revisión en el ERP y ya no podrás editarlo. Las existencias no se modificarán.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Volver'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Enviar'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    setState(() => busy = true);
    try {
      await widget.api.request(
        'POST',
        '/inventory/conteos/${count['id']}/enviar',
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Conteo enviado para revisión.')),
        );
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(errorText(e))));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final done = lines.where((l) => l['quantity'] != null).length;
    final visible = lines
        .where(
          (l) => '${l['articleName']} ${l['warehouseName']}'
              .toLowerCase()
              .contains(search.toLowerCase()),
        )
        .toList();
    return Scaffold(
      appBar: AppBar(
        title: Text(count['areaName'] as String),
        actions: [
          IconButton(
            onPressed: busy ? null : refresh,
            icon: const Icon(Icons.refresh),
            tooltip: 'Actualizar conteo',
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  '${count['businessDate']} · $done de ${lines.length} contados',
                ),
                const SizedBox(height: 8),
                LinearProgressIndicator(
                  value: lines.isEmpty ? 0 : done / lines.length,
                ),
                const SizedBox(height: 12),
                const Text(
                  'Cada artículo se guarda al confirmar su cantidad. Ingresa 0 cuando no haya existencias.',
                ),
                const SizedBox(height: 12),
                TextField(
                  decoration: const InputDecoration(
                    hintText: 'Buscar artículo o bodega',
                    prefixIcon: Icon(Icons.search),
                  ),
                  onChanged: (value) => setState(() => search = value),
                ),
              ],
            ),
          ),
          Expanded(
            child: ListView.builder(
              itemCount: visible.length,
              itemBuilder: (_, i) {
                final l = visible[i];
                return ListTile(
                  leading: Icon(
                    l['quantity'] == null
                        ? Icons.radio_button_unchecked
                        : Icons.check_circle,
                    color: l['quantity'] == null ? null : Colors.green,
                  ),
                  title: Text(l['articleName'] as String),
                  subtitle: Text(
                    '${l['warehouseName']} · ${l['unitSymbol']}${l['notes'] == null || l['notes'] == '' ? '' : '\n${l['notes']}'}',
                  ),
                  trailing: Text(
                    l['quantity'] == null
                        ? 'Contar'
                        : '${l['quantity']} ${l['unitSymbol']}',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  onTap: busy ? null : () => edit(l),
                );
              },
            ),
          ),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: !busy && done == lines.length && done > 0
                      ? submit
                      : null,
                  icon: const Icon(Icons.send),
                  label: Text(
                    busy
                        ? 'Enviando…'
                        : 'Enviar conteo ($done/${lines.length})',
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class LineDialog extends StatefulWidget {
  const LineDialog({
    super.key,
    required this.api,
    required this.countId,
    required this.line,
  });
  final InventoryApi api;
  final String countId;
  final Json line;
  @override
  State<LineDialog> createState() => _LineDialogState();
}

class _LineDialogState extends State<LineDialog> {
  late final quantity = TextEditingController(
    text: widget.line['quantity']?.toString() ?? '',
  );
  late final notes = TextEditingController(
    text: widget.line['notes'] as String? ?? '',
  );
  bool busy = false;
  String? error;
  @override
  void dispose() {
    quantity.dispose();
    notes.dispose();
    super.dispose();
  }

  Future<void> save() async {
    final text = quantity.text.trim().replaceAll(',', '.');
    final value = num.tryParse(text);
    if (value == null ||
        !value.isFinite ||
        value < 0 ||
        value > 99999999999999.9999 ||
        !RegExp(r'^\d+(\.\d{1,4})?$').hasMatch(text)) {
      setState(
        () => error =
            'Ingresa cero o una cantidad positiva con hasta 4 decimales.',
      );
      return;
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final result = await widget.api.request(
        'PUT',
        '/inventory/conteos/${widget.countId}/lineas/${widget.line['id']}',
        {
          'quantity': value,
          'notes': notes.text,
          'previousCountedAt': widget.line['countedAt'],
        },
      );
      if (mounted) Navigator.pop(context, result as Json);
    } catch (e) {
      if (mounted) {
        setState(
          () =>
              error = '${errorText(e)} Tu cantidad sigue aquí para reintentar.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !busy,
    child: AlertDialog(
      title: Text(widget.line['articleName'] as String),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              '${widget.line['warehouseName']} · Cuenta en ${widget.line['unitSymbol']}',
            ),
            const SizedBox(height: 16),
            TextField(
              controller: quantity,
              autofocus: true,
              enabled: !busy,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: InputDecoration(
                labelText: 'Cantidad (${widget.line['unitSymbol']})',
                helperText: '0 significa que no hay existencias',
              ),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: notes,
              enabled: !busy,
              maxLength: 1000,
              minLines: 2,
              maxLines: 4,
              decoration: const InputDecoration(
                labelText: 'Novedad u observación (opcional)',
              ),
            ),
            if (error != null)
              Text(
                error!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: busy ? null : () => Navigator.pop(context),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: busy ? null : save,
          child: Text(busy ? 'Guardando…' : 'Guardar cantidad'),
        ),
      ],
    ),
  );
}
