# Plantilla — informe de hallazgo

> **Entorno multi-OS del hito:** el código puede desarrollarse en Windows o Linux; el contenedor de referencia es Linux y Docker Compose, el objetivo y las herramientas de pentest se ejecutan dentro de Debian 13 XFCE en la VM del laboratorio. Los comandos de los runbooks se ejecutan desde esa VM y el objetivo es exclusivamente `http://127.0.0.1:5080` dentro del invitado.

Copiar una sección por cada uno de los cinco hallazgos.

## [ID] Título

- **Severidad y CVSS v3.1:** [puntaje] — `[vector completo]`
- **Activo afectado:**
- **Amenaza STRIDE relacionada:**
- **Versión probada:** rama, commit/tag, fecha y modo de laboratorio.
- **Herramientas/versiones:**
- **URL/ruta local y método:**
- **Rol/cuenta sintética:**

### Resumen

Qué falla, de forma breve y basada en evidencia.

### Precondiciones

Acceso, rol, estado de datos y configuración necesarios para repetirlo.

### Pasos de reproducción

Numerar pasos concisos y referenciar archivos/capturas. Incluir solo instrucciones para el objetivo local de la app.

### Evidencia observada

Request/response sanitizados, captura y resultado. Distinguir hechos observados de inferencias. No incluir secretos ni datos reales.

### Impacto y alcance demostrado

Describir el efecto realmente observado y los límites del laboratorio. No atribuir extracción/escalamiento que no se haya demostrado.

### Cálculo CVSS v3.1

1. Abrir la [calculadora oficial FIRST v3.1](https://www.first.org/cvss/calculator/3.1).
2. Puntuar métricas **Base** según la vulnerabilidad reproducida, no según una intuición sobre severidad. Justificar AV, AC, PR, UI, S y C/I/A con la secuencia de ataque observada.
3. Copiar el vector completo y puntaje exacto; usar el mismo vector en informe, resumen ejecutivo y presentación.
4. Otra persona verifica la selección de métricas contra requests/evidencias. No bajar AV porque la demostración académica corre en localhost si la vulnerabilidad de la app web se explotaría por HTTP cuando esté desplegada; explicar por separado el aislamiento del target de prueba.
5. Si el docente pide puntuación ambiental/temporal, añadirla aparte sin sustituir el Base Score requerido.

| Métrica | Valor | Justificación basada en el escenario |
|---|---|---|
| AV |  |  |
| AC |  |  |
| PR |  |  |
| UI |  |  |
| S |  |  |
| C |  |  |
| I |  |  |
| A |  |  |

**Vector:** `CVSS:3.1/...`
**Puntuación:**
**Calculadora/revisión:** FIRST CVSS v3.1; revisado por otro integrante.

### Recomendación de remediación

Control recomendado para la Fase 2 y prueba que debería demostrar que la corrección funciona.

### Repetibilidad y limpieza

Cómo restaurar la seed, repetir el caso y eliminar/invalidar cuentas, tokens o comentarios usados.
