# Plan maestro — Proyecto Integrador Avanzado ISW

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

**Título:** Ciclo Completo de Red Team + Blue Team en una Aplicación Real
**Estado:** desarrollo de la aplicación autorizado hasta Fase 1. La Fase 2 permanece fuera del alcance actual. El cierre académico requiere además evidencia manual, informe CVSS y video.
**Base:** consigna completa compartida por el estudiante.
**Equipo previsto:** 3 integrantes.
**Stack principal elegido:** .NET.

Este plan organiza alcance, stack, especificaciones, fases, herramientas, responsabilidades y criterios de salida. Los documentos complementarios son la [especificación funcional/técnica](especificacion-proyecto.md), la [matriz de entregables](matriz-entregables.md), el [plan por fases/equipo](plan-trabajo.md) y el [DFD/STRIDE inicial](../threat-model/dfd.md).

El paquete operativo de preparación para el segundo parcial (requisitos del laboratorio, selección de herramientas, procedimiento de pentest y runbooks por ataque) está en [`fase1/README.md`](fase1/README.md).

## Prioridad inmediata: hito del segundo parcial

La primera meta del equipo es completar **la Fase 1 (40%)**, incluido todo lo necesario para que sus ataques se ejecuten sobre una aplicación propia y funcional. Esto sí incluye construir la mini-app, la interfaz, login y los tres roles, productos y carrito; son prerrequisitos para demostrar las vulnerabilidades de la consigna. También incluye Docker/Git, threat modeling, los cinco PoC, CVSS v3.1, evidencia y video.

La Fase 2 permanece especificada como continuidad, pero **no forma parte del trabajo inmediato**: no se implementan aún bcrypt cost 12, Redis rate limiting, lockout, TOTP, JWT RS256/refresh, correcciones Blue Team ni gates finales. En el hito 1 se conserva la versión vulnerable en un tag/commit local aislado. Los controles seguros se describen como objetivo futuro y no deben presentarse como ya implementados.

**Gate del segundo parcial:** una persona del equipo puede clonar el repositorio, levantar el laboratorio con Docker siguiendo el README y reproducir cada ataque de manera controlada; el paquete de entrega enlaza cada amenaza, PoC, evidencia, impacto y CVSS con el commit/tag probado.

## 1. Resumen de la solución

Desarrollar una mini tienda web propia con roles **Cliente, Admin y Vendedor**, login, CRUD de productos y carrito. Se construye primero una versión funcional; la Fase 1 conserva un snapshot vulnerable para practicar los cinco ataques exigidos en un entorno local con datos ficticios; la Fase 2 corrige esos defectos y demuestra los controles con pruebas y CI.

La entrega final en la rama principal debe ser la versión segura. La versión vulnerable se preserva como tag/commit para reproducir la evaluación de Fase 1. El perfil vulnerable se limita a Docker local enlazado a loopback y no se publica.

**Compatibilidad multi-OS:** el equipo puede editar y revisar el código desde Windows o Linux. Para el hito se fija una imagen Linux como baseline de Docker/CI y Debian 13 XFCE como VM canónica para ejecutar target y pentest. No se mezclan Windows containers con Linux containers en la demostración de 16 GB; un perfil de Windows containers solo se agregaría si la consigna lo exige expresamente y se valida con recursos separados.

## 2. Decisiones de stack y herramientas

