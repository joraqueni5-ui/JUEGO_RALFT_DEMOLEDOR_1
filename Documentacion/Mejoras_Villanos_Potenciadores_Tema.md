# Mejoras: empujón del demoledor, mini villanos, potenciadores, sonido ambiente y tema de menús

Todo se conecta solo al darle Play (no hay que reconstruir escenas ni tocar los `DefinicionPiso`):
`Assets/Scripts/Extras/InstaladorExtras.cs` agrega los componentes al cargar cada escena.

## El demoledor

| Qué | Cómo funciona | Dónde se ajusta |
|---|---|---|
| **Aparición** | 1 s antes de entrar suena una alarma de obra; al romper el ventanal ruge, tiembla la cámara y aparece "¡AHÍ VIENE EL DEMOLEDOR!". | `DemoledorCenital.UpdateEsperando` |
| **Empujón** | Si te acercas a menos de `radioEmpujon` (1,4 baldosas) muestra un "!" durante 0,32 s y te aleja de un manotazo: pierdes una vida, sales despedido `distanciaEmpujon` (2,8) baldosas y quedas aturdido 0,6 s. Los muros te frenan y nunca sales del piso. Chocar con él también lo dispara. | Inspector de `DemoledorCenital` → *Empujón* |
| **Puñetazo** | Si te alcanza el puñetazo dirigido a un objeto, además de la vida te empuja. | — |
| **Presencia** | Pasos pesados que suenan más fuerte cuanto más cerca está (y la cámara vibra un poco a menos de 3,5 baldosas) y gruñidos cuando te tiene cerca. | *Presencia* (`distanciaOido`) |
| **Congelado** | El reloj de hielo lo deja quieto, celeste y con cristales de hielo. | — |

## Mini villanos (la cuadrilla del demoledor)

`GestorMiniVillanos` + `MiniVillano`. El demoledor los llama ("¡A ROMPER, MUCHACHOS!") cada 17 s en la planta baja y cada 8 s en la terraza;
en **furia** los llama más seguido, más élites y uno más a la vez. Lo que rompen cuenta como destrozo, que a su vez enfurece al demoledor.

| Mini | Desde | Qué hace | Vida | Contra ti | Puntos |
|---|---|---|---|---|---|
| **Aprendiz** (casco naranja / élite casco rojo) | Planta baja | Camina al objeto, se prepara 1 s y lo golpea. | 1 / 2 | Te empuja un poco (el élite quita vida). | 30 (+20) |
| **Termita robot** (cobre / élite tóxica) | Piso 1 | Rápida, se pega a un objeto y lo roe hasta romperlo. | 1 | Te aparta, sin daño. | 20 (+20) |
| **Mini topadora** (amarilla / élite blindada) | Piso 3 | Lenta, aplasta objetos; motor que se oye al acercarse. | 2 / 3 | Te atropella: vida y empujón. | 60 (+20), suelta un potenciador el 45 % de las veces |

Se vencen a **martillazos** (ENTER cerca: cada martillazo también les pega). Se van todos cuando el demoledor se rinde.

## Potenciadores nuevos

`GestorPotenciadoresExtra` deja uno cada 15 a 22 s (uno a la vez), además de los poderes propios de cada piso. Elige con sinergia:

| Potenciador | Efecto | Sale más cuando… |
|---|---|---|
| **Zapatillas turbo** | +60 % de velocidad durante 7 s, con estela. | estás lejos de lo roto |
| **Martillo dorado** | Martillazos el doble de rápidos y +0,45 baldosas de alcance durante 8 s. | hay mucho destrozo |
| **Reloj de hielo** | Congela a los mini villanos 6 s y al demoledor 3,6 s. | hay mini villanos o el demoledor está en furia |
| **Casco de obra** | Aguanta el próximo golpe sin perder vida (se ve sobre tu cabeza). | te queda 1 vida |

Los nuevos tipos (`Velocidad`, `ReparacionRapida`, `CongelarEnemigos`, `Casco`) también se pueden poner a mano en `DefinicionPiso.poderes`.

## Sonido

- `Assets/Resources/SonidosExtra/`: alarma, rugido, pasos, gruñido, empujón, golpe fuerte, sonidos de los mini villanos y de los potenciadores.
  `GestorAudio` los busca ahí si no están en la `BibliotecaSonidos` (volúmenes en `SndExtra.Volumen`).
- **Ambiente en bucle** según la escena: gente lejana y aire acondicionado en los pisos, viento y pájaros en la terraza (piso 6),
  ciudad y viento en la fachada, viento nocturno con golpes de obra en el menú. Baja en la pausa y respeta el volumen de Efectos.

## Visual

- Viñeta suave en los niveles que late en rojo con la furia o con 1 vida, y en celeste con el demoledor congelado; motas de polvo flotando.
- **Menús con tema uniforme y serio** (`TemaSerio`): un solo estilo de botón (acero oscuro con filo ámbar), marco de selección ámbar,
  paneles de acero, franja de obra sobria y el logotipo DEMOLEDOR / UNIFRANZ. El fondo del menú es el edificio de noche en demolición
  con reflectores, polvo y la luz de la grúa. Para volver a los menús de colores: `TemaSerio.Activo = false`.

## Arte y herramientas

El arte nuevo está **dibujado por código** a partir de especificaciones de píxeles (PixelSpec):

```bash
# Personajes y potenciadores (PixelSpec → PNG con control de calidad)
# Tools/sprites_spec/*.pixelspec.json → Tools/sprites_out/<nombre>/ (mini_aprendiz, mini_termita, mini_topadora, poder_turbo, poder_martillo, poder_reloj, poder_casco)
python Tools/generar_extras_fx.py        # onda del empujón, nube, "!", hielo
python Tools/generar_tema_menu.py        # fondo, logo, botón, marco, panel, franja, reflector
python Tools/exportar_extras.py          # copia todo a Assets/Resources/Extras con sus .meta
python Tools/generar_sonidos_extra.py    # sonidos y ambientes a Assets/Resources/SonidosExtra
```

## Segunda tanda

- **Selector de personaje** (`SelectorPersonaje`, `Personajes`): al pulsar JUGAR o elegir un piso aparece "ELIGE TU PERSONAJE".
  Diego (Ing. de Sistemas, martilla 15 % más rápido), Valeria (Medicina, empieza con casco), Camila (Administración, poderes
  duran 30 % más), Lucía (Gastronomía, más potenciadores), Mateo (Derecho, se recupera rápido del aturdimiento) y Sofía
  (Arquitectura, repara desde más lejos). Los sprites salen de la animación original con otra ropa y peinado
  (`python Tools/generar_personajes.py`), así todos se mueven igual que el muñeco.
- **Escena de llegada** (`IntroNivel`, `PersonaAmbiente`): el piso empieza tranquilo, con estudiantes y personal caminando.
  Suena la alarma, el demoledor entra, la cámara lo enfoca con franjas de cine y todos gritan y escapan corriendo.
- **Más azar**: el demoledor a veces merodea hasta un punto al azar, llega a los objetos por lados distintos y hace pausas variables;
  los poderes del piso aparecen casi siempre en lugares al azar y con tiempos más variables.
- **Demoledor débil (menos de 30 % de energía)**: lanza oleadas de 3 a 5 ayudantes a la vez (máximo 10 en el piso) y
  aparecen el doble de potenciadores (hasta 2 extras a la vez).
- **Prueba automática**: con el juego en un piso, F10 revisa los extras y escribe el resultado en la consola ([PruebaExtras]).
