# Grimorio

Sistema de gestion empresarial con backend en .NET y frontend en React para administracion interna, RRHH, nomina, horarios y atencion QR en mesas.

## Estructura de documentacion

- `README.md`: vision general del proyecto y arranque rapido.
- `CHANGELOG.md`: historial de cambios funcionales/tecnicos.
- `docs/README.md`: indice de documentacion global.
- `docs/manual/`: fuente unica del centro de ayuda autenticado, piloto de inventarios en `/ayuda/inicio`.
- `docs/technical/`: referencia privada, fuera de la ayuda web.
- `backend/docs/API_STANDARDS.md`: estandares y convenciones de endpoints backend.
- `frontend/README.md`: guia del modulo frontend.
- `mobile/README.md`: entrada de las aplicaciones moviles.
- `docs/archive/`: documentos historicos que ya no son referencia principal.

## Estructura del repositorio

```text
Grimorio/
|-- backend/
|   |-- Grimorio.API/
|   |-- Grimorio.Application/
|   |-- Grimorio.Domain/
|   |-- Grimorio.Infrastructure/
|   |-- Grimorio.SharedKernel/
|   `-- docs/
|-- frontend/
|-- mobile/
|-- docs/
|   `-- archive/
|-- CHANGELOG.md
`-- README.md
```

## Stack actual

### Backend

- .NET 10
- ASP.NET Core
- Entity Framework Core
- MediatR (CQRS)
- AutoMapper
- PostgreSQL
- SignalR

### Frontend

- React 19
- TypeScript
- Vite
- Ant Design
- Axios
- SignalR client
- Leaflet

## Arranque rapido

### Requisitos

- .NET SDK 10+
- Node.js 20+
- PostgreSQL 15+

### Backend

```bash
cd backend
dotnet build Grimorio.slnx
dotnet run --project .\Grimorio.API\Grimorio.API.csproj --urls http://0.0.0.0:5186
```

API local: `http://localhost:5186`  
Swagger: `http://localhost:5186/swagger`

### Frontend

```bash
cd frontend
npm install
npm run dev
```

App local: `http://localhost:5173`

## Convencion para nuevos documentos

- Documento global: `docs/`.
- Documento especifico de modulo: dentro del modulo (`backend/docs`, `frontend/`, `mobile/`).
- Documento temporal o historico: `docs/archive/`.

## Estado de documentacion

Esta base documental esta preparada para evolucion incremental. A partir de aqui, los cambios de arquitectura, endpoints y decisiones deben actualizarse en el documento del modulo correspondiente y reflejarse en `CHANGELOG.md`.
