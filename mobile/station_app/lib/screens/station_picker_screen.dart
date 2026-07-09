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
        title: const Text('Seleccionar estaciones',
            style: TextStyle(color: Colors.white)),
        actions: [
          TextButton.icon(
            onPressed: () => context.read<StationProvider>().logout(),
            icon: const Icon(Icons.logout, color: Colors.white54),
            label: const Text('Salir', style: TextStyle(color: Colors.white54)),
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
                  const Icon(Icons.error_outline,
                      color: Colors.redAccent, size: 48),
                  const SizedBox(height: 12),
                  Text(
                    snapshot.error.toString().replaceFirst('Exception: ', ''),
                    style: const TextStyle(color: Colors.white70),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 20),
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
                style: TextStyle(color: Colors.white54),
                textAlign: TextAlign.center,
              ),
            );
          }

          return Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Que estaciones vera esta tablet?',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Selecciona una o varias. Emplatado puede ver Fritos + Parrilla para sacar pedidos completos.',
                  style: TextStyle(color: Colors.white54),
                ),
                const SizedBox(height: 24),
                Expanded(
                  child: GridView.builder(
                    gridDelegate:
                        const SliverGridDelegateWithMaxCrossAxisExtent(
                      maxCrossAxisExtent: 280,
                      mainAxisExtent: 132,
                      crossAxisSpacing: 16,
                      mainAxisSpacing: 16,
                    ),
                    itemCount: stations.length,
                    itemBuilder: (context, i) {
                      final station = stations[i];
                      return _StationCard(
                        station: station,
                        selected: _selectedIds.contains(station.id),
                        onToggle: () => _toggleStation(station.id),
                      );
                    },
                  ),
                ),
                const SizedBox(height: 16),
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: FilledButton.icon(
                    onPressed: _selectedIds.isEmpty
                        ? null
                        : () {
                            final selected = stations
                                .where((s) => _selectedIds.contains(s.id))
                                .toList();
                            context
                                .read<StationProvider>()
                                .selectStations(selected);
                          },
                    icon: const Icon(Icons.check_rounded),
                    label: Text(
                      _selectedIds.isEmpty
                          ? 'Selecciona al menos una estacion'
                          : 'Continuar con ${_selectedIds.length} estacion${_selectedIds.length == 1 ? '' : 'es'}',
                    ),
                  ),
                ),
              ],
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
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onToggle,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              Align(
                alignment: Alignment.topRight,
                child: Icon(
                  selected ? Icons.check_circle_rounded : Icons.circle_outlined,
                  color: selected ? const Color(0xFF4EE87A) : Colors.white30,
                  size: 22,
                ),
              ),
              const Spacer(),
              Icon(_icon(station.type),
                  color: const Color(0xFFE94560), size: 36),
              const SizedBox(height: 10),
              Text(
                station.name,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                station.type,
                style: const TextStyle(color: Colors.white54, fontSize: 12),
              ),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}
