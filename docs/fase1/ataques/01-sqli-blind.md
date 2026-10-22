# F1-10 — SQL Injection blind time-based con extracción limitada

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

## Meta y alcance

Demostrar que el parámetro de búsqueda de productos permite inferir información mediante tiempos de respuesta y extraer **solo** el nombre de la base de datos y los nombres de tablas sintéticas. El objetivo exclusivo es la app local del equipo. No extraer filas ni datos de usuarios.

## Requisitos previos

- Mini-app y PostgreSQL de laboratorio arriba en `127.0.0.1:<puerto>`.
- Ruta de búsqueda fijada en el contrato (candidata: `GET /api/products/search?q=`).
- Base reiniciada y con seed ficticia; registrar el commit.
- Burp configurado para scope local; sqlmap instalado.

## Procedimiento

1. Desde el navegador, realizar una búsqueda normal y capturar el request en Burp. Guardar una copia de baseline y medir varias respuestas normales para conocer la latencia aproximada.
2. En Repeater, variar solo `q` y buscar una diferencia de respuesta/tiempo repetible. Registrar repeticiones y condición de laboratorio; no usar una carga con escritura o borrado.
3. Ejecutar sqlmap únicamente contra el request/URL local y parámetro `q`, con la técnica `T` (time-based), retraso corto y concurrencia mínima. Limitar la enumeración a `--current-db` y `--tables`; no usar `--dump` ni extraer contenido de filas.
4. Comparar el nombre de BD y tablas inferidos con la configuración/seed local, sin mostrar credenciales de conexión.
5. Repetir con seed limpia y guardar la salida relevante, request/responses y versión de sqlmap.

Para que Burp y sqlmap documenten la misma prueba: en Burp History seleccionar la solicitud `GET /api/products/search?q=demo`, elegir **Copy URL** y comparar host/parámetro con el comando de abajo. En el video mostrar primero la solicitud original y luego el comando/outcome de sqlmap.

Comando acordado para el contrato fijado (si el puerto local se cambia en Compose, sustituir solo `5080`):

```text
sqlmap -u "http://127.0.0.1:5080/api/products/search?q=demo" -p q --dbms=PostgreSQL --technique=T --time-sec=2 --current-db --tables --batch --threads=1 --timeout=10 --retries=1 --output-dir=artifacts/sqlmap
```

Este endpoint será de lectura de catálogo y no debe requerir sesión. Antes de ejecutar, copiar y revisar el comando: el host tiene que ser `127.0.0.1`, el parámetro debe ser `q`, el motor PostgreSQL y la técnica `T`. No ejecutar si el host no empieza exactamente por `http://127.0.0.1:`. `--current-db` y `--tables` enumera metadatos; no añadir `--dump`, `--os-shell`, `--file-read` ni otras opciones.

## Evidencia y aceptación

- Request normal y request modificado en Burp.
- Medidas de baseline y respuesta demorada repetibles.
- Salida que identifica el nombre de la BD y al menos una tabla ficticia.
- Captura de que el target es local, commit y seed utilizados.
- Sin filas/contraseñas volcadas. Si sqlmap no detecta la señal, documentar ese resultado y corregir el escenario de laboratorio antes de grabar; no aumentar el alcance del objetivo.

## Impacto y remediación a reportar

Explicar qué metadatos expone el flujo y qué acceso adicional podría habilitar bajo las condiciones observadas. Recomendar EF Core/consultas parametrizadas, whitelist para filtros/ordenamiento y mínimo privilegio de PostgreSQL. No afirmar extracción de datos que no se haya demostrado.
