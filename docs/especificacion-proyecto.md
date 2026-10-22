# Especificación de planificación — Mini tienda segura

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

> **Prioridad actual:** diseñar y construir lo necesario para la Fase 1 / segundo parcial (40%). Esto incluye la mini-app funcional completa y el laboratorio local con los cinco escenarios de ataque, threat model, CVSS, evidencia y video. Los controles de remediación y entregables Blue Team de Fase 2 se especifican para continuidad, pero no son parte de la implementación inicial.

**Curso:** UNAPEC — Seguridad de Software (ISW), Proyecto Integrador Avanzado
**Estado:** propuesta para revisar antes de implementar
**Stack acordado por el equipo:** .NET. Las decisiones de frontend y arquitectura que siguen son recomendaciones para aprobación.

El plan consolidado con decisiones de herramientas está en [plan-maestro.md](plan-maestro.md). Para comprobar que cada punto de la consigna tiene salida y evidencia, consultar la [matriz de entregables](matriz-entregables.md); la secuencia y reparto propuesto están en [plan-trabajo.md](plan-trabajo.md).

Las instrucciones específicas para construir y probar el laboratorio de Fase 1 están en [`fase1/README.md`](fase1/README.md), con un runbook por cada vulnerabilidad.

## 1. Propósito

Construir una aplicación web pequeña, pero completa, para demostrar con evidencia el ciclo **modelar amenazas → probar una versión de laboratorio vulnerable → corregir → probar de nuevo → bloquear regresiones en CI**. La app debe tener una interfaz real con tres roles y flujos demostrables desde navegador; no será solamente una colección de endpoints de prueba.

## 2. Alcance funcional

### Roles y permisos esperados

| Capacidad | Anónimo | Cliente | Vendedor | Admin |
|---|---:|---:|---:|---:|
| Ver catálogo | Sí | Sí | Sí | Sí |
| Iniciar sesión | Sí | Sí | Sí | Sí |
| Mantener carrito propio | No | Sí | No | No |
| Crear pedido desde carrito | No | Sí | No | No |
| Comentar producto | No | Sí | Sí | Sí |
| CRUD de productos propios | No | No | Sí | Sí |
| CRUD de cualquier producto | No | No | No | Sí |
| Consultar usuarios/productos/pedidos sintéticos | No | No | No | Sí |
| Leer/modificar carrito o pedido ajeno | No | No | No | No (Fase 1 prueba el bypass; Fase 2 lo debe bloquear) |

El Vendedor no compra en esta mini-app. Cliente solo administra su carrito/pedidos. Admin tiene CRUD global de productos y lectura administrativa de datos sintéticos, pero no modifica carritos/pedidos de clientes. Esta matriz se aplica en API/servidor; la UI no cuenta como control de acceso.

### Requisitos funcionales

| ID | Requisito | Criterio de aceptación |
|---|---|---|
| FR-01 | La aplicación muestra catálogo y detalle de producto. | El usuario puede buscar y ver nombre, descripción y precio. |
| FR-02 | Los usuarios se autentican y reciben un rol conocido. | Hay flujos visibles para iniciar/cerrar sesión y cada rol llega a su área. |
| FR-03 | El Vendedor administra sus productos. | Puede crear, editar y eliminar los propios; otro vendedor no modifica esos registros. |
| FR-04 | El Cliente administra su carrito. | Puede agregar productos, cambiar cantidades, eliminar ítems y ver un total coherente. |
| FR-05 | El Cliente puede convertir el carrito en pedido. | Se conserva titular, detalle y total del pedido de forma verificable. |
| FR-06 | Usuarios autenticados comentan productos. | Los comentarios quedan asociados al usuario/producto y se ven en el detalle. |
| FR-07 | El Admin gestiona funciones privilegiadas. | Las operaciones administrativas requieren autorización en servidor. |
| FR-08 | Se registran eventos relevantes. | Login fallido, bloqueo y cambios administrativos dejan evento sin guardar contraseñas/tokens. |

## 3. Propuesta de interfaz

### Decisión recomendada

