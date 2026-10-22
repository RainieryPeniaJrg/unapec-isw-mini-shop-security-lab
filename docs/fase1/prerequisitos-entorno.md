# Prerrequisitos y entorno recomendado — Equipo principiante

Este documento define un entorno reproducible para desarrollar en Windows o Linux y ejecutar el laboratorio del segundo parcial en una VM Linux alojada en Windows. La mini-app se construye como contenedor Linux para que el mismo proyecto pueda compilarse y ejecutarse desde equipos anfitriones Windows y Linux. Los runbooks de ataque se ejecutan dentro de la VM, contra el contenedor local de esa misma VM.

## Decisión recomendada para este equipo

Para el Lenovo LOQ con **Windows 11, Ryzen 7 y 16 GB de RAM**, usar:

- **Hipervisor:** VMware Workstation Pro en su versión estable vigente. VirtualBox 7.2 es una alternativa válida si el equipo ya la conoce o VMware presenta problemas en ese Windows.
- **VM de pentest:** Debian 13 (64 bits) con escritorio **XFCE**. Es ligera, permite usar herramientas gráficas, y Docker Engine ofrece soporte oficial para Debian 13.
- **Contenedores del laboratorio:** imágenes Linux, con ASP.NET Core 10 y PostgreSQL; Docker Engine y Compose corren dentro de Debian.
- **Desarrollo:** el código puede editarse en Windows o Linux. La persona que desarrolla en Windows puede usar Visual Studio/VS Code y Git en Windows; para una prueba reproducible, subir los cambios y clonar/pull del repositorio dentro de la VM antes de ejecutar Compose y los ataques.
- **Red:** NAT para que la VM descargue paquetes. Docker publica la app solo en `127.0.0.1:5080` dentro de Debian. No activar bridge ni port forwarding.

Así, la app, Docker, Burp, sqlmap, el navegador y la grabación de pantalla de la VM usan el mismo objetivo local. No hay que conectar el pentest a un Docker Desktop del anfitrión Windows.

## Comparativa detallada: VMware Workstation Pro vs VirtualBox

Los dos hipervisores pueden correr la misma VM Debian y no cambian los payloads ni el resultado del pentest. El rendimiento final depende también de Windows 11, VBS/Hyper-V, controladores, versión del hipervisor y memoria disponible; no hay una ventaja de rendimiento garantizada en todos los LOQ.

| Criterio | VMware Workstation Pro | VirtualBox |
|---|---|---|
| Facilidad para principiantes | Interfaz guiada y administración cómoda de VM, snapshots y redes. Descarga gratuita con cuenta de Broadcom y formulario de perfil. | Interfaz conocida en cursos y guías; instalador directo desde Oracle. No requiere cuenta para la descarga básica. |
| Costo/licencia | Broadcom indica disponibilidad gratuita para usos personales, educativos y comerciales en las versiones admitidas. | El paquete base es gratuito. No instalar Extension Pack: este laboratorio no lo necesita. |
| Snapshots y repetición | Adecuado para guardar estados antes de herramientas, antes de los PoC y para volver a un estado limpio. | También soporta snapshots y restauración de estado. |
| Red aislada | NAT permite salida a Internet desde la VM; host-only crea una red privada. No agregar reenvío de puertos. | Ofrece NAT y host-only. Mantener la misma regla: sin puente ni reenvío. |
| Compatibilidad con Windows 11 | Las versiones actuales pueden operar cuando Hyper-V/VBS está habilitado usando Windows Hypervisor Platform; puede cambiar el rendimiento frente al acceso directo a VT-x/AMD-V. | Puede correr con Hyper-V activo, pero Oracle advierte que puede haber degradación de rendimiento. |
| Recursos del LOQ | Suficiente para una VM si se limita a 4 vCPU y 6 GB de RAM, se cierra software pesado y se ejecuta Burp o ZAP de forma alternada. | La misma asignación de recursos; cambiar de hipervisor no aumenta la RAM física. |
| Soporte del equipo | Buena opción estándar si se valida en el LOQ con el Windows ya configurado. | Alternativa útil si se prioriza encontrar guías públicas o si VMware no inicia correctamente en el equipo. |

