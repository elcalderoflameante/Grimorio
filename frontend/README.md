# Frontend Grimorio

Aplicacion web administrativa para operaciones internas de Grimorio.

## Web pública del restaurante

- `/`: portada de El Caldero Flameante, adaptable a móvil, con selección de platos, experiencia, micheladas y contacto.
- `/login` y `/dashboard`: acceso interno existente. `/mesa/:token`: atención QR existente.
- Contenido y contacto: `src/pages/restaurantContent.ts`. El horario queda vacío hasta confirmación. WhatsApp usa el prefijo de Ecuador; las redes usan el identificador facilitado por el propietario. El mapa utiliza el enlace directo de Google Maps facilitado por el propietario.
- Fotos: `public/restaurant/`. Pueden reemplazarse conservando los nombres o actualizando sus referencias. El logo y tres fotos son copias de los originales del propietario. `costillas-retocadas.png` es una variante generada con imagegen; consultar `docs/restaurant-images.md` para su prompt.
- Esta primera fase es visual: no incluye carrito, pagos ni envío de pedidos al ERP. Los enlaces sociales y de mapa requieren comprobación del propietario antes de publicar.
- Ejecutar `npm run dev` y abrir la raíz para revisar. La publicación en el dominio y su configuración DNS no forman parte del cambio local.

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
