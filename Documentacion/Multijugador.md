# Multijugador por Wi-Fi (REPARALOFRANZ)

## Cómo se juega
1. Los dos equipos en la **misma red Wi-Fi** (o uno comparte el **hotspot** de su celular y el otro se conecta).
2. Menú principal → **MULTIJUGADOR**.
   - Uno pulsa **CREAR PARTIDA**, elige el piso y espera (en pantalla aparece su IP).
   - El otro pulsa **UNIRSE**: la partida aparece sola en la lista; si no, escribe la IP y **CONECTAR**.
3. Los dos eligen personaje a la vez. **No se puede repetir**: el que tomó el compañero aparece como "TU COMPAÑERO".
4. Cuando los dos están listos, cuenta 3 y empieza el piso en ambos equipos.

En Windows, la primera vez aparece el aviso del Firewall: hay que pulsar **Permitir acceso** (redes privadas).
En Android el permiso de Internet ya queda activado (Player Settings → Internet Access).

## Reglas del modo
- **Dos demoledores**, cada uno con su cuadrilla de mini villanos, su lluvia de escombros, su rabia y sus movimientos especiales.
  El segundo entra unos segundos después, por el mismo ventanal.
- Las vidas, los puntos y la energía de los demoledores son del equipo.
- Cada estudiante tiene su propia barra de FURIA (K + L o tocar la barra).
- Cualquiera de los dos puede leer los "?" y entrar al ascensor.
- La pausa del anfitrión detiene la partida de los dos; la del invitado solo abre su menú.
- El nivel final (la fachada) es de un jugador: al llegar ahí termina la sesión en red.
- Si se corta la conexión: el invitado vuelve al menú y el anfitrión sigue solo.

## Cómo funciona (para programar)
- `Assets/Scripts/Red/RedJuego.cs`: conexión TCP (puerto 47811) y anuncio/búsqueda UDP (puerto 47812). Sin paquetes externos.
- `LobbyRed.cs`: el menú MULTIJUGADOR y la elección de personajes.
- `MultijugadorNivel.cs`: en el anfitrión crea al segundo estudiante (remoto) y al segundo demoledor; en el invitado apaga la
  simulación, manda la entrada (movimiento, martillazos, K + L) y aplica el HUD.
- `EspejoRed.cs`: el anfitrión manda 25 veces por segundo los sprites y textos que se mueven (solo lo que cambió); el invitado
  los dibuja con movimiento suave. El mapa no viaja: el invitado lo arma igual y recibe el estado de cada objeto.
  Los sprites viajan por nombre y, si el invitado no los tiene cargados, como PNG una sola vez.

## Movimientos especiales nuevos del demoledor
- **EMBESTIDA**: marca una franja con flechas, se agacha y carga en línea recta rompiendo lo que haya; si choca contra una
  pared queda mareado (¡PAF!). Desde el piso 1 o con la energía bajo 75 %.
- **PISOTÓN SÍSMICO**: salta (aro de aviso en el piso) y cae: daña los objetos del radio, empuja y marea a los jugadores
  (quita vida solo cerca del centro) y deja grietas. Desde el piso 2 o con la energía bajo 55 %.
- En rabia los usa más seguido. Valores en el Inspector del demoledor: "Movimientos especiales".
