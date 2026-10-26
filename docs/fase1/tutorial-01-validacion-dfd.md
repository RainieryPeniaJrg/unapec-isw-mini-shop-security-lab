# Tutorial 1 — Validación del DFD contra la Aplicación en Ejecución

**Responsable:** Persona A  
**Entregables cubiertos:** F1-03 (DFD), F1-04 (Activos y puntos de entrada), F1-05 (Límites de confianza)  
**Tiempo estimado:** 60–90 minutos  
**Herramientas:** Terminal Linux, Burp Suite Community, navegador, curl, Docker  

---

## Introducción

### Qué es la validación del DFD

Un DFD (Data Flow Diagram) describe cómo fluyen los datos entre los actores, la aplicación, la base de datos y las herramientas de prueba. **Validar** el DFD significa comprobar que cada caja, flecha y límite de confianza del diagrama existe realmente en la aplicación en ejecución. No basta con que el diagrama "se vea bien" — necesitamos evidencia observable de que cada elemento es real.

### Por qué importa para el entregable

La rúbrica exige que el DFD refleje la aplicación implementada, no un diseño teórico. El docente puede pedir que demuestres cualquier elemento del diagrama en vivo. Si un flujo aparece en el DFD pero no existe en la app (o viceversa), se pierde puntuación en F1-03, F1-04 y F1-05.

### Archivo de referencia

El DFD que vamos a validar está en: `threat-model/dfd.md`

---

## Parte 0 — Preparación del Entorno

### 0.1 Configurar `LAB_MODE=redteam`

Antes de levantar la aplicación, asegúrate de que existe el archivo `.env` con el modo Red Team activo. Sin él, las vulnerabilidades intencionales del laboratorio quedan deshabilitadas.

**Si es una PC nueva (no existe `.env`):**

```bash
cd ~/seguridad/unapec-isw-mini-shop-security-lab
cp .env.example .env
```

**Si ya existe `.env` pero no tiene `LAB_MODE`**, edítalo con nano:

```bash
nano .env
```

Dentro de nano, asegúrate de que contenga esta línea (agrégala si falta):

```
LAB_MODE=redteam
```

Para guardar y salir: `Ctrl+O` → `Enter` → `Ctrl+X`.

**Si el compose ya estaba corriendo sin `LAB_MODE`**, después de crear/editar el `.env` necesitas recrear el contenedor:

```bash
docker compose up -d shop --force-recreate
```

Verifica que el modo quedó activo:

```bash
docker compose exec shop printenv LAB_MODE
```

Debe devolver `redteam`.

### 0.2 Levantar la aplicación

Todos los comandos se ejecutan desde el directorio raíz del proyecto.

```bash
cd ~/seguridad/unapec-isw-mini-shop-security-lab
```

Levantar los contenedores:

```bash
docker compose up --build -d
```

Verificar que están corriendo:

```bash
docker compose ps
```

**Resultado esperado:** dos contenedores activos (`shop` y `postgres`), ambos en estado `Up` o `running`.

Verificar el puerto:

```bash
docker compose port shop 8080
```

**Resultado esperado:** `127.0.0.1:5081`

> **Nota sobre el puerto:** El proyecto está diseñado para usar el puerto `5080` (definido en `compose.yaml`). En nuestro entorno usamos `5081` porque hay otro servicio ocupando el `5080`. Esto se configura en `compose.override.yaml` con `ports: !override`. En todos los comandos de este tutorial usaremos `5081`. Si en tu entorno el puerto `5080` está libre, puedes eliminar `compose.override.yaml` y usar `5080` en su lugar.

Confirmar que la app responde:

```bash
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5081/Products
```

**Resultado esperado:** `200`

### 0.3 Configurar Burp Suite Community

#### Iniciar Burp Suite

```bash
~/BurpSuite/BurpSuite &
```

1. Al abrir, selecciona **Temporary Project** → **Next** → **Use Burp defaults** → **Start Burp**.
2. Espera a que cargue completamente (puede tardar 30–60 segundos).

#### Configurar el listener del proxy

1. Ve a la pestaña **Proxy** → **Proxy settings** (o **Options** en versiones anteriores).
2. En **Proxy Listeners**, verifica que existe un listener en `127.0.0.1:8080` y está habilitado (checkbox marcado).
3. Si no existe, haz clic en **Add**, escribe `127.0.0.1` en Bind address y `8080` en Bind port.

#### Configurar el scope

1. Ve a la pestaña **Target** → **Scope settings** (o **Scope** en versiones anteriores).
2. En **Target scope**, haz clic en **Add**.
3. Configura:
   - **Protocol:** `http`
   - **Host or IP range:** `127.0.0.1`
   - **Port:** `5081`
   - **File:** dejar vacío (captura todas las rutas)
4. Haz clic en **OK**.
5. Cuando pregunte si deseas filtrar el historial por scope, selecciona **Yes**.

#### Usar el navegador integrado de Burp

