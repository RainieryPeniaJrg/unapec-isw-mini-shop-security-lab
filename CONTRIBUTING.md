# Contribuir

Este repositorio acompaña un proyecto académico de tres integrantes. Se aceptan cambios de código, documentación, threat model y pruebas que mantengan el alcance reproducible.

## Flujo recomendado

1. Lee el [README](README.md), la [matriz de entregables](docs/matriz-entregables.md) y el documento del área que cambiarás.
2. Actualiza `main` y crea una rama corta: `docs/actualizar-guia`, `feat/catalogo` o `fix/compose-health`.
3. Realiza un cambio acotado y agrega/actualiza la documentación que permita reproducirlo.
4. Ejecuta `dotnet test` en el entorno de desarrollo. Para la prueba integrada, usa la VM Debian 13 XFCE y el Compose del repositorio.
5. Revisa `git diff --check`, confirma que no agregaste secretos ni datos personales y abre un Pull Request.
6. Pide a otra persona del equipo que revise el cambio y, cuando corresponda, lo reproduzca desde una seed limpia.

## Reglas del laboratorio

- El objetivo de cualquier PoC o herramienta es exclusivamente `http://127.0.0.1:5080` dentro de Debian en la VM del laboratorio.
- No apuntes PoCs, Burp, ZAP o sqlmap a otros dominios, redes o servicios.
- Usa solo identidades, tokens, productos y pedidos ficticios.
- No incluyas en el repositorio exportaciones crudas de Burp/ZAP, cookies, contraseñas, llaves, archivos `.env`, video sin revisar ni discos de VM.
- Las evidencias que se quieran publicar deben estar saneadas y guardarse conscientemente; las carpetas `raw/` y `private/` están ignoradas por Git.
- Mantén la app multi-OS a nivel de código fuente, sin dependencias innecesarias de rutas o comandos del sistema anfitrión. El perfil integrado de Docker/ataque del hito usa Linux containers dentro de Debian.

## Convención de commits

Usa mensajes breves que indiquen la acción, por ejemplo:

- `docs: documentar instalación de Debian XFCE`
- `feat: agregar flujo de carrito`
- `test: cubrir autorización de pedidos`
- `fix: limitar Compose a loopback`

No subas directamente una versión vulnerable a un servicio externo. El tag de laboratorio solo identifica el snapshot local reproducible descrito por la consigna.
