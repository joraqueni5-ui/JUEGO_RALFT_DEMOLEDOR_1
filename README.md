# ¡Agárralo! — Demoledor Unifranz (Unity 2D)

Juego arcade estilo *Ralph el demoledor* ambientado en la Universidad Franz Tamayo.
El demoledor **no se controla**: recorre el piso rompiendo todo lo que encuentra. Tú
controlas al muñeco que repara los objetos antes de que el edificio quede inservible.

La campaña tiene **7 pisos** (planta baja y pisos 1 a 6) y un **nivel final** en la
fachada del edificio, que se desbloquea al superar la terraza.

Unity **6000.3.16f1** + URP 2D.

## Cómo probarlo

1. Instala Git LFS (`git lfs install`): los PNG, el APK y los ejecutables viven en LFS.
2. Clona el repositorio y ejecuta `git lfs pull` para bajar el arte y los binarios.
3. Abre el proyecto con **Unity 6000.3.16f1** (Unity Hub) y espera a que compile.
4. Abre **`Assets/Scenes/MenuPrincipal.unity`** y dale **Play**.
   También puedes abrir cualquier escena de `Assets/Scenes/Niveles/` para probar un piso suelto.

Las escenas de juego están en `Assets/Scenes/Niveles/`:

| Nivel | Escena |
|---|---|
| Planta baja | `Piso0_PlantaBaja.unity` |
| Piso 1 — Gastronomía y Medicina | `Piso1_GastronomiaEnfermeria.unity` |
| Piso 2 — Informática | `Piso2_Informatica.unity` |
| Piso 3 — Aulas 300 | `Piso3_Aulas300.unity` |
| Piso 4 — Aulas 400 | `Piso4_Aulas400.unity` |
| Piso 5 — Aulas 500 | `Piso5_Aulas500.unity` |
| Piso 6 — Terraza y biblioteca | `Piso6_TerrazaBiblioteca.unity` |
| Nivel final | `NivelFinal_Fachada.unity` |

`Assets/Scenes/_Antiguas/` guarda las escenas anteriores (`Universidad`, `Nivel1`,
`PisoCenital`, `SampleScene`, `NivelFinal`) solo como referencia: no están en el build.

## Cómo compilar

El orden de escenas ya está en **File → Build Settings**: menú, pisos 0 a 6, nivel final
y `FinJuego`.

- **Android**: cambia de plataforma a Android y genera el APK. Hay una compilación lista en
  `ejecutable android/DemoledorUnifranz.apk`.
- **Windows**: compila para Windows; hay una exportación en `exportado/`
  (`Demoledor Unifranz.exe`).

Los controles táctiles aparecen solos en el celular; en la computadora se juega con teclado.

## Controles

### Pisos 0 a 6 (vista cenital)

| Acción | Tecla |
|---|---|
| Moverse | `W` `A` `S` `D` o flechas |
| Martillar / reparar | `ENTER` |
| Ver el mapa completo | `TAB` (mantener) |
| Pausa | `ESC` |
| Reiniciar | `R` |

### Nivel final (fachada)

| Acción | Tecla |
|---|---|
| Caminar | `A` / `D` |
| Trepar | `↑` |
| Bajar una repisa | `S` |
| Saltar / soltarse | `ESPACIO` |
| Martillar | `ENTER` |

### Menús y pantallas

Flechas o mouse para elegir, `ENTER`/`ESPACIO` para aceptar, `ESC` para cerrar o pausar.

## Reglas

- El demoledor se mueve por el piso con búsqueda A* y rompe los objetos en tres estados:
  intacto → dañado → roto.
- Cada martillazo repara el objeto dañado o roto más cercano. Cada reparación y cada golpe
  que bloquees le baja energía al demoledor.
- **Ganas** cuando el demoledor se queda sin energía y se rinde.
- **Pierdes** si el destrozo pasa del límite del piso o si te quedas sin vidas (empiezas con 3).
- Al terminar el piso se dan **hasta 3 estrellas** según las condiciones de su definición.
- Aparecen **poderes** cada cierto tiempo: invulnerabilidad, reparación total y vida extra.
- Cada piso tiene su tutorial, puntos de información `?` con datos útiles, estudiantes de
  ambiente (el chef deja poderes) y rocas que caen.

## Novedades (empujón, mini villanos, potenciadores y tema)

