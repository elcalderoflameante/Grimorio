import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/work_station.dart';
import '../providers/station_provider.dart';

class StationPickerScreen extends StatefulWidget {
  const StationPickerScreen({super.key});

  @override
  State<StationPickerScreen> createState() => _StationPickerScreenState();
}

class _StationPickerScreenState extends State<StationPickerScreen> {
  late Future<List<WorkStation>> _future;
  final Set<String> _selectedIds = {};

  @override
  void initState() {
    super.initState();
    _future = context.read<StationProvider>().loadStations();
  }

  void _toggleStation(String id) {
    setState(() {
      if (_selectedIds.contains(id)) {
        _selectedIds.remove(id);
      } else {
        _selectedIds.add(id);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1A1A2E),
      appBar: AppBar(
        backgroundColor: const Color(0xFF16213E),
        toolbarHeight: 34,
        titleSpacing: 8,
        title: const Text(
          'Seleccionar estaciones',
          style: TextStyle(color: Colors.white, fontSize: 11),
        ),
        actions: [
          TextButton.icon(
            onPressed: () => context.read<StationProvider>().logout(),
            icon: const Icon(Icons.logout, color: Colors.white54, size: 14),
            label: const Text(
              'Salir',
              style: TextStyle(color: Colors.white54, fontSize: 9),
            ),
          ),
        ],
      ),
      body: FutureBuilder<List<WorkStation>>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(
              child: CircularProgressIndicator(color: Color(0xFFE94560)),
            );
          }

          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(
                    Icons.error_outline,
                    color: Colors.redAccent,
                    size: 24,
                  ),
                  const SizedBox(height: 6),
                  Text(
                    snapshot.error.toString().replaceFirst('Exception: ', ''),
                    style: const TextStyle(color: Colors.white70, fontSize: 9),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 10),
                  FilledButton(
                    onPressed: () => setState(() {
                      _future = context.read<StationProvider>().loadStations();
                    }),
                    child: const Text('Reintentar'),
                  ),
                ],
              ),
            );
          }

          final stations = snapshot.data ?? [];

          if (stations.isEmpty) {
            return const Center(
              child: Text(
                'No hay estaciones activas.\nCrea una en el panel de administracion.',
                style: TextStyle(color: Colors.white54, fontSize: 9),
                textAlign: TextAlign.center,
              ),
            );
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(12),
            child: SizedBox(
              width: double.infinity,
              child: Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                const Text(
                  'Que estaciones vera esta tablet?',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 4),
                const Text(
                  'Selecciona una o varias. Emplatado puede ver Fritos + Parrilla para sacar pedidos completos.',
                  style: TextStyle(color: Colors.white54, fontSize: 9),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                Wrap(
                  alignment: WrapAlignment.center,
                  spacing: 8,
                  runSpacing: 8,
                  children: stations.map((station) {
                    return SizedBox(
                      width: 150,
                      height: 84,
                      child: _StationCard(
                        station: station,
                        selected: _selectedIds.contains(station.id),
                        onToggle: () => _toggleStation(station.id),
                      ),
                    );
                  }).toList(),
                ),
                const SizedBox(height: 12),
                ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 420),
                  child: SizedBox(
                    width: double.infinity,
                    height: 34,
                    child: FilledButton.icon(
                      onPressed: _selectedIds.isEmpty
                          ? null
                          : () {
                              final selected = stations
                                  .where((s) => _selectedIds.contains(s.id))
                                  .toList();
                              context.read<StationProvider>().selectStations(
                                selected,
                              );
                            },
                      icon: const Icon(Icons.check_rounded),
                      label: Text(
                        _selectedIds.isEmpty
                            ? 'Selecciona al menos una estacion'
                            : 'Continuar con ${_selectedIds.length} estacion${_selectedIds.length == 1 ? '' : 'es'}',
                      ),
                    ),
                  ),
                ),
              ],
            ),
            ),
          );
        },
      ),
    );
  }
}

class _StationCard extends StatelessWidget {
  final WorkStation station;
  final bool selected;
  final VoidCallback onToggle;

  const _StationCard({
    required this.station,
    required this.selected,
    required this.onToggle,
  });

  IconData _icon(String type) {
    switch (type.toLowerCase()) {
      case 'cocina':
      case 'kitchen':
        return Icons.soup_kitchen_outlined;
      case 'barra':
      case 'bar':
        return Icons.local_bar_outlined;
      case 'postres':
      case 'dessert':
        return Icons.cake_outlined;
      case 'fries':
      case 'fritos':
        return Icons.local_fire_department_outlined;
      default:
        return Icons.restaurant_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Material(
      color: selected ? const Color(0xFF233A5E) : const Color(0xFF16213E),
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: onToggle,
        child: Padding(
          padding: const EdgeInsets.all(8),
          child: Column(
            children: [
              Align(
                alignment: Alignment.topRight,
                child: Icon(
                  selected ? Icons.check_circle_rounded : Icons.circle_outlined,
                  color: selected ? const Color(0xFF4EE87A) : Colors.white30,
                  size: 12,
                ),
              ),
              const Spacer(),
              Icon(
                _icon(station.type),
                color: const Color(0xFFE94560),
                size: 18,
              ),
              const SizedBox(height: 4),
              Text(
                station.name,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 10,
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                station.type,
                style: const TextStyle(color: Colors.white54, fontSize: 8),
              ),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}