1. Ve a **Proxy** → **Intercept**.
2. Asegúrate de que **Intercept is off** (botón azul desactivado). Queremos capturar el tráfico sin interceptarlo manualmente.
3. Haz clic en **Open browser**. Se abre Chromium con el proxy ya configurado.
4. Navega a `http://127.0.0.1:5081/Products` para confirmar que funciona.

**Verificación:** Ve a **Proxy** → **HTTP history**. Deberías ver la petición GET a `/Products` en la lista. Si la ves, Burp está capturando correctamente.

**Captura de pantalla 0:** Toma una captura de HTTP history mostrando la primera petición capturada y el scope configurado.

### 0.4 Cuentas de prueba disponibles

| Cuenta | Contraseña | Rol |
|---|---|---|
| `cliente-a` | `LabClienteA2026!` | Cliente |
| `cliente-b` | `LabClienteB2026!` | Cliente |
| `cliente-xss` | `LabXss2026!` | Cliente |
| `cliente-brute` | `LabPass2026!` | Cliente |
| `vendedor-lab` | `LabSeller2026!` | Vendedor |
| `admin-lab` | `LabAdmin2026!` | Admin |

---

## Parte 1 — Validar las Cajas (Entidades del DFD)

Cada "caja" del DFD representa un componente del sistema. Vamos a demostrar que cada uno existe.

---

### Paso 1 — Entidad U: "Cliente, Vendedor o Admin en navegador"

**El DFD afirma:** Existen tres roles de usuario (Cliente, Vendedor, Admin) que interactúan desde el navegador.

#### 1a. Verificar que los tres roles existen — por terminal

Inicia sesión con cada rol vía la API:

```bash
# Login como Cliente
curl -s http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -m json.tool

# Login como Vendedor
curl -s http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"vendedor-lab","password":"LabSeller2026!"}' | python3 -m json.tool

# Login como Admin
curl -s http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin-lab","password":"LabAdmin2026!"}' | python3 -m json.tool
```

**Resultado esperado para cada uno:** Un JSON con un campo `token` (JWT) y/o información del usuario. Cada respuesta debe incluir el rol correspondiente (`Cliente`, `Vendedor`, `Admin`).

#### 1b. Verificar por Burp Suite

1. En el navegador integrado de Burp, ve a `http://127.0.0.1:5081/Account/Login`.
2. Inicia sesión como `cliente-a` / `LabClienteA2026!`.
3. Observa que llegas a la página de productos como cliente.
4. En Burp → **Proxy** → **HTTP history**, busca la petición POST a `/Account/Login`.
5. Haz clic derecho → **Send to Repeater** (para guardarla como evidencia).
6. Cierra sesión (logout) y repite con `vendedor-lab` y `admin-lab`.

**Criterio PASS/FAIL:**
- **PASS:** Los tres logins son exitosos y cada uno tiene un rol diferente.
- **FAIL:** Algún login falla o los roles no se distinguen.

**Captura de pantalla 1:** HTTP history mostrando los tres POST `/Account/Login` exitosos (código 302 redirect tras login correcto).

---

### Paso 2 — Entidad W: "ASP.NET Core MVC/Razor + API"

**El DFD afirma:** Existe un único proceso web que sirve tanto HTML (MVC/Razor) como JSON (API).

#### 2a. Verificar respuesta HTML (MVC) — por terminal

```bash
curl -s -D - http://127.0.0.1:5081/Products | head -20
```

**Resultado esperado:** Headers HTTP que incluyen `Content-Type: text/html` y cuerpo HTML con etiquetas como `<html>`, `<body>`, elementos de la página de productos.

#### 2b. Verificar respuesta JSON (API) — por terminal

```bash
curl -s -D - http://127.0.0.1:5081/api/products/search?q=test | head -20
```

**Resultado esperado:** Headers HTTP que incluyen `Content-Type: application/json` y cuerpo JSON (array `[]` o con resultados).

#### 2c. Verificar que es un solo proceso — por terminal

```bash
docker compose exec shop ps aux
```

**Resultado esperado:** Un solo proceso `dotnet` ejecutando `MiniShop.Api.dll`. Si `ps` no está instalado en el contenedor:

```bash
docker compose top shop
```

Verás un solo proceso principal de dotnet.

#### 2d. Verificar por Burp Suite

1. En el navegador de Burp, visita `http://127.0.0.1:5081/Products` (devuelve HTML).
2. En HTTP history, selecciona esa petición y observa la respuesta: debe ser HTML.
3. Ahora abre una nueva pestaña y visita `http://127.0.0.1:5081/api/products/search?q=a` (devuelve JSON).
4. En HTTP history, selecciona esa petición y observa la respuesta: debe ser JSON.

**Criterio PASS/FAIL:**
- **PASS:** El mismo host (`127.0.0.1:5081`) sirve HTML y JSON desde un solo proceso.
- **FAIL:** Son procesos separados o no sirve ambos formatos.

