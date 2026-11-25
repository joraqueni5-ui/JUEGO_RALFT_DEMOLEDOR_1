"""Arte de la FURIA del estudiante y detalles de los escenarios (dibujado por código, sin modelo de imagen).

Salida en Assets/Resources/Extras/:
  Ira/aura_<0..3>.png            48x64   aura de energía (blanca: el juego la tiñe con el color de la carrera)
  Ira/simbolo_<carrera>_<n>.png  16x16   símbolos que giran alrededor (código, cruces, $, sartenes, balanzas...)
  Ira/rayo_<0..2>.png            16x32   chispas eléctricas
  Detalles/<nombre>.png          32x32   cosas en el piso: papeles, libros, mochilas, café, cinta de obra, conos...
Uso: python Tools/generar_ira_y_detalles.py
"""
import importlib.util
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location("exportar_extras", os.path.join(RAIZ, "Tools", "exportar_extras.py"))
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)
FUENTE = os.path.join(RAIZ, "Assets", "Sprites", "Menus", "Fuentes", "Silkscreen-Bold.bytes")
K = (22, 18, 26, 255)
B = (255, 255, 255, 255)


def contorno(im, color=K):
    w, h = im.size
    px = im.load()
    out = im.copy()
    po = out.load()
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


def mapa(filas, colores, w=16, h=16):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    oy = (h - len(filas)) // 2
    for y, f in enumerate(filas):
        ox = (w - len(f)) // 2
        for x, ch in enumerate(f):
            if ch in colores:
                im.putpixel((ox + x, oy + y), colores[ch])
    return contorno(im)


