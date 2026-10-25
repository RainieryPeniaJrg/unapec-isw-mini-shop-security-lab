# UNAPEC ISW — Mini Shop Security Lab

Laboratorio académico de seguridad de software para el segundo parcial de UNAPEC. La aplicación de Fase 1 es una mini tienda ASP.NET Core MVC/Razor y API con PostgreSQL: login, catálogo, comentarios, carrito, pedidos y paneles de Vendedor/Admin. El perfil Red Team contiene los cinco escenarios vulnerables definidos en la documentación. La Fase 2 queda fuera del desarrollo actual.

La evidencia manual Burp/ZAP, el informe con CVSS, el video y el tag de entrega requieren completar la revisión académica; no se consideran entregados por disponer del código y las pruebas automatizadas.

> **Aviso:** el perfil Red Team tiene vulnerabilidades intencionales. No despliegues esta aplicación en Internet, en una red del campus ni en un entorno con datos reales. Solo se prueba contra la instancia local propia y con cuentas ficticias. Las credenciales seed son públicas y desechables; no las sustituyas por cuentas personales.

## En dos minutos

- **Desarrollo:** se puede editar y revisar el código desde Windows o Linux.
- **Entorno canónico del hito:** Windows 11 como anfitrión; Debian 13 XFCE en VMware Workstation Pro (VirtualBox es alternativa); Docker Engine dentro de la VM.
- **Contenedores:** imágenes Linux, con ASP.NET Core 10 y PostgreSQL.
- **Pentest y video:** ejecutar Docker Compose, la app, Burp y los comandos de ataque dentro de la VM Debian. El objetivo es `http://127.0.0.1:5080` visto desde la VM.
- **Recursos del Lenovo LOQ de 16 GB:** VM inicial de 6 GB RAM, 4 vCPU y disco dinámico de 60 GB; no ejecutar Burp y ZAP a la vez.

La instalación de la distro, configuración de VMware/VirtualBox, aislamiento, recursos y herramientas está en [Prerrequisitos y entorno](docs/fase1/prerequisitos-entorno.md).

## Ruta recomendada para el equipo

