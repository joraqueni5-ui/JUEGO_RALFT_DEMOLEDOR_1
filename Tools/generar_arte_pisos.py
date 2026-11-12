"""
Genera el arte PROVISIONAL de los pisos 1 (Gastronomía) y 2 (Informática).

Salida: Assets/Sprites/Pisos/pisoN_*.png
  - pisoN_objeto{tipo}_{estado}.png   75 x 64 px  (4 tipos x 3 estados: intacto, roto, destruido)
  - pisoN_plataforma_{estado}.png     40 x 12 px  (intacta, agrietada, destruida)
  - pisoN_fondo.png                   704 x 396 px

Las medidas salen de los sprites actuales (ventanas y repisas) y de la cámara del juego
(tamaño 3.7 a 50 px por unidad). Cuando haya arte definitivo, basta con reemplazar los PNG
con las mismas medidas. Este script solo se necesita para regenerarlo (requiere Pillow).

Uso:  python Tools/generar_arte_pisos.py
"""
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFont

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SALIDA = os.path.join(RAIZ, "Assets", "Sprites", "Pisos")

OUT = (26, 28, 38, 255)
CLARO = (255, 255, 255, 255)


def C(r, g, b, a=255):
    return (r, g, b, a)


def oscurecer(c, k):
    return (int(c[0] * k), int(c[1] * k), int(c[2] * k), c[3])


class Lienzo:
    def __init__(self, w, h, fondo=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w, h), fondo)
        self.d = ImageDraw.Draw(self.im)

    def px(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.im.putpixel((x, y), c)

    def rect(self, x0, y0, x1, y1, c):
        self.d.rectangle([x0, y0, x1, y1], fill=c)

    def caja(self, x0, y0, x1, y1, relleno, borde=OUT):
        self.rect(x0, y0, x1, y1, borde)
        if x1 - x0 >= 2 and y1 - y0 >= 2:
            self.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, relleno)

    def linea(self, x0, y0, x1, y1, c):
        self.d.line([x0, y0, x1, y1], fill=c)

    def circ(self, cx, cy, r, c):
        self.d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c)

    def poligono(self, pts, c):
        self.d.polygon(pts, fill=c)

    def texto(self, x, y, s, c, tam=10):
        """Texto con bordes duros (sin suavizado), para que parezca pixel art."""
        try:
            fuente = ImageFont.load_default(size=tam)
        except TypeError:
            fuente = ImageFont.load_default()
        mascara = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(mascara).text((x, y), s, font=fuente, fill=255)
        mp = mascara.load()
        for yy in range(self.h):
            for xx in range(self.w):
                if mp[xx, yy] > 110:
                    self.im.putpixel((xx, yy), c)

    def guardar(self, ruta):
        os.makedirs(os.path.dirname(ruta), exist_ok=True)
        self.im.save(ruta)


# ============================================================ estados (roto / destruido)
def _opacos(im):
    px = im.load()
    return [(x, y) for y in range(im.height) for x in range(im.width) if px[x, y][3] > 0]