| Área | Decisión para el plan | Uso previsto / motivo |
|---|---|---|
| Runtime | **.NET 10 LTS** | ASP.NET Core, C#, APIs y vistas; versión LTS con soporte previsto hasta noviembre de 2028. |
| Frontend | **ASP.NET Core MVC + Razor Views**, Bootstrap servido localmente y JavaScript mínimo | Interfaz real en el mismo host .NET; formularios HTTP sencillos para Burp/ZAP; Razor codifica expresiones por defecto y permite explicar claramente la corrección XSS. En el laboratorio, el renderizado inseguro queda en una vista aislada. |
| Backend | **ASP.NET Core MVC Controllers + API Controllers** | Separación entre páginas y endpoints `/api/*` sin añadir una SPA y CORS a este alcance. |
| ORM | **Entity Framework Core** | Acceso normal a datos, LINQ y queries parametrizadas; SQL raw solo como caso vulnerable controlado y nunca en la versión final. |
| Base de datos | **PostgreSQL 18** mediante **Npgsql EF Core** | Persistencia de usuarios/productos/carritos/pedidos y apoyo a la demostración time-based en base sintética. |
| Estado distribuido | **Redis** mediante **StackExchange.Redis** | Rate limit distribuido para login y contadores/bloqueos de prueba. |
| Hash de contraseñas | **BCrypt.Net-Next**, cost 12 | Cumple alternativa bcrypt indicada; costo y latencia se verifican en el entorno. No almacenar contraseña en claro. |
| TOTP | **Otp.NET** | Implementación TOTP RFC 6238 en C# para el segundo factor. |
| Autenticación web/API | **Cookie de sesión para MVC + JWT Bearer para `/api/*`** | La consigna pide una session cookie para XSS y JWT para elevación de privilegios; se evalúan como credenciales distintas. Un usuario/rol común, políticas separadas. |
| JWT final | **RS256** con `Microsoft.AspNetCore.Authentication.JwtBearer` | Firma con clave privada protegida; expiración de 15 minutos; refresh token rotativo persistido como hash en PostgreSQL. |
| Gestión de secretos | **HashiCorp Vault Community** para secretos del laboratorio; **GitHub Secrets** para CI; `.NET User Secrets` para desarrollo local | El modo dev de Vault solo se usa en la red local efímera para la clase; jamás como configuración de producción. Ningún `.env`, token, llave o contraseña real se sube a Git. |
| Repositorio/CI | **GitHub público + GitHub Actions** | La entrega pide específicamente `.github/workflows/security.yml`; el repositorio público contiene un laboratorio educativo deliberadamente vulnerable. No subir secretos/datos reales ni desplegar el perfil Red Team. |
| SAST | **SonarQube Cloud** como opción principal | La consigna admite SonarQube o Semgrep, pero pide captura de SonarQube; Cloud permite revisar findings y quality gate desde GitHub Actions. El plan exige corregir y evidenciar al menos dos issues High reales. |
| SCA | **NuGet Audit** (`dotnet list package --vulnerable --include-transitive`) + **Trivy filesystem** | Auditoría apropiada a .NET y dependencias transitivas; no aplicar `npm audit` ni `safety` a un backend .NET. |
| DAST automatizado | **OWASP ZAP Baseline GitHub Action** | Escaneo pasivo/baseline del contenedor efímero y publicación del reporte como artifact. |
| DAST manual | **Burp Suite Community** | Proxy/Reapter para capturar solicitudes, repetir PoCs y grabar el video de Fase 1. |
| SQLi PoC | **sqlmap**, con perfil manual time-based como alternativa de demostración | Solo contra el contenedor local y datos sintéticos; prueba de extracción del nombre de BD y una tabla. |
| Fuerza bruta PoC | **Script pequeño de laboratorio** limitado a loopback | La consigna permite Hydra o script; el endpoint JSON se prueba con límites bajos y cuentas desechables. Hydra queda como alternativa si el formulario resulta compatible. |
| CVSS | **CVSS v3.1 Calculator de FIRST** + justificación escrita | Cada hallazgo recibe vector, puntaje y racional según impacto y precondiciones observados. |
| Pruebas | **xUnit + ASP.NET Core integration testing** | Smoke, CRUD, roles/ownership, login, regresión de cada PoC y respuesta a headers/cookies. |
| Contenedores/escaneo | **Docker Compose + Trivy** | Multi-stage, proceso no-root; scan del filesystem y de la imagen final; gate final en CRITICAL. |

Las versiones exactas de paquetes/Actions se fijan al iniciar implementación, se verifican por compatibilidad con .NET 10 y se guardan en lockfile/commit. Para GitHub Actions, fijar Actions por tag estable al inicio y por SHA revisado si el criterio del curso lo requiere.

### Referencias primarias que respaldan las decisiones

