enum DisplaySizeMode { automatic, compact, normal, large }

extension DisplaySizeModeLabel on DisplaySizeMode {
  String get label => switch (this) {
    DisplaySizeMode.automatic => 'Automático',
    DisplaySizeMode.compact => 'Compacto',
    DisplaySizeMode.normal => 'Normal',
    DisplaySizeMode.large => 'Grande',
  };

  String get description => switch (this) {
    DisplaySizeMode.automatic => 'Se adapta al espacio disponible',
    DisplaySizeMode.compact => 'Muestra más pedidos a la vez',
    DisplaySizeMode.normal => 'Equilibrio entre espacio y lectura',
    DisplaySizeMode.large => 'Más legible a mayor distancia',
  };
}
