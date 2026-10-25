# Plan de trabajo — Proyecto Integrador Avanzado ISW

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

El desarrollo de la aplicación está autorizado hasta Fase 1. El reparto A/B/C sigue siendo una propuesta para el equipo académico. La especificación está en [`especificacion-proyecto.md`](especificacion-proyecto.md) y la comprobación requisito por requisito en [`matriz-entregables.md`](matriz-entregables.md). Las evidencias manuales y la revisión del hito se completan por separado.

## Prioridad de esta primera etapa

El equipo trabajará inicialmente para entregar **todo el hito del segundo parcial (Fase 1, 40%)**. El alcance incluye construir la mini-app funcional que exige la consigna y usarla como objetivo local de laboratorio; no significa implementar todavía la remediación ni los controles finales de Blue Team. La Fase 2 queda planificada para una etapa posterior.

### Orden del hito

1. **Adoptar el diseño documentado y preparar el entorno:** usar los roles/permisos, pantallas, endpoints y cuentas fijados en la especificación Fase 1; crear el repositorio Git/Compose y verificar el bind local.
2. **Construir el flujo funcional:** login de Cliente/Admin/Vendedor; catálogo y comentarios; CRUD de productos; carrito y pedido. Probar primero el recorrido normal de cada rol.
3. **Completar threat model:** actualizar DFD y trust boundaries al diseño real; inventariar activos/entradas; validar un mínimo de ocho amenazas STRIDE y enlazarlas a las rutas y pruebas.
4. **Preparar y ejecutar los cinco escenarios Red Team:** SQLi blind time-based con extracción sintética; XSS almacenado con robo/replay de cookie de sesión ficticia; IDOR/BOLA sobre carrito/pedido y endpoint admin; fuerza bruta/enumeración; JWT HS256 con secreto débil y elevación de rol.
5. **Consolidar la evidencia:** requests/responses de Burp o ZAP, pasos reproducibles, impacto, CVSS v3.1 justificado por hallazgo, capturas y guion/video.
6. **Congelar el hito:** repetir los casos desde un entorno limpio, validar README y Docker en otra máquina del equipo, fijar el commit y crear el tag `fase1-redteam`.

**Criterio de cierre:** no basta con que existan cinco fallas en el código. La app debe funcionar para los tres roles, cada PoC debe repetirse contra el tag identificado y la entrega debe contener DFD/STRIDE, informe CVSS, evidencia y video. La implementación de controles robustos y el pipeline bloqueante pertenecen al hito final.

## Objetivo y alcance

Construir una mini tienda web propia con tres roles (**Cliente, Vendedor y Admin**), autenticación, CRUD de productos y carrito. El mismo producto tendrá una versión de laboratorio vulnerable para la Fase 1 y una versión corregida para la Fase 2. El objetivo de la práctica es mostrar el ciclo completo: diseño de amenazas → prueba autorizada en el laboratorio → evidencia → corrección → regresión automatizada.

**Stack propuesto para completar:** ASP.NET Core 10 MVC/Razor, Entity Framework Core con Npgsql, PostgreSQL; Redis para el límite de intentos y bloqueo en la versión segura; Docker Compose; GitHub Actions. .NET 10 es LTS con soporte de Microsoft hasta noviembre de 2028. PostgreSQL encaja con el requisito SQLi blind time-based porque permite probar demoras controladas y consultar metadatos del motor de forma reproducible.

**Matriz multi-OS:** desarrollar/editar el código desde Windows o Linux, pero ejecutar la configuración integrada del hito (Linux containers, PostgreSQL y pentest) dentro de Debian 13 XFCE VM sobre Windows 11. La VM será el entorno canónico de ataque y video. La comparativa VMware/VirtualBox y el perfil viable para el Lenovo LOQ de 16 GB están en [prerrequisitos y entorno](fase1/prerequisitos-entorno.md).

## Frontend incluido en el proyecto

La interfaz forma parte de la mini aplicación y es necesaria para demostrar el XSS almacenado en contexto real. La propuesta es **ASP.NET Core MVC con Razor Views**, usando Bootstrap local y JavaScript mínimo; se mantiene en el mismo host que la API y las solicitudes quedan sencillas para Burp.

Pantallas mínimas que se deben diseñar y luego implementar:

| Pantalla | Uso y controles visibles |
|---|---|
| Inicio de sesión | Usuario/contraseña; mensajes de error; desafío TOTP en la versión segura. |
| Catálogo | Búsqueda, productos, precio, detalle y comentarios. |
| Carrito | Ítems, cantidades, total, actualizar y confirmar pedido. |
| Vendedor | Crear, editar y eliminar únicamente sus productos. |
| Administración | Gestión de usuarios/productos; solo Admin. |
| Perfil/seguridad | Sesiones, renovación de token y segundo factor cuando aplique. |

