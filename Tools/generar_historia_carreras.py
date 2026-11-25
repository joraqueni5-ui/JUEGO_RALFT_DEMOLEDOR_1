"""Fondos de la historia, uno por carrera (pixel art por código, 480 x 270, como los de la intro):

  Historia/carrera_<id>       el lugar donde se queda dormido cada estudiante, de noche (la víspera del examen)
  Historia/carrera_<id>_dia   el mismo lugar a la mañana siguiente (el despertar del final)
  Historia/mesa_<id>          la mesa en primer plano (transparente alrededor): el personaje queda "sentado" detrás

  sistemas      laboratorio de computación (monitores con código, rack de servidores)
  medicina      laboratorio de Enfermería (esqueleto, afiche del corazón, monitor, camilla)
  administracion oficina (gráfico de barras, archivadores, pizarra con el balance)
  gastronomia   cocina de la U (azulejos, horno encendido, ollas, menú del examen práctico)
  derecho       biblioteca de Derecho (estantes de códigos, balanza de la justicia)
  arquitectura  taller (tableros de dibujo, planos, maqueta)
  odontologia   clínica (sillón dental, lámpara, afiche de dientes)
  comunicacion  estudio de radio (AL AIRE, espuma acústica, cámara)

Salida: Assets/Resources/Extras/Historia/   (uso: python Tools/generar_historia_carreras.py)
"""
import random

from PIL import Image, ImageDraw

import exportar_extras as ex

W, H = 480, 270
K = (16, 14, 24, 255)
PISO_Y = 186


def c4(c, a=255):
    return tuple(c[:3]) + (a,)


def mezcla(a, b, k):
    return tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,)


def degradado(d, x0, y0, x1, y1, arriba, abajo):
    for y in range(y0, y1):
        k = (y - y0) / max(1, y1 - y0 - 1)
        d.line([(x0, y), (x1, y)], fill=mezcla(arriba, abajo, k))


def texto(d, xy, s, color):
    d.text(xy, s, fill=c4(color))