**ASP.NET Core MVC con Razor Views**, con API Controllers en el mismo host, Bootstrap servido localmente y JavaScript mínimo. Mantiene todo en .NET, separa vistas/controladores/modelos y deja solicitudes HTTP sencillas para probar con Burp. Microsoft documenta MVC para construir aplicaciones web y APIs; Razor codifica valores por defecto y advierte sobre salida HTML sin codificar. [ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-10.0) · [Prevención XSS](https://learn.microsoft.com/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0)

### Pantallas

1. **Inicio/catálogo:** búsqueda, tarjetas de producto y acceso a detalle.
2. **Detalle:** descripción, precio, añadir al carrito y lista/formulario de comentarios.
3. **Login/segundo factor:** formularios y estados de error, bloqueo o código TOTP.
4. **Carrito/checkout:** cantidades, eliminación, subtotal/total y creación del pedido.
5. **Área Vendedor:** tabla/formulario de productos propios.
6. **Área Admin:** usuarios/productos/pedidos con acciones privilegiadas y confirmación de cambios.
7. **Mensajes del sistema:** errores consistentes, autorización denegada y estados vacíos.

### Reglas UI

- La UI oculta o deshabilita acciones que el rol no puede usar, para hacer clara la experiencia.
- La seguridad efectiva vive en API/servidor. Ocultar un botón no sustituye autorización ni validación de ownership.
- Las vistas normales renderizan texto como texto, con codificación contextual.
- Solo la vista deliberadamente vulnerable de comentarios/producto del perfil de laboratorio se usa para la demostración XSS. En la versión segura, Razor usa codificación contextual; el perfil Lab modela la salida HTML no codificada solo para el ejercicio.
- Responsive básico para móvil/escritorio, labels accesibles, validación visible y mensajes en español.

## 4. Propuesta de arquitectura y datos

### Compatibilidad de sistemas operativos

El código .NET debe poder desarrollarse y probarse desde anfitriones Windows o Linux. La imagen de referencia del hito usa Linux containers; Docker Compose, PostgreSQL, la mini-app y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. La configuración no requiere que cada integrante tenga el mismo editor ni el mismo sistema anfitrión. Windows containers no forman parte del perfil de pentest del segundo parcial; si la consigna requiere un contenedor Windows además del Linux, se documentará y probará como perfil separado, sin cambiar el objetivo/entorno canónico del video.

### Componentes

- **Web/UI + API:** ASP.NET Core 10 MVC/Razor, API Controllers REST `/api/*`.
- **Persistencia:** PostgreSQL mediante EF Core/Npgsql.
- **Rate limiting y lockout:** Redis en la versión segura.
- **Pruebas:** proyecto .NET de integración y pruebas de autorización/regresión.
- **Contenedores:** Docker Compose con web/API, PostgreSQL y Redis cuando se implemente la defensa.
- **CI:** GitHub Actions con pruebas, SAST, SCA, escaneo de imagen y DAST ZAP.

Npgsql mantiene un proveedor EF Core para PostgreSQL. El motor se selecciona para que la práctica de SQLi blind time-based pueda probarse con un comportamiento de retardo controlado (`pg_sleep`) y metadatos SQL estándar. Referencias oficiales: [Npgsql EF Core](https://www.npgsql.org/efcore/) y [funciones de fecha/hora PostgreSQL](https://www.postgresql.org/docs/current/functions-datetime.html). .NET 10 es una versión LTS soportada por Microsoft hasta noviembre de 2028: [ciclo de soporte](https://learn.microsoft.com/dotnet/core/releases-and-support).

### Entidades candidatas

- `User`: id, username/email, password hash, role, estado de bloqueo y estado TOTP.
- `Product`: id, seller id, nombre, descripción, precio, estado.
- `Comment`: id, product id, user id, cuerpo, fecha.
- `CartItem`: id, owner user id, product id, quantity.
- `Order` y `OrderItem`: propietario, estado, importe congelado y líneas compradas.
- `RefreshToken`: usuario, hash del token, expiración, revocación, token reemplazante.
- `AuditEvent`: actor, acción, objeto, fecha, correlation/request id y resultado.

No almacenar tokens en claro si no es necesario; no registrar contraseñas, códigos TOTP, cookies ni bearer tokens. Se recomienda guardar refresh tokens en forma de hash y rotarlos al usarse.

### Separación de autenticación (decisión a validar en diseño)

La consigna requiere tanto robo de **session cookie** como fallas de **JWT**. La especificación de Fase 1 separa los escenarios: la interfaz MVC usa cookie de autenticación de un usuario ficticio y `/api/auth/login` emite JWT para la API. En Red Team, la cookie se captura mediante `/lab/collect` y se reproduce solo contra la app local; el JWT débil se usa solo contra `/api/admin`. La versión segura habilita atributos de cookie y validación estricta de token.

## 5. Diseño de las dos versiones de seguridad

### Versión Fase 1 — laboratorio Red Team

Crear un perfil de configuración local explícito que habilite las fallas de laboratorio. Datos sintéticos, usuarios desechables y servicios solo accesibles desde loopback. La configuración segura debe ser la predeterminada; los endpoints vulnerables no deben estar disponibles en el despliegue final.

| ID | Vulnerabilidad exigida | Superficie candidata | Evidencia y límite |
|---|---|---|---|
| RT-01 | SQLi blind time-based y extracción | Búsqueda de catálogo | Usar la base ficticia local; evidenciar metadatos de BD y una tabla con Burp/sqlmap. |
| RT-02 | XSS almacenado y robo de cookie | Comentarios o descripción | Capturar y reproducir la cookie de sesión de un usuario ficticio solo contra la app local; sin salida a terceros. |
| RT-03 | IDOR/BOLA | Carrito/pedido por id y `/api/admin` | Cliente de prueba intenta consultar/modificar recurso de otro cliente y alcanzar endpoint privilegiado. |
| RT-04 | Autenticación débil | Login | Enumeración y fuerza bruta únicamente contra cuentas sintéticas locales; demostrar ausencia de rate limit en Fase 1. |
| RT-05 | JWT `none` o secreto débil | Emisión/validación de bearer token | Mostrar el fallo de validación de firma/rol con usuario y recurso de prueba. |

Cada hallazgo tendrá: identificador, versión/commit, endpoint, cuenta usada, precondición, request/response, resultado, impacto, captura, CVSS v3.1 calculado y recomendación. Los puntajes se determinan con evidencias reales; no se fijan por adelantado.

### Versión Fase 2 — Blue Team

| Área | Requisito verificable | Prueba de aceptación |
|---|---|---|
| Entrada/SQL | EF Core y parámetros; whitelist para campos/orden/filtros. | El PoC no extrae metadatos ni altera resultados; payload se trata como valor. |
| XSS/headers | Codificación contextual; CSP; cookie `HttpOnly`, `Secure`, `SameSite`. | El comentario se muestra como texto; el script de laboratorio no se ejecuta; encabezados verificados. |
| Acceso | Middleware/policies RBAC + comprobación de ownership ABAC en cada lectura/escritura. | Cliente A no lee ni cambia carrito/pedido de Cliente B; endpoints admin niegan roles no autorizados. |
| Credenciales | bcrypt cost 12; rate limit Redis; lockout tras 5 fallos; TOTP con Otp.NET. | Hash no reversible; intentos se limitan/bloquean; TOTP requerido según política. |
| JWT | RS256, validación de algoritmo/emisor/audiencia, access token de 15 min, refresh rotativo persistido. | `none`, firma inválida, token vencido o refresh reutilizado son rechazados. |
| Secretos | Secretos por user-secrets/env/secret store; `.env` y llaves excluidos de Git e imagen. | Escaneo y revisión del repo no encuentran secreto; rotación documentada. |
| Docker | Build multi-stage; runtime no-root; Trivy sin CRITICAL en imagen final. | Inspección del usuario del contenedor y reporte Trivy pasan. |

## 6. Threat modeling requerido

### Activos

Credenciales/hashes, cookie de sesión, access/refresh tokens, roles, datos de usuario, productos, comentarios, carritos, pedidos, eventos de auditoría, claves de firma y disponibilidad de la app.

### Entradas y límites de confianza

Entradas: login, búsqueda, formularios CRUD, comentarios, cantidad/product id, cart/order id, endpoints admin y token/cookie. Límites: navegador→servidor; servidor→PostgreSQL; servidor→Redis; contenedores→host; workflow CI→artifacts/secrets.

### STRIDE inicial (mínimo; ajustar al DFD final)

1. **S:** suplantación por credenciales débiles.
2. **S/E:** JWT alterado para elevar rol.
3. **T:** SQLi modifica consulta o infiere datos.
4. **T/I:** XSS persistente altera vista y expone cookie de lab.
5. **I:** IDOR revela carrito/pedido ajeno.
6. **E:** cliente accede a operaciones Admin.
7. **D:** fuerza bruta consume recursos o evita disponibilidad.
8. **R:** cambios privilegiados sin auditoría atribuible.
9. **I:** enumeración de usuarios por diferencias de respuesta.
10. **T:** vendedor altera o elimina producto de otro vendedor.

El DFD se debe completar y validar con el equipo antes del primer tag vulnerable. La tabla STRIDE debe enlazar amenaza→flujo/activo→control→test y mantener mínimo ocho amenazas no redundantes.

## 7. Especificación del pipeline

1. Restaurar dependencias y ejecutar pruebas unitarias/de integración.
2. **SAST:** SonarQube Cloud como herramienta principal para cumplir también con la evidencia solicitada de SonarQube; conservar el reporte y corregir al menos dos findings High. Semgrep queda como alternativa si el docente confirma que acepta esa evidencia.
3. **SCA:** auditoría de paquetes NuGet y dependencias transitivas; definir política para findings sin fix.
4. Construir imagen multi-stage como parte del pipeline.
5. **Trivy:** escanear filesystem/dependencias e imagen; bloquear vulnerabilidades CRITICAL en la entrega final.
6. Desplegar Compose efímero para **ZAP Baseline**; guardar reporte con alcance y fecha.
7. Cargar reportes como artifacts sin secretos ni tokens.
8. La versión Fase 1 documenta hallazgos esperados; la configuración final bloquea regresiones SAST/test y las políticas de severidad acordadas.

La evidencia de “pipeline en verde” debe indicar qué gate pasó, qué commit, qué versión de imagen y qué política estaba activa. El DAST automatizado complementa Burp; no reemplaza las pruebas autenticadas/autoradas manualmente.

## 8. Entregables y trazabilidad con la rúbrica

| Peso | Entregable | Evidencia mínima |
|---:|---|---|
| Pentesting real con Burp: 25% | 5 hallazgos, PoCs, video. | Requests/responses, capturas, versión vulnerable. |
| Threat Model + CVSS: 15% | DFD, STRIDE y matriz de riesgo. | Activos, puntos de entrada, límites, ocho amenazas, CVSS v3.1 por hallazgo. |
| Secure Coding: 30% | Código corregido y pruebas. | Diff antes/después; test que demuestra bloqueo por vulnerabilidad. |
| DevSecOps: 20% | Workflow funcional. | GitHub Actions, SAST/SCA/ZAP/Trivy, Docker hardening, artifacts/capturas. |
| Documentación/defensa: 10% | Informe Blue Team y defensa en vivo. | Carta gerencial, lecciones, matriz ASVS L2 y demo ataque/fix/gate. |

## 9. Plan de trabajo para tres integrantes (propuesta, sin asignación nominal)

### Líneas de responsabilidad

| Línea | Responsable primario propuesto | Alcance | Revisor cruzado |
|---|---|---|---|
| UI/UX y XSS | Persona A | Flujos MVC/Razor; estados y formularios; vista de comentario vulnerable en perfil Lab; corrección de encoding/CSP/cookie; evidencia visual. | Persona C prueba desde navegador/Burp; Persona B valida contrato API. |
| API, datos y autorización | Persona B | EF Core/PostgreSQL; CRUD/carrito/pedido; SQLi e IDOR del lab; ORM, whitelist, ownership/ABAC/RBAC y tests. | Persona A prueba flujos completos; Persona C revisa STRIDE/controles. |
| Autenticación y DevSecOps | Persona C | Login; enumeración/fuerza bruta y JWT lab; correcciones de hash/TOTP/Redis/RS256; Docker, workflow y reportes. | Persona B revisa seguridad de API; Persona A prueba experiencia de login y errores. |

Threat model, CVSS, video, informe y defensa se elaboran en conjunto: quien implementa/prepara una prueba aporta evidencia; otra persona la reproduce y valida; una tercera revisa redacción y vínculo con rúbrica. La persona C no debe quedarse sola con todos los escaneos: A mantiene y prueba la UI de ZAP; B mantiene tests y dependencia .NET; C integra pipeline e imagen.

### Secuencia por hitos (sin calendario hasta conocer la fecha de entrega)

| Hito | Resultado de salida | Dependencia/gate |
|---|---|---|
| H0 — Plan/spec aprobado | Roles, arquitectura, pantallas, alcance, DFD inicial y criterios de aceptación. | Confirmar decisiones abiertas. |
| H1 — MVP funcional | UI y API recorren login/roles/productos/carrito/pedido con datos sintéticos. | Los tres roles pueden completar su flujo normal. |
| H2 — Fase 1 evaluable | Cinco fallos reproducibles, DFD/STRIDE, CVSS, video y tag congelado. | Cada PoC se repite sobre el commit etiquetado. |
| H3 — Remediación | Fix separado por hallazgo y prueba de regresión asociada. | Cada prueba ofensiva falla contra la versión corregida. |
| H4 — Entrega Blue Team | Pipeline/gates, Docker endurecido, informe y defensa ensayada. | Reintroducir de forma controlada un defecto en rama de prueba hace fallar el gate. |

## 10. Dependencias externas pendientes antes de calendarizar

Las decisiones de stack, frontend, permisos, cuentas, rutas, herramientas y ataques de Fase 1 están fijadas en este plan y en [`fase1/especificacion-laboratorio.md`](fase1/especificacion-laboratorio.md). Solo quedan dependencias externas al diseño:

1. Fecha/ventana académica para convertir la secuencia relativa en calendario.
2. Confirmar con el docente que SonarQube Cloud satisface la captura de SonarQube solicitada en Fase 2.
3. Repo/equipo real para aplicar la división A/B/C; no hay nombres asignados en esta planificación.

## 11. Estado del repositorio en esta etapa

El directorio actual contiene un scaffold de API .NET y PostgreSQL, más documentos iniciales. No contiene todavía las vistas MVC/Razor ni implementa los flujos funcionales o vulnerabilidades. Esta especificación es la base para revisar alcance y arquitectura antes de convertir requisitos en código o asignar personas.
