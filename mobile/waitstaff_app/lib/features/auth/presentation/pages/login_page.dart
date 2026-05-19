import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../providers/auth_controller.dart';

const _kBgDeep = Color(0xFF080612);
const _kGold = Color(0xFFD4A017);
const _kGoldLight = Color(0xFFFFD060);
const _kGoldDark = Color(0xFF8B6400);
const _kParchment = Color(0xFFF5E6C8);
const _kBrown = Color(0xFF3E2008);
const _kCardBg = Color(0xFF0F0820);

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage>
    with SingleTickerProviderStateMixin {
  late final TextEditingController _emailController;
  late final TextEditingController _passwordController;
  late final AnimationController _glowController;
  bool _obscurePassword = true;

  @override
  void initState() {
    super.initState();
    _emailController =
        TextEditingController(text: 'admin@elcalderoflameante.com');
    _passwordController = TextEditingController();
    _glowController = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 3),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _glowController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;

    if (email.isEmpty || password.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Ingresa correo y contraseña.',
            style: GoogleFonts.cinzel(color: _kParchment, fontSize: 13),
          ),
          backgroundColor: _kBrown,
          behavior: SnackBarBehavior.floating,
        ),
      );
      return;
    }

    final success = await ref
        .read(authControllerProvider.notifier)
        .login(email: email, password: password);

    if (!mounted) return;

    if (success) {
      context.go('/home');
      return;
    }

    final message =
        ref.read(authControllerProvider).errorMessage ?? 'No se pudo iniciar sesión.';
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          style: GoogleFonts.cinzel(color: _kParchment, fontSize: 13),
        ),
        backgroundColor: const Color(0xFF6B1A1A),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authControllerProvider);

    return Scaffold(
      backgroundColor: _kBgDeep,
      body: Stack(
        children: [
          // Fondo estrellado
          const Positioned.fill(child: _StarryBackground()),

          // Halo mágico pulsante detrás del logo
          Positioned.fill(
            child: AnimatedBuilder(
              animation: _glowController,
              builder: (context, child) => Container(
                decoration: BoxDecoration(
                  gradient: RadialGradient(
                    center: const Alignment(0, -0.55),
                    radius: 0.6,
                    colors: [
                      _kGoldDark.withAlpha(
                          ((.08 + _glowController.value * .08) * 255).round()),
                      Colors.transparent,
                    ],
                  ),
                ),
              ),
            ),
          ),

          // Contenido
          SafeArea(
            child: SingleChildScrollView(
              padding:
                  const EdgeInsets.symmetric(horizontal: 28, vertical: 12),
              child: Column(
                children: [
                  const SizedBox(height: 16),

                  // Logo con halo dorado pulsante
                  AnimatedBuilder(
                    animation: _glowController,
                    builder: (_, child) => Container(
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: _kGold.withAlpha(((.12 +
                                        _glowController.value * .18) *
                                    255)
                                .round()),
                            blurRadius: 70,
                            spreadRadius: 10,
                          ),
                        ],
                      ),
                      child: child,
                    ),
                    child: Image.asset(
                      'assets/images/ecf_logo.png',
                      height: 150,
                      fit: BoxFit.contain,
                    ),
                  ),

                  const SizedBox(height: 18),

                  // Título GRIMORIO con degradado dorado
                  ShaderMask(
                    shaderCallback: (bounds) => const LinearGradient(
                      colors: [_kGoldDark, _kGoldLight, _kGold, _kGoldDark],
                      stops: [0.0, 0.35, 0.65, 1.0],
                    ).createShader(bounds),
                    child: Text(
                      'GRIMORIO',
                      style: GoogleFonts.cinzel(
                        fontSize: 34,
                        fontWeight: FontWeight.w900,
                        color: Colors.white,
                        letterSpacing: 10,
                      ),
                    ),
                  ),

                  const SizedBox(height: 4),

                  Text(
                    'El Caldero Flameante',
                    style: GoogleFonts.cinzel(
                      fontSize: 12,
                      color: _kParchment.withAlpha(160),
                      letterSpacing: 3,
                    ),
                  ),

                  // Separador mágico
                  Padding(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 20, vertical: 20),
                    child: Row(
                      children: [
                        Expanded(
                          child: Container(
                            height: 1,
                            decoration: BoxDecoration(
                              gradient: LinearGradient(
                                colors: [
                                  Colors.transparent,
                                  _kGold.withAlpha(130),
                                ],
                              ),
                            ),
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          child: Icon(
                            Icons.auto_awesome,
                            color: _kGold.withAlpha(180),
                            size: 14,
                          ),
                        ),
                        Expanded(
                          child: Container(
                            height: 1,
                            decoration: BoxDecoration(
                              gradient: LinearGradient(
                                colors: [
                                  _kGold.withAlpha(130),
                                  Colors.transparent,
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),

                  // Tarjeta de acceso estilo grimorio
                  Container(
                    decoration: BoxDecoration(
                      color: _kCardBg,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: _kGold.withAlpha(80),
                        width: 1.5,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: _kGold.withAlpha(25),
                          blurRadius: 24,
                          spreadRadius: 2,
                        ),
                      ],
                    ),
                    padding: const EdgeInsets.fromLTRB(22, 22, 22, 26),
                    child: Column(
                      children: [
                        Text(
                          '✦  Acceso para Magos  ✦',
                          style: GoogleFonts.cinzel(
                            fontSize: 12,
                            color: _kGold.withAlpha(180),
                            letterSpacing: 2,
                          ),
                        ),
                        const SizedBox(height: 22),

                        // Campo email
                        _MagicTextField(
                          controller: _emailController,
                          label: 'Correo del Mago',
                          icon: Icons.alternate_email_rounded,
                          keyboardType: TextInputType.emailAddress,
                        ),
                        const SizedBox(height: 14),

                        // Campo contraseña
                        _MagicTextField(
                          controller: _passwordController,
                          label: 'Contraseña Secreta',
                          icon: Icons.lock_outline_rounded,
                          obscureText: _obscurePassword,
                          onToggleObscure: () =>
                              setState(() => _obscurePassword = !_obscurePassword),
                          onSubmitted: (_) => _handleLogin(),
                        ),
                        const SizedBox(height: 26),

                        // Botón Entrar
                        SizedBox(
                          width: double.infinity,
                          height: 52,
                          child: _GoldButton(
                            onPressed: authState.isLoading ? null : _handleLogin,
                            isLoading: authState.isLoading,
                            label: 'Entrar al Grimorio',
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 28),

                  Text(
                    '✦  Sistema de Gestión  ✦',
                    style: GoogleFonts.cinzel(
                      fontSize: 10,
                      color: _kParchment.withAlpha(60),
                      letterSpacing: 2,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 16),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// Campo de texto con estética mágica
class _MagicTextField extends StatelessWidget {
  const _MagicTextField({
    required this.controller,
    required this.label,
    required this.icon,
    this.keyboardType,
    this.obscureText = false,
    this.onToggleObscure,
    this.onSubmitted,
  });

  final TextEditingController controller;
  final String label;
  final IconData icon;
  final TextInputType? keyboardType;
  final bool obscureText;
  final VoidCallback? onToggleObscure;
  final ValueChanged<String>? onSubmitted;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      obscureText: obscureText,
      onSubmitted: onSubmitted,
      style: GoogleFonts.lato(
        color: _kParchment,
        fontSize: 14,
      ),
      cursorColor: _kGold,
      decoration: InputDecoration(
        labelText: label,
        labelStyle: GoogleFonts.cinzel(
          color: _kGold.withAlpha(170),
          fontSize: 12,
        ),
        prefixIcon:
            Icon(icon, color: _kGold.withAlpha(160), size: 20),
        suffixIcon: onToggleObscure != null
            ? IconButton(
                icon: Icon(
                  obscureText
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined,
                  color: _kGold.withAlpha(120),
                  size: 20,
                ),
                onPressed: onToggleObscure,
              )
            : null,
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: _kGold.withAlpha(60), width: 1),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(10),
          borderSide: BorderSide(color: _kGold.withAlpha(180), width: 1.5),
        ),
        filled: true,
        fillColor: const Color(0xFF080612),
      ),
    );
  }
}

// Botón dorado con degradado
class _GoldButton extends StatelessWidget {
  const _GoldButton({
    required this.label,
    required this.isLoading,
    this.onPressed,
  });

  final String label;
  final bool isLoading;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        gradient: isLoading
            ? null
            : const LinearGradient(
                colors: [_kGoldDark, _kGold, _kGoldLight, _kGold, _kGoldDark],
                stops: [0.0, 0.25, 0.5, 0.75, 1.0],
              ),
        color: isLoading ? const Color(0xFF2A2040) : null,
        borderRadius: BorderRadius.circular(12),
        boxShadow: isLoading
            ? []
            : [
                BoxShadow(
                  color: _kGold.withAlpha(80),
                  blurRadius: 14,
                  offset: const Offset(0, 4),
                ),
              ],
      ),
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.transparent,
          shadowColor: Colors.transparent,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        child: isLoading
            ? const SizedBox(
                height: 22,
                width: 22,
                child: CircularProgressIndicator(
                  strokeWidth: 2.5,
                  color: _kGold,
                ),
              )
            : Text(
                label,
                style: GoogleFonts.cinzel(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: _kBrown,
                  letterSpacing: 1.5,
                ),
              ),
      ),
    );
  }
}

// Fondo estrellado pintado con Canvas
class _StarryBackground extends StatelessWidget {
  const _StarryBackground();

  @override
  Widget build(BuildContext context) {
    return CustomPaint(painter: _StarsPainter());
  }
}

class _StarsPainter extends CustomPainter {
  static final List<_Star> _stars = _buildStars();

  static List<_Star> _buildStars() {
    final rng = math.Random(7);
    return List.generate(
      140,
      (_) => _Star(
        x: rng.nextDouble(),
        y: rng.nextDouble(),
        radius: rng.nextDouble() * 1.6 + 0.3,
        opacity: rng.nextDouble() * 0.55 + 0.15,
      ),
    );
  }

  @override
  void paint(Canvas canvas, Size size) {
    for (final s in _stars) {
      canvas.drawCircle(
        Offset(s.x * size.width, s.y * size.height),
        s.radius,
        Paint()
          ..color = Colors.white.withAlpha((s.opacity * 255).round())
          ..style = PaintingStyle.fill,
      );
    }
  }

  @override
  bool shouldRepaint(_StarsPainter _) => false;
}

class _Star {
  const _Star({
    required this.x,
    required this.y,
    required this.radius,
    required this.opacity,
  });
  final double x;
  final double y;
  final double radius;
  final double opacity;
}
