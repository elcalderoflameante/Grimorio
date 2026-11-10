# Mantener la documentación

## Una fuente por procedimiento

- Manual operativo publicable: `docs/manual/articles`, con registro en su catálogo.
- Documentación técnica privada: `docs/technical` y documentos pendientes según
  el [inventario documental](documentation-audit.md).
- README de módulos: entrada breve y compilación local, no otra copia del manual.
- Historia: `docs/archive`, fuera del catálogo y del paquete API.

No borrar ni publicar automáticamente los documentos pendientes. Fusionar solo
tras contrastar contenido y actualizar todos los enlaces. Se permiten entradas
de transición que enlacen la fuente central, sin copiar los procedimientos.

## Plantilla de guía

1. Objetivo concreto: una tarea, no un catálogo de botones.
2. Requisitos, permisos y preparación.
3. Pasos con nombres de acciones y pantallas reales.
4. Resultado esperado y qué **no** cambia la operación.
5. Problemas habituales y enlaces apropiados.

En `catalog.json` registrar título, resumen, sección, público, tipo, orden,
`verifiedOn`, alcance real de la verificación, palabras de búsqueda y permisos.
No actualizar fechas automáticamente al compilar ni afirmar pruebas en teléfono
o producción cuando solo se revisó el código.

## Capturas

Usar datos ficticios, sin PIN, tokens, correos, personas o cantidades comerciales
reales. Capturar la interfaz existente con fixtures reproducibles; no dibujar
una pantalla inexistente. Registrar imágenes como assets del catálogo y enlazar
con `asset:identificador`. Nunca poner capturas internas en `frontend/public`.

## Revisión y publicación

Un cambio funcional relevante incluye actualizar su guía y el changelog:

- Contrastar pasos, nombres, unidades y permisos con implementación y pruebas.
- Ejecutar `node tools/check-help-docs.mjs` y las comprobaciones del centro de ayuda.
- Probar enlaces, sesiones limitadas, acceso anónimo, móvil y escritorio.
- Comprobar que el paquete no exponga documentos técnicos.
- Dejar explícitas las verificaciones o entregas todavía pendientes.

Modificar Markdown o subir Git no despliega el VPS. El contenido viaja con la
API; frontend y backend deben corresponderse. Ver la [entrega técnica](technical/help-center.md).