**Captura de pantalla 2:** En HTTP history, selecciona la petición HTML y la petición JSON, mostrando los diferentes Content-Type en las respuestas.

---

### Paso 3 — Entidad D: "PostgreSQL con datos sintéticos"

**El DFD afirma:** Existe una base PostgreSQL con 7 tablas de datos sintéticos.

#### 3a. Verificar que PostgreSQL está corriendo

```bash
docker compose exec postgres pg_isready
```

**Resultado esperado:** `localhost:5432 - accepting connections`

#### 3b. Verificar las 7 tablas

```bash
docker compose exec postgres psql -U minishop -d minishop -c "\dt"
```

**Resultado esperado:** Una tabla con al menos estas 7 tablas:
- `Users`
- `Products`
- `Comments`
- `Carts`
- `CartItems`
- `Orders`
- `OrderItems`

(También puede aparecer `__EFMigrationsHistory` — esa es interna de EF Core.)

#### 3c. Verificar datos sintéticos (cuentas seed)

```bash
docker compose exec postgres psql -U minishop -d minishop \
  -c "SELECT \"Id\", \"Username\", \"Role\" FROM \"Users\";"
```

**Resultado esperado:** 6 usuarios con los roles Cliente, Vendedor y Admin.

#### 3d. Verificar productos sintéticos

```bash
docker compose exec postgres psql -U minishop -d minishop \
  -c "SELECT \"Id\", \"Name\", \"PriceCents\" FROM \"Products\" LIMIT 5;"
```

**Resultado esperado:** Productos con nombres y precios en centavos.

**Criterio PASS/FAIL:**
- **PASS:** PostgreSQL responde, tiene las 7 tablas y contiene datos sintéticos (usuarios, productos).
- **FAIL:** PostgreSQL no responde, faltan tablas o no hay datos seed.

**Captura de pantalla 3:** Terminal mostrando la salida de `\dt` con las 7 tablas.

---

### Paso 4 — Entidad P: "Burp/ZAP y herramientas de prueba"

**El DFD afirma:** Las herramientas de pentesting (Burp/ZAP/sqlmap) existen como actores externos que apuntan solo al loopback.

#### 4a. Verificar que Burp está operativo

Si seguiste el Paso 0, Burp ya está corriendo. Verifica que el proxy captura tráfico:

```bash
curl -s -x http://127.0.0.1:8080 http://127.0.0.1:5081/Products -o /dev/null -w "%{http_code}"
```

El flag `-x http://127.0.0.1:8080` enruta el tráfico a través del proxy de Burp.

**Resultado esperado:** `200` y la petición aparece en HTTP history de Burp.

#### 4b. Verificar que sqlmap está disponible

```bash
sqlmap --version
```

**Resultado esperado:** Imprime la versión de sqlmap (ej. `1.8.x`). Si no está instalado:

```bash
sudo apt install sqlmap -y
sqlmap --version
```

#### 4c. Verificar que las herramientas solo apuntan a loopback

Esto es una verificación conceptual: el scope de Burp está configurado para `127.0.0.1:5081` (lo hicimos en el Paso 0). No hay ninguna URL externa en el scope.

En Burp, ve a **Target** → **Scope settings** y confirma que solo aparece `http://127.0.0.1:5081`.

**Criterio PASS/FAIL:**
- **PASS:** Burp está capturando tráfico local, sqlmap está disponible, el scope es solo loopback.
- **FAIL:** Las herramientas no están instaladas o el scope incluye hosts externos.

**Captura de pantalla 4:** Scope de Burp mostrando solo `127.0.0.1:5081`.

---

### Paso 5 — Entidad L: "Salida de contenedor" (Logs)

**El DFD afirma:** Los logs de la aplicación van a la salida estándar del contenedor.

#### 5a. Verificar los logs del contenedor

```bash
docker compose logs shop --tail 20
```

**Resultado esperado:** Líneas de log de ASP.NET Core mostrando inicio de la aplicación, conexión a PostgreSQL, peticiones HTTP procesadas, etc.

#### 5b. Generar una petición y ver el log en tiempo real

En una terminal:

```bash
docker compose logs shop -f --tail 5
```

En otra terminal (o en el navegador de Burp), genera tráfico:

```bash
curl -s http://127.0.0.1:5081/Products > /dev/null
```

**Resultado esperado:** En la primera terminal, aparece una nueva línea de log registrando la petición GET a `/Products`.

Presiona `Ctrl+C` para salir del seguimiento de logs.

**Criterio PASS/FAIL:**
- **PASS:** Los logs se emiten a stdout del contenedor y son visibles con `docker compose logs`.
- **FAIL:** No hay logs o no se registran las peticiones.

**Captura de pantalla 5:** Terminal mostrando `docker compose logs shop` con líneas de peticiones HTTP.

---

## Parte 2 — Validar las Flechas (Flujos de Datos)

Cada flecha del DFD representa un flujo de datos entre dos entidades. Vamos a demostrar que cada flujo es real.

