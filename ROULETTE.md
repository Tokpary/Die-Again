# Ruleta de DiceMaster — integración en Ruleta.unity

**Escena objetivo: `Assets/Scenes/Ruleta.unity` del proyecto `C:\Users\PCcom\Documents\GitHub\DiceMaster`.** Esta entrega sustituye la integración anterior en SampleScene. SampleScene se ha restaurado a su contenido original en la copia de preparación y no forma parte del parche.

Los archivos corregidos están preparados en la copia de trabajo. Todavía no se han escrito en el repositorio original porque este chat no tiene acceso de escritura a esa carpeta. No se han realizado commits ni pushes.

## Integración en la escena correcta

Se utiliza el objeto **Roulette que ya existía en Ruleta**, con el mismo FBX, posición, rotación, escala y materiales. Solo se añaden como hijos la superficie giratoria de sectores y el indicador fijo. No hay una segunda ruleta ni un prefab que sustituya al modelo original.

La escena artística no contenía GameManager, UIManager ni EventSystem. Se incorporan esas clases existentes y las cuatro pantallas del flujo existente. El modelo se hace hijo del GameManager, situado en el origen, para conservar su posición y su propiedad durante la partida. No se copian el suelo, la cámara ni los dados de SampleScene.

Ruleta arranca directamente en `RouletteState` mediante el campo de prueba `Start In Roulette For Testing` del GameManager. **El giro sigue necesitando pulsar GIRAR / REPETIR**. Se activa la cámara de juego que ya existía en Ruleta (`Main Camera`), se desactiva la cámara del menú (`Main Camera (1)`) y se deja desactivado el grupo `---MENU PRINCIPAL` para esta prueba. Sus objetos, scripts y transformaciones se conservan. No se añade otra cámara ni se modifica la iluminación.

GameManager admite que DiceThrower no esté asignado en esta escena de prueba. Esto evita inicializar un componente inexistente; no modifica el sistema de dados ni simula gameplay completo.

## Funcionamiento

- Probabilidades por defecto: días 1/2/3/4 = **1/25/50/100 %**. Una configuración del día 4 distinta de 100 se rechaza.
- El Lucky Number se sortea dentro del intervalo inclusivo preparado. No depende del inventario y también se genera para jefes.
- El rango queda congelado mediante `GameProgress.PrepareNextRound(minimum, maximum)` antes de anunciarlo. El resultado no cambia ese rango.
- `RouletteRules` selecciona el resultado antes de animar. `RouletteView` desacelera hasta un ángulo interior al sector elegido y emite `SpinCompleted`. `RouletteState` publica entonces el resultado y habilita Continuar.
- El botón de prueba y la futura activación automática comparten `RouletteState.RequestSpin()`.
- Repetir giros antes de continuar no avanza días ni semanas. Una ronda ya empezada no se vuelve a sortear.
- `GameContext.Progress.CurrentResult` proporciona el número, el encuentro, la semana, el día, el rango y la tolerancia correspondiente. No se implementan puntuación ni penalizaciones de jefes.
- Solo `CompleteRound()`, llamado por una victoria confirmada, avanza el día o la semana. El tercer jefe completa la partida. Los botones Next actuales de Gameplay/Shop son navegación provisional y no cuentan como victoria.
- Se mantienen la máquina de estados, el patrón de eventos con suscripción/retirada en Enter/Exit, los CanvasGroups y el Input System.

Para el futuro flujo de tienda: confirmar victoria, llamar a `CompleteRound()` una sola vez, comprobar `IsComplete`, preparar el próximo rango y mostrar exactamente `NextLuckyNumberMinimum/Maximum`. La siguiente ruleta consume esos datos sin recalcularlos.

## Probar en Unity 6000.3.9f1

1. Tras aplicar los cambios, abre **Assets/Scenes/Ruleta.unity**.
2. En **GameManager**, configura `Initial Week` (1–3), `Initial Day` (1–4) y deja activado `Start In Roulette For Testing`.
3. En **Assets/Roulette/RouletteTest.asset**, configura mínimo/máximo y las probabilidades. **1–6 es solo un intervalo técnico de prueba, no balance definitivo ni una deducción de los dados.** Cambia el rango antes de Play.
4. En el componente **RouletteView de GameManager**, ajusta `Spin Duration` y `Full Turns`. Valores iniciales: 4 segundos y 5 vueltas.
5. Ejecuta **DiceMaster → Roulette → Validate connected Ruleta** fuera de Play. Para la simulación rápida, ejecuta **Validate rules (400000 outcomes)**.
6. En Play pulsa **GIRAR / REPETIR**. No debe revelarse el número hasta terminar; Continuar permanece bloqueado mientras gira. Verde = normal, rojo = jefe.
7. Repite: el día, la semana y el rango deben mantenerse. Pulsa Continuar para devolver el control a GameplayState. El recorrido provisional Gameplay → Shop → Roulette conserva la ronda ya empezada; no permite volver a sortearla.
8. Sal de Play y prueba día 4: todos los resultados deben ser jefe. Prueba también mínimo = máximo.
9. Para cancelación, desactiva RouletteView durante un giro: no debe publicarse un resultado ni avanzar. Reactívalo y repite.

