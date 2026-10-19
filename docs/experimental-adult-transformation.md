# Transformación adulta experimental — iteración 1

## Alcance comprobable

El motor `FO4_NPC_Manager/NPC/AdultTransformation.vb` transforma **copias de DTO sintéticos**,
no plugins. El proyecto WinForms lo incorpora por los items de compilación predeterminados;
`Tools/AdultTransformationGate` compila ese mismo archivo sin copiar la implementación,
sin dependencias NuGet ni recursos de los juegos. No hay ESP, FaceGen, voces nuevas ni
instalador en esta iteración. La selección de apariencia/cuerpo es una referencia a un fixture,
no una modificación de NIF, DDS o morphs.

El lote admite de 1 a 10 entradas. Solo convierte varones con evidencia explícita de adultez,
rasgos de plantilla resueltos y raza coincidente con el perfil adulto femenino. Edad desconocida,
menores, otras razas, mujeres y herencia pendiente quedan excluidos con un motivo individual.
Una identidad repetida o snapshot incompleto rechaza el lote entero antes de producir resultados.
La evidencia de edad es un dato del fixture: **no existe todavía un clasificador de edad real**.
Las claves de los fixtures no son FormIDs resueltos por un load order.

La transformación cambia únicamente el bit femenino y la apariencia sintética, y selecciona
un cuerpo sintético. Conserva los otros bits, identidad, raza, voz, equipamiento y datos opacos
de scripts/facciones/packages. Las colecciones de salida son copias independientes.
Esto demuestra conservación en el modelo, **no preservación de misiones o diálogos en Fallout 4**.
La voz original puede ser incompatible con una conversión; no se reemplaza automáticamente.

## Reproducir las pruebas

Instalar SDK .NET **8.0.408** (fijado en el `global.json` del arnés). Desde el checkout:

```bash
cd Tools/AdultTransformationGate
dotnet run --project AdultTransformationGate.vbproj --configuration Release -- /tmp/fo4-experimental-report.json
```

En Windows usar una ruta de reporte nueva, por ejemplo `experimental-report.json`.
El comando no sobrescribe reportes existentes. No requiere instalación de juegos, secretos ni GUI.
En la máquina cloud usar `/workspace/.tools/dotnet/dotnet` y definir
`DOTNET_CLI_HOME=/workspace/.tools/dotnet-home` y `NUGET_PACKAGES=/workspace/.tools/nuget`.
La inicialización de NuGet requiere acceso a su mutex de sistema; el sandbox puede bloquearlo.

Cobertura del arnés: selección del lote mixto; conservación de flags altos, identidad, raza,
voz, equipo y datos opacos; independencia de las copias; exclusiones por edad, plantilla,
raza y sexo; reejecución sin cambios; límite de lote y límite máximo admitido; identidades
duplicadas incluso con distinto case; entradas nulas/incompletas; perfiles inválidos;
determinismo y round-trip JSON. Un fallo termina con código 1. No es `dotnet test`:
es un ejecutable con aserciones explícitas, conteo y código de salida.

## Infraestructura Windows existente

Se parte de `ci/windows-fo4-npc-manager`, commit
`3fd21dab95b66bdbddf9471d89f3bd16a8cf2ca6`.
La [ejecución 39180016849](https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39180016849)
muestra `Success` en su página pública. Se preservan el runner `windows-2025-vs2026`,
MSBuild, GUI `Publish|x64`, CLI `Release|x64`, paralelismo 2 y los seis commits fijados
de dependencias. No se reconstruye esa infraestructura en Linux.

Artefactos observados en la página de esa ejecución:

| Artefacto | Tamaño publicado | SHA-256 publicado |
| --- | --- | --- |
| `fo4-npc-manager-windows-x64-UNTESTED` | 28.1 MB | `94d44956252b67efa98e9c0936ee6002779a56552a52513e1615fc320b64b528` |
| `windows-build-diagnostics` | 3.09 MB | `af320131bdd3e9f6af39bf95fc852cab6420477e0ab1cec12ec43c6727e79bb0` |

Son metadatos publicados, no hashes recalculados localmente. La API de GitHub está bloqueada
por el proxy (403 en CONNECT) y la descarga web del artefacto devuelve 404 sin sesión;
no se pudo inspeccionar el contenido de los ZIP ni ejecutar sus binarios aquí.

El workflow ahora incluye el arnés en Linux/Windows antes de compilar, filtros para cambios
de código y pull requests, y reportes `EXPERIMENTAL-synthetic-transformation-*`.
Se conservan los archivos de licencia del proyecto; antes de subir binarios se comprueba
que los siete archivos legales esperados existan y no estén vacíos. Se añade aviso experimental,
revisiones reales de las fuentes y hashes por archivo. Esta comprobación no sustituye una auditoría
de licencias. Las actions nuevas y existentes están fijadas por SHA.

## Arquitectura reutilizable y orden de integración