---

### Paso 6 — Flujo U→W: "HTTP, formulario, cookie o Bearer token"

**El DFD afirma:** El usuario envía peticiones HTTP al servidor con formularios, cookies de sesión o tokens JWT Bearer.

#### 6a. Formulario (login MVC)

En el navegador de Burp, ve a `http://127.0.0.1:5081/Account/Login` y envía el formulario con `cliente-a` / `LabClienteA2026!`.

En Burp → HTTP history, busca el POST a `/Account/Login`. Haz clic en la petición.

**Qué buscar en el Request:** En la pestaña **Request**, verás:
- `Content-Type: application/x-www-form-urlencoded`
- Cuerpo con `Username=cliente-a&Password=LabClienteA2026!`

Esto demuestra que el navegador envía datos por formulario HTTP.

#### 6b. Cookie de sesión

Después del login, navega a `http://127.0.0.1:5081/Products`.

En HTTP history, busca el GET a `/Products` posterior al login. En los headers del Request verás:

```
Cookie: LabSession=<valor largo codificado>
```

Esto demuestra que el navegador envía la cookie de sesión automáticamente.

#### 6c. Bearer Token (API)

```bash
# Obtener token JWT
TOKEN=$(curl -s http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

echo "Token: $TOKEN"

# Usar el token en una petición API
curl -s -D - http://127.0.0.1:5081/api/cart/1 \
  -H "Authorization: Bearer $TOKEN" | head -10
```

**Resultado esperado del echo:** Un string JWT largo (tres partes separadas por puntos).  
**Resultado esperado del curl:** Headers con código `200` y contenido JSON del carrito.

**Criterio PASS/FAIL:**
- **PASS:** Se observan los tres mecanismos: formulario, cookie `LabSession` y header `Authorization: Bearer`.
- **FAIL:** Alguno de los tres mecanismos no funciona.

**Captura de pantalla 6:** En Burp Repeater o HTTP history, selecciona una petición mostrando la cookie `LabSession` en los headers. Adicionalmente, una petición API mostrando `Authorization: Bearer`.

---

### Paso 7 — Flujo W→U: "HTML/JSON, cookie de sesión"

**El DFD afirma:** El servidor responde con HTML o JSON y establece cookies de sesión.

#### 7a. Respuesta HTML

En Burp HTTP history, selecciona un GET a `/Products`. En la pestaña **Response**:

**Qué buscar:**
- Header `Content-Type: text/html; charset=utf-8`
- Cuerpo HTML con etiquetas como `<!DOCTYPE html>`, `<html>`, estructura de la página

#### 7b. Respuesta JSON

En HTTP history, selecciona un GET a `/api/products/search`. En la Response:

**Qué buscar:**
- Header `Content-Type: application/json; charset=utf-8`
- Cuerpo JSON con un array de productos

#### 7c. Cookie de sesión establecida

En HTTP history, busca la respuesta al POST de `/Account/Login` (status 302). En los headers de la Response:

**Qué buscar:**
- Header `Set-Cookie: LabSession=<valor>;`
- Atributos de la cookie (path, etc.)

O por terminal:

```bash
curl -s -D - http://127.0.0.1:5081/Account/Login \
  -d "Username=cliente-a&Password=LabClienteA2026!" | grep -i "set-cookie"
```

**Resultado esperado:** Una línea `Set-Cookie: LabSession=...` con el valor de la cookie.

**Criterio PASS/FAIL:**
- **PASS:** El servidor devuelve HTML para rutas MVC, JSON para rutas API, y establece cookie `LabSession`.
- **FAIL:** Algún tipo de respuesta no corresponde o la cookie no se establece.

**Captura de pantalla 7:** Respuesta HTTP en Burp mostrando el header `Set-Cookie: LabSession=...`.

---

### Paso 8 — Flujo P→W: "HTTP local dentro del scope"

**El DFD afirma:** Las herramientas de prueba envían peticiones HTTP al servidor dentro del scope local.

#### 8a. Enviar una petición a través del proxy de Burp

```bash
curl -s -x http://127.0.0.1:8080 http://127.0.0.1:5081/Products -o /dev/null -w "%{http_code}\n"
```

**Resultado esperado:** `200` y la petición aparece en HTTP history con la fuente como proxy.

#### 8b. Verificar en Burp HTTP history

1. En Burp → **Proxy** → **HTTP history**, localiza la última petición.
2. Confirma que el host es `127.0.0.1:5081` (dentro del scope).
3. Confirma que no hay peticiones a hosts externos.

**Criterio PASS/FAIL:**
- **PASS:** Las herramientas envían tráfico HTTP local al servidor y se captura en Burp.
- **FAIL:** El tráfico no pasa por el proxy o apunta a un host externo.

**Captura de pantalla 8:** HTTP history con la petición proxyada mostrando host `127.0.0.1:5081`.

---

### Paso 9 — Flujo W→D: "EF Core/Npgsql" (ORM normal)

