# Especificación de la mini-app laboratorio — Fase 1

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

## Propósito

Construir una tienda pequeña pero realista que permita recorrer operaciones normales y probar las cinco clases de vulnerabilidad de la consigna. La app debe ser propia, contener datos sintéticos y funcionar únicamente como laboratorio local.

## Stack fijado para planificación

- ASP.NET Core 10 MVC/Razor y API Controllers en el mismo host.
- Entity Framework Core y Npgsql.
- PostgreSQL en Docker Compose; usar un volumen/estado descartable para repetir las pruebas.
- GitHub/Git para historial y tag del hito.
- Burp Suite Community como proxy/manual Repeater; OWASP ZAP como herramienta complementaria; sqlmap solo para la prueba SQLi definida en su runbook.
- UI en español, Bootstrap local y JavaScript mínimo.

## Roles y recorridos funcionales

| Rol | Cuenta/contraseña seed local | Puede hacer en flujo normal | Prueba que habilita |
|---|---|---|---|
| Cliente A | `cliente-a` / `LabClienteA2026!` | Catálogo, comentario, carrito y pedido propio | Publica XSS; intenta IDOR contra Cliente B |
| Cliente B | `cliente-b` / `LabClienteB2026!` | Tiene carrito y pedido sintéticos | Víctima de lectura/edición BOLA |
| Víctima XSS | `cliente-xss` / `LabXss2026!` | Navega autenticado por MVC | Cookie de sesión robada y reproducida localmente |
| Cuenta brute force | `cliente-brute` / `LabPass2026!` | Login API normal | Enumeración y script de fuerza bruta de cinco candidatos |
| Vendedor | `vendedor-lab` / `LabSeller2026!` | CRUD de productos propios | Flujo de vendedor y alternativa para publicar XSS |
| Admin | `admin-lab` / `LabAdmin2026!` | Administración de datos sintéticos | Comparación de `/api/admin` normal y elevación JWT |

Permisos fijados para implementar: Vendedor no compra; administra solo sus productos (Admin administra todos). Cliente solo mantiene carrito/pedido propio. Admin puede listar usuarios/productos/pedidos sintéticos y realizar CRUD de productos, pero no modificar carritos/pedidos de clientes. La UI no es la barrera: las operaciones se prueban por HTTP directo.

## Pantallas mínimas

1. Login con mensajes de error observables para las pruebas de enumeración.
2. Catálogo, detalle, búsqueda y comentarios persistentes.
3. Carrito: listar, agregar, cambiar cantidad y retirar productos; permitir identificar los registros por ID en la solicitud.
4. Confirmación/pedidos: crear pedido sintético y consultar su detalle.
5. Panel Vendedor: CRUD de producto.
6. Panel Admin con una operación legible por `/api/admin`.

## Datos de laboratorio

- Usar las cuentas/contraseñas de la tabla de roles anterior; todas se crean solo por seed local y son ficticias.
- Mínimo dos productos del vendedor y productos de prueba para búsqueda.
- Un comentario persistente ligado a producto y autor.
- Un carrito y un pedido del Cliente B para comparar la autorización con el Cliente A.
- Tablas identificables para el ejercicio de metadatos SQLi, por ejemplo `Products`, `Comments`, `Carts`, `CartItems`, `Orders`, `Users`.
- No usar datos personales reales ni reusar credenciales personales. El seed debe ser determinista y reiniciable.

Las credenciales se incluyen en el seed del laboratorio; el README de entrega debe advertir que no son válidas fuera del entorno y no se deben cambiar por cuentas personales.

## Contrato de rutas fijado para implementar

Usar este contrato como referencia entre UI, API, DFD, STRIDE y runbooks. El equipo puede renombrarlo solo antes de iniciar implementación; cualquier cambio se actualiza en todos los documentos.

| Uso | Ruta | Requisito funcional de la versión de laboratorio |
|---|---|---|
| Login MVC/cookie | `POST /Account/Login` | Cookie `LabSession` válida para cuentas ficticias, emitida con `HttpOnly=false`, `Secure=false` para HTTP local y `SameSite=Lax`; mensaje deliberadamente distinto entre usuario inexistente y password incorrecto; sin rate limit en perfil Red Team |
| Login API/JWT | `POST /api/auth/login` | JSON `{ "username": "...", "password": "..." }`; responde token JWT HS256 firmado con `MiniShopLab2026` en el perfil Red Team |
| Buscar productos | `GET /api/products/search?q=` | Búsqueda vulnerable deliberadamente a SQLi blind time-based; consulta solo el schema de laboratorio |
| Ver/crear comentarios | `GET/POST /api/products/{id}/comments` | Guardar comentario que vuelve a la vista sin codificación contextual en el perfil Red Team |
| Captura de cookie XSS | `GET /lab/collect?c=` | Solo en perfil Red Team; responde 204 y no persiste el valor; Burp muestra la solicitud entrante |
| Perfil de usuario MVC | `GET /Account/Profile` | Devuelve el usuario autenticado por `LabSession`; permite evidenciar el replay local de la cookie robada |
| Productos | `GET /api/products`, `POST /api/products`, `PUT/DELETE /api/products/{id}` | CRUD de Vendedor/Admin con identidad y ownership de prueba |
| Carrito | `GET /api/cart/{cartId}`, `PUT /api/cart/{cartId}/items/{itemId}` | Identificador manipulable para verificar lectura y edición IDOR/BOLA |
| Pedido | `GET /api/orders/{orderId}`, `PUT /api/orders/{orderId}` | Recurso sintético de Cliente B para probar ownership de lectura/escritura |
| Admin | `GET /api/admin` | Endpoint protegido en el flujo normal; prueba directa con Cliente y JWT forjado |

