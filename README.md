# ¡Agárralo! — El demoledor automático y el que repara (Unity 2D)

Juego estilo *Ralph el demoledor* (arcade) ambientado en la universidad:

- **El demoledor NO se controla.** Él solo se pasea por la fachada, empieza por el último
  piso y va **rompiendo los bloques de uno en uno**, piso por piso, de arriba hacia abajo.
- **Tú controlas al muñeco** (el que hace de Félix): camina, **se trepa a la fachada** y con el
  martillo **repara un bloque por golpe** antes de que el edificio se venga abajo.
- El edificio está partido en **93 bloques** (rejilla de 12 × 8 celdas de 48×40 px), así que la
  destrucción es **bloque a bloque**, no por sectores grandes.
- Si la estructura pasa de **70 % destruida**, el edificio se cae y termina la partida (`R`
  para volver a empezar). Cada reparación suma **10 puntos**.

Unity **6000.3.16f1** + URP 2D.

## Cómo probarlo

1. Abre el proyecto con Unity 6000.3 (Unity Hub).
2. Abre la escena **`Assets/Scenes/Universidad.unity`** (ya está como primera escena en
   *Build Settings*, así que también sirve para el build).
3. Dale **Play**.

## Controles (solo el muñeco)

| Acción | Tecla |
|---|---|
| Caminar | `A` / `D` |
| Saltar | `ESPACIO` |
| Treparse a la fachada / subir | `↑` (estando debajo del edificio) |
| Bajar por la fachada | `↓` |
| Soltarse de la fachada | `ESPACIO` (o `↓` al llegar al suelo) |
| Martillar / reparar | `ENTER` |
| Reiniciar la partida | `R` |

## Cómo funciona

**Edificio rompible (`BloqueEdificio` + `Edificio`)**
- 93 bloques independientes con 3 estados: **intacto → agrietado → destruido**.
- Un golpe del demoledor agrieta **ese** bloque (sprite con grietas y esquina picada); el
  segundo lo destruye y deja el hueco con escombros que caen y se desvanecen.
- Detrás de los bloques hay una capa de interior oscuro recortada con la silueta del edificio,
  así que los huecos se ven como el interior y no como cielo.
- `Edificio` guarda la rejilla `(fila, columna)` y responde: qué bloque hay en una celda, a qué
  altura está un piso, dónde está una columna y qué bloque se repara.

**Demoledor automático (`DemoledorAutomatico`)**
- Empieza en el último piso y recorre cada piso en zigzag: camina hasta el bloque que toca,
  se para a un lado y golpea hasta destruirlo (bloque a bloque, con animación de golpe y pausa).
- Al terminar un piso “salta” al siguiente (usa los cuadros de salto) y sigue; al llegar abajo
  vuelve a subir, así que la partida es continua mientras queden bloques.
- Se puede parar/reiniciar desde código (`Detener()`, `Reiniciar()`); el gestor lo hace cuando
  la partida termina o se reinicia.

**Muñeco (`Muneco`, el personaje jugable)**
- Camina y salta en la calle.
- Con `↑` debajo del edificio se agarra a la fachada: se apaga la gravedad y se mueve en las
  4 direcciones dentro del edificio (usa la animación de espaldas, la que antes no se usaba).
- Con `ENTER` martilla y repara **el bloque más cercano** a su pecho (uno solo por golpe).

**Gestor (`GestorJuego`)**
- HUD: puntos, reparaciones, % de estructura destruida, barra que se pone roja a medida que el
  edificio se cae, aviso de derrumbe y reinicio con `R`.
- Al 70 % destruido termina la partida (el demoledor se detiene y el muñeco deja de responder).

## Cómo está armado

Escena `Universidad.unity`:

```
Main Camera            ortográfica, encuadra el edificio (size 3.2)
Global Light 2D
Fondo                  cielo + edificios lejanos (orden -50)
Interior               interior oscuro recortado con la silueta (orden 0)
Edificio               (script Edificio) → 93 hijos Bloque_<piso>_<columna>
Suelo                  sprite + BoxCollider2D (borde superior en y = 0)
Demoledor              sprite + script DemoledorAutomatico (sin física: se mueve por guion)
Muneco                 Rigidbody2D + BoxCollider2D + script Muneco
GestorJuego            HUD, puntos, fin de partida y reinicio
```

Scripts (`Assets/Scripts`):

