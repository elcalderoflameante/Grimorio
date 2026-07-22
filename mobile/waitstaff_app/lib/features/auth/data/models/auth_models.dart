class LoginRequest {
  const LoginRequest({required this.email, required this.password});

  final String email;
  final String password;

  Map<String, dynamic> toJson() {
    return {'email': email, 'password': password};
  }
}

class PinBranch {
  const PinBranch({required this.id, required this.name, required this.code});
  final String id;
  final String name;
  final String code;

  String get displayName => code.isEmpty ? name : '$name ($code)';

  factory PinBranch.fromJson(Map<String, dynamic> json) => PinBranch(
    id: json['id']?.toString() ?? '',
    name: json['name']?.toString() ?? '',
    code: json['code']?.toString() ?? '',
  );
}

class PinUser {
  const PinUser({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.hasPin,
  });
  final String id;
  final String firstName;
  final String lastName;
  final bool hasPin;

  String get displayName => '$firstName $lastName'.trim();

  factory PinUser.fromJson(Map<String, dynamic> json) => PinUser(
    id: json['id']?.toString() ?? '',
    firstName: json['firstName']?.toString() ?? '',
    lastName: json['lastName']?.toString() ?? '',
    hasPin: json['hasKdsPin'] as bool? ?? false,
  );
}

class AuthSession {
  const AuthSession({
    required this.userId,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.accessToken,
    required this.refreshToken,
    required this.expiresAt,
    required this.permissions,
  });

  final String userId;
  final String email;
  final String firstName;
  final String lastName;
  final String accessToken;
  final String refreshToken;
  final DateTime expiresAt;
  final List<String> permissions;

  String get fullName => '$firstName $lastName'.trim();

  bool get isExpired => DateTime.now().toUtc().isAfter(expiresAt);

  factory AuthSession.fromJson(Map<String, dynamic> json) {
    return AuthSession(
      userId: json['userId']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      firstName: json['firstName']?.toString() ?? '',
      lastName: json['lastName']?.toString() ?? '',
      accessToken: json['accessToken']?.toString() ?? '',
      refreshToken: json['refreshToken']?.toString() ?? '',
      expiresAt:
          DateTime.tryParse(json['expiresAt']?.toString() ?? '')?.toUtc() ??
          DateTime.now().toUtc(),
      permissions: (json['permissions'] as List<dynamic>? ?? const [])
          .map((item) => item.toString())
          .toList(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'email': email,
      'firstName': firstName,
      'lastName': lastName,
      'accessToken': accessToken,
      'refreshToken': refreshToken,
      'expiresAt': expiresAt.toUtc().toIso8601String(),
      'permissions': permissions,
    };
  }
}