- [Soporte de .NET](https://learn.microsoft.com/dotnet/core/releases-and-support)
- [ASP.NET Core MVC y Razor](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-10.0)
- [Prevención de XSS en ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0)
- [Proveedor EF Core/Npgsql](https://www.npgsql.org/efcore/)
- [PostgreSQL `pg_sleep`](https://www.postgresql.org/docs/current/functions-datetime.html)
- [SonarQube Cloud + GitHub](https://docs.sonarsource.com/sonarqube-cloud/managing-your-projects/administering-your-projects/devops-platform-integration/github)
- [ZAP Baseline Action](https://github.com/marketplace/actions/zap-baseline-scan)
- [Otp.NET](https://www.nuget.org/packages/Otp.NET) · [BCrypt.Net-Next](https://www.nuget.org/packages/BCrypt.Net-Next) · [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis)
- [HashiCorp Vault dev mode — solo experimentación](https://developer.hashicorp.com/vault/docs/concepts/dev-server)

## 3. Arquitectura funcional propuesta

### Componentes

1. **Navegador:** vistas de login, catálogo, detalle/comentarios, carrito, Vendedor y Admin.
2. **ASP.NET Core MVC/API:** páginas Razor, validaciones, middleware/policies y endpoints REST.
3. **PostgreSQL:** usuarios/roles, productos, comentarios, carritos, pedidos, refresh tokens y auditoría.
4. **Redis:** contadores de rate limit y estado temporal de bloqueo.
5. **Vault:** credenciales/claves de firma para el entorno local; GitHub Secrets para el workflow.
6. **GitHub Actions:** build/tests → SAST/SCA → Docker/Trivy → Compose temporal → ZAP/reportes.

### Vistas/pantallas

- `/Account/Login`: usuario/contraseña y segunda pantalla TOTP en la versión segura.
- `/Products`: listado, búsqueda, precio, detalle.
- `/Products/Details/{id}`: descripción, comentarios y agregar al carrito.
- `/Cart`: cantidades, eliminación, subtotal y checkout.
- `/Seller/Products`: CRUD de productos propios.
- `/Admin`: usuarios/productos/pedidos según permisos definidos.
- Vistas de error/acceso denegado y estado de bloqueo.

La interfaz orienta al usuario ocultando acciones que no le corresponden, pero toda decisión se vuelve a comprobar en el servidor. La aplicación debe incluir APIs directas además de vistas para que Burp pueda verificar autorización independientemente de los botones.

### Contrato de endpoints a diseñar antes de codificar

| Grupo | Endpoints candidatos | Regla esperada en versión segura |
|---|---|---|
| Auth | `POST /Account/Login`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /Account/Logout`, `POST /api/auth/totp/*` | Cookie MVC y JWT API separados; la versión segura añade mensajes uniformes, límite, lockout y MFA. |
| Catálogo | `GET /api/products`, `GET /api/products/{id}`, `GET /api/products/search?q=` | Público de solo lectura; parámetros/filtros validados. |
| Productos | `POST/PUT/DELETE /api/products/{id}` | Vendedor dueño del registro o Admin; validación whitelist. |
| Comentarios | `GET/POST /api/products/{id}/comments` | Lectura pública según política; escritura autenticada; renderizado seguro. |
| Carrito | `GET /api/cart/{cartId}`, `PUT /api/cart/{cartId}/items/{itemId}` | Versión segura valida ownership en cada operación; contrato de laboratorio preparado para cambiar ID y comparar Cliente A/B. |
| Pedidos | `POST /api/orders`, `GET /api/orders/me`, `GET /api/orders/{id}` | Dueño del pedido o permiso Admin explícito. |
| Admin | `GET /api/admin/*` y mutaciones administrativas aprobadas | Solo Admin; autorización en cada endpoint/método. |

El contrato de trabajo para el hito Fase 1 está fijado en [`fase1/especificacion-laboratorio.md`](fase1/especificacion-laboratorio.md); los nombres y códigos se mantienen consistentes entre app, runbooks y DFD.

### Modelo de datos mínimo

`User`, `Role` (enum/claim normalizado), `Product`, `Comment`, `Cart`, `CartItem`, `Order`, `OrderItem`, `RefreshToken`, `AuditEvent`. Índices: username/email único; owner/product en carrito; owner/createdAt en pedidos; token hash único. Precios se guardan en unidad monetaria entera (centavos) para evitar errores de punto flotante.

## 4. Separación de perfiles y repositorio

- **Perfil Red Team:** versión Fase 1, secretos/datos de laboratorio, bind `127.0.0.1`, cuentas desechables y endpoint XSS `/lab/collect` en el mismo host. Los endpoints vulnerables conservan las rutas de aplicación que exige el caso (`/api/admin`, carrito/pedido y búsqueda), dentro de ese snapshot aislado.
- **Perfil Blue Team:** corrección de las mismas rutas, configuración segura por defecto y sin endpoint/código de demostración `/lab/collect` habilitado.
- No mezclar PoCs con sistemas externos; registrar host objetivo en el guion y limitar herramientas a `localhost`/Docker.
- `main` conserva el desarrollo integrado. En el hito Fase 1, crear tag inmutable `fase1-redteam`; luego commits de remediación y tag `fase2-blueteam`. Para screenshots, poner commit/tag y rama en el nombre del artifact.
- El repo solicitado será público; mantener cuentas/datos sintéticos y documentar que el modo Red Team no se despliega ni se expone fuera de loopback.

## 5. Plan por fases y gates

### H0 — Planificación y cierre de especificaciones (antes de construir)

**Trabajo:** cerrar permisos/flows; contrato de endpoints; entidades; arquitectura DFD; alcance de pedidos; decisiones de cookie+JWT; configurar responsabilidad por PoC; verificar con docente SonarQube Cloud frente a la captura pedida.

**Salida H0:** spec aprobada por el equipo, matriz de entregables, permisos, contrato API, DFD borrador y riesgos conocidos. No se inicia Red Team hasta fijar un flujo funcional para cada rol.

### H1 — Aplicación funcional y entorno de laboratorio (hito 2.º parcial)

**Trabajo:** MVC/Razor UI, login base, catálogo, CRUD productos, comentarios, carrito, checkout/pedido, PostgreSQL/EF Core, Docker Compose, GitHub repo y smoke/integration tests.

**Salida H1:** Cliente compra, Vendedor administra sus productos y Admin accede a sus funciones desde navegador. Otra persona puede levantar el ambiente desde README. Sin pagos reales ni información real.

### H2 — Threat Modeling (paralelo con el diseño y el MVP)

**Trabajo:** DFD con trust boundaries; inventario de activos/entradas; STRIDE inicial de 8–10 amenazas; enlazar riesgo→componente→PoC/control. Revisar autorización y datos de prueba.

**Salida H2:** DFD revisado, amenaza/control/test trazables; no usar FODA.

### H3 — Red Team / Fase 1 (cierre del hito 2.º parcial)

**Orden propuesto:** confirmar reset reproducible → ejecutar/capturar SQLi → XSS → IDOR/BOLA → autenticación débil → JWT → CVSS → video → congelar tag.

**Salida H3:** cinco PoCs reproducibles en la app local, evidencia Burp/ZAP, CVSS v3.1, informe y video. Tag `fase1-redteam`. Cualquier intento contra fuera de localhost queda fuera de alcance.

**Fuera del hito actual:** H4, H5 y H6 (remediación Blue Team, pipeline final y defensa final). El workflow que ya existe en el scaffold es preliminar y no equivale al pipeline endurecido exigido para la Fase 2.

### H4 — Secure Coding / Blue Team (2 semanas estimadas)

**Orden:** parametrizar/whitelist; corregir XSS y cookies/CSP; corregir ownership/RBAC/ABAC; robustecer login/2FA/lockout; endurecer JWT y refresh; migrar secretos a Vault/secret store. Cada fix incluye test de regresión y diff.

**Salida H4:** cada PoC de H3 falla en la versión corregida; tests explican por qué. Commit/tag vulnerable sigue disponible para comparación.

### H5 — DevSecOps final (1–1.5 semanas estimadas)

**Pipeline ordenado:** restore/build/test → SonarQube Cloud quality gate → NuGet Audit y Trivy filesystem → Docker build multi-stage/no-root → Trivy imagen (CRITICAL bloquea) → Compose efímero → ZAP Baseline → artifacts.

**Salida H5:** `.github/workflows/security.yml` corre en PR/push; reportes disponibles; captura Sonar y ZAP en verde. Una rama de demostración que reintroduce un fallo comprobable produce un run rojo, y revertirlo devuelve verde.

### H6 — Informe final/defensa (3–4 días estimados)

**Trabajo:** diff Antes/Después; carta para gerencia; lecciones aprendidas; matriz ASVS L2 aplicable/implementado/testeado/pendiente; ensayo de demo Red vs Blue.

**Salida H6:** enlace repo, workflow, capturas, informe, video y demo en vivo con resultados reproducibles.

**Estimación calendario global:** 7–9 semanas de trabajo con solapamiento. Es una base de planificación; al conocer la fecha real se ajusta duración, no se eliminan gates de entrega.

## 6. Especificación de PoCs y remediaciones

| Hallazgo | Estado Red Team que se busca evidenciar | Control Blue Team | Regresión/gate esperado |
|---|---|---|---|
| SQLi blind time-based | Búsqueda concatenada expone señales de tiempo; inferencia de nombre de BD y nombre de una tabla sintética. | ORM/parametrización total, whitelist de campos y privilegios DB mínimos. | Request malicioso tratado como dato; no hay retardo condicional ni inferencia; test por integración. |
| XSS almacenado/cookie | Comentario/descripcion persiste y ejecuta en vista; cookie de sesión válida de `cliente-xss` se captura en `/lab/collect` y se reproduce solo en la app local. | Razor contextual encoding, CSP, cookie `HttpOnly`, `Secure`, `SameSite`; no render de HTML sin sanitización. | Payload aparece como texto/no ejecuta; JS no lee cookie; test de salida/header. |
| IDOR/BOLA + Admin | Cliente A cambia IDs para leer/modificar recurso B y prueba `/api/admin`. | RBAC en policy/middleware más ABAC owner check por objeto y acción. | Cliente A recibe 403/404 y no cambia fila; rol no-admin no llega a `/api/admin`. |
| Fuerza bruta/enumeración | Respuestas diferenciales para usuario inexistente/válido y múltiples fallos sin limitación en el perfil Red. | BCrypt cost 12, respuesta uniforme, Redis rate limit, lockout tras cinco fallos, TOTP. | Sexto intento entra en bloqueo según regla y usuarios no se enumeran por respuesta. |
| JWT débil → Admin | En lab, token con clave débil o validación defectuosa permite claim/rol Admin. | RS256 con algoritmo fijado, emisor/audiencia, expiración 15 min, refresh rotativo. | `none`, firma inválida, rol alterado, token expirado y refresh reusado se rechazan. |

La consigna permite `none` o secreto débil. **Plan recomendado:** elegir secreto débil si el middleware .NET impide `none` de forma segura por defecto; no escribir un validador JWT casero. La forma exacta se valida en un spike de diseño antes de Fase 1. La remediación final siempre fija RS256.

## 7. Plan de trabajo para tres personas

Los identificadores A/B/C son perfiles, no nombres ni asignaciones actuales.

| Persona | Dominio primario | Fase 1 | Fase 2 | Revisión requerida |
|---|---|---|---|---|
| **A — UI/experiencia/XSS** | MVC/Razor, formularios, navegación, comentarios | Flujos de roles, catálogo/carrito UI, XSS persistente de vista y prueba en navegador. | Encoding, CSP/cookies, revisión de errores/estados UI, prueba visual Antes/Después. | B valida contrato API; C reproduce XSS y revisa headers/cookies. |
| **B — Dominio/API/datos** | EF Core, PostgreSQL, productos/carrito/pedidos | CRUD y rutas de negocio; PoC SQLi y IDOR/BOLA en carrito/pedido. | ORM/whitelist y checks owner/role por endpoint; regression tests de SQLi/BOLA. | A prueba journeys completos; C valida STRIDE/gate. |
| **C — Auth/plataforma/CI** | login, JWT, Docker, Actions, reportes | Enumeración/fuerza bruta, JWT débil; Compose y pipeline base; consolida CI/evidencia. | BCrypt/TOTP/Redis lockout/RS256-refresh; Sonar, SCA, ZAP, Trivy y no-root/multistage. | B revisa auth/API; A prueba UI/login; ambas personas verifican su evidencia. |

### Actividades que se hacen entre los tres

- Firmar decisiones de roles, modelos, contrato API, DFD y límites del laboratorio.
- Revisar una amenaza y un caso de prueba de cada integrante.
- Calcular/validar CVSS en pareja; el dueño aporta el impacto observado y el revisor verifica vector.
- Revisar pull requests por rotación; nadie se aprueba su propio cambio.
- Preparar cada persona un bloque de video/defensa; entregar un solo informe coherente.
- La función de “integración” de C no reemplaza el trabajo de A/B en pipeline: A valida rutas web en ZAP y B mantiene SCA/tests de backend.

## 8. Estrategia Git y trabajo coordinado

1. Rama principal `main`; ramas cortas `feature/<area>` y `fix/<finding>`.
2. Cada PR debe indicar requisito `FR/RT/F2`, archivos tocados, test/evidencia y riesgo de seguridad.
3. Un revisor distinto valida cada PR; los cambios a Auth, ownership, consultas o perfil Red requieren prueba adjunta.
4. Integrar temprano: primero contrato de endpoints, estructura de datos y Compose; después UI y API trabajan contra ese contrato.
5. Tag `fase1-redteam` antes de cambios defensivos; conservarlo. No reescribir historia para ocultar el antes/después.
6. Cada hallazgo se corrige en PR propio y se enlaza a test. Ejemplos: `fix/sql-injection`, `fix/xss`, `fix/idor`, `fix/auth`, `fix/jwt`.
7. `main` al final contiene seguridad aplicada y pipeline bloqueante; tag `fase2-blueteam` referencia entrega final.

## 9. Estructura propuesta de documentos/evidencia

```text
docs/
  plan-maestro.md
  especificacion-proyecto.md
  matriz-entregables.md
  plan-trabajo.md
  reports/
    fase1-findings-cvss.md
    blue-team-final.md
    remediation-letter.md
  evidence/
    fase1/burp-zap/     # capturas redactadas y nombradas por RT-ID
    fase2/              # reportes de CI, Sonar, ZAP, Trivy
  demo/
    script-defensa.md
.github/workflows/
  security.yml
threat-model/
  dfd.md
  stride.md
```

No versionar cookies/tokens reales, contraseñas, screenshots con información personal o secretos de Vault. Usar solo capturas ficticias del laboratorio y reportes sin valores sensibles.

## 10. Criterios de cierre por rúbrica

| Rubro final | Peso | Gate de salida |
|---|---:|---|
| Pentesting real con Burp | 25% | Cinco PoCs y video sobre tag Red con evidencia legible. |
| Threat Model + CVSS | 15% | DFD, activos/entradas/trust boundaries, 8+ amenazas STRIDE, CVSS v3.1 por hallazgo. |
| Secure Coding avanzado | 30% | Cada control listado implementado y comprobado por test; diff Antes/Después. |
| DevSecOps Pipeline | 20% | Workflow en verde, SAST/SCA/DAST, Trivy CRITICAL=0, contenedor no-root/multistage y captura. |
| Documentación y Defensa | 10% | Carta, lecciones, ASVS L2 trazable, demo de PoC fallido y pipeline rojo ante regresión. |

## 11. Riesgos, decisiones y dependencias

| Riesgo/decisión | Tratamiento de planificación |
|---|---|
| SonarQube o Semgrep en requisito, pero captura de SonarQube solicitada | Se elige SonarQube Cloud como camino base y se pide confirmación docente. Sin esa confirmación, mantener Semgrep como alternativa documentada, no correr dos SAST sin necesidad. |
| `JWT none` puede ser rechazado por middleware seguro | Elegir secreto débil como vulnerabilidad JWT permitida si `none` requiere código de validación inseguro propio; validar spike sin construir JWT casero. |
| SQLite dificulta time-based | PostgreSQL se fija desde el diseño; no mantener SQLite en los docs de arquitectura. |
| DAST Baseline no prueba todos los flujos autenticados ni lógica de ownership | Mantener Burp manual y tests autenticados de regresión; ZAP no se presenta como cobertura total. |
| Interfaz puede ocultar privilegios sin proteger API | Cada endpoint debe probarse directamente con cookies/tokens de rol y owner distintos. |
| Vault dev server es inseguro por definición | Solo red local efímera; usarlo para demostrar lectura de secretos. CI utiliza GitHub Secrets; ningún dev-mode server se presenta como production-ready. |
| Fecha de segundo parcial/final desconocida | Se deja secuencia relativa de 7–9 semanas; después se alinea a calendario académico. |
| Código vulnerable en repositorio público | Aviso claro de laboratorio, datos desechables, perfil loopback-only, no despliegue del modo Red Team y revisión de secretos antes de cada push. |

## 12. Decisiones a cerrar en revisión de especificación

La arquitectura, permisos, contrato y herramientas de Fase 1 quedan decididos en el plan y en [`fase1/especificacion-laboratorio.md`](fase1/especificacion-laboratorio.md). Solo falta recopilar estos datos externos al diseño:

1. Fijar fecha de Fase 1 y final para convertir H0–H6 en calendario.
2. Confirmar con el docente que SonarQube Cloud cumple la captura solicitada para la Fase 2.
3. Conocer los integrantes reales y el repo de GitHub antes de aplicar nombres o crear issues/ramas.

El estado actual del directorio es un scaffold .NET API-only. Este plan fija cómo completar las especificaciones; el frontend, los flujos funcionales y las vulnerabilidades no se consideran implementados por el hecho de estar descritos aquí.
