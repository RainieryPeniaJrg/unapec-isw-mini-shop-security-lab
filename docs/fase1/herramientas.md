# Herramientas del hito — segundo parcial

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

| Herramienta | Uso en esta entrega | Preparación/criterio |
|---|---|---|
| Git + GitHub | Historial, revisión por pares, tag de versión vulnerable | Repo público educativo; PRs pequeños; nunca subir secretos/datos reales; tag `fase1-redteam` inmutable |
| Docker + Docker Compose | Ejecutar app y PostgreSQL de laboratorio | Bind únicamente `127.0.0.1`; datos desechables; README con start/reset/stop |
| .NET SDK 10 + EF Core/Npgsql | Construcción de la mini-app .NET y persistencia | Build/test local antes de integrar; no subir secretos |
| PostgreSQL | Base sintética compatible con la demostración blind time-based y metadatos | Base aislada y reconstruible; el usuario DB tiene solo permisos de laboratorio necesarios |
| Burp Suite Community | Proxy HTTP, historial, Repeater, Intruder acotado y capturas manuales/video | Proxy del navegador y del script de autenticación; scope incluye solo `127.0.0.1:5080` |
| OWASP ZAP | Proxy/validación complementaria y evidencia DAST manual | Contexto/scope restringido al host local; no sustituye las pruebas de lógica manuales |
| sqlmap | Confirmar SQLi blind time-based y extraer solo nombre de base y nombres de tablas sintéticas | Usar exclusivamente la URL local del runbook, técnica time-based y límites bajos; evitar extracción de filas/dumps |
| Python 3 + `requests` + PyJWT | Script secuencial de cinco intentos para login JSON y firma local del JWT modificado | Entornos virtuales locales; script de fuerza bruta fija/verifica loopback y pasa por Burp; PyJWT opera solo sobre el token propio del lab |
| CVSS v3.1 Calculator de FIRST | Puntuar y justificar cada hallazgo | Vector, condiciones e impacto deben coincidir con evidencia reproducida |
| Grabador de pantalla | Video demostrativo de Burp y resultado | Capturar URL local, cuenta ficticia, request/response y efecto; ocultar secretos/credenciales |
| Editor de Markdown | Informe, STRIDE, DFD, evidencias y runbooks | Mantener enlaces relativos desde el repositorio |

## Referencias oficiales de uso

- [Burp Intruder — documentación PortSwigger](https://portswigger.net/burp/documentation/desktop/tools/intruder) y [guía de inicio](https://portswigger.net/burp/documentation/desktop/tools/intruder/getting-started).
- [sqlmap — Usage (opciones y técnicas)](https://github.com/sqlmapproject/sqlmap/wiki/Usage).
- [OWASP ZAP — Scope y Context](https://www.zaproxy.org/docs/desktop/start/features/scope/).
- [FIRST — calculadora CVSS v3.1](https://www.first.org/cvss/calculator/3.1) y [especificación](https://www.first.org/cvss/v3-1/specification-document).

## Ajustes de Burp/ZAP

1. Levantar el proyecto y verificar `docker compose ps`; confirmar que el mapeo de puerto aparece como `127.0.0.1:5080->...`. Si aparece `0.0.0.0`, detener el laboratorio y corregir Compose.
2. En Burp, abrir su navegador integrado o configurar el navegador de laboratorio para usar el proxy de Burp. En **Target → Scope**, incluir solo `http://127.0.0.1:5080`; rechazar requests fuera de scope.
3. Iniciar sesión con cada cuenta sintética y generar una solicitud normal de cada ruta. Guardar la evidencia limpia en Proxy HTTP history.
4. Para cada prueba, usar **Send to Repeater**; modificar una variable por vez y guardar las requests/responses con nombre del ID del hallazgo.
5. Para F1-13, ejecutar el script Python del runbook. El script manda cinco solicitudes secuenciales mediante el proxy local de Burp; revisar todas en Proxy HTTP history. Intruder queda disponible para observar/validar manualmente, pero el script constituye la automatización pedida por la consigna.
6. En ZAP, usar **Quick Start → Manual Explore** para abrir el navegador proxy, o configurar el proxy del navegador; crear un Context que contenga únicamente `127.0.0.1:5080`. Para este hito, usarlo como historial/proxy y Passive Scan complementario; Burp conserva la evidencia principal y el video.
7. No lanzar Automated Scan/Active Scan de ZAP ni crawling amplio como primera acción; si el equipo lo añade como comprobación auxiliar, debe apuntar al Context local recién revisado y partir de seed limpia.
8. Registrar versión de Burp/ZAP, scope, host, commit y fecha para que el caso se pueda repetir.

## Uso de sqlmap

El runbook `ataques/01-sqli-blind.md` define una sola ruta y un solo parámetro. Primero confirmar la señal manual; luego usar sqlmap con técnica time-based para comprobar el nombre de la base y las tablas. Registrar comando/opciones, versión, hora, latencia y resultado. No usar `--dump`, no extraer filas de usuarios, no aumentar concurrencia ni cambiar de host.

## Lo que no hace falta para el segundo parcial

SonarQube, Trivy, SCA, DAST en GitHub Actions y gates de regresión son entregables de Fase 2. Se puede conservar el workflow scaffold como referencia, pero un check verde de ese workflow no reemplaza el informe, PoCs ni evidencia manual del segundo parcial.
