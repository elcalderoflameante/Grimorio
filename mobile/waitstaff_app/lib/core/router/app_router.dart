import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/table_requests/presentation/pages/table_requests_page.dart';

// Null check to ensure we have a navigatorKey for GoRouter
final navigatorKey = GlobalKey<NavigatorState>();

final appRouter = GoRouter(
  navigatorKey: navigatorKey,
  initialLocation: '/login',
  routes: [
    GoRoute(
      path: '/login',
      name: 'login',
      builder: (context, state) => const LoginPage(),
    ),
    GoRoute(
      path: '/home',
      name: 'home',
      builder: (context, state) => const TableRequestsPage(),
    ),
  ],
  // redirect: (context, state) async {
  //   // TODO: Implement auth check redirect logic
  //   return null;
  // },
  errorBuilder: (context, state) => Scaffold(
    appBar: AppBar(title: const Text('Error')),
    body: Center(
      child: Text('Route not found: ${state.uri}'),
    ),
  ),
);
