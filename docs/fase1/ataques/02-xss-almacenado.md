# F1-11 — XSS almacenado y demostración de cookie de laboratorio

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

## Meta y límite

Mostrar que contenido guardado como comentario o descripción vuelve a ejecutarse en la vista de producto, lee y roba la cookie de sesión de un **usuario ficticio**, y permite reproducir esa sesión en la misma app local. Esa cuenta solo existe en la base descartable del laboratorio; no usar cookies personales ni sistemas externos.

## Requisitos previos

- Comentarios persistentes funcionando y producto sintético de prueba.
- Cuenta atacante sintética (`cliente-a` o `vendedor-lab`) y cuenta víctima `cliente-xss`, autenticada por MVC con cookie `LabSession` válida dentro de la app. La cookie no da acceso fuera del contenedor y se invalida al resetear el laboratorio.
- Endpoint `GET /lab/collect?c=` disponible solo en perfil Red Team y app ligada a `127.0.0.1:5080`.
- Navegador de laboratorio separado de sesiones personales.

## Procedimiento

1. Iniciar sesión en MVC como atacante sintético (`cliente-a` o `vendedor-lab`) y capturar en Burp el envío normal de un comentario.
2. Guardar un comentario identificable con un marcador benigno y verificar persistencia.
3. Sustituir el contenido por la carga siguiente **solo en `127.0.0.1:5080`**:

   ```html
   <img src=x onerror="fetch('/lab/collect?c='+encodeURIComponent(document.cookie))">
   ```

   La ruta es relativa, así que el navegador envía la cookie al mismo host local. No cambiarla por una URL externa. `/lab/collect` responde 204 y no persiste el valor; Burp muestra la solicitud entrante.

4. Cerrar sesión del atacante. Iniciar sesión como víctima `cliente-xss` en un perfil de navegador limpio y abrir el producto con el comentario.
5. En Burp, guardar el request `GET /lab/collect?c=...` y su respuesta 204. La cookie se envía en el parámetro y puede verse en Burp durante la prueba.
6. Copiar temporalmente la cookie capturada a una petición Repeater `GET /Account/Profile` contra `127.0.0.1:5080`. Mostrar que la aplicación responde con la identidad `cliente-xss`; no probarla contra ningún otro host.
7. Reiniciar el contenedor/keys/seed; comprobar que la cookie deja de funcionar y repetir con el mismo commit.

## Evidencia y aceptación

- Request de creación y respuesta que confirme almacenamiento.
- Registro persistido antes de abrir la vista.
- Captura del comentario guardado por el atacante y de `cliente-xss` visualizándolo.
- Request a `/lab/collect` (cookie redactada en archivos permanentes) y response 204.
- Request de replay a `/Account/Profile` y response que identifica a `cliente-xss`; evidencia del reset que invalida la sesión.
- Evidencia del reset que invalida la sesión.
- URL, cookie, tokens o datos personales ajenos al laboratorio implican detener la prueba y descartar la evidencia.

## Impacto y remediación a reportar

Describir que una víctima que visita contenido persistido por otro usuario ejecuta el script, expone su sesión y permite el replay en el laboratorio. Recomendar codificación contextual Razor, evitar renderizar HTML no confiable, CSP y cookies `HttpOnly`, `Secure`, `SameSite`.
