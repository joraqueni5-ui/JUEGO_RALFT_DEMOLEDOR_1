# Ralft Demoledor

Videojuego 2D desarrollado en Unity 6 con el pipeline URP. Esta version
corresponde al Hito 3 e incorpora la interfaz de usuario completa: menu
principal, navegacion entre escenas, menu de pausa y pantalla de fin de
juego (Victoria y Game Over).

## Integrantes

- Shaden
- Jhonatan (oraqueni)
- Alejandro (acho)
- Jorge

## Requisitos

- Unity 6 (6000.3.16f1) con soporte de compilacion para Windows
- Windows de 64 bits para ejecutar la version compilada

## Escenas

| Indice | Escena | Descripcion |
| ------ | ------ | ----------- |
| 0 | MenuPrincipal | Titulo del juego y botones Jugar, Controles y Salir |
| 1 | Nivel1 | Sala de demolicion con personaje, ladrillos, enemigos, HUD y menu de pausa |
| 2 | FinJuego | Resultado de la partida (Victoria o Game Over) con Reintentar y Menu principal |

## Controles

| Tecla | Accion |
| ----- | ------ |
| W A S D o flechas | Mover al personaje |
| Escape | Abrir o cerrar el menu de pausa |

El objetivo del nivel es recolectar los 5 ladrillos y evitar a los
demoledores, que descuentan una vida al tocar al personaje.

## Como jugar

1. Abrir la carpeta `ejecutable` y ejecutar `RalftDemoledor.exe`.
2. En el menu principal pulsar JUGAR para entrar al nivel.
3. Con el boton SALIR se cierra la aplicacion (dentro del editor de Unity
   se detiene la reproduccion).

## Estructura del proyecto

```
Assets
  Editor/            GeneradorJuego.cs (genera recursos, escenas y el ejecutable)
  Scenes/            MenuPrincipal, Nivel1, FinJuego y SampleScene
  Scripts/
    Game/            PlayerController, EnemyPatrol, Recolectable, GameManager,
                     GameSession y Escenas
    UI/              MainMenuController, PauseMenuController, GameOverController
                     y HUDController
  UI/
    Sprites/         Sprites de botones, paneles, fondos y personajes
    Fonts/           Tipografias de la interfaz
Herramientas/        Script que genera la documentacion en PDF
Documentacion/       Documento del Hito 3
```

## Generar el ejecutable

1. Abrir el proyecto con Unity 6.
2. En el menu superior elegir `Ralft Demoledor > Generar interfaz, escenas y ejecutable`.
3. El ejecutable se genera en la carpeta `ejecutable` y las capturas de la
   interfaz en la carpeta `caps`.

Tambien se puede compilar desde la linea de comandos:

```
Unity.exe -batchmode -quit -projectPath "<ruta del proyecto>" -executeMethod GeneradorJuego.ConstruirTodo
```

## Generar la documentacion

```
python Herramientas/generar_documentacion.py
```

El documento se guarda en `Documentacion/Hito3_Interfaz_Usuario_Ralft_Demoledor.pdf`.