No mantener dos contratos distintos en UI y API. El login MVC usa cookie; el login API emite JWT para aislar claramente los dos escenarios exigidos (cookie XSS y JWT). Al implementar, mantener ambos solo con identidades ficticias.

### Comportamiento vulnerable que debe construir el equipo

- `GET /api/products/search?q=` concatena/interpola `q` en una consulta PostgreSQL de lectura y permite una señal time-based verificable con `pg_sleep`; el endpoint/rol de búsqueda no debe poder modificar filas. Para aislarlo, usar un principal `minishop_search` con `SELECT` de catálogo y acceso de lectura a metadatos necesarios.
- En la vista de comentarios, solo bajo `LAB_MODE=redteam`, omitir codificación contextual para que el XSS persistente se ejecute. Otras vistas permanecen normales.
- El comentario lo publica `cliente-a` o `vendedor-lab`; la víctima `cliente-xss` lo abre con una cookie MVC `LabSession` que no tiene `HttpOnly`. La carga envía la cookie a `/lab/collect`, que responde 204 sin persistirla. El replay a `/Account/Profile` muestra `cliente-xss`.
- IDOR/BOLA permite que Cliente A lea y cambie cantidad del carrito/pedido de Cliente B por ID; el mismo Cliente puede llamar `/api/admin` con cookie MVC y obtener el resultado indebido planificado. Un Bearer JWT de Cliente conserva el rechazo 403 en esa ruta, para contrastarlo con la elevación JWT del escenario separado.
- Login diferencia cuenta inexistente de password incorrecta y no tiene rate limit solo en modo Red. La cuenta `cliente-brute` usa la contraseña seed de la tabla de cuentas.
- El JWT de `cliente-a` se firma con HS256 y el secreto de laboratorio `MiniShopLab2026`; cambiar el claim de rol a Admin permite acceder a `/api/admin` en Red.

## Perfil Red Team y aislamiento

- `LAB_MODE=redteam` debe ser explícito; si falta la variable, la app no debe habilitar comportamientos deliberadamente vulnerables.
- El despliegue vulnerable solo se acepta en Docker local y bind `127.0.0.1`. El equipo debe verificar el mapeo de puertos antes de lanzar Burp, ZAP o sqlmap.
- La configuración de laboratorio puede relajar validaciones solo en las superficies identificadas. No debe incluir secretos reales, credenciales de terceros ni infraestructura externa.
- Generar una seed limpia y documentar un comando de reset para que la grabación y las pruebas partan del mismo estado.
- Para el XSS, el atacante sintético (`cliente-a` o `vendedor-lab`) guarda el comentario y la víctima sintética `cliente-xss` lo abre con cookie de sesión válida. La carga captura la cookie y la envía a `/lab/collect`; luego se reproduce contra `GET /Account/Profile` en el mismo host. Nunca usar una sesión personal.
- Para JWT, la clave HS256 débil se configura mediante una variable local del perfil Red Team y es desechable. No se guarda una clave real en GitHub.
- `LabSession` se configura deliberadamente sin `HttpOnly` en Fase 1 para que el XSS pueda leerla. Este es un requisito vulnerable del laboratorio y debe desaparecer/cambiar en el perfil seguro de Fase 2.
- Guardar hash/ID del commit probado junto a las capturas. Crear `fase1-redteam` después de revisión cruzada.

## Requisitos de aceptación funcional

- Otra persona puede clonar, levantar con Docker y completar el README sin pasos secretos.
- Los tres roles pueden iniciar sesión y la app permite los recorridos normales definidos.
- Los comentarios, carrito y pedidos se persisten y se ven desde la pantalla/API apropiada.
- Las cuentas A y B tienen recursos distintos y verificables.
- Los cinco runbooks apuntan a rutas realmente existentes y cada prueba tiene una señal observable.
- El DFD y STRIDE representan la app construida, no una arquitectura hipotética anterior.
