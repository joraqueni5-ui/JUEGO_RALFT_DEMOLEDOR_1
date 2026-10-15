# -*- coding: utf-8 -*-
"""
Genera el documento PDF del Hito 3 (interfaz de usuario) del proyecto
Ralft Demoledor. El PDF incluye la descripcion de las escenas, el detalle
de los scripts de interfaz y las capturas tomadas por la herramienta de
editor (GeneradorJuego).

Uso:  python Herramientas/generar_documentacion.py
Salida: Documentacion/Hito3_Interfaz_Usuario_Ralft_Demoledor.pdf
"""
import os
import struct
import time

ANCHO = 595.0
ALTO = 842.0
MARGEN = 46.0


class Escritor:
    """Escritor minimo de PDF (sin dependencias externas)."""

    def __init__(self):
        self.objetos = []

    def agregar(self, datos):
        self.objetos.append(datos)
        return len(self.objetos)

    def reservar(self):
        self.objetos.append(None)
        return len(self.objetos)

    def definir(self, numero, datos):
        self.objetos[numero - 1] = datos

    def serializar(self):
        salida = bytearray(b"%PDF-1.4\n")
        posiciones = [0]

        for i, obj in enumerate(self.objetos, start=1):
            posiciones.append(len(salida))
            salida += ("%d 0 obj\n" % i).encode("ascii")
            salida += obj
            salida += b"\nendobj\n"

        inicio_xref = len(salida)
        total = len(self.objetos) + 1
        salida += ("xref\n0 %d\n" % total).encode("ascii")
        salida += b"0000000000 65535 f \n"

        for i in range(1, total):
            salida += ("%010d 00000 n \n" % posiciones[i]).encode("ascii")

        salida += ("trailer\n<< /Size %d /Root 1 0 R >>\nstartxref\n%d\n%%%%EOF\n" % (total, inicio_xref)).encode("ascii")
        return bytes(salida)


def escapar(texto):
    texto = texto.replace("\\", r"\\").replace("(", r"\(").replace(")", r"\)")
    return texto.encode("latin-1", "replace")


def color_pdf(color):
    return "%.3f %.3f %.3f" % color


def medidas_jpeg(ruta):
    with open(ruta, "rb") as f:
        datos = f.read()

    i = 2

    while i < len(datos):
        if datos[i] != 0xFF:
            i += 1
            continue

        marca = datos[i + 1]

        if marca in (0xC0, 0xC1, 0xC2):
            alto, ancho = struct.unpack(">HH", datos[i + 5:i + 9])
            componentes = datos[i + 9]
            return ancho, alto, componentes

        if marca in (0xD8, 0xD9) or 0xD0 <= marca <= 0xD7:
            i += 2
            continue

        longitud = struct.unpack(">H", datos[i + 2:i + 4])[0]
        i += 2 + longitud

    raise ValueError("No se pudieron leer las medidas de " + ruta)


class Pagina:
    """Acumula las instrucciones de dibujo de una pagina."""

    def __init__(self):
        self.instrucciones = []

    def rectangulo(self, x, y, w, h, color):
        self.instrucciones.append(
            "%s rg %s %s %s %s re f" % (color_pdf(color), x, y, w, h))

    def linea(self, x1, y1, x2, y2, grosor, color):
        self.instrucciones.append(
            "%s RG %s w %s %s m %s %s l S" % (color_pdf(color), grosor, x1, y1, x2, y2))

    def texto(self, x, y, contenido, tamano, negrita=False, color=(0.10, 0.11, 0.15)):
        fuente = "/F2" if negrita else "/F1"
        self.instrucciones.append(
            "BT %s %s Tf %s rg %s %s Td (%s) Tj ET"
            % (fuente, tamano, color_pdf(color), x, y, escapar(contenido).decode("latin-1")))

    def parrafo(self, x, y, contenido, tamano, ancho, interlinea, negrita=False,
                color=(0.10, 0.11, 0.15)):
        """Escribe un parrafo ajustando las palabras al ancho indicado."""
        palabras = contenido.split()
        linea = ""
        lineas = []

        for palabra in palabras:
            prueba = (linea + " " + palabra).strip()

            if len(prueba) * tamano * 0.50 > ancho and linea:
                lineas.append(linea)
                linea = palabra
            else:
                linea = prueba

        if linea:
            lineas.append(linea)

        actual = y

        for linea in lineas:
            self.texto(x, actual, linea, tamano, negrita, color)
            actual -= interlinea

        return actual

    def viñeta(self, x, y, contenido, tamano, ancho, interlinea):
        self.texto(x, y, "-", tamano, True, (0.25, 0.35, 0.65))
        return self.parrafo(x + 12.0, y, contenido, tamano, ancho - 12.0, interlinea)