La UI puede ocultar opciones según el rol para orientar al usuario, pero la API debe autorizar cada operación por rol y ownership. Para el XSS, el comentario de un atacante sintético debe persistirse sin codificación contextual; la víctima `cliente-xss` lo abre con la cookie `LabSession` accesible a JavaScript, y el equipo demuestra su captura y replay solo en localhost. La versión corregida debe volver a codificar el texto y aplicar CSP y atributos seguros de cookie. La mayoría de vistas usarán salida codificada.

El DFD debe reflejar navegador ↔ ASP.NET Core MVC/API ↔ PostgreSQL; Redis se agrega como almacén de contador/bloqueo durante la fase segura. Burp/ZAP observarán solicitudes HTTP y las operaciones de API.

## Límite del laboratorio

- La variante Red Team debe usar datos y cuentas ficticias y quedar accesible solo por loopback mediante Docker Compose.
- Activar el comportamiento vulnerable solo mediante un perfil/configuración explícita del laboratorio. Las pruebas deben usar las rutas de la app que pide la consigna (`/api/admin`, carrito/pedido, búsqueda); la versión final usa controles corregidos y no inicia en modo vulnerable.
- El robo/replay de cookie se demuestra con una cuenta víctima ficticia y `/lab/collect` en el mismo host; la cookie no se envía a terceros y se invalida con el reset del laboratorio.
- Las herramientas Burp, ZAP, sqlmap y cualquier script se apuntan únicamente al contenedor de esta aplicación.
- Conservar el punto vulnerable como tag o commit de Fase 1. La entrega final en `main` contiene las correcciones; no dejar el contenedor vulnerable publicado en Internet.

## Fases y entregables

### Preparación — base común del hito actual

1. Acordar historias y permisos de Cliente, Vendedor y Admin.
2. Diseñar flujo de pantallas y navegación para login, catálogo, detalle/comentarios, carrito, vendedor y administración.
3. Terminar API funcional y modelo de datos: usuarios, productos, carritos, pedidos y comentarios.
4. Definir el contrato de rutas, códigos HTTP, modelo de errores y forma en que la UI consume la API.
5. Crear Compose con UI/API, PostgreSQL y, más adelante, Redis. Sembrar datos ficticios.
6. Probar un flujo completo por rol desde navegador, antes de introducir las fallas deliberadas.

### Fase 1 — segundo parcial (40%): Threat Modeling y ataque

**Entrega 1: Threat Modeling.** DFD con procesos, almacenes, flujos y límites de confianza; inventario de activos y entradas; mínimo ocho amenazas STRIDE. Asociar cada amenaza a endpoint, actor, impacto, control esperado y evidencia.

**Entrega funcional base:** los tres roles inician sesión y usan las pantallas permitidas; Cliente busca/agrega al carrito, Vendedor gestiona productos propios y Admin gestiona recursos administrativos. Se registra el flujo normal antes de activar las fallas de laboratorio.

**Entrega 2: cinco hallazgos reproducibles.** Cada uno incluye precondición, cuenta de prueba, request/respuesta en Burp o ZAP, resultado, impacto, CVSS v3.1 y recomendación:

| Hallazgo requerido | Parte de la aplicación | Evidencia que se prepara |
|---|---|---|
| SQLi blind time-based con extracción sintética | Búsqueda de productos contra PostgreSQL | Demostrar inferencia de metadatos de BD y una tabla en el contenedor local. |
| XSS almacenado y robo de cookie | Comentarios o descripción de producto | Payload persistente, captura de cookie de sesión ficticia en `/lab/collect` y replay de esa sesión solo en la app local. |
| IDOR/BOLA | Carrito/pedido por identificador | Cliente A consulta o modifica datos del Cliente B; acceso indebido al endpoint admin. |
| Autenticación débil | Login | Enumeración diferenciable y múltiples intentos sin límite en cuentas ficticias. |
| JWT `none` o secreto débil | Endpoint protegido/admin | Alteración demostrativa del rol en el laboratorio aislado. |

**Entrega 3: demostración.** Colección de requests, capturas de Burp/ZAP, tabla CVSS y guion de video. El video debe permitir repetir el caso desde el estado limpio del contenedor.

**Cierre de fase:** etiquetar el commit vulnerable con `fase1-redteam`. Registrar versiones de herramientas y configuración para que la defensa final use exactamente la misma prueba.

### Fase 2 — final (60%): defensa, Secure Coding y DevSecOps (etapa posterior)

**Secure Coding (30%).** Remediar cada hallazgo y conservar una prueba negativa que confirme que el ataque ya no funciona. Incluir ORM/consultas parametrizadas y whitelist; salida codificada, CSP y cookies seguras; RBAC más ownership/ABAC por endpoint; bcrypt cost 12, rate limit Redis, bloqueo tras cinco intentos y TOTP; JWT RS256 de 15 minutos con refresh rotativo persistido; secretos fuera de Git y de la imagen.