def glifo(texto, tam=8):
    f = ImageFont.truetype(FUENTE, tam)
    c = f.getbbox(texto)
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.text(((16 - (c[2] - c[0])) // 2 - c[0], (16 - (c[3] - c[1])) // 2 - c[1]), texto, font=f, fill=B)
    return contorno(im)


# ------------------------------------------------------------------ símbolos por carrera (blancos: se tiñen en el juego)
CORAZON = [".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..."]
CRUZ = ["..XXX..", "..XXX..", "XXXXXXX", "XXXXXXX", "XXXXXXX", "..XXX..", "..XXX.."]
DIENTE = [".XXXXX.", "XXXXXXX", "XXXXXXX", "XXXXXXX", ".XX.XX.", ".XX.XX.", ".X...X."]
LLAMA = ["...X...", "..XX...", "..XXX..", ".XXXX..", ".XXXXX.", "XXXXXXX", "XXXXXXX", ".XXXXX."]
TENEDOR = ["X.X.X", "X.X.X", "XXXXX", ".XXX.", "..X..", "..X..", "..X..", "..X.."]
REGLA = ["XXXXXXXXX", "X.X.X.X.X", "X.X.X.X.X", "XXXXXXXXX"]
TRIANGULO = ["X......", "XX.....", "X.X....", "X..X...", "X...X..", "X....X.", "XXXXXXX"]
BALANZA = ["...X...", "XXXXXXX", "X..X..X", "XX.X.XX", "...X...", "..XXX.."]
MICRO = [".XXX.", "XXXXX", "XXXXX", "XXXXX", ".XXX.", "X.X.X", ".XXX.", "..X..", ".XXX."]
GLOBO = ["XXXXXXX", "XXXXXXX", "XXXXXXX", "XXXXXXX", "XX.....", "X......"]
BARRAS = ["......X", "....X.X", "....X.X", "..X.X.X", "X.X.X.X", "XXXXXXX"]
DESTELLO = ["...X...", "...X...", "..XXX..", "XXXXXXX", "..XXX..", "...X...", "...X..."]

SIMBOLOS = {
    "sistemas": [glifo("{}"), glifo("</>", 8), glifo("01"), glifo("1"), glifo("0")],
    "medicina": [mapa(CRUZ, {"X": B}), mapa(CORAZON, {"X": B}), glifo("+")],
    "administracion": [glifo("$"), glifo("%"), mapa(BARRAS, {"X": B})],
    "gastronomia": [mapa(LLAMA, {"X": B}), mapa(TENEDOR, {"X": B}), glifo("*")],
    "derecho": [mapa(BALANZA, {"X": B}), glifo("§") if True else glifo("S"), glifo("!")],
    "arquitectura": [mapa(REGLA, {"X": B}), mapa(TRIANGULO, {"X": B}), glifo("#")],
    "odontologia": [mapa(DIENTE, {"X": B}), mapa(DESTELLO, {"X": B}), glifo("+")],
    "comunicacion": [mapa(MICRO, {"X": B}), mapa(GLOBO, {"X": B}), glifo("!")],
}


def aura(i):
    """Aura tipo llamarada alrededor del cuerpo (48x64), con el borde más brillante."""
    W, H = 48, 64
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = im.load()
    rnd = random.Random(40 + i)
    fase = i * math.pi / 2
    for y in range(H):
        for x in range(W):
            nx = (x - W / 2) / (W / 2)
            ny = (y - H * 0.6) / (H * 0.42)
            ondas = 0.12 * math.sin(y * 0.45 + fase) + 0.08 * math.sin(y * 0.9 - fase * 1.5)
            borde = 1.0 + ondas - max(0, -ny) * 0.35      # se afina hacia arriba como una llama
            d = math.sqrt(nx * nx + ny * ny * 0.8)
            if d < borde:
                interior = d / borde
                a = int(255 * (interior ** 2.2))
                if interior > 0.82:
                    a = 255
                if rnd.random() < 0.04:
                    a = min(255, a + 80)
                px[x, y] = (255, 255, 255, max(0, min(255, a)))
    # lenguas de fuego hacia arriba
    for k in range(5):
        cx = int(W * (0.2 + 0.15 * k)) + int(3 * math.sin(fase + k))
        alto = rnd.randint(8, 16)
        for y in range(alto):
            yy = int(H * 0.18) - y
            if 0 <= yy < H:
                ancho = max(0, (alto - y) // 4)
                for x in range(cx - ancho, cx + ancho + 1):
                    if 0 <= x < W:
                        px[x, yy] = (255, 255, 255, 200)
    return im


def rayo(i):
    rnd = random.Random(70 + i)
    im = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x = 8
    pts = []
    for y in range(0, 32, 4):
        pts.append((x, y))
        x = max(2, min(13, x + rnd.choice([-3, -2, 2, 3])))
    d.line(pts, fill=B, width=2)
    d.line(pts, fill=(255, 255, 255, 140), width=4)
    d.line(pts, fill=B, width=1)
    return im


# ------------------------------------------------------------------ detalles de escenario (vistos desde arriba)
def lienzo():
    return Image.new("RGBA", (32, 32), (0, 0, 0, 0))


def papeles():
    im = lienzo()
    d = ImageDraw.Draw(im)
    for (x, y, a) in [(6, 8, -12), (13, 12, 18)]:
        hoja = Image.new("RGBA", (12, 15), (246, 244, 236, 255))
        hd = ImageDraw.Draw(hoja)
        for ly in range(3, 13, 3):
            hd.line([2, ly, 9, ly], fill=(140, 160, 200, 255))
        hd.line([0, 0, 11, 0], fill=(255, 255, 255, 255))
        hoja = hoja.rotate(a, expand=True, resample=Image.NEAREST)
        im.alpha_composite(hoja, (x, y))
    return contorno(im, (60, 60, 70, 160))


def bola_papel():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.ellipse([11, 12, 20, 20], fill=(236, 234, 226, 255))
    for (a, b) in [((12, 15), (17, 14)), ((14, 18), (19, 17)), ((13, 13), (15, 19))]:
        d.line([a, b], fill=(186, 184, 178, 255))
    return contorno(im, (70, 70, 80, 200))


def libro():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.rectangle([6, 10, 25, 22], fill=(170, 40, 40, 255))
    d.rectangle([7, 11, 15, 21], fill=(246, 242, 228, 255))
    d.rectangle([16, 11, 24, 21], fill=(238, 234, 220, 255))
    for y in range(13, 21, 2):
        d.line([8, y, 14, y], fill=(150, 150, 160, 255))
        d.line([17, y, 23, y], fill=(150, 150, 160, 255))
    d.line([15, 11, 15, 21], fill=(120, 30, 30, 255))
    return contorno(im)


def mochila():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([8, 8, 23, 25], radius=4, fill=(52, 96, 170, 255))
    d.rounded_rectangle([10, 16, 21, 24], radius=2, fill=(40, 76, 140, 255))
    d.line([10, 16, 21, 16], fill=(230, 190, 60, 255))
    d.arc([10, 3, 21, 12], 180, 360, fill=(30, 50, 90, 255), width=2)
    d.point((12, 11), fill=(120, 160, 230, 255))
    return contorno(im)


def cafe():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.ellipse([6, 14, 22, 24], fill=(110, 66, 36, 160))    # mancha
    d.ellipse([14, 7, 24, 17], fill=(246, 244, 240, 255))  # vaso visto desde arriba
    d.ellipse([16, 9, 22, 15], fill=(96, 56, 30, 255))
    d.point((17, 10), fill=(160, 110, 70, 255))
    return contorno(im, (60, 50, 50, 200))


def lapices():
    im = lienzo()
    d = ImageDraw.Draw(im)
    for (y, c) in [(12, (230, 190, 40)), (16, (60, 140, 220)), (20, (220, 70, 60))]:
        d.line([6, y, 22, y - 4], fill=(*c, 255), width=2)
        d.point((23, y - 5), fill=(60, 50, 40, 255))
        d.point((5, y + 1), fill=(240, 160, 170, 255))
    return contorno(im)


def cinta():
    im = Image.new("RGBA", (32, 10), (0, 0, 0, 0))
    for x in range(32):
        for y in range(2, 8):
            im.putpixel((x, y), (250, 210, 40, 255) if ((x + y) // 3) % 2 == 0 else (30, 30, 34, 255))
    return contorno(im)


def cono():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.rectangle([7, 7, 24, 24], fill=(200, 80, 20, 255))
    d.ellipse([9, 9, 22, 22], fill=(250, 120, 30, 255))
    d.ellipse([12, 12, 19, 19], fill=(250, 250, 250, 255))
    d.ellipse([14, 14, 17, 17], fill=(250, 120, 30, 255))
    return contorno(im)


def escombritos():
    rnd = random.Random(5)
    im = lienzo()
    d = ImageDraw.Draw(im)
    for _ in range(9):
        x, y = rnd.randint(5, 24), rnd.randint(8, 24)
        w, h = rnd.randint(2, 4), rnd.randint(2, 3)
        c = rnd.choice([(150, 146, 140), (120, 116, 110), (170, 80, 50), (190, 186, 178)])
        d.rectangle([x, y, x + w, y + h], fill=(*c, 255))
    for _ in range(14):
        d.point((rnd.randint(4, 27), rnd.randint(6, 27)), fill=(200, 196, 188, 200))
    return contorno(im, (50, 46, 52, 180))


def piso_mojado():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.polygon([(16, 4), (26, 26), (6, 26)], fill=(250, 210, 40, 255))
    d.polygon([(16, 9), (22, 23), (10, 23)], fill=(255, 230, 90, 255))
    d.rectangle([15, 12, 17, 18], fill=(30, 30, 34, 255))
    d.rectangle([15, 20, 17, 21], fill=(30, 30, 34, 255))
    return contorno(im)


def grieta_piso():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.line([(4, 18), (10, 15), (14, 17), (19, 12), (27, 13)], fill=(40, 36, 40, 200), width=1)
    d.line([(14, 17), (16, 23)], fill=(40, 36, 40, 200))
    d.line([(19, 12), (21, 7)], fill=(40, 36, 40, 200))
    return im


def hoja_planta():
    im = lienzo()
    d = ImageDraw.Draw(im)
    d.ellipse([10, 12, 20, 18], fill=(70, 150, 70, 255))
    d.line([10, 15, 20, 15], fill=(40, 100, 40, 255))
    d.ellipse([16, 18, 23, 22], fill=(90, 170, 80, 255))
    return contorno(im, (30, 60, 30, 200))


DETALLES = {"papeles": papeles, "bola_papel": bola_papel, "libro": libro, "mochila": mochila, "cafe": cafe,
            "lapices": lapices, "cinta_obra": cinta, "cono": cono, "escombritos": escombritos,
            "piso_mojado": piso_mojado, "grieta_piso": grieta_piso, "hoja_planta": hoja_planta}


def main():
    for i in range(4):
        ex.escribir(f"Ira/aura_{i}", aura(i), 32, True)
    for i in range(3):
        ex.escribir(f"Ira/rayo_{i}", rayo(i), 32)
    for carrera, lista in SIMBOLOS.items():
        for n, im in enumerate(lista):
            ex.escribir(f"Ira/simbolo_{carrera}_{n}", im, 32)
    for nombre, f in DETALLES.items():
        ex.escribir(f"Detalles/{nombre}", f(), 32)

    # hoja de revisión
    piezas = [aura(i) for i in range(4)] + [im for l in SIMBOLOS.values() for im in l] + [f() for f in DETALLES.values()]
    s = Image.new("RGBA", (len(piezas) * 70, 140), (50, 56, 80, 255))
    for k, p in enumerate(piezas):
        e = max(1, min(128 // p.width, 128 // p.height))
        s.alpha_composite(p.resize((p.width * e, p.height * e), Image.NEAREST), (k * 70, 0))
    os.makedirs(os.path.join(RAIZ, "Tools", "sprites_out"), exist_ok=True)
    s.save(os.path.join(RAIZ, "Tools", "sprites_out", "ira_detalles.png"))
    print("ok", len(piezas))


if __name__ == "__main__":
    main()
