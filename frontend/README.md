# Frontend Grimorio

Aplicacion web administrativa para operaciones internas de Grimorio.

## Stack

- React 19
- TypeScript
- Vite
- Ant Design
- Axios
- SignalR (`@microsoft/signalr`)

## Scripts

```bash
npm run dev
npm run build
npm run lint
npm run preview
```

## Estructura principal

```text
src/
|-- components/
|-- context/
|-- pages/
|-- services/
|-- types/
`-- utils/
```

## Convenciones

- Clientes HTTP con sufijo `Api` (ejemplo: `userApi`, `tableServiceApi`).
- Tipos compartidos centralizados en `src/types/index.ts`.
- Hooks de contexto separados de providers cuando aplique (ejemplo: `useAuth`).
- Toda nueva pagina/componente debe compilar sin errores de TypeScript y sin warnings de lint.

## Integracion con backend

- URL API por `VITE_API_URL` en `.env`.
- En local, valor sugerido: `http://localhost:5186/api`.

## Notas de mantenimiento

- Evitar reintroducir nombres `*Service` para clientes HTTP del frontend.
- Registrar cambios importantes del modulo en `CHANGELOG.md` raiz.
