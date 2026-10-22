# Paquete de preparación — Fase 1 / segundo parcial

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

**Meta:** dejar especificado lo que el equipo debe desarrollar y demostrar para completar el segundo parcial. Este paquete guía la construcción futura; no significa que la mini-app o los PoC ya estén implementados.

## Qué se debe entregar

1. Mini-app web desarrollada por el equipo en .NET, con Cliente, Vendedor y Admin; login, catálogo, CRUD de productos, comentarios y carrito/pedido.
2. Repositorio Git y entorno Docker reproducible, aislado en `127.0.0.1`, con cuentas y datos ficticios.
3. DFD de la app real, inventario de activos/entradas/límites de confianza y registro STRIDE con al menos ocho amenazas.
4. Cinco demostraciones autorizadas, una por vulnerabilidad exigida, documentadas con Burp Suite o OWASP ZAP; SQLi puede complementarse con sqlmap.
5. Informe por hallazgo con precondiciones, evidencia, impacto, vector y puntuación CVSS v3.1, y recomendación.
6. Video que repita los ataques contra el mismo commit/tag de laboratorio.

La matriz completa de entregables y aceptación está en [`../matriz-entregables.md`](../matriz-entregables.md); el diseño de la app está en [`../especificacion-proyecto.md`](../especificacion-proyecto.md).

## Orden de preparación y ejecución

| Orden | Trabajo | Salida que permite avanzar |
|---|---|---|
| 1 | Aprobar decisiones de roles, permisos, stack, endpoints, cuentas y datos | Especificación cerrada y contrato de API |
| 2 | Diseñar DFD y STRIDE según ese contrato | DFD revisado y amenazas enlazadas a superficies concretas |
| 3 | Construir app funcional, Docker y datos sintéticos | Los tres roles completan su flujo normal desde navegador/API |
| 4 | Preparar perfil local Red Team y herramientas | Reset reproducible y comprobación de que el target solo es localhost |
| 5 | Ejecutar los cinco runbooks | Requests/responses y evidencia asociados al commit probado |
| 6 | Calcular CVSS, preparar informe y grabar video | Paquete de entrega revisado por otra persona |
| 7 | Congelar el estado | Commit y tag inmutable `fase1-redteam` |

## Levantar y reiniciar el laboratorio

Ejecutar estos comandos dentro de la VM Debian 13 XFCE, con Docker Engine instalado allí. El repositorio, Docker Compose, contenedor objetivo, Burp y los runbooks quedan en la misma VM; el anfitrión Windows no publica el target ni necesita Docker Desktop para el pentest. Cuando se implemente la mini-app, el README de desarrollo debe ofrecer estos comandos equivalentes:

```bash
docker compose up --build -d
docker compose ps
docker compose port shop 8080
curl -i http://127.0.0.1:5080/api/health
```

`docker compose port shop 8080` debe mostrar `127.0.0.1:5080`. Si muestra otra interfaz o el health check no responde, no iniciar herramientas de pentest. Para resetear datos, tokens y claves efímeras:

```bash
docker compose down -v
docker compose up --build -d
```

La seed vuelve a las cuentas del documento [`especificacion-laboratorio.md`](especificacion-laboratorio.md). El equipo registra la salida del mapeo de puerto en la evidencia de entorno.

## Documentos del paquete

- [`prerequisitos-entorno.md`](prerequisitos-entorno.md): requisitos de equipo, VM recomendada, instalación y verificaciones para el equipo principiante.
- [`especificacion-laboratorio.md`](especificacion-laboratorio.md): funciones, roles, datos y requisitos del perfil de laboratorio.
- [`herramientas.md`](herramientas.md): selección de herramientas, propósito y preparación.
- [`procedimiento-pentest.md`](procedimiento-pentest.md): flujo común, reglas de ejecución y evidencias.
- [`ataques/01-sqli-blind.md`](ataques/01-sqli-blind.md)
- [`ataques/02-xss-almacenado.md`](ataques/02-xss-almacenado.md)
- [`ataques/03-idor-bola.md`](ataques/03-idor-bola.md)
- [`ataques/04-autenticacion.md`](ataques/04-autenticacion.md)
- [`ataques/05-jwt.md`](ataques/05-jwt.md)
- [`plantilla-informe-hallazgo.md`](plantilla-informe-hallazgo.md): formato uniforme de evidencia y CVSS.
- [`video-guion.md`](video-guion.md): secuencia de demostración para la entrega en video.
- [`evidencias/README.md`](evidencias/README.md): estructura y convención para guardar capturas/requests.

## Límites de seguridad del laboratorio

- El único objetivo es la mini-app creada para esta asignatura, en `http://127.0.0.1:5080` (o el puerto local que se apruebe).
- Usar una base PostgreSQL efímera, cuentas inventadas, correo `example.invalid` y contenido ficticio. No cargar datos de clientes, compañeros, cuentas personales ni secretos reales.
- El perfil vulnerable debe estar desactivado por defecto y Compose debe publicar solo sobre loopback. El modo Red Team no se despliega en hosting ni se expone a la red del campus.
- La prueba XSS usa una sesión válida de `cliente-xss` (identidad ficticia), un comentario publicado por otro usuario y `/lab/collect`; el reset debe invalidar la cookie. No enviar cookies/tokens a sitios externos.
- Detenerse si la URL resuelta, el host, el certificado o el destino de una herramienta no corresponden exactamente al contenedor local.
- Usar cargas pequeñas y secuenciales; no ejecutar pruebas de estrés, escaneos agresivos ni comandos contra dominios/IP que no pertenezcan al contenedor.

## Lo que se deja para la etapa siguiente

La Fase 2 implementará correcciones, pruebas negativas/regresión, controles de autenticación robusta, hardening y gates finales. En esta etapa se dejan documentados como destino de remediación, pero no se declara que estén implementados.