# ------------------------------------------------------------------ base común
def base(pared, piso, dia, reloj=(420, 50), ventana=(20, 30), baldosas=False):
    im = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(im)
    claro = mezcla(pared, (255, 255, 255), 0.12)
    oscuro = mezcla(pared, (0, 0, 0), 0.12)
    if dia:
        claro = mezcla(claro, (255, 236, 200), 0.25)
        oscuro = mezcla(oscuro, (255, 226, 180), 0.2)
    degradado(d, 0, 0, W, PISO_Y, claro, oscuro)
    if baldosas:
        for y in range(8, PISO_Y, 16):
            d.line([(0, y), (W, y)], fill=mezcla(oscuro, (255, 255, 255), 0.25))
        for y in range(8, PISO_Y, 16):
            off = 0 if (y // 16) % 2 == 0 else 8
            for x in range(off, W, 16):
                d.line([(x, y), (x, y + 16)], fill=mezcla(oscuro, (255, 255, 255), 0.25))
    d.rectangle([0, PISO_Y, W, H], fill=c4(piso))
    for y in range(PISO_Y + 4, H, 12):
        d.line([(0, y), (W, y)], fill=mezcla(piso, (0, 0, 0), 0.15))
    for x in range(0, W + 40, 40):
        d.line([(x, PISO_Y), (x - 30, H)], fill=mezcla(piso, (0, 0, 0), 0.1))
    d.rectangle([0, PISO_Y - 4, W, PISO_Y], fill=mezcla(pared, (0, 0, 0), 0.35))   # zócalo
    # Ventana.
    vx, vy = ventana
    if vx is not None:
        if dia:
            d.rectangle([vx, vy, vx + 70, vy + 80], fill=(140, 205, 250, 255))
            d.ellipse([vx + 40, vy + 8, vx + 60, vy + 28], fill=(255, 236, 120, 255))
            d.ellipse([vx + 6, vy + 46, vx + 34, vy + 60], fill=(255, 255, 255, 255))
            d.ellipse([vx + 20, vy + 40, vx + 46, vy + 56], fill=(255, 255, 255, 255))
            # Las laderas de La Paz.
            for i in range(0, 70, 4):
                d.rectangle([vx + i, vy + 68 - (i * 7) % 9, vx + i + 3, vy + 80], fill=(190, 110, 80, 255))
        else:
            d.rectangle([vx, vy, vx + 70, vy + 80], fill=(20, 30, 66, 255))
            d.ellipse([vx + 8, vy + 10, vx + 24, vy + 26], fill=(250, 246, 210, 255))
            for (x, y) in ((50, 14), (60, 40), (16, 60), (46, 66), (30, 36)):
                d.point((vx + x, vy + y), fill=(255, 255, 255, 255))
            for i in range(0, 70, 4):
                d.rectangle([vx + i, vy + 68 - (i * 7) % 9, vx + i + 3, vy + 80], fill=(40, 42, 70, 255))
                if (i // 4) % 3 == 0:
                    d.point((vx + i + 1, vy + 74), fill=(255, 220, 120, 255))
        d.rectangle([vx, vy, vx + 70, vy + 80], outline=(230, 230, 230, 255), width=4)
        d.line([(vx + 35, vy), (vx + 35, vy + 80)], fill=(230, 230, 230, 255), width=3)
    # Reloj: casi medianoche de noche, 7:15 de día.
    if reloj is not None:
        rx, ry = reloj
        d.ellipse([rx - 20, ry - 20, rx + 20, ry + 20], fill=(240, 240, 236, 255), outline=(40, 40, 50, 255), width=3)
        if dia:
            d.line([(rx, ry), (rx + 13, ry + 2)], fill=K, width=1)      # minutero en el 3
            d.line([(rx, ry), (rx + 4, ry + 9)], fill=K, width=2)       # hora cerca del 7
        else:
            d.line([(rx, ry), (rx, ry - 14)], fill=K, width=2)
            d.line([(rx, ry), (rx - 2, ry - 13)], fill=K, width=1)
    # Lámpara colgante.
    d.line([(240, 0), (240, 12)], fill=(50, 50, 60, 255))
    d.polygon([(228, 12), (252, 12), (247, 20), (233, 20)], fill=(250, 220, 120, 255))
    return im, d


def mesa_base(color=(150, 104, 60), ancho=(140, 340), frente=True):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x0, x1 = ancho
    d.rectangle([x0, 196, x1, 205], fill=c4(color), outline=K)
    d.rectangle([x0 + 2, 196, x1 - 2, 197], fill=mezcla(color, (255, 255, 255), 0.3))
    if frente:
        d.rectangle([x0 + 6, 205, x1 - 6, 248], fill=mezcla(color, (0, 0, 0), 0.25), outline=K)
        d.rectangle([x0 + 12, 212, x1 - 12, 242], outline=mezcla(color, (0, 0, 0), 0.4))
    else:
        for x in (x0 + 6, x1 - 12):
            d.rectangle([x, 205, x + 6, 250], fill=(70, 70, 80, 255), outline=K)
    return im, d


def libros(d, x, y, n=4, colores=((200, 60, 60), (60, 110, 200), (60, 160, 90), (230, 180, 60))):
    for i in range(n):
        c = colores[i % len(colores)]
        d.rectangle([x, y - 5 * (i + 1), x + 26 - (i % 2) * 4, y - 5 * i - 1], fill=c4(c), outline=K)
        d.line([(x + 3, y - 5 * i - 3), (x + 18, y - 5 * i - 3)], fill=(240, 236, 220, 255))


def taza(d, x, y, color=(240, 240, 236)):
    d.rectangle([x, y - 10, x + 8, y], fill=c4(color), outline=K)
    d.arc([x + 6, y - 8, x + 12, y - 3], 270, 90, fill=K)


def lampara_escritorio(d, x, y):
    d.rectangle([x, y - 3, x + 14, y], fill=(60, 60, 70, 255))
    d.line([(x + 7, y - 3), (x + 2, y - 22)], fill=(60, 60, 70, 255), width=2)
    d.polygon([(x - 6, y - 26), (x + 8, y - 20), (x + 2, y - 14)], fill=(240, 200, 80, 255), outline=K)


# ------------------------------------------------------------------ carreras
def sistemas(dia):
    im, d = base((70, 86, 110), (80, 84, 96), dia, reloj=(440, 40))
    # Rack de servidores con lucecitas.
    d.rectangle([380, 70, 430, 186], fill=(30, 32, 40, 255), outline=K)
    for y in range(78, 180, 10):
        d.rectangle([384, y, 426, y + 7], fill=(50, 54, 66, 255))
        for i in range(3):
            d.point((390 + i * 4, y + 3), fill=(80, 255, 120, 255) if (y + i) % 3 else (255, 120, 80, 255))
    # Fila de monitores en la pared del fondo (con código).
    for i, x in enumerate((110, 180, 290)):
        d.rectangle([x, 96, x + 56, 134], fill=(20, 24, 30, 255), outline=K)
        d.rectangle([x + 3, 99, x + 53, 131], fill=(16, 40, 34, 255) if not dia else (30, 50, 46, 255))
        rng = random.Random(i)
        for k in range(6):
            w = rng.randint(10, 40)
            col = rng.choice(((120, 255, 160), (120, 200, 255), (255, 220, 120)))
            d.line([(x + 6 + (k % 2) * 4, 103 + k * 5), (x + 6 + w, 103 + k * 5)], fill=c4(col))
        d.rectangle([x + 24, 134, x + 32, 142], fill=(40, 40, 50, 255))
    # Afiche.
    d.rectangle([110, 30, 210, 80], fill=(30, 40, 70, 255), outline=K)
    texto(d, (118, 38), "HELLO,", (120, 255, 160))
    texto(d, (118, 52), "UNIFRANZ!", (120, 255, 160))
    texto(d, (118, 64), "{ }  < />", (255, 180, 80))
    d.rectangle([250, 30, 350, 80], fill=(240, 140, 40, 255), outline=K)
    texto(d, (258, 40), "HACKATHON", (255, 255, 255))
    texto(d, (258, 56), "UNIFRANZ", (40, 30, 30))
    return im


def mesa_sistemas():
    im, d = mesa_base((120, 124, 136))
    # Monitor de costado, teclado y mouse, latas de energizante.
    d.rectangle([150, 160, 196, 192], fill=(24, 26, 32, 255), outline=K)
    d.rectangle([153, 163, 193, 189], fill=(20, 60, 50, 255))
    for k in range(4):
        d.line([(156, 167 + k * 5), (156 + 10 + k * 7, 167 + k * 5)], fill=(120, 255, 160, 255))
    d.rectangle([168, 192, 176, 196], fill=(40, 40, 50, 255))
    d.rectangle([206, 190, 274, 196], fill=(50, 52, 60, 255), outline=K)
    for x in range(209, 272, 4):
        d.point((x, 193), fill=(150, 150, 160, 255))
    d.ellipse([282, 190, 290, 196], fill=(60, 60, 70, 255), outline=K)
    for x in (300, 312):
        d.rectangle([x, 182, x + 8, 196], fill=(40, 200, 120, 255), outline=K)
        d.line([(x + 1, 187), (x + 7, 187)], fill=(255, 255, 255, 255))
    return im


def medicina(dia):
    im, d = base((176, 214, 210), (190, 196, 200), dia, reloj=(440, 40), baldosas=True)
    # Esqueleto en su soporte.
    sx = 380
    d.line([(sx, 60), (sx, 180)], fill=(90, 90, 100, 255), width=2)
    d.rectangle([sx - 14, 180, sx + 14, 184], fill=(90, 90, 100, 255))
    hueso = (240, 236, 220, 255)
    d.ellipse([sx - 9, 62, sx + 9, 80], fill=hueso, outline=K)
    d.point((sx - 4, 70), fill=K); d.point((sx + 3, 70), fill=K)
    d.line([(sx, 80), (sx, 130)], fill=hueso, width=3)
    for y in range(88, 116, 5):
        d.line([(sx - 12, y), (sx + 12, y)], fill=hueso)
    d.line([(sx - 14, 90), (sx - 20, 128)], fill=hueso, width=2)
    d.line([(sx + 14, 90), (sx + 20, 128)], fill=hueso, width=2)
    d.line([(sx - 6, 130), (sx - 10, 176)], fill=hueso, width=2)
    d.line([(sx + 6, 130), (sx + 10, 176)], fill=hueso, width=2)
    # Afiche del corazón.
    d.rectangle([110, 30, 190, 100], fill=(250, 250, 246, 255), outline=K)
    d.ellipse([126, 46, 152, 72], fill=(210, 60, 70, 255))
    d.ellipse([146, 46, 172, 72], fill=(210, 60, 70, 255))
    d.polygon([(127, 62), (171, 62), (149, 92)], fill=(210, 60, 70, 255))
    d.line([(150, 40), (150, 52)], fill=(80, 100, 200, 255), width=3)
    texto(d, (118, 88), "ANATOMIA", (40, 40, 60))
    # Monitor de signos vitales.
    d.rectangle([270, 40, 340, 90], fill=(30, 34, 40, 255), outline=K)
    d.rectangle([274, 44, 336, 80], fill=(10, 30, 20, 255))
    d.line([(276, 64), (296, 64), (300, 52), (304, 74), (308, 64), (334, 64)], fill=(80, 255, 120, 255))
    texto(d, (276, 80), "72 BPM", (80, 255, 120))
    # Camilla.
    d.rectangle([296, 150, 360, 158], fill=(230, 236, 240, 255), outline=K)
    d.rectangle([300, 158, 304, 184], fill=(150, 150, 160, 255)); d.rectangle([352, 158, 356, 184], fill=(150, 150, 160, 255))
    d.ellipse([296, 144, 316, 152], fill=(255, 255, 255, 255), outline=K)
    return im


def mesa_medicina():
    im, d = mesa_base((210, 214, 220))
    libros(d, 150, 196, 5, ((200, 60, 60), (240, 240, 236), (60, 110, 200), (200, 60, 60), (60, 160, 90)))
    # Libro abierto de anatomía.
    d.polygon([(205, 196), (238, 190), (238, 196)], fill=(250, 250, 246, 255), outline=K)
    d.polygon([(242, 190), (275, 196), (242, 196)], fill=(250, 250, 246, 255), outline=K)
    d.ellipse([250, 191, 258, 195], fill=(210, 60, 70, 255))
    # Estetoscopio.
    d.arc([288, 176, 318, 200], 180, 360, fill=(40, 40, 50, 255), width=2)
    d.ellipse([312, 190, 322, 198], fill=(170, 170, 180, 255), outline=K)
    # Modelo de corazón.
    d.ellipse([300, 180, 312, 192], fill=(210, 60, 70, 255), outline=K)
    taza(d, 330, 196, (120, 200, 230))
    return im


def administracion(dia):
    im, d = base((200, 196, 186), (110, 96, 84), dia, reloj=(440, 40))
    # Gráfico de barras.
    d.rectangle([110, 28, 210, 100], fill=(250, 250, 246, 255), outline=K)
    for i, h in enumerate((20, 30, 26, 44, 56)):
        d.rectangle([120 + i * 17, 92 - h, 132 + i * 17, 92], fill=((60, 120, 210, 255), (240, 140, 40, 255))[i % 2])
    d.line([(118, 92), (204, 92)], fill=K)
    texto(d, (120, 30), "VENTAS", (40, 40, 60))
    # Pizarra con el balance.
    d.rectangle([250, 30, 350, 100], fill=(250, 252, 255, 255), outline=(90, 96, 108, 255), width=3)
    texto(d, (258, 38), "ACTIVO", (40, 80, 200))
    texto(d, (258, 52), "PASIVO", (200, 60, 60))
    texto(d, (258, 70), "= ???", (40, 40, 60))
    d.line([(300, 42), (340, 42)], fill=(40, 80, 200, 255)); d.line([(300, 56), (340, 56)], fill=(200, 60, 60, 255))
    # Archivadores.
    for x in (370, 410):
        d.rectangle([x, 96, x + 36, 186], fill=(130, 136, 150, 255), outline=K)
        for y in (104, 134, 164):
            d.rectangle([x + 4, y, x + 32, y + 22], outline=(90, 96, 108, 255))
            d.rectangle([x + 14, y + 9, x + 22, y + 12], fill=(60, 60, 70, 255))
    # Planta.
    d.rectangle([100, 168, 116, 186], fill=(180, 90, 60, 255), outline=K)
    d.ellipse([94, 140, 122, 170], fill=(60, 150, 80, 255), outline=K)
    return im


def mesa_administracion():
    im, d = mesa_base((110, 76, 52))
    # Pilas de papeles, calculadora, taza.
    for i in range(6):
        d.rectangle([150 + (i % 2), 192 - i * 3, 186 + (i % 2), 194 - i * 3], fill=(250, 250, 246, 255), outline=(170, 170, 170, 255))
    d.rectangle([194, 182, 214, 196], fill=(60, 64, 74, 255), outline=K)
    d.rectangle([197, 184, 211, 188], fill=(170, 220, 170, 255))
    for x in range(198, 212, 4):
        for y in (190, 193):
            d.point((x, y), fill=(220, 220, 220, 255))
    for i in range(4):
        d.rectangle([286, 192 - i * 3, 322, 194 - i * 3], fill=(250, 250, 246, 255), outline=(170, 170, 170, 255))
    texto(d, (290, 178), "$", (60, 160, 90))
    taza(d, 328, 196, (240, 140, 40))
    return im


def gastronomia(dia):
    im, d = base((236, 234, 226), (170, 60, 50), dia, reloj=(440, 30), ventana=(None, None), baldosas=True)
    # Piso a cuadros.
    for y in range(PISO_Y, H, 12):
        for x in range(0, W, 12):
            if ((x // 12) + (y // 12)) % 2 == 0:
                d.rectangle([x, y, x + 11, y + 11], fill=(240, 240, 236, 255))
            else:
                d.rectangle([x, y, x + 11, y + 11], fill=(40, 40, 50, 255))
    # Campana extractora y cocina con ollas.
    d.polygon([(20, 40), (130, 40), (114, 70), (36, 70)], fill=(180, 186, 196, 255), outline=K)
    d.rectangle([24, 120, 126, 186], fill=(170, 176, 186, 255), outline=K)
    d.rectangle([30, 140, 120, 180], fill=(60, 62, 70, 255), outline=K)
    d.rectangle([36, 146, 114, 172], fill=(255, 140, 40, 255) if not dia else (120, 80, 60, 255))   # horno encendido
    if not dia:
        d.rectangle([40, 160, 110, 170], fill=(255, 210, 90, 255))
    d.rectangle([60, 150, 90, 160], fill=(240, 220, 160, 255) if not dia else (60, 40, 30, 255), outline=K)   # el soufflé (de día, quemado)
    if dia:
        for i in range(5):
            d.ellipse([62 + i * 5, 118 - i * 4, 72 + i * 5, 128 - i * 4], fill=(90, 90, 96, 200))      # humito
    for x in (34, 78):
        d.rectangle([x, 104, x + 34, 120], fill=(150, 156, 166, 255), outline=K)                    # ollas
        d.rectangle([x + 4, 100, x + 30, 104], fill=(120, 126, 136, 255), outline=K)
    # Utensilios colgados.
    d.line([(150, 40), (240, 40)], fill=(90, 90, 100, 255), width=2)
    for i, x in enumerate(range(156, 236, 16)):
        d.line([(x, 40), (x, 50)], fill=(90, 90, 100, 255))
        if i % 2 == 0:
            d.ellipse([x - 6, 50, x + 6, 62], fill=(170, 176, 186, 255), outline=K)   # cucharón
        else:
            d.rectangle([x - 1, 50, x + 1, 70], fill=(170, 176, 186, 255))            # espátula
            d.rectangle([x - 4, 66, x + 4, 72], fill=(170, 176, 186, 255), outline=K)
    # Pizarra del menú del examen.
    d.rectangle([270, 34, 390, 110], fill=(40, 46, 44, 255), outline=(110, 76, 50, 255), width=4)
    texto(d, (280, 42), "EXAMEN PRACTICO", (240, 240, 230))
    texto(d, (280, 58), "* SOUFFLE", (255, 220, 120))
    texto(d, (280, 72), "* SALTENAS", (255, 220, 120))
    texto(d, (280, 86), "* API MORADO", (200, 160, 255))
    d.line([(310, 70), (312, 69), (314, 70)], fill=(255, 220, 120, 255))
    # Refrigerador.
    d.rectangle([410, 70, 460, 186], fill=(236, 240, 244, 255), outline=K)
    d.line([(410, 116), (460, 116)], fill=K)
    d.rectangle([452, 84, 455, 106], fill=(150, 150, 160, 255)); d.rectangle([452, 124, 455, 150], fill=(150, 150, 160, 255))
    d.rectangle([420, 80, 436, 92], fill=(255, 230, 120, 255))   # notita
    return im


def mesa_gastronomia():
    im, d = mesa_base((200, 204, 212))
    d.rectangle([140, 196, 340, 199], fill=(230, 232, 236, 255))
    # Harina, huevos, bol con batidor y rodillo.
    d.polygon([(150, 196), (156, 176), (176, 176), (182, 196)], fill=(250, 246, 236, 255), outline=K)
    for x in (188, 196):
        d.ellipse([x, 188, x + 7, 196], fill=(250, 240, 220, 255), outline=K)
    d.chord([284, 178, 320, 204], 180, 360, fill=(240, 140, 40, 255), outline=K)
    d.line([(300, 186), (310, 168)], fill=(170, 176, 186, 255), width=2)
    d.ellipse([306, 162, 314, 172], outline=(170, 176, 186, 255))
    d.rectangle([206, 192, 262, 196], fill=(200, 150, 90, 255), outline=K)
    d.rectangle([200, 193, 206, 195], fill=(150, 100, 60, 255)); d.rectangle([262, 193, 268, 195], fill=(150, 100, 60, 255))
    # Libro de recetas.
    d.rectangle([322, 186, 338, 196], fill=(200, 60, 60, 255), outline=K)
    return im


def derecho(dia):
    im, d = base((150, 110, 90), (100, 70, 50), dia, reloj=(440, 40))
    # Estantes llenos de códigos.
    for x0 in (110, 300):
        d.rectangle([x0, 30, x0 + 110, 186], fill=(90, 60, 40, 255), outline=K)
        for y in (34, 72, 110, 148):
            d.rectangle([x0 + 4, y + 32, x0 + 106, y + 36], fill=(70, 46, 30, 255))
            rng = random.Random(x0 + y)
            x = x0 + 6
            while x < x0 + 100:
                w = rng.randint(5, 9)
                c = rng.choice(((120, 30, 30), (30, 50, 100), (40, 80, 50), (110, 80, 40)))
                hh = rng.randint(22, 30)
                d.rectangle([x, y + 32 - hh, x + w, y + 32], fill=c4(c), outline=K)
                d.line([(x + 1, y + 32 - hh + 4), (x + w - 1, y + 32 - hh + 4)], fill=(230, 200, 100, 255))
                x += w + 1
    # Balanza de la justicia.
    bx = 260
    d.line([(bx, 40), (bx, 90)], fill=(230, 190, 80, 255), width=2)
    d.line([(bx - 24, 50), (bx + 24, 50)], fill=(230, 190, 80, 255), width=2)
    for s in (-1, 1):
        d.line([(bx + s * 24, 50), (bx + s * 24 - 6, 66)], fill=(230, 190, 80, 255))
        d.line([(bx + s * 24, 50), (bx + s * 24 + 6, 66)], fill=(230, 190, 80, 255))
        d.chord([bx + s * 24 - 9, 60, bx + s * 24 + 9, 72], 0, 180, fill=(230, 190, 80, 255))
    d.rectangle([bx - 10, 90, bx + 10, 94], fill=(230, 190, 80, 255))
    return im


def mesa_derecho():
    im, d = mesa_base((90, 56, 36))
    # Pilas de códigos y el mazo.
    libros(d, 148, 196, 6, ((120, 30, 30), (30, 50, 100), (40, 80, 50)))
    texto(d, (150, 160), "CODIGO", (230, 200, 100))
    libros(d, 300, 196, 3, ((30, 50, 100), (120, 30, 30)))
    d.rectangle([258, 188, 290, 194], fill=(110, 70, 40, 255), outline=K)       # cabeza del mazo
    d.line([(274, 191), (296, 182)], fill=(110, 70, 40, 255), width=2)
    d.rectangle([254, 194, 270, 196], fill=(80, 50, 30, 255))
    lampara_escritorio(d, 200, 196)
    return im


def arquitectura(dia):
    im, d = base((210, 210, 214), (140, 140, 146), dia, reloj=(440, 40))
    # Planos azules clavados.
    for i, x in enumerate((110, 196)):
        d.rectangle([x, 30, x + 76, 92], fill=(40, 80, 160, 255), outline=K)
        for k in range(0, 76, 8):
            d.line([(x + k, 30), (x + k, 92)], fill=(60, 100, 180, 255))
        d.rectangle([x + 12, 46, x + 60, 80], outline=(230, 240, 255, 255))
        d.line([(x + 12, 60), (x + 40, 60)], fill=(230, 240, 255, 255))
        d.line([(x + 40, 46), (x + 40, 80)], fill=(230, 240, 255, 255))
        d.point((x + 4, 34), fill=(230, 60, 60, 255))
    texto(d, (198, 82), "TORRE U", (230, 240, 255))
    # Tablero de dibujo inclinado.
    d.polygon([(300, 110), (380, 96), (390, 150), (310, 160)], fill=(240, 240, 232, 255), outline=K)
    d.line([(320, 120), (370, 112)], fill=(80, 80, 90, 255))
    d.line([(305, 160), (300, 186)], fill=(80, 80, 90, 255), width=2); d.line([(385, 150), (392, 186)], fill=(80, 80, 90, 255), width=2)
    # Escalímetro y escuadra en la pared.
    d.polygon([(400, 40), (450, 90), (400, 90)], outline=(240, 140, 40, 255))
    return im


def mesa_arquitectura():
    im, d = mesa_base((236, 236, 232))
    # Maqueta de un edificio (la U) y herramientas.
    d.rectangle([286, 156, 318, 196], fill=(250, 250, 246, 255), outline=K)
    for y in range(160, 194, 8):
        for x in range(290, 316, 8):
            d.rectangle([x, y, x + 4, y + 4], fill=(140, 200, 240, 255))
    d.rectangle([318, 174, 336, 196], fill=(240, 140, 40, 255), outline=K)
    d.rectangle([150, 188, 200, 196], fill=(240, 240, 232, 255), outline=K)    # rollo de planos
    d.ellipse([196, 188, 204, 196], fill=(40, 80, 160, 255), outline=K)
    d.line([(210, 194), (262, 186)], fill=(240, 200, 80, 255), width=2)          # regla
    d.rectangle([266, 188, 272, 196], fill=(255, 255, 255, 255), outline=K)       # pegamento
    d.rectangle([267, 184, 271, 188], fill=(240, 140, 40, 255))
    return im


def odontologia(dia):
    im, d = base((190, 222, 236), (176, 186, 196), dia, reloj=(440, 40), baldosas=True)
    # Afiche de un diente sonriente.
    d.rectangle([110, 30, 190, 100], fill=(250, 250, 246, 255), outline=K)
    d.rounded_rectangle([132, 40, 168, 74], 8, fill=(255, 255, 255, 255), outline=K)
    d.polygon([(134, 70), (140, 92), (148, 72)], fill=(255, 255, 255, 255), outline=K)
    d.polygon([(152, 72), (160, 92), (166, 70)], fill=(255, 255, 255, 255), outline=K)
    d.point((143, 54), fill=K); d.point((157, 54), fill=K)
    d.arc([142, 56, 158, 66], 20, 160, fill=K)
    texto(d, (116, 88), "CEPILLATE!", (40, 120, 200))
    # Sillón dental con lámpara.
    d.polygon([(300, 150), (360, 150), (380, 120), (390, 124), (372, 160), (300, 160)], fill=(60, 170, 190, 255), outline=K)
    d.rectangle([328, 160, 336, 186], fill=(150, 150, 160, 255))
    d.rectangle([310, 182, 356, 186], fill=(150, 150, 160, 255))
    d.line([(300, 40), (300, 70), (330, 80)], fill=(150, 150, 160, 255), width=2)
    d.ellipse([326, 76, 346, 88], fill=(255, 250, 220, 255), outline=K)
    # Rayos X en la pared.
    d.rectangle([220, 34, 270, 74], fill=(30, 34, 44, 255), outline=K)
    for x in range(226, 266, 8):
        d.rounded_rectangle([x, 44, x + 6, 64], 2, fill=(220, 230, 240, 255))
    return im


def mesa_odontologia():
    im, d = mesa_base((236, 240, 244))
    # Modelo de dientes gigante, instrumentos e hilo dental.
    d.chord([284, 164, 336, 204], 180, 360, fill=(240, 140, 150, 255), outline=K)
    for x in range(288, 334, 6):
        d.rounded_rectangle([x, 172, x + 5, 184], 1, fill=(255, 255, 255, 255), outline=K)
    for i, x in enumerate((152, 160, 168)):
        d.line([(x, 196), (x + 4, 178)], fill=(170, 176, 186, 255), width=2)
        d.ellipse([x + 2, 174, x + 7, 179], fill=(200, 206, 216, 255))
    d.rectangle([186, 188, 200, 196], fill=(80, 200, 220, 255), outline=K)
    taza(d, 268, 196, (120, 200, 230))
    return im


def comunicacion(dia):
    im, d = base((70, 60, 90), (60, 50, 60), dia, reloj=(440, 40))
    # Espuma acústica.
    for y in range(24, 150, 12):
        for x in range(110, 380, 12):
            c = (60, 50, 80) if ((x // 12) + (y // 12)) % 2 else (80, 66, 100)
            d.polygon([(x, y + 12), (x + 6, y), (x + 12, y + 12)], fill=c4(c))
    # Cartel AL AIRE (apagado de día).
    d.rectangle([200, 30, 290, 54], fill=(200, 30, 40, 255) if not dia else (110, 40, 50, 255), outline=K)
    texto(d, (218, 36), "AL AIRE", (255, 255, 255) if not dia else (200, 170, 170))
    # Cámara en trípode y logo de la radio U.
    d.rectangle([396, 90, 440, 116], fill=(40, 40, 50, 255), outline=K)
    d.ellipse([384, 94, 400, 112], fill=(80, 140, 220, 255), outline=K)
    d.line([(418, 116), (400, 186)], fill=(60, 60, 70, 255), width=2)
    d.line([(418, 116), (436, 186)], fill=(60, 60, 70, 255), width=2)
    d.line([(418, 116), (418, 186)], fill=(60, 60, 70, 255), width=2)
    d.rectangle([110, 160, 180, 180], fill=(240, 140, 40, 255), outline=K)
    texto(d, (116, 164), "RADIO U", (255, 255, 255))
    return im


def mesa_comunicacion():
    im, d = mesa_base((50, 50, 60))
    # Consola de mezcla, micrófono con brazo y audífonos.
    d.rectangle([148, 184, 204, 196], fill=(30, 30, 40, 255), outline=K)
    for i, x in enumerate(range(152, 202, 7)):
        d.line([(x, 186), (x, 194)], fill=(90, 90, 100, 255))
        d.rectangle([x - 1, 187 + (i * 3) % 6, x + 1, 189 + (i * 3) % 6], fill=(240, 240, 240, 255))
    d.line([(300, 196), (300, 170), (276, 160)], fill=(60, 60, 70, 255), width=2)
    d.rounded_rectangle([268, 150, 280, 168], 4, fill=(60, 60, 70, 255), outline=K)
    for y in range(154, 166, 3):
        d.line([(270, y), (278, y)], fill=(110, 110, 120, 255))
    d.arc([310, 180, 334, 204], 180, 360, fill=(30, 30, 40, 255), width=3)
    d.ellipse([306, 188, 314, 198], fill=(240, 140, 40, 255), outline=K)
    d.ellipse([330, 188, 338, 198], fill=(240, 140, 40, 255), outline=K)
    return im


CARRERAS = {
    "sistemas": (sistemas, mesa_sistemas),
    "medicina": (medicina, mesa_medicina),
    "administracion": (administracion, mesa_administracion),
    "gastronomia": (gastronomia, mesa_gastronomia),
    "derecho": (derecho, mesa_derecho),
    "arquitectura": (arquitectura, mesa_arquitectura),
    "odontologia": (odontologia, mesa_odontologia),
    "comunicacion": (comunicacion, mesa_comunicacion),
}


def mesa_aula():
    im, d = mesa_base((150, 104, 60))
    libros(d, 150, 196, 4)
    d.rectangle([206, 190, 270, 196], fill=(250, 250, 246, 255), outline=K)    # cuaderno abierto
    d.line([(238, 190), (238, 196)], fill=(170, 170, 170, 255))
    texto(d, (210, 176), "x=?", (60, 60, 200))
    taza(d, 300, 196)
    return im


def main():
    for clave, (fondo, mesa) in CARRERAS.items():
        ex.escribir(f"Historia/carrera_{clave}", fondo(False), 100)
        ex.escribir(f"Historia/carrera_{clave}_dia", fondo(True), 100)
        ex.escribir(f"Historia/mesa_{clave}", mesa(), 100)
    ex.escribir("Historia/mesa_aula", mesa_aula(), 100)
    print("Listo:", len(CARRERAS) * 3 + 1, "imágenes")


if __name__ == "__main__":
    main()