**El DFD afirma:** La app consulta PostgreSQL usando Entity Framework Core con el proveedor Npgsql (flujo ORM estándar).

#### 9a. Verificar que la app usa PostgreSQL para servir datos

```bash
# Contar productos en la base de datos
docker compose exec postgres psql -U minishop -d minishop \
  -c "SELECT COUNT(*) FROM \"Products\";"
```

Ahora pedir los productos vía la app:

```bash
curl -s http://127.0.0.1:5081/api/products/search?q=a | python3 -m json.tool | head -20
```

**Resultado esperado:** La API devuelve productos que coinciden con los de la base de datos.

#### 9b. Verificar la conexión EF Core en los logs

```bash
docker compose logs shop 2>&1 | grep -i -E "npgsql|postgres|entity" | head -10
```

**Resultado esperado:** Líneas que indican conexión a PostgreSQL o uso de EF Core/Npgsql.

Si no hay líneas explícitas de conexión, simplemente el hecho de que los datos se sirven correctamente confirma que EF Core está conectado (la app no tiene otra fuente de datos).

**Criterio PASS/FAIL:**
- **PASS:** La app lee datos de PostgreSQL y los sirve al usuario; se confirma la conexión ORM.
- **FAIL:** Los datos no provienen de PostgreSQL o la conexión falla.

**Captura de pantalla 9:** Terminal mostrando la consulta SQL directa y la respuesta API con los mismos datos.

---

### Paso 10 — Flujo W→D: "SQL de búsqueda Red Team, rol minishop_search"

**El DFD afirma:** En modo Red Team, la búsqueda ejecuta SQL directo (no ORM) usando un rol PostgreSQL de solo lectura llamado `minishop_search`.

#### 10a. Verificar que el rol `minishop_search` existe

```bash
docker compose exec postgres psql -U minishop -d minishop \
  -c "SELECT rolname FROM pg_roles WHERE rolname = 'minishop_search';"
```

**Resultado esperado:** Una fila con `minishop_search`.

#### 10b. Verificar los permisos del rol (solo lectura)

```bash
docker compose exec postgres psql -U minishop -d minishop \
  -c "SELECT privilege_type FROM information_schema.role_table_grants WHERE grantee = 'minishop_search';"
```

**Resultado esperado:** Solo permisos de tipo `SELECT` (lectura). No debe haber `INSERT`, `UPDATE` ni `DELETE`.

#### 10c. Verificar la señal de SQL directo (time-based)

```bash
# Búsqueda normal — medir tiempo
time curl -s "http://127.0.0.1:5081/api/products/search?q=laptop" -o /dev/null

# Búsqueda con inyección de delay — medir tiempo
time curl -s "http://127.0.0.1:5081/api/products/search?q=laptop'%3BSELECT+pg_sleep(2)--" -o /dev/null
```

**Resultado esperado:**
- Búsqueda normal: ~0.03–0.1 segundos (`real 0m0.0XXs`)
- Búsqueda con inyección: ~2 segundos (`real 0m2.XXXs`)

La diferencia de ~2 segundos demuestra que la búsqueda ejecuta SQL directo (no ORM parametrizado), porque `pg_sleep(2)` se ejecutó dentro de la base de datos.

**Criterio PASS/FAIL:**
- **PASS:** El rol `minishop_search` existe con permisos SELECT, y la búsqueda ejecuta SQL directo (demostrado por el delay de `pg_sleep`).
- **FAIL:** El rol no existe, tiene permisos de escritura, o la inyección no produce delay.

**Captura de pantalla 10:** Terminal mostrando la diferencia de tiempo entre la búsqueda normal y la inyectada.

---

### Paso 11 — Flujo W→L: "Logs de aplicación"

**El DFD afirma:** La aplicación emite logs hacia la salida del contenedor.

Este paso ya se cubrió parcialmente en el Paso 5. Aquí lo complementamos validando que los logs capturan actividad relevante.

#### 11a. Generar actividad y verificar que se registra

```bash
# Limpiar y seguir logs en segundo plano
docker compose logs shop -f --tail 0 > /tmp/dfd-logs-test.txt 2>&1 &
LOG_PID=$!

# Generar varias peticiones
curl -s http://127.0.0.1:5081/Products > /dev/null
curl -s http://127.0.0.1:5081/api/products/search?q=test > /dev/null
curl -s http://127.0.0.1:5081/Account/Login > /dev/null

# Esperar un segundo y detener el seguimiento
sleep 1
kill $LOG_PID 2>/dev/null

# Ver los logs capturados
cat /tmp/dfd-logs-test.txt
```

**Resultado esperado:** Líneas de log mostrando las peticiones HTTP procesadas.

**Criterio PASS/FAIL:**
- **PASS:** Los logs se emiten a stdout del contenedor y registran la actividad HTTP.
- **FAIL:** No se generan logs o no reflejan las peticiones.

