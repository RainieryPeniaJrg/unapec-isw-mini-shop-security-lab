# Matriz de entregables y criterios de aceptación

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

**Enfoque de planificación actual:** completar primero las filas F1-01 a F1-17 para el segundo parcial. F1-01 a F1-06 (mini-app, roles, login, CRUD, carrito, Git/Docker) son prerequisitos funcionales del laboratorio y también forman parte del alcance de esta entrega. Las filas F2 describen el hito final y quedan para después; no deben usarse como criterio para declarar terminado el segundo parcial.

**Uso:** revisar que ningún punto de la consigna quede fuera del plan. Esta matriz traduce requisitos; no significa que el código o las evidencias estén hechos ni que los responsables estén asignados.

## Fase 1 — Segundo parcial (40%): Threat Modeling y ataque

| ID | Entregable/requisito de la consigna | Evidencia que debe prepararse | Criterio de aceptación | Responsable propuesto |
|---|---|---|---|---|
| F1-01 | Mini-App web propia desarrollada desde cero | Repositorio Git, frontend y API .NET, Docker Compose y README de ejecución. | Arranca en el entorno de laboratorio y representa los tres roles. | A: UI; B: API/datos; C: Compose/integración. |
| F1-02 | Tres roles: Cliente, Admin y Vendedor | Matriz de permisos y cuentas ficticias por rol; navegación/pantallas asociadas. | Cada rol tiene un recorrido funcional y permisos documentados. | A lidera UI; B API; C revisa controles. |
| F1-03 | Login | Pantalla de login conectada a API, cuentas de laboratorio y requests capturados. | Los tres roles pueden autenticarse; sus identidades se distinguen. | A frontend; C autenticación. |
| F1-04 | CRUD de productos | Pantallas de catálogo y CRUD; requests/responses de crear, consultar, editar y eliminar. | Vendedor administra sus productos y Admin realiza acciones privilegiadas previstas. | A frontend; B API. |
| F1-05 | Carrito de compra | UI de carrito, cambio de cantidades y flujo de checkout/pedido si se acuerda en alcance. | El Cliente agrega, modifica y elimina ítems en su carrito. | A frontend; B API. |
| F1-06 | Git y Docker obligatorios | Repo con historial/ramas o tags; Dockerfile y Compose. | El código puede editarse desde Windows o Linux; otra persona clona y levanta la imagen Linux dentro de Debian 13 XFCE VM siguiendo README; el target vulnerable queda en loopback del invitado. | C coordina; los tres aportan. |
| F1-07 | Diagrama DFD de la aplicación propia | DFD con navegador/UI, API, PostgreSQL, almacenes, flujos, Redis si está en alcance y trust boundaries. | Todo activo y flujo crítico del alcance aparece y tiene límites de confianza identificados. | C compila con aportes de A/B. |
| F1-08 | Threat Modeling profesional con STRIDE, mínimo 8 amenazas | Registro STRIDE enlazado a activo, actor, entrada, impacto y control esperado. | Hay al menos 8 amenazas clasificadas; no es FODA; las categorías y elementos del DFD son coherentes. | C compila; cada dueño valida sus amenazas. |
| F1-09 | Activos, puntos de entrada y límites de confianza | Inventario en el informe: credenciales, roles, sesiones/tokens, catálogo, comentarios, carritos/pedidos, claves y disponibilidad; endpoints/forms y límites. | Los activos y entradas se pueden ubicar en el DFD y mapear a amenazas. | Compartido. |
| F1-10 | SQLi blind time-based + extracción | Evidencia Burp/sqlmap o prueba manual sobre el contenedor; reporte con inferencia del nombre de BD y al menos una tabla. [Runbook](fase1/ataques/01-sqli-blind.md). | La prueba es reproducible contra datos sintéticos y no se dirige fuera del laboratorio. | B. |
| F1-11 | XSS almacenado + robo de cookie | Comentario/descripción persiste; video/captura de ejecución en navegador; cookie de sesión de usuario ficticio recibida en `/lab/collect` y reproducida contra la app local. [Runbook](fase1/ataques/02-xss-almacenado.md). | Se muestra almacenamiento, ejecución, captura y replay de sesión local sin enviar a terceros. | A. |
| F1-12 | Broken Access Control: IDOR + BOLA | Requests de Cliente A cambiando el ID para leer y modificar carrito/pedido de Cliente B; request a `/api/admin`. [Runbook](fase1/ataques/03-idor-bola.md). | Evidencia deja claro quién es el usuario autenticado, el recurso ajeno y la respuesta indebida. | B. |
| F1-13 | Broken Authentication: fuerza bruta, sin Rate Limiting, enumeración | Captura de respuestas distintas para usuario válido/inválido y prueba controlada de múltiples intentos con cuentas sintéticas. [Runbook](fase1/ataques/04-autenticacion.md). | Se documenta enumeración y ausencia de límite/bloqueo, sin carga excesiva. | C. |
| F1-14 | JWT `none` o secreto débil con elevación a Admin | Token de laboratorio antes/después y request al endpoint privilegiado. [Runbook](fase1/ataques/05-jwt.md). | Se demuestra el cambio de privilegio y se limita a identidad/datos de prueba. | C. |
| F1-15 | Documentar con Burp Suite / OWASP ZAP | Historial de requests/responses, capturas anotadas y configuración/versiones de herramientas. [Procedimiento](fase1/procedimiento-pentest.md). | Cada hallazgo tiene evidencia rastreable al endpoint y versión probados. | Cada líder de PoC; C consolida. |
| F1-16 | Informe con CVSS v3.1 calculado para cada hallazgo | Sección por vulnerabilidad con vector, puntuación, justificación y condiciones de explotación. [Plantilla](fase1/plantilla-informe-hallazgo.md). | Cinco hallazgos tienen puntuación y justificación coherentes con la evidencia. | Cada líder propone; revisión cruzada. |
| F1-17 | Video de ataque con Burp | Guion corto y video mostrando preparación, request, impacto y versión atacada. Seguir el [procedimiento común](fase1/procedimiento-pentest.md). | Incluye los ataques exigidos o el alcance acordado con docente; se identifica el entorno local. | Los tres presentan su sección; C integra. |

