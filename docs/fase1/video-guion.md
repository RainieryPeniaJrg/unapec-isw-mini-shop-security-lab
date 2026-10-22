# Guion de video — demostración Red Team del segundo parcial

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

**Duración propuesta:** 8–12 minutos.
**Requisito de la consigna:** que Burp Suite aparezca como herramienta de prueba principal. Para SQLi se muestra además sqlmap; no sustituye la evidencia HTTP capturada en Burp.

## Preparación antes de grabar

- Repositorio clonado y tag/commit que se va a demostrar identificable en pantalla.
- Docker Compose limpio; puerto ligado a `127.0.0.1:5080`; seed sintética cargada.
- Burp scope fijado solo a `127.0.0.1:5080`; navegador de laboratorio abierto.
- Cuentas `cliente-a`, `cliente-b`, `cliente-xss`, `cliente-brute`, `vendedor-lab`, `admin-lab` listas.
- sqlmap instalado; PyJWT local preparado; no tener tokens/secrets reales en pantalla.
- Carpeta de evidencias abierta para mostrar nombres/orden y documento CVSS.

## Secuencia de grabación

1. **Presentación (30–45 s):** nombre del proyecto, curso, equipo, objetivo; enseñar `docker compose ps`, URL local y tag/commit.
2. **Aplicación normal (1 min):** login como Cliente, catálogo, comentario, carrito; mostrar panel Vendedor y área Admin con las cuentas ficticias.
3. **Threat model (1 min):** mostrar DFD, límites de confianza, activos y al menos ocho amenazas STRIDE; indicar que el ID de cada amenaza apunta a una prueba.
4. **SQLi (1–1.5 min):** capturar la búsqueda normal en Burp, mostrar la solicitud, ejecutar el comando sqlmap preconfigurado contra `127.0.0.1:5080`, evidenciar base actual y nombres de tablas. No mostrar/dumpear filas.
5. **XSS almacenado/cookie (1–1.5 min):** iniciar como `cliente-a`, guardar el comentario; abrir otro perfil como víctima `cliente-xss`; mostrar request `/lab/collect` en Burp y reproducir la cookie en `/Account/Profile` para demostrar la suplantación local. Resetear para invalidarla.
6. **IDOR/BOLA y Admin (1–1.5 min):** iniciar como Cliente A; cambiar solo el cart/order ID por el de B en Repeater; mostrar lectura/modificación; luego llamar `/api/admin` como Cliente.
7. **Autenticación (1 min):** comparar en Repeater usuario inexistente y `cliente-brute`; ejecutar el script de cinco passwords secuenciales y mostrar sus requests en Burp Proxy history; resaltar la respuesta aceptada y falta de bloqueo.
8. **JWT (1–1.5 min):** mostrar que el JWT normal de Cliente recibe 401/403 en `/api/admin`; generar localmente la variante con rol Admin usando el secreto débil del laboratorio; enviar en Repeater y mostrar acceso.
9. **Cierre (45 s):** mostrar el informe de cinco hallazgos, vectores/puntajes CVSS v3.1, evidencias, recomendaciones, repo y tag `fase1-redteam`.

## Captura y edición

- Mantener visibles la URL local, la herramienta, el endpoint y el resultado.
- Ocultar claves y credenciales en pantalla, excepto los valores sintéticos que el docente necesite para validar el ejercicio; redactar la cookie inmediatamente después de demostrar el replay.
- No cortar el request/response que prueba el resultado. Si se corta para acortar, conservar el flujo completo como artifact sin edición.
- El video debe corresponder al commit/tag cuyos archivos de evidencia se entregan.

## Checklist de revisión

- [ ] Se ve explícitamente `127.0.0.1` y Docker en ejecución.
- [ ] Aparecen los tres roles y el flujo funcional de la app.
- [ ] STRIDE muestra ocho amenazas como mínimo y el DFD tiene trust boundaries.
- [ ] Los cinco ataques se demuestran con request/resultados y las identidades son ficticias.
- [ ] SQLi muestra nombre de BD y tablas, sin dump de filas.
- [ ] XSS muestra almacenamiento, cookie de sesión capturada y replay local.
- [ ] Se muestran cinco hallazgos con CVSS v3.1 y el commit/tag probado.