**Captura de pantalla 11:** Terminal mostrando los logs con las peticiones recientes.

---

### Paso 12 — Flujo U→W: "Cookie de sesión robada: GET /lab/collect"

**El DFD afirma:** Existe un flujo especial donde el navegador envía una cookie robada (vía XSS) al endpoint `/lab/collect`, que responde 204 sin persistir el valor.

#### 12a. Verificar que el endpoint `/lab/collect` existe y responde 204

```bash
curl -s -D - "http://127.0.0.1:5081/lab/collect?c=test-cookie-value" -o /dev/null
```

**Resultado esperado:** `HTTP/1.1 204 No Content` — sin cuerpo de respuesta.

#### 12b. Verificar que no persiste datos

```bash
# Enviar un valor
curl -s -D - "http://127.0.0.1:5081/lab/collect?c=valor-secreto-123" -o /dev/null

# Consultar la base de datos para verificar que no se guardó
docker compose exec postgres psql -U minishop -d minishop \
  -c "SELECT * FROM \"Comments\" WHERE \"Body\" LIKE '%valor-secreto%';"
```

**Resultado esperado:**
- El primer comando devuelve `204 No Content`.
- El segundo comando devuelve `(0 rows)` — el valor no se guardó en ninguna tabla.

#### 12c. Verificar en Burp

1. En el navegador de Burp, visita: `http://127.0.0.1:5081/lab/collect?c=demo-cookie`
2. En HTTP history, busca el GET a `/lab/collect`.
3. Verifica que la respuesta es `204` y el cuerpo está vacío.

**Criterio PASS/FAIL:**
- **PASS:** `/lab/collect` responde 204, acepta un parámetro `c`, y no persiste el valor.
- **FAIL:** El endpoint no existe, devuelve otro código, o persiste datos.

**Captura de pantalla 12:** Burp HTTP history mostrando GET `/lab/collect` con respuesta 204.

---

## Parte 3 — Validar los Límites de Confianza (Trust Boundaries)

Los límites de confianza separan zonas donde los datos cambian de nivel de confianza. El DFD define 5 límites.

---

### Paso 13 — Límite 1: "Navegador → App (entrada no confiable)"

**El DFD afirma:** Todo lo que viene del navegador (formularios, parámetros, IDs, cookies, tokens) es entrada no confiable.

#### 13a. Demostrar que el servidor procesa entrada del usuario

```bash
# Enviar un parámetro de búsqueda — el servidor lo procesa
curl -s "http://127.0.0.1:5081/api/products/search?q=PRUEBA_DFD" | python3 -m json.tool
```

**Resultado esperado:** Un array JSON (probablemente vacío) — el servidor tomó `PRUEBA_DFD` como entrada y la procesó en una consulta.

#### 13b. Demostrar que la entrada cruza un límite de confianza

El hecho de que podamos enviar cualquier valor en `q=` y el servidor lo procesa demuestra que la entrada del navegador cruza hacia el servidor sin que el navegador tenga "autoridad" sobre qué se procesa — es el servidor quien decide. Este es el límite de confianza.

```bash
# Enviar caracteres especiales — el servidor los recibe
curl -s "http://127.0.0.1:5081/api/products/search?q=<script>alert(1)</script>" -o /dev/null -w "%{http_code}\n"
```

**Resultado esperado:** `200` — el servidor procesa la entrada incluso con caracteres potencialmente maliciosos, lo que confirma que todo input del navegador es no confiable.

**Criterio PASS/FAIL:**
- **PASS:** El servidor acepta y procesa entrada arbitraria del usuario, confirmando la necesidad del trust boundary.
- **FAIL:** El servidor rechaza toda entrada no estándar (lo cual no ocurre en este lab).

**Captura de pantalla 13:** Terminal o Burp mostrando que el servidor procesó entrada con caracteres especiales.

---

### Paso 14 — Límite 2: "Herramientas → App (solo loopback)"

**El DFD afirma:** Las herramientas de prueba solo apuntan a la URL loopback; no hay acceso externo.

#### 14a. Verificar que Docker solo expone en loopback

```bash
docker compose port shop 8080
```

**Resultado esperado:** `127.0.0.1:5081` — el puerto solo está ligado a la interfaz loopback.

#### 14b. Verificar con netstat/ss que no hay bind a 0.0.0.0

```bash
ss -tlnp | grep 5081
```

**Resultado esperado:** La línea muestra `127.0.0.1:5081`, NO `0.0.0.0:5081` ni `*:5081`.

#### 14c. Verificar el scope de Burp

En Burp → **Target** → **Scope settings**, confirma que solo hay una entrada: `http://127.0.0.1:5081`.

**Criterio PASS/FAIL:**
- **PASS:** El puerto está ligado solo a `127.0.0.1` y el scope de las herramientas se limita a loopback.
- **FAIL:** El puerto está expuesto en `0.0.0.0` o el scope incluye hosts externos. **Si falla, DETÉN el laboratorio inmediatamente.**