### Entrega empaquetada de Fase 1

1. URL del repositorio y referencia inmutable de la versión vulnerable (`fase1-redteam`).
2. App funcional por roles y guía Docker para reproducirla.
3. DFD, STRIDE y registro de amenazas.
4. Informe de cinco hallazgos con CVSS v3.1 y evidencia Burp/ZAP.
5. Video de demostración.

## Fase 2 — Final (60%): Defensa, Secure Coding y DevSecOps

### Secure Coding — 30%

| ID | Entregable/requisito | Evidencia | Criterio de aceptación | Responsable propuesto |
|---|---|---|---|---|
| F2-01 | Consultas 100% parametrizadas + ORM | Código EF Core, revisión de consultas directas y test de inyección. | No se concatena entrada del usuario en SQL; datos inválidos se rechazan/parametrizan. | B. |
| F2-02 | Validación con whitelist | Validadores DTO/query: campos, filtros, orden, longitudes y rangos permitidos. | Entradas fuera de lista/rango se rechazan; entradas válidas siguen funcionando. | B. |
| F2-03 | CSP Header | Headers de respuesta y prueba en navegador/ZAP. | CSP definida para la UI; la carga/ejecución del payload de prueba queda bloqueada. | A con C. |
| F2-04 | Cookies HttpOnly, Secure, SameSite | Inspección de `Set-Cookie` en entorno apropiado HTTPS. | Flags requeridos presentes; JS no lee cookie de sesión. | A/C. |
| F2-05 | RBAC + ABAC en middleware/policies | Matriz rol→operación y comprobación de ownership en cada endpoint protegido. | No basta ocultar botones: llamadas directas con rol/owner indebido reciben 401/403 sin filtrar datos. | B. |
| F2-06 | bcrypt cost 12 (selección del plan) | Configuración y pruebas de registro/login; inspección de almacenamiento. | Passwords no están en claro; algoritmo y costo documentados. | C. |
| F2-07 | Rate Limiting con Redis | Configuración, dashboard/logs de prueba y respuesta al exceder límite. | Límite se aplica a login y se comparte correctamente vía Redis en el Compose final. | C. |
| F2-08 | Bloqueo tras 5 intentos | Prueba de cinco fallos y política de recuperación documentada. | El intento siguiente queda bloqueado según política; no permite bypass trivial por usuario/IP. | C. |
| F2-09 | 2FA TOTP | Setup/enrolamiento y login con código TOTP. | Sin código válido no se completa el flujo protegido; secretos TOTP no aparecen en logs. | C. |
| F2-10 | JWT RS256, privado, expira en 15 min | Configuración de firma/validación y pruebas de token vencido, firma inválida y claims. | Solo se acepta RS256 con issuer/audience/expiración esperados; rol no se puede forjar. | C. |
| F2-11 | Refresh token rotativo en DB | Requests de refresh y registros hash/revocación/reemplazo. | Cada refresh rota; reutilizar token anterior revoca/rechaza según política. | C/B. |
| F2-12 | Gestión de secretos: no `.env` en GitHub; env vars/vault | `.gitignore`, revisión de historial, configuración del workflow y README de secrets. | No hay secretos reales en repo, logs ni imagen; CI usa GitHub Secrets/vars según corresponda. | C integra; todos revisan. |
| F2-13 | Pruebas Antes/Después | Tests de regresión y diff Git separado por vulnerabilidad. | Cada PoC de Fase 1 deja de funcionar contra el final y el test del control pasa. | Cada dueño; revisión cruzada. |

### DevSecOps — 20%

