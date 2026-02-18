# Changelog

Todos los cambios notables del proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere al [Versionamiento Semántico](https://semver.org/lang/es/).

## [Sin versionar]

### Agregado
- Sistema de autenticación con JWT
- Gestión de usuarios, roles y permisos
- Módulo de RRHH (empleados, posiciones)
- Sistema de planificación de horarios
  - Generación automática de turnos
  - Plantillas de horarios
  - Áreas de trabajo y roles de trabajo
  - Visualización de calendario mensual
- Gestión de indisponibilidad de empleados
  - Soporte para fechas individuales
  - Soporte para rangos de fechas
- Gestión de sucursales
  - Configuración básica (nombre, código, dirección, teléfono, email)
  - Geolocalización con mapas interactivos (Leaflet + OpenStreetMap)
  - Reverse geocoding automático (coordenadas → dirección)
  - Almacenamiento de coordenadas GPS (precisión ~10cm)
- Breadcrumb navigation en Dashboard
- Manejo centralizado de errores con mensajes en español
- Migraciones de base de datos

### Técnico
- Arquitectura backend con CQRS (MediatR)
- Entity Framework Core con PostgreSQL
- Frontend con React 18, TypeScript y Ant Design
- Patrón Multi-tenancy con soporte para múltiples sucursales
- Documentación XML en controladores del backend
- Error handler centralizado en frontend

---

## Formato para futuras entradas:

### [Versión] - YYYY-MM-DD

#### Agregado
- Nueva funcionalidad para usuarios finales

#### Cambiado
- Cambios en funcionalidad existente

#### Obsoleto
- Funcionalidad que será removida pronto

#### Removido
- Funcionalidad eliminada

#### Corregido
- Corrección de bugs

#### Seguridad
- Cambios relacionados con vulnerabilidades