**Captura de pantalla 14:** Salida de `docker compose port` y `ss -tlnp` mostrando bind a 127.0.0.1 únicamente.

---

### Paso 15 — Límite 3: "App → PostgreSQL (conexión EF Core)"

**El DFD afirma:** La conexión entre la app y PostgreSQL cruza un límite de proceso/datos.

#### 15a. Verificar que son procesos separados (contenedores distintos)

```bash
docker compose ps --format "table {{.Name}}\t{{.Image}}\t{{.Status}}"
```

**Resultado esperado:** Dos contenedores distintos: uno para la app (.NET) y otro para PostgreSQL.

#### 15b. Verificar la red interna de Docker

```bash
docker network ls | grep mini
```

Luego inspeccionar la red:

```bash
docker network inspect $(docker network ls --filter "name=mini" -q) --format '{{range .Containers}}{{.Name}} {{.IPv4Address}}{{"\n"}}{{end}}'
```

**Resultado esperado:** Dos contenedores con IPs internas diferentes en la misma red Docker — la app se conecta a PostgreSQL a través de esta red interna.

#### 15c. Verificar que la búsqueda SQL Red Team también cruza este límite

Ya lo demostramos en el Paso 10 — la inyección `pg_sleep` demuestra que la app envía SQL a PostgreSQL y espera la respuesta, confirmando la comunicación entre procesos.

**Criterio PASS/FAIL:**
- **PASS:** App y PostgreSQL corren en contenedores separados, conectados por red Docker interna.
- **FAIL:** Corren en el mismo contenedor o no hay red interna.

**Captura de pantalla 15:** Salida de `docker compose ps` mostrando los dos contenedores separados.

---

### Paso 16 — Límite 4: "/lab/collect (flujo XSS cookie)"

**El DFD afirma:** Existe un límite especial: el contenido almacenado (XSS) hace que el navegador de la víctima envíe su cookie a `/lab/collect` — un flujo que cruza del navegador al servidor con datos robados.

#### 16a. Verificar que la cookie `LabSession` es accesible por JavaScript

```bash
# Login y obtener la cookie
curl -s -D - http://127.0.0.1:5081/Account/Login \
  -d "Username=cliente-xss&Password=LabXss2026!" 2>&1 | grep -i "set-cookie"
```

**Qué buscar en la respuesta:** La cookie `LabSession` **NO** debe tener el atributo `HttpOnly` (en modo Red Team está deshabilitado). Esto significa que JavaScript puede leerla con `document.cookie`.

#### 16b. Verificar que se puede invocar `/lab/collect` con la cookie

```bash
curl -s -D - "http://127.0.0.1:5081/lab/collect?c=LabSession%3Dvalor-simulado" -o /dev/null
```

**Resultado esperado:** `204 No Content` — el endpoint acepta el valor como si fuera una cookie robada.

#### 16c. Simular el flujo completo por Burp

1. En el navegador de Burp, inicia sesión como `cliente-xss`.
2. Abre la consola del navegador (`F12` → **Console**).
3. Escribe: `document.cookie` y presiona Enter.
4. Deberías ver `LabSession=<valor>` — confirma que JavaScript lee la cookie.
5. Ahora visita manualmente: `http://127.0.0.1:5081/lab/collect?c=` seguido del valor de la cookie.
6. En Burp HTTP history, verifica que la petición aparece con código 204.

**Criterio PASS/FAIL:**
- **PASS:** La cookie `LabSession` no tiene `HttpOnly`, JavaScript la lee, y `/lab/collect` la recibe con 204.
- **FAIL:** La cookie tiene `HttpOnly` (no estás en modo Red Team) o `/lab/collect` no responde 204.

**Captura de pantalla 16:** Consola del navegador mostrando `document.cookie` con la cookie `LabSession`, y HTTP history mostrando el GET a `/lab/collect` con 204.

---

### Paso 17 — Límite 5: "Contenedor → Host/Red (solo 127.0.0.1)"

**El DFD afirma:** El puerto publicado del contenedor solo está en `127.0.0.1`; no se publica en interfaces de red ni Internet.

#### 17a. Verificar el bind en compose.yaml

```bash
grep -A2 "ports:" compose.yaml
```

**Resultado esperado:** `"127.0.0.1:5080:8080"` (o en nuestro caso, el override tiene `"127.0.0.1:5081:8080"`).

Verificar también el override:

```bash
cat compose.override.yaml
```

**Resultado esperado:** `"127.0.0.1:5081:8080"`

#### 17b. Verificar con Docker

```bash
docker compose port shop 8080
```

**Resultado esperado:** `127.0.0.1:5081`

#### 17c. Verificar que no es accesible desde otra interfaz

```bash
# Obtener las interfaces de red
ip -4 addr show | grep "inet " | grep -v "127.0.0.1"
```

Si tienes una IP como `192.168.x.x` o `10.x.x.x`:

```bash
# Intentar acceder desde otra interfaz (debería fallar)
curl -s --connect-timeout 2 http://$(hostname -I | awk '{print $1}'):5081/Products -o /dev/null -w "%{http_code}\n" 2>&1 || echo "CONEXIÓN RECHAZADA (esperado)"
```

**Resultado esperado:** `000` o `CONEXIÓN RECHAZADA` — el puerto no es accesible desde interfaces que no son loopback.

**Criterio PASS/FAIL:**
- **PASS:** El puerto solo está ligado a `127.0.0.1` en la configuración de Compose y no es accesible desde otras interfaces.
- **FAIL:** El puerto está en `0.0.0.0` o es accesible desde una IP no-loopback. **Si falla, DETÉN el laboratorio y corrige compose.yaml.**

**Captura de pantalla 17:** Salida de `docker compose port shop 8080` mostrando `127.0.0.1:5081` y del grep en compose.yaml/compose.override.yaml.

---

## Resumen — Checklist de Validación

| # | Elemento del DFD | Tipo | Método de verificación | PASS / FAIL |
|---|---|---|---|---|
| 1 | U: Cliente, Vendedor, Admin | Caja (entidad) | Login con 3 roles vía API y Burp | |
| 2 | W: ASP.NET Core MVC + API | Caja (entidad) | HTML y JSON desde el mismo host, un solo proceso | |
| 3 | D: PostgreSQL con datos sintéticos | Caja (entidad) | 7 tablas, datos seed verificados | |
| 4 | P: Burp/ZAP herramientas | Caja (entidad) | Proxy activo, scope solo loopback | |
| 5 | L: Salida de contenedor | Caja (entidad) | `docker compose logs` muestra actividad | |
| 6 | U→W: HTTP, formulario, cookie, Bearer | Flecha (flujo) | Tres mecanismos verificados en Burp | |
| 7 | W→U: HTML/JSON, Set-Cookie | Flecha (flujo) | Respuestas con Content-Type y Set-Cookie | |
| 8 | P→W: HTTP local en scope | Flecha (flujo) | Petición vía proxy capturada en Burp | |
| 9 | W→D: EF Core/Npgsql (ORM) | Flecha (flujo) | Datos de PostgreSQL servidos vía API | |
| 10 | W→D: SQL Red Team, minishop_search | Flecha (flujo) | Rol existe, pg_sleep confirma SQL directo | |
| 11 | W→L: Logs de aplicación | Flecha (flujo) | Peticiones reflejadas en logs del contenedor | |
| 12 | U→W: Cookie robada → /lab/collect | Flecha (flujo) | Endpoint responde 204 sin persistir | |
| 13 | Límite: Navegador → App | Trust boundary | Servidor procesa entrada arbitraria | |
| 14 | Límite: Herramientas → App loopback | Trust boundary | Bind 127.0.0.1, scope Burp restringido | |
| 15 | Límite: App → PostgreSQL | Trust boundary | Contenedores separados, red Docker interna | |
| 16 | Límite: /lab/collect (XSS cookie) | Trust boundary | Cookie sin HttpOnly, JS la lee, /lab/collect acepta | |
| 17 | Límite: Contenedor → Host/Red | Trust boundary | Puerto solo en 127.0.0.1, no accesible externamente | |

---

## Organización de Evidencias

Crea una carpeta para guardar las capturas:

```bash
mkdir -p docs/fase1/evidencias/dfd-validacion
```

Guarda cada captura con el nombre del paso:

```
docs/fase1/evidencias/dfd-validacion/
├── paso-00-burp-setup.png
├── paso-01-tres-roles-login.png
├── paso-02-html-json-dual.png
├── paso-03-tablas-postgresql.png
├── paso-04-burp-scope.png
├── paso-05-logs-contenedor.png
├── paso-06-formulario-cookie-bearer.png
├── paso-07-set-cookie-response.png
├── paso-08-proxy-http-history.png
├── paso-09-orm-datos.png
├── paso-10-sql-directo-pgsleep.png
├── paso-11-logs-actividad.png
├── paso-12-lab-collect-204.png
├── paso-13-entrada-no-confiable.png
├── paso-14-loopback-bind.png
├── paso-15-contenedores-separados.png
├── paso-16-cookie-no-httponly.png
└── paso-17-bind-127001.png
```

---

## Notas Finales

- **Si algún paso falla:** Anota el resultado obtenido, toma la captura igualmente, y reporta al equipo. Es posible que los contenedores necesiten reconstruirse (`docker compose down -v && docker compose up --build -d`).
- **Si el puerto 5081 no responde:** Verifica con `docker compose ps` que los contenedores están corriendo. Si no, levántalos de nuevo.
- **No modifiques los datos ni el código** durante la validación — estamos verificando el estado actual tal como está.
- **Todos los datos son sintéticos y desechables.** No uses cuentas personales ni datos reales.
- Al terminar, estos resultados se usarán para el informe final y el video demostrativo.
