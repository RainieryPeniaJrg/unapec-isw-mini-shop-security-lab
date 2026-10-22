# Estructura de evidencias — Fase 1

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

Guardar capturas y requests redactados en esta estructura. Los tokens/cookies originales solo se usan temporalmente en la estación de laboratorio y no se suben al repositorio.

```text
evidencias/fase1/
  00-entorno/
    compose-ps.png
    commit-tag.txt
    versiones-herramientas.txt
  01-threat-model/
    dfd-render.png
    stride.pdf-o-captura.png
  F1-10-sqli/
    baseline-request.txt
    sqlmap-metadata-redacted.txt
    timing.png
  F1-11-xss/
    comment-create-redacted.txt
    stored-render.png
    lab-collect-redacted.txt
    session-replay.png
  F1-12-idor-bola/
    cart-owner-a.txt
    cart-id-b-read.txt
    cart-id-b-update.txt
    api-admin-as-client.txt
  F1-13-auth/
    enumeration-comparison-redacted.txt
    intruder-five-attempts.png
  F1-14-jwt/
    client-denied.png
    altered-token-admin-response-redacted.txt
  informe-cvss.md
  video-guion.md
  video-entrega.mp4
```

Si el repo es público, no subir captures con session cookie/JWT ni el secreto débil. Entregar evidencia sanitizada por el medio que acepte la asignatura. Nunca guardar la cookie completa en logs de app; redactar cualquier copia del request.

## Convención de nombres

`<ID>-<paso>-<commit-corto>-<fecha>.ext`, por ejemplo `F1-12-read-bola-a1b2c3d-2026-10-22.png`.

Cada archivo debe poder asociarse al hallazgo, orden de paso, versión probada y cuenta sintética sin revelar secretos reutilizables.
