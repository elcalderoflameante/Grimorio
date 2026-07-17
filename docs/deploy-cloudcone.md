# Deploy En CloudCone

Esta configuracion deja tres servicios en el VPS:

- `db`: PostgreSQL 15
- `api`: ASP.NET Core 10 en `:8080` interno
- `web`: Caddy sirviendo el frontend y proxyando `/api` y `/hubs`

## 1. Requisitos del VPS

- Ubuntu 24.04 LTS recomendado
- Docker Engine y Docker Compose plugin instalados
- Dominio apuntando al VPS
- Puertos `80` y `443` abiertos
- Swap de 2 a 4 GB recomendado si el VPS tiene 4 GB de RAM

## 2. Preparar secretos y variables

1. Copia `deploy/cloudcone/.env.example` a `deploy/cloudcone/.env`
2. Ajusta `APP_DOMAIN`, `VITE_PUBLIC_APP_URL`, `CORS_ALLOWED_ORIGINS`, credenciales de DB, JWT, usuario admin inicial y `ALEXA_KITCHEN_COMMAND_KEY`
3. Coloca tu Firebase Admin SDK en `backend/secrets/firebase-adminsdk.json`
4. Mantén `DATA_PROTECTION_KEYS_PATH=/app/keys`; las llaves se guardan en un volumen persistente y no en `backend/secrets`

## 3. Levantar el stack

Desde la raiz del repo:

```bash
cd deploy/cloudcone
docker compose --env-file .env up -d --build
```

## 4. Verificar

```bash
docker compose ps
docker compose logs -f api
docker compose logs -f web
```

Checks esperados:

- `https://tu-dominio.com` carga el frontend
- `https://tu-dominio.com/api/health` o un endpoint valido responde desde la API
- `https://tu-dominio.com/hubs/table-service` queda accesible para SignalR

## 5. Operacion basica

Actualizar despliegue:

```bash
git pull
cd deploy/cloudcone
docker compose --env-file .env up -d --build
```

Detener:

```bash
cd deploy/cloudcone
docker compose down
```

No uses `docker compose down -v` en produccion: elimina los volumenes persistentes, incluida la base de datos.

Backup manual de PostgreSQL:

```bash
cd deploy/cloudcone
docker compose --env-file .env exec -T db pg_dump -U "$DB_USER" "$DB_NAME" > "backup_$(date +%F).sql"
```

## Notas

- Esta configuracion usa Postgres dentro del VPS para simplificar el primer despliegue.
- En un VPS de 4 GB, evita agregar Adminer, Redis o procesos extra hasta medir consumo real.
- Si luego migras la base a un proveedor externo, basta con cambiar `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER` y `DB_PASSWORD`.
- Si no defines `GRIMORIO_ADMIN_EMAIL` y `GRIMORIO_ADMIN_PASSWORD`, el sistema usara credenciales de desarrollo. No expongas produccion con esos valores.
- `ALEXA_KITCHEN_COMMAND_KEY` debe ser un secreto largo y aleatorio. Docker lo expone al API como `Alexa__KitchenCommandKey`, que es la clave que valida `POST /api/alexa/kitchen-command`.
