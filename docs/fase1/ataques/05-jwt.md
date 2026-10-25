# F1-14 — JWT débil o `none` y elevación de privilegio

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

## Decisión del escenario

La consigna acepta algoritmo `none` **o** secreto débil. Para este proyecto se fija **secreto débil HS256** (`MiniShopLab2026`) en el perfil Red Team: es reproducible con .NET sin crear un validador casero. La variante `none` queda como alternativa solo si el docente la exige explícitamente.

## Meta y límites

Con un usuario Cliente sintético, alterar únicamente un JWT emitido por la propia app para intentar elevar el claim/rol a Admin y llamar `/api/admin`. No usar tokens de servicios reales, llaves de producción ni herramientas web externas donde pegar un token.

## Requisitos previos

- Endpoint API que emita JWT de laboratorio tras login.
- Cliente ficticio y endpoint administrativo; registrar el estado normal (Cliente recibe 401/403).
- Herramienta local para decodificar/firmar JWT; no cargar tokens en jwt.io ni servicios públicos.
- Token/cuenta invalidados al terminar.

## Procedimiento — secreto débil (opción recomendada)

1. En Burp, iniciar sesión por `POST /api/auth/login` como `cliente-a` y guardar el JWT solo temporalmente. Guardar la respuesta normal de `GET /api/admin` (debe ser 403 con un JWT Cliente válido). Enviar únicamente `Authorization: Bearer <token>`; retirar la cookie MVC de esta prueba para aislarla del bypass con sesión del runbook F1-12.
2. Decodificar header/payload localmente para identificar algoritmo y nombre del claim de rol. No cambiar firma todavía; confirmar que el token normal no da acceso a `/api/admin`.
3. Con PyJWT local, mantener los claims del token emitido y cambiar únicamente el claim de rol a Admin. El siguiente script solicita token y secreto de forma interactiva para no dejarlos en el historial del shell. Ajustar `role` solo si el JWT de la app usa otro nombre de claim:

   ```python
   # forge_lab_token.py — usar solo con el JWT de Mini Shop local
   from getpass import getpass
   import jwt

   original = getpass("JWT de cliente emitido por el laboratorio: ")
   secret = getpass("Secreto HS256 de laboratorio (MiniShopLab2026): ")
   claims = jwt.decode(original, options={"verify_signature": False, "verify_exp": False})
   claims["role"] = "Admin"
   print(jwt.encode(claims, secret, algorithm="HS256"))
   ```

   Preparación de entorno, si PyJWT no está instalado: `python3 -m venv .venv-jwt` y luego `./.venv-jwt/bin/pip install 'PyJWT>=2,<3'`. Guardar el script temporalmente en la VM o en una carpeta local ignorada; no incluir tokens/secretos en el repositorio público.

4. Copiar el token recién generado en Burp Repeater, modificar `Authorization: Bearer <token>` de la llamada a `GET /api/admin` y enviar. Registrar si el servidor acepta la firma y concede acceso sintético.
5. Revocar/eliminar token y reiniciar seed. Repetir desde estado limpio.

## Variante `none` (solo si fue elegida)

Seguir la misma secuencia de token de Cliente; cambiar el claim de rol y probar un token sin firma únicamente contra el validador local de laboratorio. Registrar el algoritmo enviado y respuesta. No desarrollar validación criptográfica propia para forzar la demostración.

## Evidencia y aceptación

- Token de Cliente original redactado; header/payload con valores ficticios; método/configuración de firma de laboratorio.
- Respuesta normal denegada y respuesta del token alterado que muestra acceso al endpoint, con datos sintéticos.
- Evidencia de URL localhost, commit y versión de librería.
- No publicar una firma secreta que se reutilice fuera del perfil de laboratorio; describirla como clave desechable y eliminarla al concluir.

## Impacto y remediación a reportar

Explicar la elevación de privilegio observada y el activo administrativo accesible. Recomendar algoritmo permitido fijado en RS256, clave privada protegida, validación de issuer/audience/expiración, token corto y refresh rotativo en DB (Fase 2).
