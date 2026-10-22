# F1-12 — Broken Access Control: IDOR/BOLA y endpoint Admin

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

## Meta

Demostrar con dos clientes sintéticos que la app confía en un identificador suministrado por el solicitante y permite leer/modificar un carrito o pedido ajeno. Comprobar también que un Cliente puede alcanzar indebidamente `/api/admin` en el perfil de laboratorio.

## Requisitos previos

- Cuentas Cliente A y Cliente B separadas, ambas ficticias.
- Carrito/pedido de B con datos sintéticos y su ID; conservar el ID de A como control.
- Iniciar sesión como A y capturar la cookie/token de laboratorio en Burp. Mantener B desconectado.

## Procedimiento

1. Como A, consultar el propio carrito/pedido y guardar la respuesta de referencia.
2. Enviar la solicitud a Repeater y reemplazar únicamente el ID de A con el de B. Forma de la petición de lectura:

   ```http
   GET /api/cart/<CART_B_ID> HTTP/1.1
   Host: 127.0.0.1:5080
   Authorization: Bearer <JWT_DE_CLIENTE_A>
   ```

   `<CART_B_ID>` se obtiene del seed local; `JWT_DE_CLIENTE_A` se obtuvo al autenticar Cliente A. Mantener el token de A sin cambios.

3. Probar lectura primero. Si se observa acceso indebido, documentar qué campos sintéticos quedaron expuestos.
4. Para demostrar edición, cambiar solo una propiedad inocua de un ítem de prueba (por ejemplo, cantidad):

   ```http
   PUT /api/cart/<CART_B_ID>/items/<ITEM_B_ID> HTTP/1.1
   Host: 127.0.0.1:5080
   Authorization: Bearer <JWT_DE_CLIENTE_A>
   Content-Type: application/json

   {"quantity": 2}
   ```

   Comparar el estado antes/después consultando como B o inspeccionando la seed local; restaurar la seed después.

5. Con la misma identidad A, llamar `GET /api/admin` y registrar respuesta/código y contenido visible.
6. Repetir desde estado limpio. No borrar cuentas ni modificar recursos fuera de los datos de prueba.

## Evidencia y aceptación

- Identidades A/B, IDs de recurso y titular esperado, redactando tokens.
- Request original de A y request alterado; responses completos con datos ficticios.
- Para escritura, prueba de que el estado de B cambió y luego fue restaurado.
- Request a `/api/admin` y evidencia del acceso obtenido como Cliente.

## Impacto y remediación a reportar

Separar exposición de datos (BOLA) de modificación no autorizada (IDOR/authorization bypass). Recomendar obtener el owner desde la identidad autenticada y verificar autorización por rol y ownership en cada acción/objeto. Cambiar un ID no debe bastar para acceder al recurso.
