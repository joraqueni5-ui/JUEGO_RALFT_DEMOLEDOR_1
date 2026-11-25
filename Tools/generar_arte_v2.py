"""Arte de la versión 2 (pixel art por código):

  Peligros/    charcos, aceite, cartel de piso mojado, cables, chispazos, gotas, manchas del techo, baldes, hojas y viento
  Accesorios/  gorros y sombreros que se desbloquean (mismo tamaño de píxel que los personajes: 100 px por unidad)
  Martillos/   partículas de los martillos especiales (corazón, estrellita, llama, copo)
  Historia/    fondos del aula, el cuarto y la universidad de día; despertador, cono y nube de sueño
  Icono/       icono de la aplicación y logo para la pantalla de inicio

Salida: Assets/Resources/Extras/  (uso: python Tools/generar_arte_v2.py)
"""
import math
import os
import random

from PIL import Image, ImageDraw

import exportar_extras as ex

K = (16, 14, 24, 255)
RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def contorno(im, color=K):
    w, h = im.size
    px = im.load(); out = im.copy(); po = out.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 100:
                    po[x, y] = color
                    break
    return out


def lienzo(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


# ================================================================== peligros
def charco(i, base, claro, brillo):
    im = lienzo(44, 26)
    d = ImageDraw.Draw(im)
    d.ellipse([2, 4, 30, 22], fill=base)
    d.ellipse([16, 8, 42, 24], fill=base)
    d.ellipse([6, 2, 24, 14], fill=base)
    d.ellipse([6, 7, 26, 17], fill=claro)
    d.ellipse([20, 11, 37, 20], fill=claro)
    # brillo que se mueve
    x = 8 + i * 8
    d.line([(x, 9), (x + 5, 8)], fill=brillo, width=1)
    d.line([(x + 14, 15), (x + 17, 14)], fill=brillo, width=1)
    return im


def cartel_mojado():
    im = lienzo(18, 24)
    d = ImageDraw.Draw(im)
    d.polygon([(9, 1), (16, 20), (2, 20)], fill=(250, 206, 40, 255))
    d.polygon([(9, 4), (14, 18), (4, 18)], fill=(255, 226, 90, 255))
    d.rectangle([7, 8, 10, 13], fill=K)        # monito resbalando
    d.point((11, 7), fill=K)
    d.line([(6, 15), (11, 15)], fill=K)
    d.rectangle([2, 20, 16, 22], fill=(190, 140, 20, 255))
    return contorno(im)


def cable(i):
    im = lienzo(48, 14)
    d = ImageDraw.Draw(im)
    pts = [(1, 8), (10, 5), (20, 9), (30, 5), (40, 8), (46, 6)]
    d.line(pts, fill=(30, 30, 36, 255), width=3)
    d.line(pts, fill=(70, 70, 80, 255), width=1)
    # punta pelada con cobre
    d.line([(44, 6), (47, 4)], fill=(220, 130, 50, 255), width=2)
    d.line([(44, 7), (47, 9)], fill=(240, 160, 70, 255), width=1)
    if i == 1:
        d.ellipse([40, 1, 47, 10], outline=(120, 220, 255, 255))
    return im


def chispazo(i):
    rng = random.Random(40 + i)
    im = lienzo(36, 36)
    d = ImageDraw.Draw(im)
    c = 18
    for k in range(6 + i):
        a = rng.uniform(0, math.tau)
        x, y = c, c
        pts = [(x, y)]
        for s in range(4):
            r = 4 + s * 3.5
            a2 = a + rng.uniform(-0.6, 0.6)
            pts.append((c + math.cos(a2) * r, c + math.sin(a2) * r))
        d.line(pts, fill=(140, 230, 255, 255), width=2)
        d.line(pts, fill=(255, 255, 255, 255), width=1)
    r = 5 - i
    d.ellipse([c - r, c - r, c + r, c + r], fill=(255, 255, 220, 255))
    return im


def gota():
    im = lienzo(6, 9)
    d = ImageDraw.Draw(im)
    d.polygon([(3, 0), (5, 5), (1, 5)], fill=(120, 190, 255, 255))
    d.ellipse([0, 3, 5, 8], fill=(120, 190, 255, 255))
    d.point((2, 5), fill=(230, 245, 255, 255))
    return im


def salpica(i):
    im = lienzo(20, 12)
    d = ImageDraw.Draw(im)
    r = 3 + i * 3
    d.ellipse([10 - r, 7 - r // 2, 10 + r, 7 + r // 2], outline=(170, 215, 255, 255 - i * 60))
    for k in range(4):
        a = math.pi + k * math.pi / 3
        x = 10 + math.cos(a) * (2 + i * 3); y = 6 - (3 - i) * 1.5 - abs(math.sin(a)) * 2
        d.point((x, y), fill=(200, 230, 255, 255))
    return im


def mancha_techo():
    im = lienzo(24, 14)
    d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 23, 13], fill=(0, 0, 0, 90))
    d.ellipse([5, 3, 18, 10], fill=(0, 0, 0, 120))
    return im


def balde():
    im = lienzo(16, 16)
    d = ImageDraw.Draw(im)
    d.polygon([(2, 3), (14, 3), (12, 15), (4, 15)], fill=(60, 120, 200, 255))
    d.polygon([(3, 3), (6, 3), (5, 15), (4, 15)], fill=(110, 170, 240, 255))
    d.ellipse([2, 1, 14, 6], fill=(30, 70, 130, 255))
    d.ellipse([4, 2, 12, 5], fill=(130, 190, 255, 255))
    d.arc([1, -4, 15, 8], 200, 340, fill=(80, 80, 90, 255))
    return contorno(im)


def hoja(i):
    colores = [((110, 170, 60, 255), (70, 120, 40, 255)), ((210, 150, 50, 255), (160, 100, 30, 255)), ((190, 80, 40, 255), (130, 50, 30, 255))]
    c, s = colores[i]
    im = lienzo(10, 8)
    d = ImageDraw.Draw(im)
    d.ellipse([0, 1, 9, 6], fill=c)
    d.line([(1, 4), (8, 3)], fill=s)
    return im


def rafaga(i):
    im = lienzo(48, 16)
    d = ImageDraw.Draw(im)
    for k, (y, x0, x1) in enumerate([(3, 4, 40), (8, 0, 30), (12, 10, 46)]):
        x0 += i * 3; x1 += i * 3
        d.line([(x0, y), (min(47, x1), y)], fill=(255, 255, 255, 150 - k * 30), width=1)
        d.arc([min(47, x1) - 6, y - 5, min(47, x1), y + 1], 270, 90, fill=(255, 255, 255, 120))
    return im


# ================================================================== accesorios (100 px/unidad, como los personajes)
def acc_gorra():
    im = lienzo(34, 14)
    d = ImageDraw.Draw(im)
    d.pieslice([3, 1, 31, 25], 180, 360, fill=(210, 40, 40, 255))
    d.rectangle([0, 11, 33, 13], fill=(160, 24, 24, 255))     # visera
    d.rectangle([15, 1, 18, 3], fill=(255, 255, 255, 255))
    d.line([(8, 6), (12, 3)], fill=(255, 130, 120, 255))
    return contorno(im)


def acc_auriculares():
    im = lienzo(40, 24)
    d = ImageDraw.Draw(im)
    d.arc([5, 0, 35, 34], 180, 360, fill=(40, 40, 50, 255), width=3)
    for x in (1, 32):
        d.rounded_rectangle([x, 12, x + 7, 23], 2, fill=(60, 200, 230, 255))
        d.rectangle([x + 1, 14, x + 2, 21], fill=(170, 240, 255, 255))
    return contorno(im)


def acc_sombrero():
    im = lienzo(44, 18)
    d = ImageDraw.Draw(im)
    d.ellipse([0, 10, 43, 17], fill=(222, 186, 96, 255))
    d.rounded_rectangle([10, 1, 33, 14], 4, fill=(232, 198, 110, 255))
    d.rectangle([10, 9, 33, 12], fill=(170, 40, 40, 255))
    for x in range(12, 32, 4):
        d.point((x, 4), fill=(190, 150, 70, 255))
    return contorno(im)


def acc_birrete():
    im = lienzo(40, 20)
    d = ImageDraw.Draw(im)
    d.rectangle([9, 9, 30, 17], fill=(30, 30, 40, 255))
    d.polygon([(20, 0), (39, 6), (20, 12), (0, 6)], fill=(40, 40, 54, 255))
    d.polygon([(20, 2), (34, 6), (20, 10), (6, 6)], fill=(60, 60, 78, 255))
    d.line([(20, 6), (34, 10), (34, 16)], fill=(250, 200, 40, 255))
    d.rectangle([33, 15, 35, 19], fill=(250, 200, 40, 255))
    return contorno(im)


def acc_vikingo():
    im = lienzo(46, 22)
    d = ImageDraw.Draw(im)
    d.pieslice([8, 4, 37, 34], 180, 360, fill=(150, 156, 170, 255))
    d.rectangle([8, 17, 37, 20], fill=(120, 80, 40, 255))
    d.line([(22, 4), (22, 18)], fill=(110, 116, 130, 255), width=2)
    # cuernos
    d.polygon([(9, 14), (1, 6), (2, 0), (6, 8), (12, 11)], fill=(240, 232, 210, 255))
    d.polygon([(36, 14), (44, 6), (43, 0), (39, 8), (33, 11)], fill=(240, 232, 210, 255))
    d.line([(14, 8), (18, 6)], fill=(220, 226, 240, 255))
    return contorno(im)


def acc_corona():
    im = lienzo(32, 18)
    d = ImageDraw.Draw(im)
    d.polygon([(1, 17), (1, 5), (8, 11), (16, 1), (24, 11), (31, 5), (31, 17)], fill=(250, 200, 40, 255))
    d.rectangle([1, 13, 31, 17], fill=(220, 160, 20, 255))
    for x, c in ((8, (220, 40, 60, 255)), (16, (60, 160, 240, 255)), (24, (60, 200, 90, 255))):
        d.rectangle([x - 1, 14, x + 1, 16], fill=c)
    d.point((16, 3), fill=(255, 250, 200, 255))
    return contorno(im)


def acc_fiesta():
    im = lienzo(22, 30)
    d = ImageDraw.Draw(im)
    d.polygon([(11, 2), (20, 29), (2, 29)], fill=(150, 80, 220, 255))
    for k, y in enumerate(range(8, 29, 6)):
        w = (y - 2) * 9 // 27
        d.line([(11 - w, y), (11 + w, y)], fill=[(255, 220, 60, 255), (60, 220, 200, 255)][k % 2], width=2)
    d.ellipse([8, 0, 14, 5], fill=(255, 90, 120, 255))
    return contorno(im)


# ================================================================== partículas de martillos
def corazon():
    im = lienzo(9, 8)
    d = ImageDraw.Draw(im)
    d.ellipse([0, 0, 4, 4], fill=(255, 80, 120, 255)); d.ellipse([4, 0, 8, 4], fill=(255, 80, 120, 255))
    d.polygon([(0, 2), (8, 2), (4, 7)], fill=(255, 80, 120, 255))
    d.point((2, 1), fill=(255, 200, 220, 255))
    return im


def estrellita():
    im = lienzo(9, 9)
    d = ImageDraw.Draw(im)
    pts = []
    for k in range(10):
        r = 4.4 if k % 2 == 0 else 1.8
        a = -math.pi / 2 + k * math.pi / 5
        pts.append((4 + math.cos(a) * r, 4.4 + math.sin(a) * r))
    d.polygon(pts, fill=(255, 230, 80, 255))
    d.point((4, 4), fill=(255, 255, 220, 255))
    return im


def llama(i):
    im = lienzo(9, 12)
    d = ImageDraw.Draw(im)
    s = [0, 1, -1][i]
    d.polygon([(4 + s, 0), (8, 7), (6, 11), (2, 11), (0, 7)], fill=(240, 90, 30, 255))
    d.polygon([(4 + s, 4), (6, 8), (5, 11), (3, 11), (2, 8)], fill=(255, 200, 60, 255))
    return im


def copo():
    im = lienzo(9, 9)
    d = ImageDraw.Draw(im)
    for a in range(3):
        ang = a * math.pi / 3
        dx, dy = math.cos(ang) * 4, math.sin(ang) * 4
        d.line([(4 - dx, 4 - dy), (4 + dx, 4 + dy)], fill=(200, 240, 255, 255))
    d.point((4, 4), fill=(255, 255, 255, 255))
    return im


# ================================================================== historia
def degradado(d, w, h, arriba, abajo, y0=0, y1=None):
    y1 = h if y1 is None else y1
    for y in range(y0, y1):
        k = (y - y0) / max(1, y1 - y0 - 1)
        c = tuple(int(arriba[i] + (abajo[i] - arriba[i]) * k) for i in range(3)) + (255,)
        d.line([(0, y), (w, y)], fill=c)


def fondo_aula():
    w, h = 480, 270
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    degradado(d, w, h, (190, 196, 214), (160, 166, 188), 0, 190)       # pared
    d.rectangle([0, 186, w, h], fill=(120, 92, 70, 255))                 # piso de madera
    for y in range(190, h, 12):
        d.line([(0, y), (w, y)], fill=(100, 76, 58, 255))
    for x in range(0, w, 40):
        d.line([(x, 186), (x - 30, h)], fill=(108, 82, 62, 255))
    # pizarra
    d.rectangle([110, 30, 370, 130], fill=(70, 50, 34, 255))
    d.rectangle([116, 36, 364, 124], fill=(40, 78, 62, 255))
    d.rectangle([116, 36, 364, 40], fill=(52, 96, 76, 255))
    # escritura de tiza
    tiza = (230, 236, 226, 255)
    d.text((130, 48), "EXAMEN FINAL", fill=tiza)
    d.text((130, 64), "MANANA 8:00", fill=tiza)
    d.line([(142, 62), (143, 61), (144, 62), (145, 61)], fill=tiza)   # la tilde de la Ñ (la fuente no la trae)
    d.text((130, 86), "E = m c^2   x = -b/2a", fill=(200, 210, 200, 255))
    d.line([(300, 52), (350, 100)], fill=tiza); d.line([(350, 52), (300, 100)], fill=tiza)
    d.rectangle([150, 124, 330, 128], fill=(90, 66, 46, 255))
    # reloj de pared (casi medianoche)
    d.ellipse([400, 30, 440, 70], fill=(240, 240, 236, 255), outline=(40, 40, 50, 255), width=3)
    d.line([(420, 50), (420, 36)], fill=K, width=2); d.line([(420, 50), (418, 37)], fill=K, width=1)
    # ventana nocturna
    d.rectangle([20, 30, 90, 110], fill=(20, 30, 66, 255), outline=(230, 230, 230, 255), width=4)
    d.line([(55, 30), (55, 110)], fill=(230, 230, 230, 255), width=3)
    d.ellipse([28, 40, 44, 56], fill=(250, 246, 210, 255))
    for x, y in ((70, 44), (80, 70), (36, 90), (66, 96)):
        d.point((x, y), fill=(255, 255, 255, 255))
    # pupitres
    for x in (40, 200, 360):
        d.rectangle([x, 196, x + 90, 206], fill=(150, 104, 60, 255))
        d.rectangle([x + 4, 206, x + 8, 240], fill=(70, 70, 80, 255))
        d.rectangle([x + 82, 206, x + 86, 240], fill=(70, 70, 80, 255))
        d.rectangle([x + 20, 188, x + 50, 196], fill=(230, 230, 220, 255))  # libros
        d.rectangle([x + 22, 184, x + 48, 188], fill=(200, 60, 60, 255))
    # lámpara colgante
    d.line([(240, 0), (240, 14)], fill=(50, 50, 60, 255))
    d.polygon([(226, 14), (254, 14), (248, 22), (232, 22)], fill=(250, 220, 120, 255))
    return im


def fondo_cuarto():
    w, h = 480, 270
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    degradado(d, w, h, (250, 226, 190), (236, 200, 160), 0, 200)    # pared cálida de mañana
    d.rectangle([0, 196, w, h], fill=(150, 110, 80, 255))
    for y in range(200, h, 10):
        d.line([(0, y), (w, y)], fill=(132, 96, 70, 255))
    # ventana con sol
    d.rectangle([300, 24, 420, 120], fill=(130, 200, 250, 255), outline=(255, 255, 255, 255), width=5)
    d.ellipse([370, 34, 402, 66], fill=(255, 236, 120, 255))
    d.ellipse([318, 70, 360, 90], fill=(255, 255, 255, 255)); d.ellipse([340, 64, 376, 86], fill=(255, 255, 255, 255))
    d.line([(360, 24), (360, 120)], fill=(255, 255, 255, 255), width=3)
    # rayos de sol en el piso
    d.polygon([(300, 120), (420, 120), (470, 260), (250, 260)], fill=(255, 240, 200, 255))
    # cama
    d.rectangle([30, 160, 250, 220], fill=(90, 60, 40, 255))
    d.rectangle([24, 120, 40, 230], fill=(110, 74, 48, 255))
    d.rectangle([36, 150, 250, 180], fill=(80, 130, 210, 255))       # frazada
    d.rectangle([36, 150, 250, 156], fill=(120, 170, 240, 255))
    d.rounded_rectangle([44, 136, 100, 156], 6, fill=(250, 250, 250, 255))  # almohada
    # mesa de luz con despertador (el despertador es un sprite aparte)
    d.rectangle([262, 170, 300, 224], fill=(160, 110, 70, 255))
    d.rectangle([262, 170, 300, 176], fill=(190, 136, 90, 255))
    # póster de la U y estante
    d.rectangle([130, 40, 210, 100], fill=(30, 50, 110, 255), outline=(250, 200, 40, 255), width=3)
    d.text((142, 62), "UNIFRANZ", fill=(250, 200, 40, 255))
    d.rectangle([20, 60, 110, 66], fill=(130, 90, 60, 255))
    for i, c in enumerate([(200, 60, 60), (60, 140, 200), (240, 190, 60), (90, 170, 90)]):
        d.rectangle([26 + i * 14, 40, 36 + i * 14, 60], fill=c + (255,))
    return im


def fondo_u_dia():
    w, h = 480, 270
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    degradado(d, w, h, (96, 170, 240), (196, 230, 255), 0, 220)
    # sol y nubes
    d.ellipse([400, 20, 440, 60], fill=(255, 236, 130, 255))
    for x, y in ((40, 40), (180, 26), (300, 60)):
        d.ellipse([x, y, x + 50, y + 20], fill=(255, 255, 255, 255)); d.ellipse([x + 20, y - 8, x + 66, y + 18], fill=(255, 255, 255, 255))
    # montañas lejanas
    d.polygon([(0, 190), (70, 130), (140, 180), (210, 120), (300, 190)], fill=(150, 170, 200, 255))
    d.polygon([(200, 130), (210, 120), (222, 132)], fill=(255, 255, 255, 255))
    # edificio de la U
    d.rectangle([230, 70, 460, 222], fill=(220, 214, 200, 255))
    d.rectangle([230, 70, 460, 80], fill=(30, 60, 140, 255))
    d.rectangle([310, 50, 380, 70], fill=(30, 60, 140, 255))
    d.text((318, 55), "UNIFRANZ", fill=(250, 200, 40, 255))
    for fila in range(4):
        for col in range(7):
            x = 240 + col * 31; y = 88 + fila * 30
            d.rectangle([x, y, x + 20, y + 18], fill=(120, 180, 230, 255))
            d.line([(x + 3, y + 3), (x + 8, y + 3)], fill=(220, 240, 255, 255))
    # grieta (¿o no fue un sueño?)
    d.line([(450, 96), (444, 110), (452, 120), (446, 136)], fill=(60, 50, 50, 255), width=2)
    d.rectangle([330, 186, 360, 222], fill=(90, 60, 40, 255))       # puerta
    # árboles y vereda
    d.rectangle([0, 220, w, h], fill=(170, 170, 176, 255))
    d.rectangle([0, 216, w, 222], fill=(120, 190, 90, 255))
    for x in (40, 150):
        d.rectangle([x + 16, 170, x + 24, 220], fill=(110, 76, 44, 255))
        d.ellipse([x, 120, x + 40, 180], fill=(80, 160, 70, 255)); d.ellipse([x + 8, 112, x + 36, 150], fill=(100, 180, 86, 255))
    # arbusto con el casco del demoledor escondido
    d.ellipse([196, 196, 238, 224], fill=(70, 150, 64, 255))
    d.pieslice([206, 190, 226, 210], 180, 360, fill=(250, 196, 30, 255))
    return im


def despertador(i):
    im = lienzo(28, 30)
    d = ImageDraw.Draw(im)
    s = 2 if i == 1 else 0
    d.ellipse([3 - s, 0, 11 - s, 8], fill=(240, 200, 40, 255)); d.ellipse([17 + s, 0, 25 + s, 8], fill=(240, 200, 40, 255))
    d.ellipse([3, 4, 25, 26], fill=(220, 50, 50, 255))
    d.ellipse([6, 7, 22, 23], fill=(250, 250, 244, 255))
    d.line([(14, 15), (14, 9)], fill=K, width=1); d.line([(14, 15), (18, 15)], fill=K, width=1)
    d.line([(6, 25), (3, 29)], fill=K, width=2); d.line([(22, 25), (25, 29)], fill=K, width=2)
    return contorno(im)


def cono():
    im = lienzo(30, 26)
    d = ImageDraw.Draw(im)
    d.polygon([(15, 0), (24, 22), (6, 22)], fill=(250, 120, 30, 255))
    d.polygon([(12, 8), (18, 8), (20, 13), (10, 13)], fill=(250, 250, 250, 255))
    d.rectangle([1, 21, 28, 25], fill=(220, 90, 20, 255))
    return contorno(im)


def nube_sueno():
    im = lienzo(120, 80)
    d = ImageDraw.Draw(im)
    for x, y, r in ((30, 40, 26), (60, 30, 30), (92, 40, 25), (48, 55, 22), (78, 56, 22)):
        d.ellipse([x - r, y - r, x + r, y + r], fill=(250, 246, 255, 255))
    for x, y, r in ((40, 36, 18), (66, 28, 20), (88, 42, 16)):
        d.ellipse([x - r, y - r, x + r, y + r], fill=(236, 230, 252, 255))
    return contorno(im, (120, 110, 160, 255))


# ================================================================== icono y logo
def icono_app():
    tam = 128
    im = Image.new("RGBA", (tam, tam), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for r in range(tam // 2, 0, -1):
        k = r / (tam / 2)
        c = (int(255 - 40 * k), int(200 - 110 * k), int(50 - 20 * k), 255)
        d.ellipse([tam / 2 - r * 1.42, tam / 2 - r * 1.42, tam / 2 + r * 1.42, tam / 2 + r * 1.42], fill=c)
    d.rounded_rectangle([0, 0, tam - 1, tam - 1], 18, outline=(40, 20, 10, 255), width=4)
    dem = Image.open(os.path.join(RAIZ, "Assets", "Resources", "Extras", "Menu", "demoledor_golpe_2.png")).convert("RGBA")
    dem = dem.crop(dem.getbbox())
    esc = min(104 / dem.width, 104 / dem.height)
    dem = dem.resize((max(1, int(dem.width * esc)), max(1, int(dem.height * esc))), Image.NEAREST)
    im.alpha_composite(dem, ((tam - dem.width) // 2, tam - dem.height - 8))
    # grietas
    d.line([(10, 30), (22, 40), (16, 52)], fill=(60, 30, 10, 255), width=2)
    d.line([(116, 22), (106, 34), (114, 44)], fill=(60, 30, 10, 255), width=2)
    # esquinas redondeadas transparentes
    mascara = Image.new("L", (tam, tam), 0)
    ImageDraw.Draw(mascara).rounded_rectangle([0, 0, tam - 1, tam - 1], 18, fill=255)
    im.putalpha(Image.composite(im.getchannel("A"), mascara, mascara))
    return im.resize((512, 512), Image.NEAREST)


def logo_inicio():
    logo = Image.open(os.path.join(RAIZ, "Assets", "Resources", "Extras", "Tema", "logo_demoledor.png")).convert("RGBA")
    dem = Image.open(os.path.join(RAIZ, "Assets", "Resources", "Extras", "Menu", "demoledor_caminar_0.png")).convert("RGBA")
    dem = dem.crop(dem.getbbox())
    w = max(logo.width, dem.width) + 20
    h = dem.height + logo.height + 16
    im = lienzo(w, h)
    im.alpha_composite(dem, ((w - dem.width) // 2, 0))
    im.alpha_composite(logo, ((w - logo.width) // 2, dem.height + 10))
    return im.resize((w * 3, h * 3), Image.NEAREST)


def escribir_fuera(ruta_rel, imagen):
    import unity_assets as ua
    ruta = os.path.join(RAIZ, ruta_rel + ".png")
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    imagen.save(ruta)
    meta = ua.PNG_META.format(guid=ex.guid(ruta), ppu=100, alignment=0, pivot_x=0.5, pivot_y=0.5, filter=0)
    with open(ruta + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)
    carpeta = os.path.dirname(ruta)
    if not os.path.exists(carpeta + ".meta"):
        with open(carpeta + ".meta", "w", encoding="utf-8", newline="\n") as f:
            f.write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                    % ex.guid(carpeta))


def main():
    for i in range(3):
        ex.escribir(f"Peligros/charco_{i}", charco(i, (70, 130, 210, 170), (120, 180, 240, 170), (240, 250, 255, 255)), 32)
        ex.escribir(f"Peligros/aceite_{i}", charco(i, (200, 150, 40, 180), (240, 200, 80, 180), (255, 250, 210, 255)), 32)
        ex.escribir(f"Peligros/chispazo_{i}", chispazo(i), 32)
        ex.escribir(f"Peligros/salpica_{i}", salpica(i), 32)
        ex.escribir(f"Peligros/hoja_{i}", hoja(i), 32)
        ex.escribir(f"Martillos/llama_{i}", llama(i), 32)
    for i in range(2):
        ex.escribir(f"Peligros/cable_{i}", cable(i), 32)
        ex.escribir(f"Peligros/rafaga_{i}", rafaga(i), 32)
        ex.escribir(f"Historia/despertador_{i}", despertador(i), 100)
    ex.escribir("Peligros/cartel_mojado", cartel_mojado(), 32, abajo=True)
    ex.escribir("Peligros/gota", gota(), 32)
    ex.escribir("Peligros/mancha_techo", mancha_techo(), 32)
    ex.escribir("Peligros/balde", balde(), 32, abajo=True)

    for nombre, f in (("gorra", acc_gorra), ("auriculares", acc_auriculares), ("sombrero", acc_sombrero), ("birrete", acc_birrete),
                      ("vikingo", acc_vikingo), ("corona", acc_corona), ("fiesta", acc_fiesta)):
        ex.escribir("Accesorios/" + nombre, f(), 100, abajo=True)

    ex.escribir("Martillos/corazon", corazon(), 32)
    ex.escribir("Martillos/estrellita", estrellita(), 32)
    ex.escribir("Martillos/copo", copo(), 32)

    ex.escribir("Historia/fondo_aula", fondo_aula(), 100)
    ex.escribir("Historia/fondo_cuarto", fondo_cuarto(), 100)
    ex.escribir("Historia/fondo_u_dia", fondo_u_dia(), 100)
    ex.escribir("Historia/cono", cono(), 100, abajo=True)
    ex.escribir("Historia/nube_sueno", nube_sueno(), 100)

    # Icono y logo fuera de Resources (no hace falta cargarlos en el juego: los usa ConstructorBuilds).
    escribir_fuera("Assets/Icono/icono_app", icono_app())
    escribir_fuera("Assets/Icono/logo_inicio", logo_inicio())
    print("Arte v2 listo")


if __name__ == "__main__":
    main()
