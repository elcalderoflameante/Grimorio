import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class TableRequestsPage extends StatefulWidget {
  const TableRequestsPage({super.key});

  @override
  State<TableRequestsPage> createState() => _TableRequestsPageState();
}

class _TableRequestsPageState extends State<TableRequestsPage> {
  // TODO: Replace with actual data from Riverpod state
  final List<Map<String, dynamic>> mockRequests = [
    {
      'id': 1,
      'tableNumber': '5',
      'status': 'pending',
      'statusLabel': 'Pendiente',
      'description': 'Pedir cuenta',
      'timestamp': DateTime.now().subtract(const Duration(minutes: 5)),
    },
    {
      'id': 2,
      'tableNumber': '8',
      'status': 'in_progress',
      'statusLabel': 'En proceso',
      'description': 'Más agua',
      'timestamp': DateTime.now().subtract(const Duration(minutes: 2)),
    },
    {
      'id': 3,
      'tableNumber': '3',
      'status': 'pending',
      'statusLabel': 'Pendiente',
      'description': 'Cambiar plato',
      'timestamp': DateTime.now().subtract(const Duration(seconds: 30)),
    },
  ];

  Color _getStatusColor(String status) {
    switch (status) {
      case 'pending':
        return Colors.orange;
      case 'in_progress':
        return Colors.blue;
      case 'completed':
        return Colors.green;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Solicitudes de Mesa'),
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () {
              context.go('/login');
            },
          ),
        ],
      ),
      body: mockRequests.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.check_circle,
                    size: 64,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Sin solicitudes pendientes',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ],
              ),
            )
          : ListView.builder(
              padding: const EdgeInsets.all(8),
              itemCount: mockRequests.length,
              itemBuilder: (context, index) {
                final request = mockRequests[index];
                return _buildRequestCard(context, request);
              },
            ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          // TODO: Implement refresh from SignalR
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Actualizando solicitudes...'),
              duration: Duration(seconds: 1),
            ),
          );
        },
        tooltip: 'Actualizar',
        child: const Icon(Icons.refresh),
      ),
    );
  }

  Widget _buildRequestCard(
    BuildContext context,
    Map<String, dynamic> request,
  ) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
      child: ListTile(
        contentPadding: const EdgeInsets.all(16),
        // Table number as title
        title: Text(
          'Mesa ${request['tableNumber']}',
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
        // Description and request details
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 8),
            Text(
              request['description'],
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 8),
            Text(
              _formatTime(request['timestamp']),
              style: Theme.of(context).textTheme.labelSmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
        // Status badge
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: _getStatusColor(request['status']).withAlpha(51),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(
            request['statusLabel'],
            style: TextStyle(
              color: _getStatusColor(request['status']),
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        // Action button
        onTap: () {
          _showActionBottomSheet(context, request);
        },
      ),
    );
  }

  void _showActionBottomSheet(
    BuildContext context,
    Map<String, dynamic> request,
  ) {
    showModalBottomSheet(
      context: context,
      builder: (context) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Mesa ${request['tableNumber']}',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            Text(request['description']),
            const SizedBox(height: 24),
            if (request['status'] == 'pending')
              ElevatedButton.icon(
                onPressed: () {
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Marcado como en proceso'),
                    ),
                  );
                },
                icon: const Icon(Icons.play_arrow),
                label: const Text('Empezar'),
              ),
            if (request['status'] == 'in_progress')
              ElevatedButton.icon(
                onPressed: () {
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Completado'),
                    ),
                  );
                },
                icon: const Icon(Icons.check),
                label: const Text('Completar'),
              ),
            const SizedBox(height: 8),
            OutlinedButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancelar'),
            ),
          ],
        ),
      ),
    );
  }

  String _formatTime(DateTime dateTime) {
    final now = DateTime.now();
    final difference = now.difference(dateTime);

    if (difference.inSeconds < 60) {
      return 'Hace unos segundos';
    } else if (difference.inMinutes < 60) {
      return 'Hace ${difference.inMinutes} min';
    } else if (difference.inHours < 24) {
      return 'Hace ${difference.inHours} horas';
    } else {
      return 'Hace ${difference.inDays} días';
    }
  }
}