| ID | Entregable/requisito | Evidencia | Criterio de aceptación | Responsable propuesto |
|---|---|---|---|---|
| F2-14 | Pipeline GitHub Actions o GitLab CI | `.github/workflows/security.yml` (si GitHub) y run exitoso ligado al commit final. | Corre en push/PR; tests, escaneos y artifacts quedan visibles. | C. |
| F2-15 | SAST SonarQube Cloud (principal); corregir ≥2 High | Reporte antes y después, issues identificados, commits que los corrigen. | Al menos dos findings High corregidos y evidencia de que el gate detecta regresión. | C integra; B corrige issues de código, A issues de UI. |
| F2-16 | SCA | Auditoría NuGet/transitiva (apropiada a .NET), política y reporte. | Pipeline identifica dependencias vulnerables y gate bloquea las severidades acordadas. | C/B. |
| F2-17 | DAST ZAP Baseline contra Docker | Job levanta Compose efímero, escanea URL de contenedor y publica reporte. | Escaneo se ejecuta automatizado; reporte final en verde según umbral aprobado. | C integra; A valida UI/rutas. |
| F2-18 | Docker hardening: no-root, multi-stage | Dockerfile e inspección del usuario/capas de la imagen final. | Runtime no-root; build separado de runtime; sin secretos en capas. | C con B. |
| F2-19 | Trivy sin CRITICAL | Reporte Trivy de imagen final. | Cero findings CRITICAL según política de entrega (documentar excepciones de unfixed si se aceptan). | C. |
| F2-20 | Evidencia de SonarQube Cloud y ZAP en verde | Capturas o artifacts con fecha, commit y rama/versión. | Evidencia muestra escaneo final aprobado. Confirmar con el docente que SonarQube Cloud satisface la captura de SonarQube solicitada. | C. |
| F2-21 | Pipeline falla al reintroducir vulnerabilidad | Demo en rama temporal o commit demostrativo que active prueba/gate y run rojo; luego revertir. | Una vulnerabilidad reintroducida hace fallar un check identificado; final vuelve a verde tras retirarla. | Los tres observan; C presenta el run. |

### Informe Blue Team y defensa — 10%

| ID | Entregable/requisito | Evidencia | Criterio de aceptación | Responsable propuesto |
|---|---|---|---|---|
| F2-22 | Código Antes/Después con diff Git | PRs/commits de cada fix y tabla de enlace vulnerabilidad→commit→test. | Un revisor externo puede entender qué cambió y cómo se validó. | Cada dueño aporta; B consolida. |
| F2-23 | Carta de remediación para gerencia no técnica | Carta breve: riesgo, impacto al negocio, acción, estado residual y responsable. | Comprensible sin jerga; coincide con severidad/evidencia del informe. | A redacta con aportes de equipo. |
| F2-24 | Lecciones aprendidas | Sección del informe basada en resultados del equipo. | Incluye causas, controles preventivos/detectivos y cambios de proceso. | Compartido. |
| F2-25 | Cumplimiento OWASP ASVS L2 | Matriz requisito aplicable→implementación→test/evidencia→estado/no aplicable. | No declarar cumplimiento global sin revisar controles; brechas/no aplicables se explican. | C consolida; cada responsable aporta pruebas. |
| F2-26 | Defensa final obligatoria | Guion y ambiente con versión Fase 1 y versión final reproducibles. | 1) ataque de Fase 1 falla en el final; 2) regresión deliberada hace fallar pipeline; 3) pipeline pasa al retirar regresión. | Los tres presentan sus bloques. |
| F2-27 | Link al repositorio en entrega | URL del repositorio y referencias a tag vulnerable/final. | Acceso probado por docente; README indica cómo levantar versión final y dónde ver workflow/evidencias. | C integra; equipo verifica acceso. |

## Rúbrica y trazabilidad

| Criterio de rúbrica | Peso declarado | IDs principales |
|---|---:|---|
| Pentesting real con Burp | 25% | F1-10 a F1-17 |
| Threat Model + CVSS | 15% | F1-07 a F1-09, F1-16 |
| Secure Coding avanzado | 30% | F2-01 a F2-13 |
| DevSecOps Pipeline | 20% | F2-14 a F2-21 |
| Documentación y Defensa | 10% | F2-22 a F2-26 |

## Observaciones de planificación

1. La entrega “frontend” no está separada como porcentaje en la rúbrica, pero es requisito de la aplicación propia y soporta los flujos de roles, CRUD, carrito y XSS.
2. Para SQLi blind time-based, PostgreSQL es la base planificada por su función de retardo; el PoC/extracción debe ejecutarse solamente en la app local de entrenamiento.
3. En `.NET`, SCA debe auditar paquetes NuGet/transitivos; `npm audit` y `safety` son ejemplos de otros stacks, no herramientas adecuadas para el backend .NET.
4. La consigna permite Semgrep o SonarQube, pero además pide captura de SonarQube. El plan selecciona SonarQube Cloud; confirmar con el docente que la edición Cloud cumple con esa evidencia.
5. El DAST Baseline de ZAP no garantiza detectar cada fallo de lógica autenticada. Los PoCs manuales y pruebas de regresión por vulnerabilidad siguen siendo entregables separados.