def construir_pdf(paginas, imagenes, salida):
    w = Escritor()
    catalogo = w.reservar()
    arbol = w.reservar()

    f_normal = w.agregar(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
    f_negrita = w.agregar(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")

    ids_imagenes = {}
    recursos_xobject = []

    for alias, ruta in imagenes:
        if not os.path.exists(ruta):
            print("  aviso: no se encontro " + ruta)
            continue

        ancho_px, alto_px, componentes = medidas_jpeg(ruta)

        with open(ruta, "rb") as f:
            datos = f.read()

        cabecera = ("<< /Type /XObject /Subtype /Image /Width %d /Height %d "
                    "/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length %d >>\nstream\n"
                    % (ancho_px, alto_px, len(datos))).encode("ascii")

        numero = w.agregar(cabecera + datos + b"\nendstream")
        ids_imagenes[alias] = (numero, ancho_px, alto_px)
        recursos_xobject.append("/%s %d 0 R" % (alias, numero))

    ids_paginas = []

    for pagina in paginas:
        contenido = "\n".join(pagina.instrucciones).encode("latin-1", "replace")
        flujo = w.agregar(("<< /Length %d >>\nstream\n" % len(contenido)).encode("ascii") + contenido + b"\nendstream")
        recursos = ("<< /Font << /F1 %d 0 R /F2 %d 0 R >>" % (f_normal, f_negrita))

        if recursos_xobject:
            recursos += " /XObject << " + " ".join(recursos_xobject) + " >>"

        recursos += " >>"
        pagina_obj = w.agregar(("<< /Type /Page /Parent %d 0 R /MediaBox [0 0 %.0f %.0f] /Resources %s /Contents %d 0 R >>"
                                % (arbol, ANCHO, ALTO, recursos, flujo)).encode("ascii"))
        ids_paginas.append(pagina_obj)

    w.definir(arbol, ("<< /Type /Pages /Count %d /Kids [%s] >>"
                      % (len(ids_paginas), " ".join("%d 0 R" % i for i in ids_paginas))).encode("ascii"))
    w.definir(catalogo, ("<< /Type /Catalog /Pages %d 0 R >>" % arbol).encode("ascii"))

    os.makedirs(os.path.dirname(salida), exist_ok=True)

    with open(salida, "wb") as f:
        f.write(w.serializar())

    return ids_imagenes


def dibujar_imagen(pagina, alias, x, y, w, h):
    pagina.instrucciones.append(
        "q %.1f 0 0 %.1f %.1f %.1f cm /%s Do Q" % (w, h, x, y, alias))


def encabezado(pagina, titulo, subtitulo):
    pagina.rectangulo(0.0, ALTO - 92.0, ANCHO, 92.0, (0.09, 0.08, 0.15))
    pagina.rectangulo(0.0, ALTO - 96.0, ANCHO, 4.0, (0.95, 0.72, 0.20))
    pagina.texto(MARGEN, ALTO - 46.0, titulo, 19, True, (1.0, 1.0, 1.0))
    pagina.texto(MARGEN, ALTO - 68.0, subtitulo, 10.5, False, (0.85, 0.86, 0.95))


def titulo_seccion(pagina, y, numero, texto):
    pagina.rectangulo(MARGEN, y - 3.0, 4.0, 12.0, (0.95, 0.72, 0.20))
    pagina.texto(MARGEN + 12.0, y, "%s. %s" % (numero, texto), 12.5, True, (0.12, 0.14, 0.24))
    return y - 17.0


def leer_commits():
    import subprocess

    try:
        salida = subprocess.check_output(
            ["git", "log", "--pretty=format:%h|%an|%s", "-n", "14"],
            cwd=os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
            stderr=subprocess.DEVNULL)
        commits = []

        for linea in salida.decode("utf-8", "replace").splitlines():
            partes = linea.split("|")

            if len(partes) == 3:
                commits.append((partes[0], partes[1], partes[2]))

        return commits
    except Exception as error:
        print("  aviso: no se pudo leer el historial de git (" + str(error) + ")")
        return []


def main():
    base = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    caps = os.path.normpath(os.path.join(base, "..", "caps"))

    imagenes = [
        ("Im01", os.path.join(caps, "01_menu_principal", "01_menu_principal.jpg")),
        ("Im02", os.path.join(caps, "02_nivel_hud", "02_nivel_hud.jpg")),
        ("Im03", os.path.join(caps, "03_menu_pausa", "03_menu_pausa.jpg")),
        ("Im04", os.path.join(caps, "04_pantalla_game_over", "04_pantalla_game_over.jpg")),
        ("Im05", os.path.join(caps, "05_pantalla_victoria", "05_pantalla_victoria.jpg")),
        ("Im06", os.path.join(caps, "06_panel_controles", "06_panel_controles.jpg")),
    ]

    pagina1 = Pagina()
    encabezado(pagina1, "Ralft Demoledor - Hito 3: Interfaz de Usuario",
               "Juego 2D en Unity - Documentacion de la implementacion de la UI")

    y = ALTO - 118.0
    pagina1.texto(MARGEN, y, "Integrantes: Shaden, Jhonatan, Alejandro, Jorge", 9.5, True)
    y -= 13.0
    pagina1.texto(MARGEN, y, "Motor: Unity 6 (6000.3.16f1) - URP 2D - Plataforma: Windows 64 bits", 9.5)
    y -= 13.0
    pagina1.texto(MARGEN, y, "Fecha de entrega: %s" % time.strftime("%d/%m/%Y"), 9.5)

    y -= 30.0
    y = titulo_seccion(pagina1, y, "1", "Estructura de escenas")
    escenas = [
        "MenuPrincipal (indice 0): pantalla inicial con el titulo del juego, el boton Jugar, el boton Controles y el boton Salir.",
        "Nivel1 (indice 1): escena jugable. Contiene la sala de demolicion, el personaje, los ladrillos, los enemigos, el HUD y el panel de pausa.",
        "FinJuego (indice 2): pantalla de Game Over o de Victoria segun el resultado de la partida, con los botones Reintentar y Menu principal.",
    ]

    for texto in escenas:
        y = pagina1.parrafo(MARGEN, y, "- " + texto, 9.5, ANCHO - 2.0 * MARGEN, 12.0)
        y -= 5.0

    y -= 15.0
    pagina1.texto(MARGEN, y, "Carpetas del proyecto", 10.5, True, (0.25, 0.35, 0.65))
    y -= 14.0
    y = pagina1.parrafo(MARGEN, y, "Assets/Scripts/UI (scripts de interfaz), Assets/Scripts/Game (logica del juego), Assets/UI/Sprites (sprites de botones, paneles, fondos y personajes), Assets/UI/Fonts y Assets/Scenes (escenas del juego).", 9.5, ANCHO - 2.0 * MARGEN, 12.0)

    y -= 20.0
    y = titulo_seccion(pagina1, y, "2", "Scripts de la interfaz")
    scripts = [
        "MainMenuController: controla el menu principal. El boton Jugar carga la escena Nivel1 con SceneManager.LoadScene; el boton Salir ejecuta Application.Quit y, dentro del editor, detiene la reproduccion; el boton Controles activa y desactiva el panel de ayuda.",
        "PauseMenuController: abre y cierra el menu de pausa deteniendo el juego con Time.timeScale. Incluye los botones Reanudar, Reiniciar nivel y Menu principal.",
        "GameOverController: lee el resultado almacenado en GameSession y muestra el titulo Victoria o Game Over, el puntaje final y el detalle de la partida.",
        "HUDController: actualiza en pantalla el puntaje, las vidas y el contador de ladrillos recolectados.",
        "GameManager: aplica las reglas del nivel, entrega los datos al HUD, recibe el dano del jugador y carga la pantalla final cuando se gana o se pierde.",
        "GameSession y Escenas: clases auxiliares que guardan los datos de la partida y los nombres de las escenas para evitar errores de escritura.",
        "GeneradorJuego (Assets/Editor): herramienta de editor que genera los sprites, construye las escenas, configura el Build Settings, compila el ejecutable y toma las capturas.",
    ]

    for texto in scripts:
        y = pagina1.parrafo(MARGEN, y, "- " + texto, 9.0, ANCHO - 2.0 * MARGEN, 11.5)
        y -= 4.0

    y -= 18.0
    y = titulo_seccion(pagina1, y, "3", "Capturas de la interfaz implementada")
    y -= 4.0
    dibujar_imagen(pagina1, "Im01", MARGEN, y - 138.0, 240.0, 135.0)
    dibujar_imagen(pagina1, "Im02", MARGEN + 260.0, y - 138.0, 240.0, 135.0)
    y -= 146.0
    pagina1.texto(MARGEN, y, "Figura 1: menu principal (Jugar, Controles, Salir y titulo del juego).", 8.5)
    pagina1.texto(MARGEN + 260.0, y, "Figura 2: nivel de juego con el HUD en ejecucion.", 8.5)

    pagina2 = Pagina()
    encabezado(pagina2, "Ralft Demoledor - Hito 3: Interfaz de Usuario",
               "Navegacion entre escenas, pausa, fin de juego y control de versiones")

    y = ALTO - 118.0
    y = titulo_seccion(pagina2, y, "4", "Navegacion entre escenas")
    y = pagina2.parrafo(MARGEN, y, "El flujo de navegacion implementado es el siguiente:", 9.5, ANCHO - 2.0 * MARGEN, 12.0)
    y -= 6.0
    y = pagina2.parrafo(MARGEN, y, "MenuPrincipal --(Jugar)--> Nivel1 --(5 ladrillos o 0 vidas)--> FinJuego --(Reintentar)--> Nivel1 / --(Menu principal)--> MenuPrincipal", 9.5, ANCHO - 2.0 * MARGEN, 12.0, True, (0.20, 0.30, 0.60))
    y -= 6.0
    y = pagina2.parrafo(MARGEN, y, "Dentro del nivel, la tecla Escape abre el menu de pausa, que permite continuar la partida, reiniciar el nivel o regresar al menu principal. En todos los casos, antes de cambiar de escena se restablece Time.timeScale en 1 para evitar que el juego quede detenido.", 9.5, ANCHO - 2.0 * MARGEN, 12.0)

    y -= 22.0
    y = titulo_seccion(pagina2, y, "5", "Capturas del menu de pausa y del fin de juego")
    y -= 4.0
    dibujar_imagen(pagina2, "Im03", MARGEN, y - 96.0, 158.0, 89.0)
    dibujar_imagen(pagina2, "Im04", MARGEN + 171.0, y - 96.0, 158.0, 89.0)
    dibujar_imagen(pagina2, "Im05", MARGEN + 342.0, y - 96.0, 158.0, 89.0)
    y -= 104.0
    pagina2.texto(MARGEN, y, "Figura 3: menu de pausa.", 8.5)
    pagina2.texto(MARGEN + 171.0, y, "Figura 4: pantalla de Game Over.", 8.5)
    pagina2.texto(MARGEN + 342.0, y, "Figura 5: pantalla de Victoria.", 8.5)

    y -= 22.0
    y = titulo_seccion(pagina2, y, "6", "Control de versiones y entregables")
    y = pagina2.parrafo(MARGEN, y, "El avance se registro con commits descriptivos distribuidos entre los integrantes del equipo:", 9.5, ANCHO - 2.0 * MARGEN, 12.0)

    for hash_corto, autor, asunto in leer_commits():
        y = pagina2.parrafo(MARGEN, y, "- %s  %s: %s" % (hash_corto, autor, asunto), 8.5, ANCHO - 2.0 * MARGEN, 10.5)
        y -= 1.5

    y -= 14.0
    y = pagina2.parrafo(MARGEN, y, "Entregables: repositorio Git con el proyecto actualizado, ejecutable de Windows en la carpeta 'ejecutable' (RalftDemoledor.exe) y capturas de pantalla en la carpeta 'caps'.", 9.5, ANCHO - 2.0 * MARGEN, 12.0)

    salida = os.path.join(base, "Documentacion", "Hito3_Interfaz_Usuario_Ralft_Demoledor.pdf")
    construir_pdf([pagina1, pagina2], imagenes, salida)
    print("PDF generado: " + salida)


if __name__ == "__main__":
    main()