| Componente existente | Uso previsto y condición antes de integrarlo |
| --- | --- |
| `NpcTemplateMaterializer`, `NpcTemplateHelpers` | Resolver sexo/raza efectivos y materializar Traits. No basta cambiar ACBS cuando el motor hereda Traits. Exigir resolución inequívoca y conservar el snapshot de origen. |
| `NpcRecordOverride` / `NpcRecordOverrideApplier` | Transportar cambios explícitos y mantener campos ajenos. `TraitsChanged` y `MaterializedSources` son parte del contrato, no flags que se puedan omitir. |
| `NpcRecordOverlay` / `LooksmenuLoader` | Aplicar bundle femenino compatible con raza, head parts y tints. No reutilizar ciegamente recursos masculinos. |
| `NpcOverrideSaver.ComposeSaveShadow` | Reutilizar el orden materialización → overlay → escalares y la resolución de sexo/raza; nunca implementar un segundo escritor parcial de NPC_. |
| `FaceGenBuilder`, `FaceGenBuildPipeline`, `FaceGenPaths`, `NpcFaceGenPacker` | Bake offline y empaquetado por identidad/plugin. Exigir bundle completo, comprobación de DDS/NIF y ausencia de referencias al original antes de marcar éxito. |
| `FO4_FaceTint_CLI` | Harness Windows para `--buildfacegen`, comparación de NIF y `--ddscompare`, con defaults explícitos y datos autorizados. |
| `BodySlideMorphResolver`, `BodyGenIniWriter`, `NpcApplyScriptEmitter` | Elegir una ruta de body morphs y evitar aplicación doble; comprobar cuerpo y recursos femeninos por separado del sexo. |
| `OutfitDraft`, `ArmoDraft`, `ArmaDraft`, resolvedores de slots | Verificar modelos femeninos/ARMA/slots y fallback por outfit sin perder inventario ni objetos de misión. |

No se conectó todavía el DTO al escritor: falta validar el snapshot real de herencia, identidad
por plugin/local FormID, precedencia del load order y evidencia de adultez. El siguiente hito
es un adaptador de solo lectura y plan de cambios sobre un fixture de plugin FO4 autorizado,
seguido de round-trip del patch con el lector existente. Después: un solo NPC con bake FaceGen
y recursos de cuerpo/equipo; finalmente ampliar el lote.

Una condición de aceptación adicional sale del contrato real de `SaveExecutionResult`:
`Success=True` puede coexistir con `BakeCancelled=True` y un plugin ya escrito sin todo su FaceGen.
El futuro coordinador debe comprobar también `ChargenSuccess`, cancelación y bundles completos,
y mantener la salida en staging si falla cualquiera de esas fases. No presentar el éxito del
escritor de ESP como éxito de la conversión integral.

Voces: conservar VTCK evita sustituirlo sin inventario de diálogo, pero no demuestra compatibilidad.
Antes de cambiar voz se necesitan INFO/DIAL, condiciones por sexo/raza/voz, aliases/scripts de quests,
disponibilidad de audio y regresiones de escenas. No se promete conservar todo por cambiar un flag.

## COMMONWEALTH REBORN ULTIMATE y WoD

No hay manifiestos ni referencias a esas colecciones en este checkout. Se solicitaron enlaces,
versiones y la identificación de «WoD»; no se presupone el significado de esa sigla.
Sin esos datos no es posible afirmar compatibilidad ni identificar ganadores de conflictos.

La integración prevista usa un perfil separado del gestor de mods o una copia de prueba,
manifiestos/load order exportados y lectura de recursos originales. Toda salida debe quedar
en un directorio staging nuevo fuera de Data, con reporte de conflictos por record/campo,
masters explícitos y procedencia de recursos. El usuario instala el mod de salida voluntariamente
en ese perfil; el motor no debe reemplazar archivos originales. No redistribuir recursos de la
colección sin permisos. Comprobar también runtime FO4/F4SE/LooksMenu, versiones de cuerpo y
head parts, overrides ganadores, ESL/FormIDs compactados y reglas específicas de la colección.
No se ejecutó ni modificó ninguna instalación de juego.

## Registro de validación y bloqueos

- SDK local instalado; error inicial de HOME de solo lectura resuelto con `DOTNET_CLI_HOME`.
- Mutex de NuGet bloqueado en sandbox; ejecución autorizada fuera del sandbox permitió compilar.
- Error de compilación VB por `List.Count(predicate)` corregido a la consulta LINQ explícita.
- 31 aserciones del motor pasan localmente con SDK 8.0.408; reporte JSON experimental generado.
- Prueba Papyrus existente de sexo del actor: 6 controles para SSE/FO4 pasan; no prueba una conversión.
- Rama publicada: `dev/npc-transformation-experimental`, primer commit `ca3b55a`.
- [CI experimental 39182037976](https://github.com/davistedubustamante/FO4_NPC_Manager/actions/runs/39182037976):
  ambos jobs sintéticos produjeron sus artefactos (Ubuntu y Windows). Compilaron correctamente
  DirectXTex, GUI `Publish|x64` (incluido el motor nuevo) y CLI `Release|x64`. También pasaron
  recolección de salidas, validación de licencias/procedencia y subida de ambos artefactos Windows.
  Esta ejecución valida el código de `ca3b55a`; los commits posteriores solo amplían documentación.
- Intentar sobrescribir el reporte local existente devuelve código 1 y conserva su SHA-256.
- FaceGen, ESP real, juego, quests, diálogos, voces, cuerpos y compatibilidad de colección: sin validar.

No hacer merge, publicar releases ni modificar instalaciones de juego sin autorización.