**DevSecOps (20%).** `security.yml` debe ejecutar pruebas .NET, SonarQube Cloud, auditoría de NuGet/SCA, build Docker multistage no-root, Trivy en imagen con bloqueo de CRITICAL y ZAP Baseline contra el Compose de prueba. En el tag Fase 1 se pueden publicar findings como baseline informativo; en la entrega final los gates acordados deben bloquear PR si reaparece la vulnerabilidad o falla el escaneo. Guardar reportes como artifacts del workflow, sin credenciales. Semgrep queda como alternativa si el docente confirma que sirve para la evidencia solicitada.

**Informe Blue Team (10%).** Diff Antes/Después por hallazgo, carta de remediación gerencial, lecciones aprendidas y matriz de trazabilidad de controles a OWASP ASVS L2. No declarar cumplimiento total sin documentar qué requisitos se verificaron y con qué evidencia.

**Defensa en vivo:** correr el PoC contra el tag vulnerable; repetirlo contra la versión final; mostrar que la prueba de regresión falla si se reintroduce el defecto y que el pipeline bloquea ese cambio.

## Reparto propuesto para tres integrantes

Cada persona tiene una responsabilidad primaria y revisa el trabajo de otra. Evitar que una persona sea la única que entienda CI o que pueda integrar cambios.

| Integrante | Liderazgo durante Fase 1 | Liderazgo durante Fase 2 | Revisión cruzada |
|---|---|---|---|
| **A — Frontend y flujo de usuario** | MVC/Razor: login, catálogo, detalle/comentarios, carrito, panel vendedor/admin. Lidera el escenario XSS del laboratorio y su evidencia desde el navegador. | Codificación contextual, CSP y cookies seguras; valida los recorridos y la corrección del XSS. | B revisa el contrato con API; C reproduce XSS y revisa cabeceras. |
| **B — API, datos y acceso a recursos** | EF Core/Npgsql, PostgreSQL, endpoints, CRUD/carrito/pedido. Lidera SQLi e IDOR/BOLA en el laboratorio. | Consultas parametrizadas/whitelist y controles RBAC/ABAC con ownership. | A prueba flujos desde UI; C revisa políticas y pruebas de regresión. |
| **C — Autenticación y DevSecOps** | Login, enumeración/fuerza bruta, JWT de laboratorio; Docker, workflow y escaneos base. Coordina el DFD/STRIDE y consolidación de CVSS/evidencias. | Hash robusto, rate limit/bloqueo, TOTP, JWT RS256/refresh; gates, artifacts y consolidación de ASVS. | A prueba experiencia de login; B revisa token/API; ambos reproducen casos contra la versión final. |

**Uso de la propuesta en el segundo parcial:** A/B/C son frentes de liderazgo sugeridos, no nombres asignados. Los tres participan en el diseño funcional y revisan al menos un PoC ajeno. La prioridad de C en este hito es dejar reproducible Docker/Git y coordinar threat model/evidencia; no se le atribuye todavía completar el pipeline final de Blue Team.

La matriz describe responsabilidades sugeridas; no asigna nombres ni inicia tareas. Threat modeling, CVSS, video, informe y defensa requieren aportes de los tres. Todo endpoint de escritura debe tener dueño de implementación y revisión de otra persona. El frontend y la API se revisan juntos: ocultar un botón no autoriza la operación en el servidor.

## Forma de trabajar en Git

1. `main` es la rama integrada; cada tarea sale de `feature/<tema>` o `fix/<hallazgo>`.
2. Abrir PR pequeño con: requisito, archivos, prueba ejecutada y evidencia. Solicitar una revisión de un compañero; nadie integra su propio PR.
3. Hacer integración temprana del contrato de API y Compose para evitar que cada integrante cree rutas/modelos incompatibles.
4. Antes del cierre de Fase 1, fijar el commit y crear tag `fase1-redteam`. No sobrescribirlo.
5. Corregir en PRs separados (`fix/sql-injection`, `fix/xss`, `fix/idor`, `fix/auth`, `fix/jwt`) y vincular cada cambio a su prueba de regresión.
6. Cerrar workflow con gates bloqueantes; etiquetar la entrega final `fase2-blueteam` y conservar artifacts de CI.

## Tablero inicial

1. Acordar integrante A/B/C y confirmar acceso al repositorio público del laboratorio.
2. Verificar PostgreSQL en Compose y terminar healthchecks/configuración de desarrollo.
3. Implementar entidades, login/roles, productos y carrito funcionales.
4. Aprobar DFD/STRIDE y límite de laboratorio antes de añadir fallas intencionales.
5. Implementar la variante Red Team en tareas pequeñas, grabar baseline y etiquetar Fase 1.
6. Corregir una vulnerabilidad por PR con test de regresión; luego activar gates de CI.
