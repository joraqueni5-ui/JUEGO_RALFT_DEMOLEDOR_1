"""Tema "serio" de los menús, dibujado por código (sin modelo de imagen).

Genera en Tools/sprites_out/tema/:
  fondo_menu_noche.png  480x270  el edificio de noche en demolición (se ve x4 = 1920x1080)
  logo_demoledor.png    ~380x72  logotipo REPARALOFRANZ / QUE NO SE CAIGA LA U
  boton_serio.png       24x16    botón uniforme (borde 9-slice 3,5,4,3)
  marco_serio.png       24x16    marco ámbar del botón seleccionado
  panel_serio.png       16x16    panel (borde 9-slice 6)
  franja_seria.png      8x8      franja de obra sobria (mosaico)
  haz_luz.png           48x256   haz de reflector (degradado)
  mota.png              3x3      partícula de polvo

Uso: python Tools/generar_tema_menu.py
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(RAIZ, "Tools", "sprites_out", "tema")
FUENTE_BOLD = os.path.join(RAIZ, "Assets", "Sprites", "Menus", "Fuentes", "Silkscreen-Bold.bytes")
FUENTE_REG = os.path.join(RAIZ, "Assets", "Sprites", "Menus", "Fuentes", "Silkscreen-Regular.bytes")
os.makedirs(OUT, exist_ok=True)


def hx(v, a=255):
    v = v.lstrip("#")
    return (int(v[0:2], 16), int(v[2:4], 16), int(v[4:6], 16), a)


CONTORNO = hx("0a0f24")
AMBAR = hx("ffc21a")
AMBAR_OSC = hx("c2860c")
CREMA = hx("fff4dc")

BAYER4 = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def mezcla(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(4))


# ------------------------------------------------------------------ piezas 9-slice
def boton():
    im = Image.new("RGBA", (24, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cara, cara_alta, canto = hx("1f2840"), hx("2c3858"), hx("121a2e")
    d.rectangle([1, 1, 23, 15], fill=(0, 0, 0, 110))           # sombra
    d.rectangle([0, 0, 22, 14], fill=CONTORNO)                 # contorno
    d.rectangle([1, 1, 21, 13], fill=cara)                     # cara
    d.rectangle([1, 1, 21, 5], fill=cara_alta)                 # mitad alta más clara
    d.rectangle([1, 11, 21, 13], fill=canto)                   # canto inferior
    d.line([2, 2, 20, 2], fill=hx("4a5a80"))                  # brillo
    d.line([1, 1, 1, 12], fill=AMBAR)                          # filo ámbar a la izquierda
    for p in [(1, 13), (21, 1), (21, 13)]:
        im.putpixel(p, CONTORNO)
    return im


def marco():
    im = Image.new("RGBA", (24, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 23, 15], outline=AMBAR)
    # esquinas más gruesas, como escuadras
    for (x, y, dx, dy) in [(0, 0, 1, 1), (23, 0, -1, 1), (0, 15, 1, -1), (23, 15, -1, -1)]:
        im.putpixel((x + dx, y), AMBAR)
        im.putpixel((x, y + dy), AMBAR)
        im.putpixel((x + dx, y + dy), hx("fff0a0"))
    return im


def panel():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 15, 15], fill=CONTORNO)
    d.rectangle([1, 1, 14, 14], fill=hx("3a4662"))
    d.line([1, 1, 14, 1], fill=hx("5a6888"))
    d.line([1, 14, 14, 14], fill=hx("252e46"))
    d.rectangle([3, 3, 12, 12], fill=CONTORNO)
    d.rectangle([4, 4, 11, 11], fill=hx("111829"))
    d.line([4, 4, 11, 4], fill=hx("1a2338"))
    for p in [(2, 2), (13, 2), (2, 13), (13, 13)]:
        im.putpixel(p, AMBAR_OSC)
    return im


def franja():
    im = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    for y in range(8):
        for x in range(8):
            im.putpixel((x, y), AMBAR_OSC if (x + y) % 8 < 4 else hx("1a2034"))
    return im


def haz():
    w, h = 48, 256
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        t = y / (h - 1)                 # 0 arriba (lejos) .. 1 abajo (origen)
        ancho = 2 + (1 - t) * (w / 2 - 2)
        a_base = 70 * (0.25 + 0.75 * t)
        for x in range(w):
            dx = abs(x + 0.5 - w / 2)
            if dx < ancho:
                borde = 1 - (dx / ancho) ** 2
                im.putpixel((x, y), (255, 244, 200, int(a_base * borde)))
    return im


def burbuja():
    """Globo de diálogo 16x16 (borde 9-slice 5): blanco con contorno oscuro."""
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, 15, 15], radius=4, fill=CONTORNO)
    d.rounded_rectangle([1, 1, 14, 14], radius=3, fill=hx("fbf7ee"))
    d.line([2, 13, 13, 13], fill=hx("ddd6c6"))
    return im


def cola_burbuja():
    """Piquito del globo (8x6) que apunta abajo."""
    im = Image.new("RGBA", (8, 6), (0, 0, 0, 0))
    filas = ["kwwwwwk.", ".kwwwk..", "..kwk...", "...k....", "........", "........"]
    for y, f in enumerate(filas):
        for x, ch in enumerate(f):
            if ch == "k": im.putpixel((x, y), CONTORNO)
            elif ch == "w": im.putpixel((x, y), hx("fbf7ee"))
    return im


def nube_pensamiento():
    """Tres burbujitas para los pensamientos (10x8)."""
    im = Image.new("RGBA", (10, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for (x, y, r) in [(6, 1, 2), (3, 4, 1.5), (1, 6, 1)]:
        d.ellipse([x - r, y - r, x + r, y + r], fill=hx("fbf7ee"), outline=CONTORNO)
    return im


def mota():
    im = Image.new("RGBA", (3, 3), (0, 0, 0, 0))
    for p in [(1, 0), (0, 1), (1, 1), (2, 1), (1, 2)]:
        im.putpixel(p, (255, 255, 255, 150))
    im.putpixel((1, 1), (255, 255, 255, 255))
    return im


# ------------------------------------------------------------------ logotipo
def texto_mascara(texto, ruta_fuente, tam):
    f = ImageFont.truetype(ruta_fuente, tam)
    caja = f.getbbox(texto)
    w, h = caja[2] - caja[0], caja[3] - caja[1]
    m = Image.new("L", (w + 2, h + 2), 0)
    ImageDraw.Draw(m).text((1 - caja[0], 1 - caja[1]), texto, font=f, fill=255)
    return m.point(lambda v: 255 if v > 127 else 0)


def logo():
    principal = texto_mascara("REPARALOFRANZ", FUENTE_BOLD, 32)
    sub = texto_mascara("QUE NO SE CAIGA LA U", FUENTE_BOLD, 16)
    extr = 4
    W = max(principal.width, sub.width) + extr + 8
    H = principal.height + extr + 6 + sub.height + 10
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ox, oy = 3, 2

    # extrusión (bloque 3D hacia abajo)
    for k in range(extr, 0, -1):
        c = mezcla(hx("5a1e0c"), hx("8a3410"), k / extr)
        capa = Image.new("RGBA", principal.size, c)
        im.paste(capa, (ox + k // 2, oy + k), principal)
    # cara con degradado ámbar → naranja
    cara = Image.new("RGBA", principal.size)
    for y in range(principal.height):
        t = y / max(1, principal.height - 1)
        c = mezcla(hx("ffe27a"), hx("ff8a1a"), t)
        for x in range(principal.width):
            cara.putpixel((x, y), c)
    im.paste(cara, (ox, oy), principal)
    # brillo de la primera fila de cada letra
    px = im.load()
    for x in range(W):
        for y in range(1, H):
            if px[x, y][3] and not px[x, y - 1][3] and y < oy + principal.height:
                px[x, y] = hx("fff6c8")
    # grietas: líneas oscuras finas en diagonal
    rnd = random.Random(7)
    for _ in range(9):
        x = rnd.randrange(ox + 2, ox + principal.width - 3)
        y = rnd.randrange(oy + 3, oy + principal.height - 3)
        for _ in range(rnd.randrange(3, 6)):
            if 0 <= x < W and 0 <= y < H and px[x, y][3] and px[x, y][0] > 200:
                px[x, y] = hx("7a2a0c")
            x += rnd.choice([-1, 0, 1]); y += 1

    # placa de acero para UNIFRANZ
    sy = oy + principal.height + extr + 3
    pw = sub.width + 16
    pxl = (W - pw) // 2
    d = ImageDraw.Draw(im)
    d.rectangle([pxl, sy, pxl + pw, sy + sub.height + 5], fill=CONTORNO)
    d.rectangle([pxl + 1, sy + 1, pxl + pw - 1, sy + sub.height + 4], fill=hx("2c3858"))
    d.line([pxl + 1, sy + 1, pxl + pw - 1, sy + 1], fill=hx("4a5a80"))
    for (rx, ry) in [(pxl + 3, sy + 3), (pxl + pw - 3, sy + 3), (pxl + 3, sy + sub.height + 2), (pxl + pw - 3, sy + sub.height + 2)]:
        im.putpixel((rx, ry), AMBAR_OSC)
    im.paste(Image.new("RGBA", sub.size, CREMA), (pxl + 8, sy + 3), sub)

    # contorno exterior de 1 px
    alfa = im.split()[3]
    borde = Image.new("RGBA", im.size, (0, 0, 0, 0))
    bp = borde.load()
    ap = alfa.load()
    for y in range(H):
        for x in range(W):
            if ap[x, y]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < W and 0 <= yy < H and ap[xx, yy]:
                    bp[x, y] = CONTORNO
                    break
    borde.alpha_composite(im)
    return borde


# ------------------------------------------------------------------ fondo
def fondo():
    W, H = 480, 270
    rnd = random.Random(2026)
    im = Image.new("RGBA", (W, H))
    px = im.load()

    # cielo con bandas difuminadas (Bayer)
    cielo = [hx("060a1a"), hx("0a1026"), hx("0f1733"), hx("151f42"), hx("1d2850"), hx("27325c"), hx("343b66")]
    horizonte = 215
    for y in range(H):
        t = min(1.0, y / horizonte) * (len(cielo) - 1)
        i = int(t)
        f = t - i
        for x in range(W):
            umbral = (BAYER4[y % 4][x % 4] + 0.5) / 16
            c = cielo[min(len(cielo) - 1, i + (1 if f > umbral else 0))]
            px[x, y] = c

    # estrellas
    for _ in range(140):
        x, y = rnd.randrange(W), rnd.randrange(0, 150)
        b = rnd.choice([120, 160, 200, 255])
        px[x, y] = (b, b, min(255, b + 30), 255)
    for (x, y) in [(40, 22), (205, 14), (150, 70), (440, 30), (330, 18)]:
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            px[x + dx, y + dy] = (230, 236, 255, 255)

    d = ImageDraw.Draw(im)
    # luna con halo
    mx, my, mr = 436, 34, 13
    for r in range(mr + 14, mr, -1):
        a = int(40 * (1 - (r - mr) / 14))
        capa = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(capa).ellipse([mx - r, my - r, mx + r, my + r], fill=(200, 210, 255, a))
        im.alpha_composite(capa)
    d.ellipse([mx - mr, my - mr, mx + mr, my + mr], fill=hx("ece6c8"))
    d.ellipse([mx - mr + 4, my - mr - 2, mx + mr + 6, my + mr - 4], fill=hx("ece6c8"))
    for (cx, cy, cr) in [(-5, -3, 3), (4, 5, 2), (-2, 7, 2), (6, -6, 1)]:
        d.ellipse([mx + cx - cr, my + cy - cr, mx + cx + cr, my + cy + cr], fill=hx("cfc8a6"))

    # cordillera nevada al fondo
    picos = [(0, 190), (40, 168), (70, 176), (110, 142), (128, 150), (146, 136), (170, 158), (210, 172),
             (250, 160), (300, 178), (350, 164), (400, 176), (450, 162), (480, 172), (480, 215), (0, 215)]
    d.polygon(picos, fill=hx("1c2550"))
    nieve = [(100, 150), (110, 142), (118, 147), (128, 150), (138, 141), (146, 136), (156, 146), (162, 152),
             (150, 150), (140, 147), (128, 155), (114, 151)]
    d.polygon(nieve, fill=hx("5a6698"))

    # ciudad lejana
    x = 0
    while x < W:
        w = rnd.randrange(10, 26)
        h = rnd.randrange(14, 46)
        d.rectangle([x, 215 - h, x + w, 215], fill=hx("111836"))
        for wy in range(215 - h + 3, 213, 4):
            for wx in range(x + 2, x + w - 1, 3):
                if rnd.random() < 0.18:
                    px[wx, wy] = rnd.choice([hx("ffd27a"), hx("c8a860"), hx("8ab0e0")])
        x += w + rnd.randrange(0, 4)

    # grúa (silueta) detrás del edificio
    g = hx("1c2650")
    d.rectangle([196, 40, 200, 232], fill=g)
    for y in range(44, 230, 8):
        d.line([196, y, 200, y + 8], fill=hx("2e3a6c"))
    d.rectangle([150, 36, 300, 40], fill=g)
    d.rectangle([150, 41, 160, 50], fill=g)                       # contrapeso
    d.line([198, 26, 152, 36], fill=g); d.line([198, 26, 298, 36], fill=g)
    d.rectangle([196, 24, 200, 36], fill=g)
    d.line([272, 40, 272, 96], fill=hx("1a2244"))                # cable
    d.rectangle([266, 96, 278, 104], fill=g)                      # bola de demolición
    px[198, 23] = hx("ff3a2a"); px[199, 23] = hx("ff3a2a")      # luz roja

    # edificio principal (7 plantas)
    bx0, bx1, techo, base = 238, 466, 74, 232
    d.rectangle([bx0 - 1, techo - 1, bx1 + 1, base], fill=CONTORNO)
    d.rectangle([bx0, techo, bx1, base], fill=hx("1a2440"))
    d.rectangle([bx0, techo, bx0 + 3, base], fill=hx("26335a"))  # canto iluminado por la luna
    pisos = 7
    alto_piso = (base - techo) // pisos
    for p in range(pisos):
        y0 = techo + p * alto_piso
        d.line([bx0, y0, bx1, y0], fill=hx("2c3a64"))
        for wx in range(bx0 + 8, bx1 - 10, 14):
            r = rnd.random()
            if r < 0.20:
                c = hx("ffd27a"); c2 = hx("ffe8b0")
            elif r < 0.28:
                c = hx("7ab0e8"); c2 = hx("a8d0f8")
            elif r < 0.42:
                c = hx("05070f"); c2 = None                      # ventana rota
            else:
                c = hx("2a3a66"); c2 = hx("344a7c")
            d.rectangle([wx, y0 + 4, wx + 9, y0 + alto_piso - 4], fill=c)
            if c2:
                d.line([wx + 1, y0 + 5, wx + 4, y0 + 5], fill=c2)
            else:
                d.line([wx + 2, y0 + 5, wx + 6, y0 + 10], fill=hx("3a4a78"))
                d.line([wx + 6, y0 + 10, wx + 4, y0 + alto_piso - 5], fill=hx("3a4a78"))
    # boquete en la fachada
    hueco = [(360, 150), (372, 138), (392, 142), (404, 156), (398, 176), (384, 186), (366, 180), (356, 166)]
    d.polygon(hueco, fill=hx("04060c"), outline=hx("5a4a3a"))
    for (ax, ay) in [(366, 150), (395, 160), (378, 182)]:
        d.line([ax, ay, ax + rnd.randrange(-8, 8), ay + rnd.randrange(-8, 8)], fill=hx("3a3a50"))
    # cartel del techo
    f = ImageFont.truetype(FUENTE_BOLD, 8)
    caja = f.getbbox("UNIFRANZ")
    tw = caja[2] - caja[0]
    sx = (bx0 + bx1) // 2 - tw // 2
    d.rectangle([sx - 5, techo - 16, sx + tw + 4, techo - 4], fill=CONTORNO)
    d.rectangle([sx - 4, techo - 15, sx + tw + 3, techo - 5], fill=hx("1f2840"))
    d.text((sx - caja[0], techo - 14 - caja[1]), "UNIFRANZ", font=f, fill=hx("ffb63a"))
    d.line([sx, techo - 4, sx, techo], fill=CONTORNO); d.line([sx + tw, techo - 4, sx + tw, techo], fill=CONTORNO)

    # suelo
    for y in range(232, H):
        t = (y - 232) / (H - 232)
        for x in range(W):
            umbral = (BAYER4[y % 4][x % 4] + 0.5) / 16
            px[x, y] = hx("1a1e2e") if t < umbral else hx("10131f")
    d.line([0, 232, W, 232], fill=hx("2c3250"))
    # valla de obra a la izquierda
    for x in range(0, 230, 2):
        c = AMBAR_OSC if (x // 8) % 2 == 0 else hx("161a2a")
        d.line([x, 222, x + 1, 222], fill=c)
        d.line([x, 223, x + 1, 223], fill=c)
    for x in range(4, 230, 36):
        d.rectangle([x, 216, x + 1, 232], fill=hx("2a3048"))
    # conos
    for cx in [36, 120, 214]:
        d.polygon([(cx, 236), (cx + 4, 226), (cx + 8, 236)], fill=hx("e86a1a"))
        d.line([cx + 2, 232, cx + 6, 232], fill=CREMA)
        d.rectangle([cx - 1, 236, cx + 9, 237], fill=hx("a84a10"))
    # montón de escombros donde golpea el demoledor
    for _ in range(120):
        ex = int(rnd.gauss(372, 30)); ey = int(rnd.gauss(238, 4))
        w = rnd.randrange(2, 6); h = rnd.randrange(2, 4)
        c = rnd.choice([hx("4a3a34"), hx("5a4a40"), hx("3a3040"), hx("6a5a4a"), hx("8a4a2a")])
        d.rectangle([ex, ey, ex + w, ey + h], fill=c)
    # polvo bajo
    capa = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dc = ImageDraw.Draw(capa)
    for _ in range(18):
        cx, cy, r = rnd.gauss(372, 40), rnd.gauss(232, 5), rnd.randrange(6, 16)
        dc.ellipse([cx - r * 1.6, cy - r, cx + r * 1.6, cy + r], fill=(150, 140, 160, 22))
    im.alpha_composite(capa)
    return im.convert("RGBA")


if __name__ == "__main__":
    piezas = {
        "boton_serio": boton(), "marco_serio": marco(), "panel_serio": panel(), "franja_seria": franja(),
        "haz_luz": haz(), "mota": mota(), "burbuja": burbuja(), "cola_burbuja": cola_burbuja(), "nube_pensamiento": nube_pensamiento(), "logo_demoledor": logo(), "fondo_menu_noche": fondo(),
    }
    for n, im in piezas.items():
        im.save(os.path.join(OUT, n + ".png"))
        print(n, im.size)