| Script | Qué hace |
|---|---|
| `AnimadorSprites.cs` | Animación cuadro a cuadro sobre un `SpriteRenderer` (sin `Animator`). |
| `PersonajeBase.cs` | Física común del muñeco: caminar, salto, detector de suelo, gravedad. |
| `Muneco.cs` | Control del jugador: caminar, trepar la fachada y martillar. |
| `DemoledorAutomatico.cs` | Rutina del demolador automático (barrido por pisos, golpe por bloque). |
| `BloqueEdificio.cs` | Estado del bloque, sprite agrietado, escombros y rebote al reparar. |
| `Escombro.cs` | Trocito que sale volando al romper un bloque. |
| `Edificio.cs` | Rejilla de bloques, conteos, reparación del más cercano y aviso de reparación. |
| `GestorJuego.cs` | HUD, puntos, fin de partida por derrumbe y reinicio (`R`). |

## Arte

Todo el arte sale de tus `.aseprite`:

| Archivo original | Sprites en Unity |
|---|---|
| `edificio/edificio.aseprite` | `Assets/Arte/Edificio/bloque_<fila>_<col>.png` + `_danado.png` (12×8) |
| `demoledor/animaciion_caminata_izquierda.aseprite` | `Assets/Arte/Demoledor/caminar_izq_0..5.png` |
| `demoledor/animacion_caminata_derecha.aseprite` | `Assets/Arte/Demoledor/caminar_der_0..5.png` |
| `demoledor/animacion_salto.aseprite` | `Assets/Arte/Demoledor/salto_0..3.png` |
| `demoledor/demoledor_golpe.aseprite` | `Assets/Arte/Demoledor/golpe_0..5.png` |
| `mueñeco/Movimiento Abajo.aseprite` | `Assets/Arte/Muneco/caminar_0..3.png` |
| `mueñeco/Movimiento Espalda1.aseprite` | `Assets/Arte/Muneco/espalda_0..3.png` (se usa al trepar) |
| `mueñeco/Movimiento martillo.aseprite` | `Assets/Arte/Muneco/martillo_0..3.png` |
| — (generado) | `Assets/Arte/Nivel/fondo.png`, `suelo.png`, `interior.png` |

El edificio original (640×360) se recorta a su contenido real (576×321 px) y se corta en una
rejilla de 48×40 px. Los bloques pegados reconstruyen el dibujo original exactamente; las
celdas que quedan en el cielo (arriba del techo) no se crean.

Los `.meta` de los PNG se generan con **pivote abajo-centro** para los personajes (los pies
quedan en el origen) y **centro** para los bloques, `Filter Mode: Point` y sin compresión.

Herramientas (`Tools/`, fuera de `Assets` para que Unity no las importe):

```bash
python Tools/build_art.py    # exporta el arte .aseprite a Assets/Arte (PNG + .meta)
python Tools/build_scene.py  # regenera Assets/Scenes/Universidad.unity
```

`Tools/aseprite_tool.py` lee el formato `.aseprite` (capas, cels, frames, tags) e
`Tools/unity_assets.py` escribe los `.meta` con GUID deterministas.

## Ajustes rápidos desde el Inspector

- `Demoledor → Velocidad`, `Pausa Tras Golpe`, `Espera Entre Pisos`: ritmo de la destrucción.
- `Edificio → Golpes Para Romper`: 1 = cada golpe destruye, 2 = primero se agrieta.
- `GestorJuego → Limite Derrumbe` (0.7 = 70 %) y `Puntos Por Reparacion`.
- `Muneco`: `Velocidad`, `Fuerza Salto`, `Velocidad Trepado`, `Radio Reparacion` y sus teclas.
- `Demoledor → Animacion Golpe Mira Derecha`: si el dibujo del golpe apunta al otro lado,
  destíldalo y se voltea solo.

## Verificación automática

`Assets/Editor/VerificacionProyecto.cs` valida la escena, entra en Play Mode, deja trabajar al
demoledor automático, prueba romper/reparar bloque a bloque, el trepado, el fin de partida y
mide el render:

```bash
Unity.exe -batchmode -projectPath . -executeMethod VerificacionProyecto.Ejecutar -logFile -
```

Última corrida: **63 comprobaciones OK, 0 fallos**. Capturas: `Tools/out/captura.png` (edificio
entero) y `Tools/out/captura_demolicion.png` (mientras el demoledor trabaja).

## Limitaciones / ideas para seguir

- No hay sonidos ni música (el proyecto no trae audios).
- El demoledor no se puede controlar (a propósito); si quieres, se le puede agregar una
  dificultad progresiva (romper más rápido con el tiempo).
- El muñeco no tiene animación lateral propia: se voltea el sprite al caminar.
- No hay derrumbe en cadena: cada bloque cae por separado.
