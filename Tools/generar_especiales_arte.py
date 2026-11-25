"""Arte de los movimientos especiales del demoledor (pixel art por código).

  Efectos/embestida_flecha : franja de aviso con flechas (se estira a lo largo de la embestida)
  Efectos/aro_pisoton      : aro de aviso del pisotón (rayado, se escala al radio)
  Efectos/grieta_pisoton   : grietas radiales que quedan en el piso tras el pisotón
"""
import math
import random
from PIL import Image, ImageDraw
import exportar_extras as ex

K = (12, 14, 26, 255)


def embestida_flecha():
    w, h = 96, 24
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 3, w - 1, h - 4), fill=(255, 60, 40, 70))
    d.rectangle((0, 3, w - 1, 4), fill=(255, 90, 60, 190))
    d.rectangle((0, h - 5, w - 1, h - 4), fill=(255, 90, 60, 190))
    for x0 in range(4, w - 8, 18):
        pts = [(x0, 6), (x0 + 7, 6), (x0 + 15, h // 2), (x0 + 7, h - 7), (x0, h - 7), (x0 + 8, h // 2)]
        d.polygon(pts, fill=(255, 210, 70, 230))
        d.line([(x0, 6), (x0 + 8, h // 2), (x0, h - 7)], fill=(255, 255, 255, 200))
    return im


def aro_pisoton():
    n = 96
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    px = im.load()
    c = (n - 1) / 2
    for y in range(n):
        for x in range(n):
            r = math.hypot(x - c, y - c)
            ang = math.atan2(y - c, x - c)
            if 41 <= r <= 46.5:
                rayado = int((ang + math.pi) / (2 * math.pi) * 24) % 2 == 0
                px[x, y] = (255, 200, 60, 235) if rayado else (255, 70, 40, 235)
            elif r < 41:
                a = int(70 * (r / 41) ** 2)
                px[x, y] = (255, 80, 40, a)
    return im


def grieta_pisoton():
    random.seed(7)
    n = 96
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = n // 2
    for i in range(9):
        ang = i * 2 * math.pi / 9 + random.uniform(-0.2, 0.2)
        x, y = c, c
        largo = random.randint(26, 44)
        for paso in range(largo // 4):
            ang += random.uniform(-0.45, 0.45)
            nx, ny = x + math.cos(ang) * 4, y + math.sin(ang) * 4
            grosor = 3 if paso < 3 else 2 if paso < 7 else 1
            d.line([(x, y), (nx, ny)], fill=(30, 22, 18, 230), width=grosor)
            x, y = nx, ny
    d.ellipse((c - 9, c - 7, c + 9, c + 7), fill=(40, 30, 24, 200))
    d.ellipse((c - 5, c - 4, c + 5, c + 4), fill=(20, 15, 12, 230))
    # cascotes
    for _ in range(14):
        a = random.uniform(0, 2 * math.pi)
        r = random.uniform(10, 34)
        x, y = c + math.cos(a) * r, c + math.sin(a) * r
        d.rectangle((x, y, x + 2, y + 1), fill=(120, 110, 100, 255))
    return im


def main():
    ex.escribir("Efectos/embestida_flecha", embestida_flecha(), 32)
    ex.escribir("Efectos/aro_pisoton", aro_pisoton(), 32)
    ex.escribir("Efectos/grieta_pisoton", grieta_pisoton(), 32)


if __name__ == "__main__":
    main()