Para volver más adelante al menú 3D de esta escena, será necesario integrar su entrada al flujo y restablecer los estados iniciales de sus cámaras/grupo de menú. Este cambio deja Ruleta preparada como la escena de prueba solicitada, sin implementar ese menú completo.

## Validación y límites

- Los scripts de runtime y Editor se han compilado con Roslyn y las referencias instaladas de Unity 6000.3.9f1.
- La lógica, que no ha cambiado en esta corrección de escena, superó 400.000 simulaciones con semilla 3523: día 1 = 1.028 jefes; día 2 = 25.135; día 3 = 49.879; día 4 = 100.000. Se comprobaron además las fronteras exactas de probabilidad, intervalos inclusivos, congelación del rango, ángulos, repetición, resultados antiguos, doble victoria y final de tres semanas.
- Se comprueban estáticamente las referencias locales/GUIDs, la conservación de todos los IDs existentes de Ruleta y la ausencia de modificaciones en SampleScene. El modelo original aparece una sola vez.
- **No se ha verificado esta implementación en Play Mode ni la compilación del shader.** El Editor iniciado para la copia no completó la conexión con Unity Licensing Client. Quedan pendientes la importación de Unity, el encuadre real y la correspondencia visual bajo el indicador. No se presenta la validación matemática/estática como prueba visual.
- Los intervalos finales, la puntuación, los efectos de jefes, el final de partida visual y la activación automática siguen pendientes de diseño/integración. El GDD y los archivos del sistema de dados permanecen intactos.

## Aplicación

El parche actualizado **DiceMaster-roulette.patch** modifica Ruleta y los scripts de integración. Ya no contiene cambios de SampleScene ni el prefab duplicado anterior. No incluye cambios de ShaderGraphSettings, _Recovery, Packages o Library.

Si se aplica manualmente, desde la carpeta original y fuera de Play Mode:

```powershell
git apply --check "C:\Users\PCcom\Documents\Codex\2026-11-08\new-chat\outputs\DiceMaster-roulette.patch"
```

Solo si la comprobación no da errores:

```powershell
git apply "C:\Users\PCcom\Documents\Codex\2026-11-08\new-chat\outputs\DiceMaster-roulette.patch"
```

Si el equipo ha cambiado alguno de esos archivos, revisar el conflicto antes de aplicar. No reemplazar carpetas completas ni forzar el parche. La alternativa preferida es conceder a esta sesión acceso al proyecto original para aplicar y verificar allí los cambios.

## Archivos exactos

Modificados:

```text
Assets/Code/Scripts/Core/GameManager.cs
Assets/Code/Scripts/GameContext.cs
Assets/Code/Scripts/States/State/RouletteState.cs
Assets/Code/Scripts/UI/Screens/RouletteScreen.cs
Assets/Scenes/Ruleta.unity
```

Creados:

```text
Assets/Code/Editor.meta
Assets/Code/Editor/RouletteSceneSetup.cs
Assets/Code/Editor/RouletteSceneSetup.cs.meta
Assets/Code/Editor/RouletteValidation.cs
Assets/Code/Editor/RouletteValidation.cs.meta
Assets/Code/Scripts/Core/GameProgress.cs
Assets/Code/Scripts/Core/GameProgress.cs.meta
Assets/Code/Scripts/Roulette.meta
Assets/Code/Scripts/Roulette/RouletteResult.cs
Assets/Code/Scripts/Roulette/RouletteResult.cs.meta
Assets/Code/Scripts/Roulette/RouletteRules.cs
Assets/Code/Scripts/Roulette/RouletteRules.cs.meta
Assets/Code/Scripts/Roulette/RouletteSO.cs
Assets/Code/Scripts/Roulette/RouletteSO.cs.meta
Assets/Code/Scripts/Roulette/RouletteView.cs
Assets/Code/Scripts/Roulette/RouletteView.cs.meta
Assets/Roulette.meta
Assets/Roulette/RoulettePointer.mat
Assets/Roulette/RoulettePointer.mat.meta
Assets/Roulette/RouletteSectors.mat
Assets/Roulette/RouletteSectors.mat.meta
Assets/Roulette/RouletteSectors.shader
Assets/Roulette/RouletteSectors.shader.meta
Assets/Roulette/RouletteTest.asset
Assets/Roulette/RouletteTest.asset.meta
ROULETTE.md
```
