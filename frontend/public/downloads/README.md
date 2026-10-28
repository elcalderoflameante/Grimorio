# Descargas publicas

Inventario del personal: `grimorio-inventario.apk` y `inventory-version.json`, preparados con `mobile/inventory_app/publish-apk.ps1`. Descarga desde **Inventario > Conteos del personal > Descargar app**. Manifiesto con versión y SHA-256; publicar ambos en el mismo despliegue.

Colocar aqui los instaladores que el frontend debe servir directamente.

La app de meseros debe publicarse con este nombre:

```text
grimorio-meseros.apk
```

El boton del ERP apunta por defecto a:

```text
/downloads/grimorio-meseros.apk
```

La app de estaciones debe publicarse con este nombre:

```text
grimorio-estaciones.apk
```

El boton del ERP apunta por defecto a:

```text
/downloads/grimorio-estaciones.apk
```

El actualizador de Grimorio KDS consulta:

```text
/downloads/station-version.json
```

El actualizador de Meseros, disponible desde `1.0.26+27`, consulta:

```text
/downloads/waitstaff-version.json
```

Las versiones anteriores de Meseros requieren instalar esta primera version
manualmente desde el ERP. Las siguientes muestran el aviso y descargan el APK
dentro de la app; Android solicita confirmar la instalacion. Cada app usa su
propio manifiesto y su propio APK.

Al publicar una version, incrementar `versionCode` y `versionName`, reemplazar
la APK y actualizar `sha256` con el hash SHA-256 exacto del archivo publicado.

La app de asistencia debe publicarse como:

```text
grimorio-asistencia.apk
```

El boton del ERP apunta por defecto a:

```text
/downloads/grimorio-asistencia.apk
```