- El demoledor **avisa al llegar** (alarma y rugido), se le oyen los pasos y, si te acercas, **te aleja de un manotazo y te quita una vida**.
- **Mini villanos**: aprendices, termitas robot y mini topadoras que rompen objetos; se vencen a martillazos.
- **Potenciadores nuevos**: zapatillas turbo, martillo dorado, reloj de hielo y casco de obra.
- **Sonido ambiente** en cada escena y **menús con un tema uniforme** y fondo nocturno.

Detalles y ajustes en `Documentacion/Mejoras_Villanos_Potenciadores_Tema.md`.

## Menús y progreso

- **Inicio** con "TOCA PARA EMPEZAR" y menú principal: JUGAR, OPCIONES, CÓMO JUGAR, RÉCORDS
  y SALIR, con el video de fondo y la pared de ladrillos entre escenas.
- **ELIGE UN NIVEL**: el edificio de Unifranz con una franja por piso, las estrellas de cada
  nivel y el nivel final bloqueado hasta superar la terraza.
- **Opciones**: volumen de música y efectos, sonido, vibración (celular) y pantalla completa.
- **Fin de juego**: victoria, derrumbe o sin vidas; medalla según el puntaje y tabla de los
  5 mejores con iniciales.
- El progreso y los récords se guardan en el dispositivo (`PlayerPrefs`).

## Cómo está armado

**Escenas**

- `MenuPrincipal` — inicio y menús.
- `Assets/Scenes/Niveles/PisoN_*.unity` — un piso por escena; `NivelEscena` indica qué
  `DefinicionPiso` y qué mapa se juegan.
- `NivelFinal_Fachada` — la fachada del edificio con palomas, terremoto y el final.
- `FinJuego` — resultado, medalla y récords.

**Datos**

- `Assets/Data/Pisos/Piso*.asset` — nombre, escena siguiente, mapa, objetos, poderes,
  tutorial, energía inicial y condiciones de las estrellas de cada piso.
- `Assets/Prefabs/Mapas/Mapa_Piso*.prefab` — mapas armados de cada piso.
- `Assets/Resources/ArteNiveles.asset` — sprites que el código usa en ejecución.
- `Assets/Resources/BibliotecaSonidos.asset` — sonidos y música.

**Scripts** (`Assets/Scripts`)

| Carpeta | Contenido |
|---|---|
| `Cenital/` | piso cenital: jugador, demoledor, mapa, objetos, ascensor, poderes y tutorial |
| `EdificioAlto/` | fachada: secciones, repisas, ventanas, peligros y final |
| `Menus/` | menús, pantallas, pausa, transiciones y récords |
| `Game/` | `GameSession`, nombres de escenas y flujo entre pisos |
| `Audio/` | gestor de audio y biblioteca de sonidos |
| `Arte/` | catálogo de sprites y efectos |
| `Camara/`, `UI/` | cámara de seguimiento y controles táctiles |

**Arte**

- `Assets/Arte/PisoCenital/` — suelos, paredes, puertas, ventanas, ascensor y escaleras.
- `Assets/Sprites/Pisos/` — sprites por piso, más `Comunes`.
- `Assets/Sprites/Interfaz/`, `Poderes/`, `Efectos/`, `NivelFinal/` — interfaz y efectos.
- Los `.meta` usan `Filter Mode: Point` y sin compresión.

**Herramientas** (`Tools/`, fuera de `Assets` para que Unity no las importe)

```bash
python Tools/build_art.py           # exporta el arte .aseprite a PNG + .meta
python Tools/generar_arte_pisos.py  # genera el arte de los pisos
python Tools/inspect_art.py         # revisa los .aseprite
```

`Tools/aseprite_tool.py` lee el formato `.aseprite` (capas, cels, frames, tags) y
`Tools/unity_assets.py` escribe los `.meta` con GUID deterministas.

## Ajustes rápidos desde el Inspector

- `DefinicionPiso`: energía inicial, energía por reparación y por bloqueo, límite de destrozo,
  ritmo del demoledor, poderes, tutorial y condiciones de las estrellas.
- `PisoCenital`: mapa de la escena, marcas de estado para depurar y cámara.
- `JugadorCenital`: velocidad, radio y momento de reparación, poderes y teclas.
- `Muneco`: velocidad, fuerza de salto, velocidad de trepado, radio de martillo y teclas.
- `EdificioAlto` / `PeligrosFachada`: secciones, tiempos de palomas y terremoto, poderes.
- `GestorJuego`: vidas, puntos por reparación, límite de derrumbe y pausa.

## Documentación adicional

- `Documentacion/Menus_DemoledorUnifranz.md` — diseño y navegación de los menús.
- `Documentacion/Hito3_Interfaz_Usuario_Demoledor_Unifranz.pdf` — entrega de interfaz.