**Recomendación concreta:** iniciar con VMware Workstation Pro y comprobar la prueba de aceptación de este documento. No desactivar VBS, Hyper-V, Core Isolation o funciones de seguridad de Windows para intentar ganar velocidad. Si VMware no funciona de forma estable con la configuración de Windows del LOQ, probar VirtualBox con la misma VM y repetir la prueba; no instalar ambos para el uso diario ni mantener dos VMs distintas.

La disponibilidad gratuita de Workstation Pro y el requisito de cuenta de descarga constan en [Broadcom](https://knowledge.broadcom.com/external/article/368667/download-and-license-free-vmware-desktop.html). Broadcom también documenta posibles cambios de rendimiento con VBS/Hyper-V en [Windows 11](https://knowledge.broadcom.com/external/article/417896/after-the-host-os-upgrade-to-version-win.html). Oracle advierte sobre degradación de VirtualBox cuando Hyper-V está activo en su [guía de usuario](https://www.virtualbox.org/manual/).

## Reparto de recursos del Lenovo LOQ (16 GB)

La máquina es **viable para el hito** si se trabaja de forma secuencial y se mantiene la VM ligera. No es adecuado asignar 10–12 GB a la VM con solo 16 GB físicos cuando Windows, la grabación y las herramientas también consumen memoria.

| Recurso | Asignación inicial | Ajuste si hay lentitud |
|---|---:|---:|
| Memoria VM | **6 GB** | Subir a 7 GB solo al cerrar IDEs, navegadores y apps innecesarias en Windows |
| CPU VM | **4 vCPU** | Mantener 4; no asignar todos los hilos del Ryzen 7 |
| Disco VM | **60 GB dinámico** | 70 GB si conservarán varios snapshots y reportes locales |
| Windows anfitrión | Reservar aproximadamente 8–9 GB para Windows, hipervisor y captura | Cerrar IDE, Teams, launchers de juegos y pestañas sobrantes |
| Espacio SSD | 70 GB libres antes de crear la VM | Guardar ISO, snapshots, imágenes Docker y video en SSD local |

En Windows, grabar la ventana de VMware con **Recortes / Snipping Tool** o Xbox Game Bar; evitar OBS mientras se ejecutan los PoC si la memoria está bajo presión. Ejecutar Burp como herramienta principal y cerrar ZAP. Para una demostración con Burp y ZAP abiertos simultáneamente, varias aplicaciones de desarrollo y grabación, **32 GB de RAM** es la mejora recomendada. PortSwigger indica 4 GB como mínimo para tareas básicas de Burp; por eso el laboratorio de 16 GB debe limitar el heap de Burp a unos 2 GB y evitar escaneos intensivos.

## Distro invitada

**Debian 13 XFCE, 64 bits**, instalada desde una ISO oficial de Debian. XFCE reduce el consumo del escritorio sin quitar la interfaz gráfica necesaria para Burp, ZAP y navegador. Docker Engine publica instrucciones oficiales de instalación para Debian 13. No usar Kali como base principal: trae más utilidades de las requeridas y no necesitamos mantener dos distros en este equipo.

Al instalar Debian, seleccionar el escritorio XFCE y utilidades estándar; no marcar otros escritorios. Mantener actualizaciones de seguridad. Para el desarrollo integrado del contenedor no hace falta instalar .NET SDK en la VM si el Dockerfile compila usando la imagen SDK de Microsoft. Se instala Git, Docker Engine/Compose y las herramientas del pentest.

## Configuración inicial de la VM

1. Crear la VM `ISW-MiniShop-Pentest` como Linux/Debian de 64 bits.
2. Asignar 6 GB RAM, 4 vCPU y disco virtual dinámico de 60 GB.
3. Activar virtualización AMD-V/SVM en UEFI/BIOS del Lenovo si está deshabilitada.
4. Configurar **NAT**. No seleccionar Bridge y no añadir reglas de port forwarding.
5. Instalar Debian 13 XFCE y actualizar el sistema.
6. Crear usuario local exclusivo para el laboratorio.
7. Tomar snapshot `00-debian-limpio`.
8. Instalar Git, Docker Engine/Compose, Burp Community, sqlmap, Python/PyJWT y ZAP.
9. Tomar snapshot `01-herramientas`; clonar el repo dentro de la VM, construir la app y comprobar el health endpoint.
10. Tomar snapshot `02-lab-listo` antes de ejecutar los ataques.

Los snapshots sirven para recuperar el sistema, pero no reemplazan `docker compose down -v`: el reset de Compose restaura los datos/semillas de la aplicación y debe hacerse según el runbook.

## Desarrollo y ejecución multi-OS

| Parte del trabajo | Windows 11 | Linux |
|---|---|---|
| Editar C#, Markdown, DFD/STRIDE y Dockerfile | Visual Studio o VS Code; Git para Windows | VS Code/Rider/editor disponible; Git |
| Compilar/testear en host | .NET 10 SDK opcional para feedback rápido | .NET 10 SDK opcional para feedback rápido |
| Ejecutar el laboratorio que se ataca | Se hace dentro de Debian XFCE VM para mantener app y herramientas en el mismo loopback | Se hace dentro de la misma VM Debian canónica para reproducir el ataque |
| Imágenes de contenedor del hito | Linux containers para el baseline común | Las mismas Linux containers |
| Sistema operativo del host de CI | GitHub Actions Linux runner | GitHub Actions Linux runner |

El código .NET debe evitar APIs específicas del sistema operativo y usar rutas/contenidos compatibles. El hito fija Linux containers como contrato de ejecución por ser el entorno reproducible común para Docker, Debian y CI. Windows containers no forman parte de esta configuración: requieren un daemon/modo de contenedor diferente y aumentarían la complejidad y los recursos sin aportar a los ataques solicitados. Si la consigna exige explícitamente Windows containers, habrá que añadir un segundo perfil y probarlo aparte; no alternarlo durante el pentest.

En el Lenovo, clonar el repositorio **dentro de Debian** y ejecutar los comandos del README allí. Esto evita exponer el puerto desde Windows hacia la VM. Para entregar cambios desde Windows, usar Git push/pull; no compartir `.env`, tokens ni datos personales.

## Herramientas dentro de Debian

| Componente | Uso | Verificación |
|---|---|---|
| Git | Clonar y fijar el tag `fase1-redteam` | `git --version` |
| Docker Engine + Compose plugin | Construir y correr app/PostgreSQL | `docker version`; `docker compose version` |
| Burp Suite Community | Proxy, HTTP history, Repeater y evidencia principal | Abrir Burp; listener local `127.0.0.1:8080` |
| Burp Browser | Navegador de laboratorio integrado | Abrir `http://127.0.0.1:5080` con la app arriba |
| sqlmap | Confirmación SQLi blind limitada a metadatos | `sqlmap --version` |
| Python 3 + `requests` | Script secuencial limitado de autenticación | `python3 --version`; `requests` dentro de venv |
| PyJWT | Alteración controlada del token sintético | Instalar en venv separado |
| OWASP ZAP | Comprobación pasiva complementaria | Abrir y limitar Context al loopback |
| Grabación | Video de entrega | Capturar la VM desde Windows; verificar audio/ventana antes de grabar |

No abrir Burp y ZAP durante un mismo caso con este equipo de 16 GB. Ejecutar Burp para la evidencia requerida; ZAP se puede abrir después para una verificación complementaria.

## Instalación y verificaciones

Instalar Docker Engine/Compose desde la documentación oficial de Docker para Debian 13 y descargar Burp/ZAP únicamente de sus sitios oficiales. Mantener los comandos de ataque del repo en la terminal Linux de la VM.

```bash
git --version
docker version
docker compose version
python3 --version
sqlmap --version
```

El Dockerfile compila con la imagen SDK de Microsoft y ejecuta la imagen runtime de ASP.NET Core. Esto evita obligar a todos los anfitriones a instalar el mismo SDK para construir el contenedor. Si alguien compila directamente en Windows o Linux para desarrollo, usar .NET SDK 10 y ejecutar `dotnet test` antes de integrar.

## Aislamiento de red y prueba de aceptación

- Mantener la NIC de VMware/VirtualBox en NAT para las descargas; no usar modo puente.
- Docker publica solo `127.0.0.1:5080` **dentro de Debian**.
- Navegador, Burp, ZAP y sqlmap también se ejecutan dentro de Debian y usan ese loopback.
- El repositorio y Compose se ejecutan dentro de la VM; no publicar el servicio desde Docker Desktop de Windows para esta práctica.
- No configurar port forwarding, regla de firewall ni exposición a Wi-Fi/LAN.
- Antes del pentest, `docker compose port shop 8080` debe mostrar `127.0.0.1:5080`; si muestra `0.0.0.0` u otra interfaz, detenerse.

Prueba de aceptación (hacerla con el perfil de laboratorio y la seed sintética):

```bash
git clone https://github.com/RainieryPeniaJrg/unapec-isw-mini-shop-security-lab.git
cd unapec-isw-mini-shop-security-lab
docker compose up --build -d
docker compose ps
docker compose port shop 8080
curl -i http://127.0.0.1:5080/api/health
```

Debe responder el health endpoint y el mapeo debe seguir ligado a `127.0.0.1`. Abrir la app desde Burp Browser, capturar una petición normal en HTTP history y verificar que scope contiene solo el host local. Ejecutar luego `docker compose down -v` y levantar de nuevo; confirmar que la seed regresó a valores sintéticos.

## Flujo de práctica para principiantes

1. Una persona conduce leyendo el runbook; otra registra fecha, herramienta, request/response y resultado.
2. La tercera revisa que la VM, el puerto, la URL y el host objetivo sean los del laboratorio.
3. Ejecutar un solo PoC a la vez; no lanzar ZAP, Burp Intruder ni sqlmap en paralelo.
4. Guardar evidencia, resetear Compose y repetir desde seed limpia.
5. Rotar roles. Para grabar, cerrar aplicaciones no requeridas y conservar en pantalla Windows/VM, Burp, host local, commit y evidencia.

## Lista de verificación previa

- [ ] Windows 11 del LOQ tiene virtualización AMD-V/SVM activa y al menos 70 GB libres en SSD.
- [ ] VMware Workstation Pro está instalado; si falla la prueba, VirtualBox queda como alternativa con la misma VM.
- [ ] Debian 13 XFCE inicia con 6 GB RAM y 4 vCPU sin dejar Windows inutilizable.
- [ ] Git, Docker Compose, Burp, sqlmap y Python muestran sus versiones.
- [ ] Compose corre dentro de Debian y el puerto de la app es `127.0.0.1:5080` en la VM.
- [ ] El target funciona y los tres roles usan datos sintéticos.
- [ ] Se puede volver a `02-lab-listo` y resetear la base sin perder el repo/evidencias.
- [ ] Se probó grabar una sección breve sin abrir ZAP simultáneamente con Burp.
- [ ] El repo y los runbooks identifican Debian VM como entorno de ejecución del pentest.

## Referencias oficiales

- [Docker Engine: instalación en Debian](https://docs.docker.com/engine/install/debian/)
- [Broadcom: descarga y licencia gratuita de VMware Workstation Pro](https://knowledge.broadcom.com/external/article/368667/download-and-license-free-vmware-desktop.html)
- [Broadcom: rendimiento de VMware con Windows 11 24H2/VBS](https://knowledge.broadcom.com/external/article/417896/after-the-host-os-upgrade-to-version-win.html)
- [Oracle VirtualBox: guía de usuario 7.2](https://www.virtualbox.org/manual/)
- [Debian 13 — descarga e instalación](https://www.debian.org/releases/trixie/)
- [PortSwigger: requisitos de Burp Suite](https://portswigger.net/burp/documentation/desktop/getting-started/system-requirements)
- [Microsoft: imágenes oficiales .NET para contenedores](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/net-core-net-framework-containers/official-net-docker-images)
- [Burp Suite Community](https://portswigger.net/burp/communitydownload)
- [OWASP ZAP](https://www.zaproxy.org/download/)
- [sqlmap](https://github.com/sqlmapproject/sqlmap)