def roto(src, semilla, ox0=14, ox1=58):
    """Objeto dañado: más oscuro, con grietas, un trozo desencajado y huecos en el borde."""
    rng = random.Random(semilla)
    im = src.copy()
    px = im.load()
    for (x, y) in _opacos(im):
        px[x, y] = oscurecer(px[x, y], 0.86)

    # Un tramo de arriba queda corrido un píxel: se ve "descolocado".
    corte = rng.randint(24, 34)
    original = im.copy()
    po = original.load()
    for y in range(0, corte):
        for x in range(im.width):
            px[x, y] = (0, 0, 0, 0)
    for y in range(0, corte):
        for x in range(im.width):
            sx = x - 1
            if 0 <= sx < im.width and po[sx, y][3] > 0:
                px[x, y] = po[sx, y]

    # Grietas que bajan en zigzag.
    for _ in range(3):
        x = rng.randint(ox0 + 6, ox1 - 6)
        y = rng.randint(14, 24)
        for _ in range(rng.randint(14, 24)):
            if 0 <= x < im.width and 0 <= y < im.height and px[x, y][3] > 0:
                px[x, y] = OUT
                if x + 1 < im.width and px[x + 1, y][3] > 0 and rng.random() < 0.5:
                    px[x + 1, y] = (230, 236, 244, 255)
            y += 1
            x += rng.choice((-1, 0, 0, 1))

    # Huecos en el borde y manchas de hollín.
    opacos = _opacos(im)
    bordes = [p for p in opacos if any(
        not (0 <= p[0] + dx < im.width and 0 <= p[1] + dy < im.height) or px[p[0] + dx, p[1] + dy][3] == 0
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for _ in range(3):
        if not bordes:
            break
        bx, by = rng.choice(bordes)
        for yy in range(by - 1, by + 2):
            for xx in range(bx - 1, bx + 2):
                if 0 <= xx < im.width and 0 <= yy < im.height and rng.random() < 0.8:
                    px[xx, yy] = (0, 0, 0, 0)
    for _ in range(5):
        if not opacos:
            break
        sx, sy = rng.choice(opacos)
        if px[sx, sy][3] > 0:
            px[sx, sy] = (40, 36, 40, 255)

    # Un poco de humo sobre el objeto.
    ys = [y for (_, y) in opacos]
    tope = min(ys) if ys else 20
    for _ in range(4):
        sx = rng.randint(ox0 + 8, ox1 - 8)
        sy = tope - rng.randint(1, 4)
        if 0 <= sy < im.height:
            px[sx, sy] = (170, 170, 180, 200)
    return im


def destruido(src, semilla, suelo=53, ox0=14, ox1=58, chispa=(255, 190, 70, 255)):
    """Escombros: un montón de pedazos con los colores del objeto, sobre la repisa."""
    rng = random.Random(semilla)
    muestras = [src.getpixel(p) for p in _opacos(src) if src.getpixel(p) != OUT]
    if not muestras:
        muestras = [(120, 120, 130, 255)]
    im = Image.new("RGBA", src.size, (0, 0, 0, 0))
    px = im.load()
    centro = (ox0 + ox1) / 2.0
    ancho = (ox1 - ox0) / 2.0
    for x in range(ox0 + 1, ox1):
        t = (x - centro) / ancho
        h = int(11 * (1 - t * t) + rng.uniform(-2.0, 2.0))
        h = max(2, h)
        for y in range(suelo - h + 1, suelo + 1):
            c = rng.choice(muestras)
            px[x, y] = oscurecer(c, rng.uniform(0.55, 0.85))
    # Trozos sueltos más grandes y claros.
    for _ in range(8):
        bx = rng.randint(ox0 + 3, ox1 - 6)
        by = rng.randint(suelo - 16, suelo - 4)
        c = rng.choice(muestras)
        w, h = rng.choice(((3, 2), (4, 3), (3, 3), (2, 2)))
        for yy in range(by, by + h):
            for xx in range(bx, bx + w):
                if 0 <= xx < im.width and 0 <= yy <= suelo:
                    px[xx, yy] = oscurecer(c, 0.9)
    # Contorno oscuro alrededor de todo lo que quedó.
    base = im.copy()
    pb = base.load()
    for y in range(im.height):
        for x in range(im.width):
            if pb[x, y][3] > 0:
                for dx, dy in ((1, 0), (-1, 0), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < im.width and 0 <= ny < im.height and pb[nx, ny][3] == 0:
                        px[nx, ny] = OUT
    # Chispas y humo.
    for _ in range(7):
        sx = rng.randint(ox0 + 4, ox1 - 4)
        sy = rng.randint(suelo - 22, suelo - 10)
        if px[sx, sy][3] == 0:
            px[sx, sy] = chispa if rng.random() < 0.6 else (190, 190, 200, 210)
    return im


# ============================================================ objetos del piso 1: Gastronomía
def horno():
    l = Lienzo(75, 64)
    acero, claro, osc = C(172, 180, 194), C(222, 228, 238), C(112, 120, 136)
    l.caja(14, 15, 58, 53, acero)
    l.rect(15, 16, 57, 23, claro)
    l.rect(15, 24, 57, 24, osc)
    for kx in (19, 25, 31):
        l.caja(kx, 18, kx + 3, 21, C(70, 76, 92))
        l.px(kx + 1, 19, C(220, 220, 230))
    l.caja(40, 17, 54, 22, C(26, 40, 34))
    l.rect(42, 19, 47, 20, C(90, 230, 120))
    l.caja(17, 27, 55, 51, osc)
    l.caja(20, 29, 52, 31, claro)
    l.caja(21, 34, 51, 48, C(34, 26, 26))
    l.rect(22, 44, 50, 47, C(210, 90, 30))
    l.rect(24, 45, 48, 47, C(255, 150, 40))
    l.rect(29, 46, 43, 47, C(255, 214, 100))
    l.rect(22, 40, 50, 40, C(96, 96, 108))
    l.rect(23, 36, 27, 37, C(150, 172, 196))
    l.rect(16, 52, 20, 53, OUT)
    l.rect(52, 52, 56, 53, OUT)
    return l.im


def refrigerador():
    l = Lienzo(75, 64)
    blanco, sombra = C(230, 241, 248), C(176, 196, 212)
    l.caja(22, 14, 50, 53, blanco)
    l.rect(46, 15, 49, 52, sombra)
    l.rect(24, 15, 44, 15, CLARO)
    l.rect(23, 30, 49, 30, OUT)
    l.rect(23, 31, 49, 31, C(120, 140, 160))
    l.caja(43, 19, 44, 27, C(110, 120, 138))
    l.caja(43, 34, 44, 47, C(110, 120, 138))
    l.rect(26, 20, 30, 23, C(230, 80, 80))     # imanes
    l.rect(32, 24, 35, 26, C(80, 160, 230))
    l.rect(27, 36, 37, 44, C(250, 244, 200))   # nota pegada
    l.rect(29, 38, 35, 38, C(120, 110, 90))
    l.rect(29, 41, 33, 41, C(120, 110, 90))
    l.rect(24, 52, 28, 53, OUT)
    l.rect(44, 52, 48, 53, OUT)
    return l.im


def estufa():
    l = Lienzo(75, 64)
    rojo, claro, osc = C(206, 74, 58), C(238, 126, 100), C(150, 46, 40)
    plata = C(198, 204, 216)
    l.caja(14, 28, 58, 53, rojo)
    l.rect(15, 29, 57, 32, C(46, 46, 56))
    for ax in (21, 37, 49):
        l.rect(ax, 29, ax + 7, 29, C(130, 130, 142))
    l.rect(15, 33, 57, 33, claro)
    for kx in (18, 26, 34, 42, 50):
        l.caja(kx, 35, kx + 3, 38, C(236, 232, 224))
    l.caja(17, 41, 55, 51, osc)
    l.caja(22, 43, 50, 49, C(34, 26, 26))
    l.rect(24, 47, 48, 48, C(240, 130, 40))
    l.rect(24, 40, 48, 40, C(236, 236, 242))
    # olla con tapa
    l.caja(18, 19, 34, 28, plata)
    l.caja(19, 15, 33, 19, C(216, 222, 232))
    l.rect(25, 13, 27, 14, OUT)
    l.rect(15, 21, 17, 22, OUT)
    l.rect(35, 21, 37, 22, OUT)
    l.rect(21, 21, 23, 26, C(240, 244, 250))
    # sartén
    l.caja(41, 25, 55, 28, C(70, 70, 82))
    l.rect(56, 26, 58, 27, OUT)
    # vapor
    for (sx, sy) in ((24, 11), (28, 12), (26, 10)):
        l.px(sx, sy, C(235, 235, 245, 200))
    return l.im


def mesa():
    l = Lienzo(75, 64)
    acero, claro, osc = C(196, 204, 216), C(238, 242, 248), C(130, 138, 154)
    l.caja(14, 38, 58, 42, acero)
    l.rect(15, 39, 57, 39, claro)
    l.rect(17, 43, 55, 44, osc)
    l.caja(16, 43, 19, 53, C(150, 158, 172))
    l.caja(53, 43, 56, 53, C(150, 158, 172))
    l.caja(20, 49, 52, 51, C(120, 128, 144))
    # tabla con cuchillo
    l.caja(18, 35, 36, 37, C(196, 146, 84))
    l.rect(22, 33, 31, 34, C(210, 214, 224))
    l.rect(32, 33, 35, 34, C(110, 70, 40))
    # tazón con ingredientes
    l.caja(40, 29, 54, 37, C(76, 124, 204))
    l.rect(41, 30, 53, 31, C(130, 176, 244))
    l.rect(43, 27, 51, 29, C(250, 210, 90))
    l.rect(45, 25, 49, 27, C(240, 90, 70))
    # botella de aceite
    l.caja(37, 26, 39, 37, C(70, 150, 84))
    l.rect(37, 23, 39, 25, OUT)
    return l.im


# ============================================================ objetos del piso 2: Informática
def monitor():
    l = Lienzo(75, 64)
    l.caja(16, 14, 56, 46, C(98, 108, 132))
    l.rect(19, 17, 53, 42, C(24, 64, 130))
    rng = random.Random(7)
    verde, blanco, naranja = C(120, 255, 170), C(220, 232, 255), C(255, 190, 90)
    y = 19
    while y < 41:
        x = 21 + rng.choice((0, 0, 3, 6))
        largo = rng.randint(8, 24)
        l.rect(x, y, min(x + largo, 51), y, rng.choice((verde, blanco, naranja, verde)))
        y += 3
    l.rect(21, 40, 23, 41, C(255, 255, 255))      # cursor
    l.rect(19, 17, 26, 18, C(70, 120, 200))       # brillo
    l.rect(52, 44, 53, 44, C(90, 230, 120))
    l.caja(33, 47, 39, 50, C(70, 76, 92))
    l.caja(26, 51, 46, 53, C(54, 60, 76))
    return l.im


def servidor():
    l = Lienzo(75, 64)
    l.caja(22, 14, 50, 53, C(88, 100, 128))
    rng = random.Random(11)
    for i in range(5):
        y0 = 16 + i * 7
        l.caja(24, y0, 48, y0 + 5, C(46, 52, 70))
        for vx in range(26, 38, 2):
            l.px(vx, y0 + 2, C(30, 34, 46))
            l.px(vx, y0 + 3, C(30, 34, 46))
        l.rect(40, y0 + 2, 41, y0 + 3, rng.choice((C(90, 240, 130), C(90, 240, 130), C(255, 190, 70))))
        l.rect(44, y0 + 2, 45, y0 + 3, rng.choice((C(90, 200, 255), C(90, 240, 130), C(255, 90, 80))))
    l.rect(24, 52, 48, 52, C(20, 22, 32))
    return l.im


def computadora():
    l = Lienzo(75, 64)
    # torre
    l.caja(15, 22, 33, 53, C(200, 200, 188))
    l.rect(16, 23, 18, 52, C(226, 226, 214))
    l.caja(19, 26, 31, 28, C(120, 120, 112))
    l.caja(19, 31, 31, 33, C(120, 120, 112))
    l.circ(25, 40, 2, C(64, 210, 96))
    l.circ(25, 40, 1, C(190, 255, 200))
    for vy in (45, 47, 49):
        l.rect(20, vy, 30, vy, C(150, 150, 142))
    # laptop
    l.caja(38, 33, 57, 47, C(54, 60, 76))
    l.rect(40, 35, 55, 45, C(60, 130, 220))
    l.rect(42, 37, 50, 38, C(230, 240, 255))
    l.rect(42, 40, 53, 40, C(160, 210, 255))
    l.rect(42, 42, 48, 42, C(160, 210, 255))
    l.caja(36, 48, 59, 53, C(120, 126, 142))
    for kx in range(39, 57, 3):
        l.rect(kx, 50, kx + 1, 50, C(200, 206, 220))
    return l.im


def impresora():
    l = Lienzo(75, 64)
    l.caja(24, 24, 48, 36, C(246, 246, 250))        # papel arriba
    l.rect(27, 28, 44, 28, C(150, 156, 170))
    l.rect(27, 31, 40, 31, C(150, 156, 170))
    l.caja(14, 36, 58, 53, C(206, 210, 220))
    l.rect(15, 37, 57, 37, C(240, 242, 248))
    l.caja(16, 39, 32, 45, C(60, 66, 84))
    l.rect(18, 41, 24, 42, C(90, 230, 130))
    l.rect(27, 41, 30, 43, C(240, 110, 90))
    for cx, col in ((40, C(60, 200, 240)), (44, C(240, 70, 160)), (48, C(250, 220, 70)), (52, C(60, 60, 70))):
        l.rect(cx, 40, cx + 2, 43, col)
    l.rect(18, 47, 54, 49, C(36, 38, 48))
    l.caja(26, 49, 46, 53, C(250, 250, 252))
    l.rect(29, 51, 43, 51, C(170, 176, 190))
    return l.im


# ============================================================ plataformas (40 x 12, pivote arriba a la izquierda)
def plataforma(estilo):
    l = Lienzo(40, 12)
    if estilo == "gastro":
        alta, medio, baja, sombra = C(240, 244, 250), C(204, 212, 224), C(160, 170, 188), C(112, 120, 138)
        pata = C(96, 104, 120)
    else:
        alta, medio, baja, sombra = C(96, 110, 146), C(70, 82, 112), C(50, 58, 82), C(34, 40, 60)
        pata = C(30, 34, 48)
    l.rect(0, 0, 39, 1, alta)
    l.rect(0, 2, 39, 2, medio)
    l.rect(0, 3, 39, 6, baja)
    l.rect(0, 7, 39, 7, sombra)
    if estilo == "gastro":
        for rx in (3, 12, 21, 30, 36):
            l.px(rx, 4, sombra)
    else:
        l.rect(2, 4, 37, 4, C(70, 230, 255))        # tira de luz LED
        l.rect(2, 5, 37, 5, C(30, 120, 160))
    l.caja(3, 8, 6, 11, pata)
    l.caja(33, 8, 36, 11, pata)
    return l.im


def plataforma_agrietada(base, semilla):
    rng = random.Random(semilla)
    im = base.copy()
    px = im.load()
    for x0 in (9, 24):
        x, y = x0 + rng.randint(-1, 1), 0
        while y < 8:
            if px[x, y][3] > 0:
                px[x, y] = OUT
            y += 1
            x = max(0, min(39, x + rng.choice((-1, 0, 1))))
    for cx in (15, 30):
        px[cx, 0] = (0, 0, 0, 0)
        px[cx + 1, 1] = (0, 0, 0, 0)
    return im


def plataforma_destruida(base, semilla):
    rng = random.Random(semilla)
    im = Image.new("RGBA", base.size, (0, 0, 0, 0))
    px, pb = im.load(), base.load()
    for y in range(12):
        for x in range(40):
            if x <= 6 + (y % 3) or x >= 33 - (y % 2):
                px[x, y] = pb[x, y]
    for x in range(7, 33):
        if rng.random() < 0.45:
            top = rng.randint(5, 8)
            for y in range(top, top + rng.randint(1, 3)):
                if y < 12:
                    px[x, y] = pb[x, y] if pb[x, y][3] > 0 else OUT
    for x in range(1, 12):
        if rng.random() < 0.5:
            px[rng.randint(0, 39), 0] = (0, 0, 0, 0)
    return im


# ============================================================ fondos (704 x 396)
ANCHO, ALTO = 704, 396
Y_VIGA = 124           # donde se para el demoledor
Y_SUELO = 344          # inicio del piso


def mosaico(l, x0, y0, x1, y1, tam, c1, c2, junta, semilla, variacion=4):
    rng = random.Random(semilla)
    for ty in range(y0, y1, tam):
        for tx in range(x0, x1, tam):
            v = rng.randint(-variacion, variacion)
            c = c1 if ((tx // tam + ty // tam) % 2 == 0 or c2 is None) else c2
            c = (max(0, min(255, c[0] + v)), max(0, min(255, c[1] + v)), max(0, min(255, c[2] + v)), 255)
            l.rect(tx, ty, min(tx + tam - 1, x1 - 1), min(ty + tam - 1, y1 - 1), c)
            if junta is not None:
                l.rect(tx, ty, min(tx + tam - 1, x1 - 1), ty, junta)
                l.rect(tx, ty, tx, min(ty + tam - 1, y1 - 1), junta)


def sombrear(l, x0, y0, x1, y1, alfa):
    capa = Image.new("RGBA", (x1 - x0 + 1, y1 - y0 + 1), (8, 6, 14, alfa))
    l.im.alpha_composite(capa, (x0, y0))


def viga(l, tope, cara, borde, luz=None):
    l.rect(0, Y_VIGA + 1, ANCHO - 1, Y_VIGA + 18, cara)
    l.rect(0, Y_VIGA + 1, ANCHO - 1, Y_VIGA + 3, tope)
    l.rect(0, Y_VIGA + 18, ANCHO - 1, Y_VIGA + 19, borde)
    l.rect(0, Y_VIGA + 4, ANCHO - 1, Y_VIGA + 4, oscurecer(cara, 0.7))
    for rx in range(10, ANCHO, 34):
        l.rect(rx, Y_VIGA + 9, rx + 1, Y_VIGA + 10, oscurecer(cara, 0.6))
    if luz:
        l.rect(0, Y_VIGA + 1, ANCHO - 1, Y_VIGA + 1, luz)
        sombrear_luz = Image.new("RGBA", (ANCHO, 6), (luz[0], luz[1], luz[2], 40))
        l.im.alpha_composite(sombrear_luz, (0, Y_VIGA + 20))


def fondo_gastronomia():
    l = Lienzo(ANCHO, ALTO)
    mosaico(l, 0, 0, ANCHO, Y_SUELO, 16, C(236, 230, 212), None, C(206, 198, 178), 1, 3)
    # zócalo de azulejo terracota bajo el nivel de las repisas
    mosaico(l, 0, 313, ANCHO, Y_SUELO, 16, C(190, 96, 54), C(176, 84, 46), C(120, 56, 32), 2, 3)
    l.rect(0, 311, ANCHO - 1, 312, C(228, 150, 96))
    # piso a cuadros
    mosaico(l, 0, Y_SUELO, ANCHO, ALTO, 22, C(238, 232, 218), C(64, 66, 76), None, 3, 0)
    l.rect(0, Y_SUELO, ANCHO - 1, Y_SUELO + 1, C(60, 40, 30))

    # --- zona alta: techo oscuro con ducto, luces y letrero
    sombrear(l, 0, 0, ANCHO - 1, Y_VIGA, 120)
    l.rect(0, 6, ANCHO - 1, 26, C(150, 158, 172))
    l.rect(0, 6, ANCHO - 1, 8, C(204, 212, 224))
    l.rect(0, 24, ANCHO - 1, 26, C(96, 104, 120))
    for rx in range(14, ANCHO, 52):
        l.rect(rx, 6, rx + 1, 26, C(112, 120, 136))
    for lx in (96, 280, 464, 628):
        l.rect(lx - 6, 32, lx + 56, 38, C(250, 250, 244))
        l.rect(lx - 6, 39, lx + 56, 40, C(200, 200, 190))
        sombra = Image.new("RGBA", (70, 26), (255, 246, 200, 34))
        l.im.alpha_composite(sombra, (lx - 12, 41))
    # letrero
    l.caja(18, 52, 176, 94, C(34, 30, 38))
    l.rect(22, 56, 172, 58, C(236, 140, 50))
    l.rect(22, 88, 172, 90, C(236, 140, 50))
    l.texto(30, 63, "GASTRONOMIA", C(255, 214, 120), 14)
    # cartel de cocina caliente a la derecha
    l.caja(520, 52, 690, 94, C(34, 30, 38))
    l.texto(534, 63, "COCINA  CALIENTE", C(255, 214, 120), 11)

    viga(l, C(214, 164, 108), C(150, 98, 56), C(70, 44, 26))

    # --- izquierda: campana extractora y estante de ollas
    l.poligono([(14, 190), (146, 190), (126, 158), (34, 158)], C(172, 180, 194))
    l.poligono([(20, 190), (140, 190), (122, 162), (38, 162)], C(196, 204, 216))
    l.rect(30, 150, 130, 158, C(150, 158, 172))
    l.rect(14, 190, 146, 193, OUT)
    for gx in range(24, 138, 6):
        l.rect(gx, 184, gx + 2, 188, C(104, 112, 128))
    l.rect(60, 144, 100, 150, C(130, 138, 152))
    # estante
    for sy in (238, 268, 298):
        l.rect(16, sy, 156, sy + 3, C(120, 128, 144))
        l.rect(16, sy + 3, 156, sy + 3, OUT)
    for px_ in (18, 154):
        l.rect(px_, 204, px_ + 2, 300, C(104, 112, 128))
    for i in range(5):                      # platos apilados
        l.caja(26 + i * 0, 228 - i * 2, 56, 236 - i * 2 + 1, C(246, 246, 250))
    l.caja(64, 220, 96, 237, C(184, 190, 204))   # olla
    l.rect(60, 224, 64, 226, OUT)
    l.rect(96, 224, 100, 226, OUT)
    l.caja(104, 222, 116, 237, C(206, 76, 58))
    l.caja(122, 226, 146, 237, C(76, 124, 204))
    for i, col in enumerate((C(240, 200, 90), C(190, 60, 50), C(80, 160, 90), C(240, 200, 90))):
        l.caja(26 + i * 12, 252, 34 + i * 12, 267, col)        # frascos
    l.caja(86, 250, 120, 267, C(224, 196, 150))                # costal de harina
    l.rect(92, 256, 112, 258, C(150, 120, 80))
    l.caja(126, 254, 150, 267, C(196, 204, 216))
    for i in range(3):
        l.caja(26 + i * 20, 282 - i, 40 + i * 20, 297, C(236, 190, 120))

    # --- derecha: riel con utensilios, pizarra de menú, reloj
    l.rect(506, 150, 640, 152, C(120, 128, 144))
    for hx, tipo in ((516, 0), (534, 1), (552, 2), (572, 0), (592, 1), (612, 2), (630, 0)):
        l.rect(hx, 152, hx, 158, OUT)
        if tipo == 0:      # cucharón
            l.rect(hx - 1, 158, hx + 1, 176, C(184, 190, 204))
            l.circ(hx, 180, 4, C(150, 158, 172))
        elif tipo == 1:    # batidor
            l.rect(hx, 158, hx, 168, C(184, 190, 204))
            l.d.ellipse([hx - 4, 168, hx + 4, 184], outline=C(184, 190, 204))
        else:              # espátula
            l.rect(hx, 158, hx, 172, C(110, 70, 40))
            l.rect(hx - 3, 172, hx + 3, 182, C(206, 76, 58))
    l.circ(676, 176, 14, OUT)
    l.circ(676, 176, 12, C(246, 244, 236))
    l.rect(676, 168, 676, 176, OUT)
    l.rect(676, 176, 682, 176, OUT)
    l.caja(512, 200, 680, 266, C(120, 76, 42))
    l.rect(516, 204, 676, 262, C(40, 66, 54))
    l.texto(560, 208, "MENU", C(240, 240, 230), 13)
    for i, largo in enumerate((90, 70, 100, 60)):
        l.rect(526, 232 + i * 8, 526 + largo, 233 + i * 8, C(220, 224, 214))
        l.rect(640, 232 + i * 8, 664, 233 + i * 8, C(240, 200, 90))
    l.rect(506, 292, 690, 295, C(120, 128, 144))
    l.rect(506, 295, 690, 295, OUT)
    for i, col in enumerate((C(220, 80, 60), C(90, 170, 90), C(240, 200, 90), C(120, 150, 220), C(220, 80, 60), C(90, 170, 90))):
        l.caja(514 + i * 28, 276, 526 + i * 28, 291, col)
    return l.im


def fondo_informatica():
    l = Lienzo(ANCHO, ALTO)
    mosaico(l, 0, 0, ANCHO, Y_SUELO, 44, C(50, 62, 94), C(44, 55, 86), C(66, 80, 118), 4, 2)
    # zócalo oscuro con franja de peligro
    l.rect(0, 313, ANCHO - 1, Y_SUELO, C(22, 28, 46))
    l.rect(0, 311, ANCHO - 1, 312, C(60, 220, 250))
    for hx in range(0, ANCHO, 16):
        l.poligono([(hx, 336), (hx + 8, 336), (hx + 16, 343), (hx + 8, 343)], C(240, 200, 40))
    l.rect(0, 336, ANCHO - 1, 336, OUT)
    l.rect(0, 343, ANCHO - 1, 343, OUT)
    # piso técnico
    mosaico(l, 0, Y_SUELO, ANCHO, ALTO, 22, C(44, 52, 78), C(38, 46, 70), C(64, 76, 110), 5, 0)
    l.rect(0, Y_SUELO, ANCHO - 1, Y_SUELO + 1, C(18, 22, 36))

    # --- zona alta: lluvia de dígitos, bandeja de cables, paneles de luz
    rng = random.Random(21)
    for col in range(0, ANCHO, 12):
        for fila in range(46, Y_VIGA - 4, 9):
            if rng.random() < 0.42:
                color = rng.choice((C(40, 130, 100), C(36, 110, 150), C(30, 90, 120)))
                l.texto(col, fila, rng.choice("01"), color, 9)
    sombrear(l, 0, 0, ANCHO - 1, Y_VIGA, 70)
    l.rect(0, 8, ANCHO - 1, 28, C(28, 32, 46))
    l.rect(0, 8, ANCHO - 1, 10, C(70, 78, 100))
    for cy, col in ((13, C(240, 90, 80)), (17, C(90, 200, 255)), (21, C(250, 210, 70)), (25, C(110, 240, 140))):
        l.rect(0, cy, ANCHO - 1, cy + 1, oscurecer(col, 0.8))
    for rx in range(20, ANCHO, 60):
        l.rect(rx, 8, rx + 3, 28, C(60, 68, 90))
    for lx in (110, 300, 490, 640):
        l.rect(lx, 34, lx + 54, 38, C(150, 240, 255))
        sombra = Image.new("RGBA", (70, 26), (120, 230, 255, 30))
        l.im.alpha_composite(sombra, (lx - 8, 39))
    l.caja(18, 52, 190, 94, C(16, 20, 34))
    l.rect(22, 56, 186, 57, C(70, 230, 255))
    l.rect(22, 89, 186, 90, C(70, 230, 255))
    l.texto(30, 62, "INFORMATICA", C(120, 240, 255), 14)
    l.caja(520, 52, 690, 94, C(16, 20, 34))
    l.texto(534, 62, "SALA DE SERVIDORES", C(120, 240, 255), 10)

    viga(l, C(70, 82, 112), C(44, 52, 76), C(14, 18, 30), C(70, 230, 255))

    # --- izquierda: racks de servidores (decoración)
    rng = random.Random(31)
    for i in range(3):
        x0 = 14 + i * 48
        l.caja(x0, 152, x0 + 42, 306, C(22, 26, 40))
        for u in range(12):
            y0 = 158 + u * 12
            l.caja(x0 + 3, y0, x0 + 39, y0 + 8, C(46, 54, 74))
            l.rect(x0 + 6, y0 + 3, x0 + 18, y0 + 3, C(28, 34, 50))
            l.rect(x0 + 6, y0 + 5, x0 + 18, y0 + 5, C(28, 34, 50))
            l.rect(x0 + 30, y0 + 3, x0 + 31, y0 + 4, rng.choice((C(90, 240, 130), C(90, 240, 130), C(255, 190, 70), C(90, 200, 255))))
            l.rect(x0 + 34, y0 + 3, x0 + 35, y0 + 4, rng.choice((C(90, 240, 130), C(255, 90, 80), C(90, 200, 255))))

    # --- derecha: pizarra con diagrama y código, y un afiche
    l.caja(512, 150, 680, 262, C(232, 238, 246))
    l.rect(516, 154, 676, 158, C(190, 200, 216))
    for bx, by, col in ((528, 172, C(70, 130, 220)), (588, 172, C(70, 190, 130)), (644, 172, C(230, 140, 60)), (558, 222, C(150, 100, 210))):
        l.caja(bx, by, bx + 24, by + 16, col)
    for a, b in (((552, 180), (588, 180)), ((612, 180), (644, 180)), ((540, 190), (566, 222)), ((600, 190), (582, 222))):
        l.linea(a[0], a[1], b[0], b[1], C(60, 66, 84))
    for i, largo in enumerate((70, 50, 84, 40)):
        l.rect(634 - largo, 214 + i * 8, 634, 215 + i * 8, C(90, 100, 124))
    l.rect(516, 262, 676, 264, C(150, 158, 176))
    l.caja(512, 276, 546, 300, C(250, 210, 70))
    l.texto(517, 281, "01", C(40, 40, 50), 12)
    l.caja(556, 276, 600, 300, C(70, 230, 255))
    l.rect(560, 286, 590, 287, C(20, 30, 50))
    l.rect(560, 291, 580, 292, C(20, 30, 50))
    return l.im


# ============================================================ salida
def main():
    pisos = {
        1: dict(objetos=[horno, refrigerador, estufa, mesa], estilo="gastro", fondo=fondo_gastronomia,
                chispa=(255, 190, 70, 255)),
        2: dict(objetos=[monitor, servidor, computadora, impresora], estilo="info", fondo=fondo_informatica,
                chispa=(110, 240, 255, 255)),
    }
    for n, cfg in pisos.items():
        for t, fabrica in enumerate(cfg["objetos"]):
            intacto = fabrica()
            semilla = n * 100 + t
            estados = [intacto, roto(intacto, semilla), destruido(intacto, semilla, chispa=cfg["chispa"])]
            for e, im in enumerate(estados):
                im.save(_ruta("piso%d_objeto%d_%d.png" % (n, t, e)))
        base = plataforma(cfg["estilo"])
        for e, im in enumerate([base, plataforma_agrietada(base, n), plataforma_destruida(base, n)]):
            im.save(_ruta("piso%d_plataforma_%d.png" % (n, e)))
        cfg["fondo"]().save(_ruta("piso%d_fondo.png" % n))
    print("Arte generado en", SALIDA)


def _ruta(nombre):
    os.makedirs(SALIDA, exist_ok=True)
    return os.path.join(SALIDA, nombre)


if __name__ == "__main__":
    sys.exit(main())
