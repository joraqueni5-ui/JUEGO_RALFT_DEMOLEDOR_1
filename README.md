# Demoledor Unifranz

Videojuego 2D desarrollado en Unity 6 con el pipeline URP para el proyecto
grupal de Unifranz. Esta version corresponde al Hito 3 e incorpora la
interfaz de usuario completa: menu principal, navegacion entre escenas,
menu de pausa y pantalla de fin de juego (Victoria y Game Over), ademas de
los graficos del equipo importados desde Aseprite.

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
| 1 | Nivel1 | Edificio y sala de demolicion con el personaje, ladrillos, demoledores, HUD y menu de pausa |
| 2 | FinJuego | Resultado de la partida (Victoria o Game Over) con Reintentar y Menu principal |

## Controles

| Tecla | Accion |
| ----- | ------ |
| W A S D o flechas | Mover al demoledor |
| Escape | Abrir o cerrar el menu de pausa |

El objetivo del nivel es recolectar los 5 ladrillos y evitar a los
demoledores, que descuentan una vida al tocar al personaje.

## Recursos graficos del equipo

Los dibujos se hicieron en Aseprite y se importan con el paquete
`com.unity.2d.aseprite`, que genera un sprite recortado por fotograma y los
clips de animacion:

| Archivo | Uso en el juego |
| ------- | --------------- |
| `Assets/Edificio/edificio.aseprite` | Fondo del nivel y de los menus |
| `Assets/Edificio/ventanas.aseprite` | Ventanas decorativas del escenario |
| `Assets/personaje/Movimiento Abajo.aseprite` | Personaje principal (frente) |
| `Assets/personaje/Movimiento Espalda1.aseprite` | Personaje visto de espaldas |
| `Assets/personaje/Movimiento Derecha.aseprite` | Personaje caminando de lado |
| `Assets/personaje/Movimiento martillo.aseprite` | Personaje con el martillo |
| `Assets/Animaciones/Enemigo/animacion_caminata_derecha.aseprite` | Demoledor enemigo |
| `Assets/Animaciones/Enemigo/animaciion_caminata_izquierda.aseprite` | Demoledor caminando al otro lado |
| `Assets/Animaciones/Enemigo/animacion_salto.aseprite` | Salto del demoledor |
| `Assets/Animaciones/Enemigo/demoledor_golpe.aseprite` | Golpe del demoledor |

La herramienta `Assets/Editor/GeneradorJuego.cs` conecta el controlador de
animacion al personaje y a los enemigos cuando el clip solo cambia el
sprite; si el clip tambien anima la posicion o la escala se usa un
fotograma fijo para que no interfiera con el movimiento. La herramienta
`Assets/Editor/InspeccionAseprite.cs` lista los recursos que genera el
importador para cada archivo.

## Como jugar

1. Abrir la carpeta `ejecutable` y ejecutar `DemoledorUnifranz.exe`.
2. En el menu principal pulsar JUGAR para entrar al nivel.
3. Con el boton SALIR se cierra la aplicacion (dentro del editor de Unity
   se detiene la reproduccion).

## Prueba automatica del ejecutable

El ejecutable incluye una prueba de humo que recorre el menu principal, el
nivel, el menu de pausa y la pantalla final, y escribe el resultado en el
registro del jugador:

```
ejecutable\DemoledorUnifranz.exe -autoprueba
```

El registro queda en
`%USERPROFILE%\AppData\LocalLow\Demoledor Unifranz\Demoledor Unifranz\Player.log`.

## Estructura del proyecto

```
Assets
  Animaciones/       Animaciones del demoledor en Aseprite
  Edificio/          Edificio y ventanas en Aseprite
  Editor/            GeneradorJuego.cs e InspeccionAseprite.cs
  Scenes/            MenuPrincipal, Nivel1, FinJuego y SampleScene
  Scripts/
    Game/            PlayerController, EnemyPatrol, Recolectable, GameManager,
                     GameSession y Escenas
    UI/              MainMenuController, PauseMenuController, GameOverController
                     y HUDController
  UI/
    Sprites/         Sprites de botones, paneles, muros y ladrillos
    Fonts/           Tipografias de la interfaz
  personaje/         Animaciones del personaje en Aseprite
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

El documento se guarda en `Documentacion/Hito3_Interfaz_Usuario_Demoledor_Unifranz.pdf`.
