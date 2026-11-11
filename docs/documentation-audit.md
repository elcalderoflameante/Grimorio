# Inventario documental

Revisión inicial: **2026-11-09**. Clasificación y enlaces, no certificación de que
todo el contenido anterior esté vigente o desplegado. No se borraron documentos
pendientes ni se publican por figurar en este índice.

| Documento o grupo | Público | Estado / destino |
| --- | --- | --- |
| `manual/articles/*.md` | Según permisos | 33 guías y cobertura de grupos del ERP; fuente operativa central. Ampliación contrastada con repositorio el 2026-11-10, aceptación operativa pendiente. |
| `technical/manual-coverage.md` | Técnico | Fuentes revisadas, controles de cobertura y aceptación por módulo, privados. |
| `staff-inventory-counts.md` | Transición | Enlaces al manual y referencia técnica; sin copia del procedimiento. |
| `technical/inventory-counts.md` | Técnico | Contratos, publicación Android y pruebas, privados. |
| `technical/inventory-negative-stock.md` | Técnico | Política por artículo, migración y pruebas de consumos aproximados; procedimiento en `inventario-catalogos`. Privado. |
| `technical/help-center.md` | Técnico | Arquitectura, seguridad y entrega del piloto, privados. |
| `permission-matrix.md` | Técnico | Matriz transversal. Pendiente de revisión completa; no publicar. |
| `production-access.md` | Técnico | Accesos y seguridad de producción. Privado. |
| `deploy-cloudcone.md` | Técnico | Despliegue del ERP. Privado. |
| `deploy-walkie-talkie.md` | Técnico | Despliegue de voz. Privado. |
| `walkie-talkie.md` | Técnico / transición | Uso centralizado en `comunicacion-walkie`; conserva arquitectura y evolución privadas. Audio real pendiente de aceptación. |
| `walkie-boton-volumen.md` | Mixto | Validar en teléfonos antes de publicar instrucciones. |
| `waitstaff-update-review.md` | Técnico | Actualizaciones de Meseros, pendiente de consolidación. |
| `responsabilidades-operativas.md` | Técnico / transición | Uso centralizado en `operaciones-responsabilidades`; conserva contratos y evolución privados. |
| `employee-user-link.md` | Técnico / transición | Uso centralizado en `admin-accesos`; conserva migración y pruebas privadas. |
| `marketing-fidelizacion.md` | Técnico / transición | Uso centralizado en `fidelizacion-programa`, con límites de canje y activación explícitos. Despliegue por confirmar. |
| `whatsapp-afiliacion-produccion.md` | Técnico | Integración privada. No publicar. |
| `whatsapp-afiliacion-pruebas.md` | Técnico | Pruebas privadas, no disponibilidad en producción. |
| `virtual-waiter.md` | Prototipo | Brasa en desarrollo, no manual de funcionalidad publicada. |
| `alexa-kitchen-skill.md` | Técnico | Integración privada, pendiente de revisión. |
| `attendance-biometric-architecture.md` | Técnico | Biometría y privacidad. Mantener privado. |
| `fixed-combo-options.md` | Técnico / transición | Uso centralizado en `menu-recetas`; conserva snapshots, migraciones y pruebas privadas. |
| `multi-station-combos.md` | Técnico / transición | Uso centralizado en `menu-recetas` y `apps-estaciones`; conserva detalles técnicos privados. |
| `invoice-savings.md` | Mixto | Revisar facturación antes de crear una guía operativa. |
| `archive/TYPESCRIPT_MIGRATION.md` | Histórico | Fuera del manual y del paquete API. |
| `../README.md`, `../frontend/README.md`, `../mobile/README.md` | Técnico | Entradas con enlaces, no manuales duplicados. |
| `../mobile/inventory_app/README.md` | Técnico | Compilación; el uso se enlaza al manual central. |
| `../backend/docs/API_STANDARDS.md` | Técnico | Convenciones de API existentes y privadas. |
| `../CHANGELOG.md` | Entrega | Registro de cambios, no sustituye procedimientos. |

## Próximas migraciones

1. Aceptar el piloto de inventarios con personal y un teléfono Android.
2. Aceptar las guías de caja y ventas incorporadas, separando corrección interna de documentos fiscales.
3. Aceptar con responsables las guías incorporadas de pedidos, apps, menú, compras, facturación, finanzas, personal, operaciones y fidelización.
4. Ampliar excepciones y capturas por módulo; comprobar cámara, audio e integraciones en equipos y entornos autorizados.

En cada grupo, revisar, fusionar duplicados y actualizar enlaces antes de archivar
lo obsoleto. El historial de Git conserva la trazabilidad.
