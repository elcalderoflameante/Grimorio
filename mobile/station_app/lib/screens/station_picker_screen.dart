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

  @override
  void initState() {
    super.initState();
    _future = context.read<StationProvider>().loadStations();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1A1A2E),
      appBar: AppBar(
        backgroundColor: const Color(0xFF16213E),
        title: const Text('Seleccionar estación', style: TextStyle(color: Colors.white)),
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
            return const Center(child: CircularProgressIndicator(color: Color(0xFFE94560)));
          }

          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline, color: Colors.redAccent, size: 48),
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
                'No hay estaciones activas.\nCrea una en el panel de administración.',
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
                  '¿Qué estación es esta tablet?',
                  style: TextStyle(
                      color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Esta selección se guardará en el dispositivo.',
                  style: TextStyle(color: Colors.white54),
                ),
                const SizedBox(height: 24),
                Expanded(
                  child: GridView.builder(
                    gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                      maxCrossAxisExtent: 280,
                      mainAxisExtent: 120,
                      crossAxisSpacing: 16,
                      mainAxisSpacing: 16,
                    ),
                    itemCount: stations.length,
                    itemBuilder: (context, i) {
                      final station = stations[i];
                      return _StationCard(station: station);
                    },
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
  const _StationCard({required this.station});

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
      default:
        return Icons.restaurant_outlined;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Material(
      color: const Color(0xFF16213E),
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () => context.read<StationProvider>().selectStation(station),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(_icon(station.type), color: const Color(0xFFE94560), size: 36),
              const SizedBox(height: 10),
              Text(
                station.name,
                style: const TextStyle(
                    color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                station.type,
                style: const TextStyle(color: Colors.white54, fontSize: 12),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
