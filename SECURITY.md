# Seguridad y alcance

## Uso intencionalmente vulnerable

Este proyecto es un laboratorio educativo. La versión Red Team incorpora fallas deliberadas para que el equipo practique la consigna. El código público no debe desplegarse como una aplicación real ni exponerse a Internet, LAN, Wi-Fi o redes del campus. No uses datos, cuentas, credenciales, tokens o secretos reales.

El entorno autorizado es la mini-app propia ejecutada por Docker Compose dentro de Debian 13 XFCE en la VM de laboratorio. La URL objetivo es `http://127.0.0.1:5080` desde esa VM. Las pruebas quedan dentro de ese alcance.

## Vulnerabilidades deliberadas

Los runbooks y la matriz describen los defectos académicos que se implementarán en el perfil local. Su presencia no equivale a una invitación a probar sistemas ajenos ni a reportar como sorpresa las fallas que la propia consigna pide demostrar.

## Reportar un problema no intencional

Si encuentras una exposición accidental de secretos, datos personales, puertos o infraestructura fuera del alcance documentado, no publiques el detalle ni lo pruebes contra sistemas ajenos. Contacta al mantenedor por un canal privado. Si GitHub Private Vulnerability Reporting está habilitado para este repositorio, puede utilizarse ese canal.

Antes de abrir un issue público, elimina tokens/cookies/contraseñas, datos personales y evidencia que permita acceder a servicios reales.
