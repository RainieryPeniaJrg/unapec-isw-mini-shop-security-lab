# Data Flow Diagram (DFD) — Fase 1

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

**Estado:** modelo del alcance implementado para Fase 1; los resultados de ejecución se registran por separado. Redis, Vault y refresh tokens pertenecen a la Fase 2 y no forman parte de este diagrama.

```mermaid
flowchart LR
  U["Cliente, Vendedor o Admin en navegador"] -->|"HTTP, formulario, cookie o Bearer token"| W["ASP.NET Core MVC/Razor + API"]
  W -->|"HTML/JSON, cookie de sesión"| U
  P["Burp/ZAP y herramientas de prueba"] -->|"HTTP local dentro del scope"| W
  W -->|"EF Core/Npgsql"| D[("PostgreSQL con datos sintéticos")]
  W -->|"SQL de búsqueda Red Team, rol minishop_search de solo lectura"| D
  W -->|"Logs de aplicación"| L["Salida de contenedor"]
  U -->|"Cookie de sesión robada: GET /lab/collect"| W
  B1["Límite: navegador y app"] -.-> W
  B2["Límite: app y base de datos"] -.-> D
  B3["Límite: herramientas y app local"] -.-> P
```

## Trust boundaries

1. **Navegador → app:** formularios, parámetros, IDs, cookies y tokens son entrada no confiable. La interfaz no constituye autorización.
2. **Herramientas → app:** Burp, ZAP y sqlmap solo apuntan a la URL loopback acordada; los datos/cookies usados son de prueba.
3. **App → PostgreSQL:** conexión y consultas cruzan el límite de proceso/datos; para el escenario vulnerable se introduce solo el fallo planificado en búsqueda. El resto del seed es sintético.
4. **Navegador → app (`/lab/collect`):** flujo especial de evidencia XSS; el contenido almacenado hace que el navegador envíe la cookie de sesión del usuario ficticio a un endpoint del mismo contenedor.
5. **Contenedor → host/red:** puerto publicado solo en `127.0.0.1`; no publicar en interfaces de red ni Internet.

## Assets and entry points

**Activos:** credenciales sintéticas, identidad/roles, catálogo/precios, comentarios, carrito/pedidos, cookie de sesión del usuario ficticio, JWT de laboratorio, disponibilidad e integridad de registros.

**Puntos de entrada:** `/Account/Login`, `/Account/Profile`, `/Products/*`, `/Cart/*`, `/Seller/*`, `/Admin/*`; API `/api/auth/login`, `/api/products/search`, `/api/products/{id}/comments`, `/api/cart/{cartId}`, `/api/orders/{orderId}`, `/api/admin` y `/lab/collect`. MVC usa la cookie `LabSession`; la API admite los esquemas de autenticación previstos por cada operación.

**Flujos:** login devuelve credencial de sesión/token; búsqueda consulta PostgreSQL; comentario se persiste y vuelve a la vista; operaciones de carrito/pedido leen/escriben recursos por ID; herramientas de prueba observan requests/responses locales. En Red Team, `/api/admin` permite el bypass con cookie MVC de Cliente; con JWT exige Admin, cuya firma HS256 débil se demuestra en un escenario separado. `/lab/collect` responde 204 sin persistir la cookie recibida.
