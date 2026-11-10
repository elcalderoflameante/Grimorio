# Grimorio Inventario

App Flutter Android para registrar conteos del personal sin ajustar existencias.

## Documentación central

- [Instalación y actualización](../../docs/manual/articles/inventario-instalar.md).
- [Uso del personal](../../docs/manual/articles/inventario-contar.md).
- [Configuración](../../docs/manual/articles/inventario-configurar.md).
- [Revisión y reconteos](../../docs/manual/articles/inventario-revisar.md).
- [Problemas frecuentes](../../docs/manual/articles/inventario-problemas.md).
- [Contratos, firma, actualizador y pruebas](../../docs/technical/inventory-counts.md).

En desarrollo, consultar el mismo contenido en `/ayuda/inicio` con sesión del ERP.
No mantener otra copia de los procedimientos en este README.

## Compilar y publicar

```powershell
flutter pub get
dart format lib test
flutter analyze --no-pub
flutter test --no-pub
flutter build apk --release --no-pub --dart-define=API_BASE_URL=https://erp.elcalderoflameante.com/api
./publish-apk.ps1
```

Paquete: `com.grimorio.inventory_app`. Mantener firma y aumentar versión para
actualizar. El script publica APK y manifiesto con SHA-256 en
`frontend/public/downloads`. No publicar claves ni firmar con otra identidad.
Consultar los requisitos privados de firma en la referencia técnica central.
