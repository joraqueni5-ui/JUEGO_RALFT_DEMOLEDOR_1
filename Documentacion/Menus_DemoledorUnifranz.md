# Menús de DEMOLEDOR UNIFRANZ

Aporte de **Shaden Cruz**: diseño y navegación de los menús en estilo **pixel art**, con la fuente pixel *Silkscreen*, botones con icono y los colores de obra (azul noche, amarillo, naranja y rojo).

## Cómo verlo en Unity

1. Haz `git pull` en `main` y luego `git lfs pull`. Abre el proyecto con Unity 6000.3 y espera a que compile.
2. Ejecuta **Tools → Demoledor Unifranz → Construir Todos los Menús**.
   - Arma y guarda `MenuPrincipal` y `FinJuego`.
   - La primera vez crea los sprites pixel en `Assets/Sprites/Menus/` y la fuente `.ttf` en `Assets/Sprites/Menus/Fuentes/`.
3. Abre `MenuPrincipal` y dale **Play**.
4. Haz commit de las dos escenas y de `Assets/Sprites/Menus/` para que el equipo las reciba ya construidas.

Las escenas no se editan a mano porque son archivos YAML; la herramienta las arma dentro de Unity. Se puede deshacer con Ctrl+Z y volver a ejecutar las veces que haga falta.

**Importante:** la herramienta la ejecuta **una sola persona**, que luego sube las escenas. Si cada uno la ejecuta en su computadora, los sprites reciben identificadores distintos y se generan conflictos.

## Pantallas

| Pantalla | Dónde | Qué hace |
|---|---|---|
| **Inicio** | `MenuPrincipal` | Cartel DEMOLEDOR UNIFRANZ y "TOCA PARA EMPEZAR" parpadeando. Solo aparece al abrir el juego, no al volver de una partida. |
| **Menú principal** | `MenuPrincipal` | Botones JUGAR, OPCIONES, CÓMO JUGAR, RÉCORDS y SALIR, más CRÉDITOS y el récord. De fondo, el video de una partida o, si no hay video, el muro con el demoledor golpeando. |
| **Opciones** | Ventana | Volumen y sonido. Vibración (solo en el celular) y pantalla completa (solo en la computadora). |
| **Cómo jugar** | Ventana | Controles actuales: A/D, ESPACIO o ARRIBA para saltar, S para bajar, ENTER, ESC y R. |
| **Récords / Créditos** | Ventanas | Los 5 mejores puntajes con iniciales y los nombres del equipo. |
| **Pausa** | `Universidad`, creada por `GestorJuego` | ESC o el botón ⏸. CONTINUAR, REINICIAR y MENÚ PRINCIPAL, este último con confirmación ("¿SALIR AL MENÚ?"). Muestra los controles y el estado de la partida. |
| **Cambio de escena** | Todas | Una pared de ladrillos cae, tapa la pantalla y se abre en la escena nueva. |
| **Fin de juego** | `FinJuego` | ¡VICTORIA! o GAME OVER según el final (edificio salvado, derrumbe o sin vidas). Muestra los puntos reales, una medalla y una frase, y pide 3 iniciales si el puntaje entra en el top 5. |

Medallas por puntos: bronce desde 100, plata desde 300 y oro desde 600. Se cambian en el Inspector del objeto `FinJuegoDemoledorUnifranz`, en el componente `FinJuegoArcade`.

## Video de fondo del menú (opcional)

1. Graba de 20 a 60 segundos de una partida, por ejemplo con **Win + Alt + R** en Windows.
2. Pon el `.mp4` en `Assets/Sprites/Menus/Video/`. Para Android conviene H.264, 1280×720 y sin sonido.
3. Vuelve a ejecutar **Construir Todos los Menús**. El menú lo reproduce en bucle detrás de los botones, con la etiqueta "DEMO EN VIVO".

Mientras no haya video, o si un celular no puede reproducirlo, se muestra el muro con el demoledor. El juego nunca se carga detrás del menú.

## Organización de archivos

```
Assets/
  Scripts/
    Menus/                         scripts de los menús (entran en la build)
      MenuPrincipalArcade.cs       inicio, botones, récords y ventanas del menú
      MenuOpcionesArcade.cs        volumen, sonido, vibración y pantalla completa
      MenuPausaArcade.cs           ventana de pausa (la lógica sigue en GestorJuego)
      FinJuegoArcade.cs            finales, medalla, frase e iniciales
      FondoVideoMenu.cs            video de fondo con respaldo fijo
      TablaRecords.cs              top 5 guardado en el dispositivo
      TransicionLadrillos.cs       pared de ladrillos entre escenas
      GeneradorPixelArt.cs         dibuja los sprites pixel (botones, iconos, medallas…)
      ...                          piezas comunes (fábrica de UI, botones, ventanas, sonidos)
      Editor/                      solo para el editor (Unity exige este nombre)
        HerramientasMenus.cs       menú Tools, sprites y fuente
        ConstructorMenuPrincipal.cs
        ConstructorFinJuego.cs
  Sprites/
    Menus/                         PNG pixel generados por la herramienta
    Menus/Fuentes/                 Silkscreen (licencia SIL OFL, ver OFL-Silkscreen.txt)
    Menus/Video/                   (opcional) video del menú
```

No se modificaron `GestorJuego`, `GameSession`, el código del juego, la escena `Universidad`, `ProjectSettings` ni `Packages`.

## Pruebas manuales

- **Inicio.** Al dar Play aparece "TOCA PARA EMPEZAR". Cualquier tecla, clic o toque muestra los botones. Al volver de una partida ya no aparece.
- **Menú.** Las flechas o el mouse mueven el marco blanco y la flecha de selección. JUGAR carga `Universidad` con la pared de ladrillos. Revisa OPCIONES, CÓMO JUGAR, RÉCORDS y CRÉDITOS; ESC cierra cada ventana. SALIR detiene el Play Mode.
- **Opciones.** El volumen cambia el sonido de los botones; SONIDO en NO silencia todo. En la computadora aparece PANTALLA COMPLETA y en el celular VIBRACIÓN.
- **Pausa.** ESC o ⏸ congelan el juego. CONTINUAR sigue la partida. REINICIAR empieza de nuevo. MENÚ PRINCIPAL pregunta antes de salir, con NO elegido por defecto.
- **Fin de juego.** Pierde, por derrumbe o sin vidas, o gana. Revisa el título, la medalla y los números. Si el puntaje entra al top 5, escribe 3 letras (con las flechas ▲▼ o el teclado) y pulsa LISTO.
- **Celular.** Los botones vibran (si está activado), la pausa tiene su botón ⏸ y los controles táctiles siguen funcionando.
- **Resoluciones.** Prueba 16:9, 18:9 y 4:3 en la vista Game.