| Paso | Qué hacer | Guía / resultado |
|---|---|---|
| 1 | Revisar alcance, restricciones y criterios de aceptación | [Plan maestro](docs/plan-maestro.md) · [Matriz de entregables](docs/matriz-entregables.md) |
| 2 | Preparar Windows, la VM Debian y las herramientas | [Prerrequisitos y entorno](docs/fase1/prerequisitos-entorno.md) |
| 3 | Cerrar funciones, roles, rutas, cuentas ficticias y datos | [Especificación del laboratorio](docs/fase1/especificacion-laboratorio.md) · [Especificación del proyecto](docs/especificacion-proyecto.md) |
| 4 | Construir la app .NET y el Compose local | [Plan de trabajo](docs/plan-trabajo.md) · [README de Fase 1](docs/fase1/README.md) |
| 5 | Completar el DFD y registrar amenazas STRIDE | [DFD](threat-model/dfd.md) · [STRIDE](threat-model/stride.md) |
| 6 | Preparar Burp/ZAP y las evidencias | [Herramientas](docs/fase1/herramientas.md) · [Procedimiento de pentest](docs/fase1/procedimiento-pentest.md) |
| 7 | Ejecutar cada prueba solo contra la app propia en la VM | [Runbooks de ataques](#runbooks-de-ataques) |
| 8 | Completar informe, CVSS, video y revisión | [Plantilla de hallazgo](docs/fase1/plantilla-informe-hallazgo.md) · [Guion de video](docs/fase1/video-guion.md) |
| 9 | Verificar, etiquetar y entregar el commit reproducible | [Criterios y matriz](docs/matriz-entregables.md) |

## Alcance del segundo parcial

1. Mini-app web propia en .NET con roles Cliente, Vendedor y Admin, login, catálogo, CRUD de productos, comentarios y carrito/pedido.
2. Git y Docker Compose reproducibles, con PostgreSQL y datos/cuentas sintéticos.
3. DFD de la app real e inventario de activos, entradas y límites de confianza.
4. Registro STRIDE con al menos ocho amenazas enlazadas a superficies y pruebas.
5. Cinco demostraciones locales: SQLi blind time-based, XSS almacenado, IDOR/BOLA, autenticación débil y elevación mediante JWT débil o `none` según la consigna.
6. Evidencias Burp Suite (herramienta principal) y ZAP cuando aplique; sqlmap complementa SQLi.
7. Informe de hallazgos con impacto, precondiciones, evidencia, CVSS v3.1 y recomendación.
8. Video que corresponde al mismo commit/tag que se probó.

La [matriz](docs/matriz-entregables.md) es la fuente de aceptación por fila. La [planificación consolidada](docs/plan-maestro.md) separa el hito del segundo parcial del trabajo posterior de remediación.

## Preparar y levantar el entorno

Estos comandos se ejecutan **dentro de Debian 13 XFCE en la VM**, no en PowerShell ni contra Docker Desktop del anfitrión. El código puede editarse desde Windows o Linux; sincroniza cambios con Git y clona el repo en la VM para la prueba integrada.

```bash
git clone https://github.com/RainieryPeniaJrg/unapec-isw-mini-shop-security-lab.git
cd unapec-isw-mini-shop-security-lab
cp .env.example .env
docker compose up --build -d
docker compose ps
docker compose port shop 8080
curl -i http://127.0.0.1:5080/api/health
```

La copia de `.env.example` activa explícitamente `LAB_MODE=redteam` y la clave JWT desechable. Sin esa configuración el modo vulnerable está desactivado; ese comportamiento base no equivale a implementar los controles de Fase 2. No subas `.env` al repositorio.

El mapeo debe ser `127.0.0.1:5080`. Detente si `docker compose port shop 8080` muestra `0.0.0.0` u otra interfaz. No configures Bridge, port forwarding, reglas de router/firewall para exponer el puerto, ni escanees sistemas externos.

Abre `http://127.0.0.1:5080/Products`. El login web está en `/Account/Login`; el login API es `POST /api/auth/login` con JSON `username` y `password`.

| Cuenta sintética | Contraseña local | Recorrido |
|---|---|---|
| `cliente-a` | `LabClienteA2026!` | Catálogo, comentarios, carrito y pedidos |
| `cliente-b` | `LabClienteB2026!` | Recursos propios para comparación BOLA |
| `cliente-xss` | `LabXss2026!` | Víctima ficticia y perfil MVC |
| `cliente-brute` | `LabPass2026!` | Intentos limitados del runbook de autenticación |
| `vendedor-lab` | `LabSeller2026!` | CRUD propio en `/Seller/Products` |
| `admin-lab` | `LabAdmin2026!` | Panel `/Admin` y CRUD de productos |

El Cliente usa `/Cart` y `/Orders`. En una seed limpia, A tiene carrito/pedido `1`; B tiene carrito/pedido `2` e ítem de carrito `2`. Los precios se almacenan en centavos. Vendedor no compra y Admin consulta datos sintéticos sin modificar carritos/pedidos de clientes.

Los cinco escenarios están aislados en Red Team: búsqueda SQLi con principal PostgreSQL de solo lectura, comentarios sin codificación contextual, BOLA en recursos de clientes, enumeración/intentos de login sin límite y JWT HS256 con clave débil. El bypass de `/api/admin` con cookie MVC es distinto de la elevación JWT: el JWT Cliente normal recibe 403; el token firmado como Admin permite la comparación del runbook.

Para restablecer PostgreSQL y los datos sintéticos después de guardar evidencia:

```bash
docker compose down -v
docker compose up --build -d
```

`down -v` elimina el volumen de PostgreSQL. Guarda primero las evidencias necesarias y no incluyas cookies, contraseñas, tokens ni datos personales en commits públicos.

## Modelo y mantenimiento del código

### Actualizar una copia existente

`git clone` descarga el repositorio por primera vez; `git pull` actualiza una copia ya clonada. Dentro de la carpeta del repositorio, ejecuta:

```bash
git status
git pull --ff-only origin main
docker compose up --build -d
docker compose ps
```

Si tienes cambios locales, consérvalos en un commit o mediante `git stash push -u` antes del pull; después puedes recuperarlos con `git stash apply` y revisar posibles conflictos. Si Git indica que las ramas divergieron, no fuerces la actualización: revisa `git log --oneline --graph --all` con el equipo.

Conserva tu `.env` y el volumen de PostgreSQL al actualizar. Copia `.env.example` únicamente cuando aún no exista `.env`; una actualización normal no requiere `docker compose down -v`. El arranque aplica las migraciones y valida el esquema anterior antes de adoptarlo.

### Estructura Code First

Las entidades se definen en C# y su configuración relacional vive en clases Fluent API por entidad. Las migraciones de EF Core versionan el esquema; el inicializador de base de datos y la seed de laboratorio tienen responsabilidades separadas. Los controladores dependen de interfaces específicas para autenticación, catálogo, comentarios, carrito, pedidos y administración.

El refactor conserva las rutas, los DTO, el esquema de negocio y los escenarios vulnerables de Fase 1. La guía [Code First y migraciones](docs/code-first.md) explica cómo crear migraciones y adoptar una base previa creada con `EnsureCreated` sin borrar sus datos.

## Runbooks de ataques

Cada archivo contiene precondiciones, alcance, pasos y evidencia esperada. Completar las rutas/cuentas con la especificación aprobada y no cambiar el host objetivo:

1. [SQLi blind time-based](docs/fase1/ataques/01-sqli-blind.md)
2. [XSS almacenado y captura de cookie local](docs/fase1/ataques/02-xss-almacenado.md)
3. [IDOR/BOLA y control de acceso](docs/fase1/ataques/03-idor-bola.md)
4. [Autenticación débil, enumeración e intentos limitados](docs/fase1/ataques/04-autenticacion.md)
5. [JWT débil o `none`](docs/fase1/ataques/05-jwt.md)

Sigue primero el [procedimiento común de pentest](docs/fase1/procedimiento-pentest.md), registra evidencia en la estructura de [evidencias](docs/fase1/evidencias/README.md) y completa el [guion del video](docs/fase1/video-guion.md).

## Estructura del repositorio

```text
.
├── .github/workflows/       # CI de referencia
├── docs/
│   ├── fase1/               # entorno, procedimientos, runbooks y evidencias
│   ├── especificacion-proyecto.md
│   ├── matriz-entregables.md
│   ├── plan-maestro.md
│   └── plan-trabajo.md
├── src/MiniShop.Api/        # ASP.NET Core
├── tests/                   # pruebas automatizadas y configuración PostgreSQL
├── threat-model/            # DFD y STRIDE del alcance Fase 1
├── compose.yaml             # app + PostgreSQL, bind loopback
└── Dockerfile               # build multi-stage .NET
```

## Colaboración y calidad

- Usa ramas pequeñas y Pull Requests; revisa los cambios de otra persona antes de integrar.
- No subas `.env`, claves, certificados, exportaciones de Burp/ZAP sin sanear, videos sin revisar ni archivos de VM.
- Mantén README, runbooks, DFD, STRIDE y matriz sincronizados con las rutas y el commit real.
- Antes de entregar, ejecuta `dotnet test tests/MiniShop.Api.Tests/MiniShop.Api.Tests.csproj` desde un host con .NET SDK 10 y PostgreSQL de pruebas configurado según [tests/README.md](tests/README.md); repite el laboratorio desde la VM Debian limpia.
- No declares implementados los flujos o ataques que sigan marcados como pendientes.

Las reglas de contribución y seguridad están en [CONTRIBUTING.md](CONTRIBUTING.md) y [SECURITY.md](SECURITY.md). No se asigna una licencia de reutilización a este repositorio por ahora.
