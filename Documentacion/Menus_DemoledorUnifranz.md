# Menús de DEMOLEDOR UNIFRANZ

Aporte de **Shaden Cruz**: rediseño visual y de navegación del menú principal, la pausa y la pantalla de fin de juego. El estilo es arcade de obra, con azul noche, amarillo, naranja y rojo.

## Cómo verlo en Unity

1. `git pull` en la rama `main` y abre el proyecto con Unity 6000.3.
2. Ejecuta **Tools → Demoledor Unifranz → Construir Todos los Menús**. Construye y guarda `MenuPrincipal` y `FinJuego`. La primera vez también genera los sprites en `Assets/Sprites/Menus/`.
3. Abre `MenuPrincipal` y dale **Play**.
4. Haz commit de las dos escenas y de la carpeta `Assets/Sprites/Menus/` para que el equipo las reciba ya construidas.

Las escenas no se editaron a mano porque son archivos YAML. La herramienta las arma dentro de Unity. Los canvas anteriores (`CanvasMenu` y `CanvasFinJuego`) quedan **desactivados, no borrados**. Se puede deshacer con Ctrl+Z y volver a ejecutar las veces que haga falta.

## Qué hace cada menú

| Menú | Dónde | Detalle |
|---|---|---|
| **Menú principal** | `MenuPrincipal` | Muro de ladrillo y cartel amarillo de obra con **DEMOLEDOR UNIFRANZ**. El demoledor del equipo golpea dentro de un hueco del muro y lanza ladrillos. Botones **JUGAR**, **OPCIONES**, **CÓMO JUGAR** y **SALIR**. |
| **Opciones** | Ventana en `MenuPrincipal` | Volumen general y sonido activado o desactivado. Se guarda con `PlayerPrefs` y se aplica con `AudioListener.volume`. |
| **Cómo jugar** | Ventana en `MenuPrincipal` | Controles actuales del juego. |
| **Pausa** | `Universidad`, creada por `GestorJuego` | **ESC** o el botón ⏸ (celular). Ventana sobre el juego congelado con **CONTINUAR**, **REINICIAR**, **MENÚ PRINCIPAL** y el estado de la partida. |
| **Fin de juego** | `FinJuego` | Edificio de fondo, **GAME OVER** y **¡EL EDIFICIO SE CAYÓ!**. Muestra los **puntos, reparaciones y % destruido reales**, y el mejor puntaje. Botones **VOLVER A JUGAR** y **MENÚ PRINCIPAL**. |

Los botones crecen al pasar el cursor, se aplastan al presionar y suenan. Funcionan con mouse, toques, teclado y mando. Los sonidos se generan por código, porque el proyecto no tiene audios.

## Organización de archivos

Sigue la guía del curso: scripts en `Assets/Scripts` y sprites en `Assets/Sprites`.

```
Assets/
  Scripts/
    Menus/                         scripts de los menús (entran en la build)
      MenuPrincipalArcade.cs       botones y ventanas del menú principal
      MenuOpcionesArcade.cs        volumen y sonido
      MenuPausaArcade.cs           ventana de pausa (la lógica sigue en GestorJuego)
      FinJuegoArcade.cs            datos reales y botones de FinJuego
      ...                          piezas comunes (botones, ventanas, colores, sonidos)
      Editor/                      solo para el editor (Unity exige este nombre)
        HerramientasMenus.cs       menú Tools, sprites y Build Settings
        ConstructorMenuPrincipal.cs
        ConstructorFinJuego.cs
  Sprites/
    Menus/                         PNG generados por la herramienta
    Menus/Fuentes/                 (opcional) fuente propia para los menús
```

Archivos del equipo que se cambiaron (lo mínimo):

- `Scripts/GestorJuego.cs`:
  - Se resolvió el conflicto de merge.
  - Se recuperó el reinicio con R.
  - La pausa usa la ventana nueva.
  - Guarda los datos de la partida antes de ir a `FinJuego`.
  - Crea los controles táctiles también al entrar desde el menú.
- `Scripts/Game/GameSession.cs`: nuevos campos `Reparaciones` y `PorcentajeDestruido`, y el método `RegistrarDerrumbe`.

No se modificaron `Nivel1`, `Universidad`, `ProjectSettings`, `Packages` ni las reglas del juego.

## Fuente (opcional)

Por defecto se usa la fuente integrada de Unity. Para una con más personalidad, descarga una fuente con licencia SIL OFL, por ejemplo **Bungee** o **Lilita One** de Google Fonts. Ponla en `Assets/Sprites/Menus/Fuentes/` y vuelve a ejecutar **Construir Todos los Menús**.

## Pruebas manuales

- **Menú.** JUGAR carga `Universidad` con fundido. OPCIONES: mover el volumen cambia el sonido de los botones, y desactivar el sonido silencia todo. Los valores se mantienen al reiniciar. CÓMO JUGAR abre y cierra la ventana (también con ESC). SALIR detiene el Play Mode.
- **Pausa.** ESC o ⏸ congelan el juego. CONTINUAR sigue sin martillar al pulsar Enter. REINICIAR empieza de nuevo. MENÚ PRINCIPAL vuelve al menú. R reinicia fuera de la pausa.
- **Fin de juego.** Deja que el edificio pase el 70 %. A los 1,5 s se abre `FinJuego` con los números reales. VOLVER A JUGAR carga `Universidad`.
- **Celular.** Entrando desde el menú, los controles táctiles aparecen y el botón ⏸ funciona.
- **Resoluciones.** Prueba 16:9, 18:9 y 4:3 en la vista Game.
