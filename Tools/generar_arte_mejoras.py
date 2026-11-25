"""Mejoras de arte (pixel art por código):

  Mejoras/casco_obra, casco_rojo      cascos que distinguen a los dos demoledores
  Mejoras/vapor_0..3                  vapor que le sale de la cabeza en modo rabia
  Mejoras/chispa_0..2                 chispas de los objetos dañados
  Mejoras/humo_0..3                   humo de los objetos rotos
  Pared/cuadro, pizarra, letrero_salida, extintor, reloj, tablero, ventana   decoración de las paredes
Salida: Assets/Resources/Extras/  (uso: python Tools/generar_arte_mejoras.py)
"""
import math
import random
from PIL import Image, ImageDraw
import exportar_extras as ex

K = (16, 14, 24, 255)


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


def casco(cuerpo, sombra, brillo):
    im = Image.new("RGBA", (28, 18), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([1, 12, 26, 15], fill=sombra)            # ala
    d.rectangle([1, 12, 26, 13], fill=cuerpo)
    d.pieslice([4, 2, 23, 24], 180, 360, fill=cuerpo)     # cúpula
    d.rectangle([12, 3, 15, 12], fill=sombra)             # cresta
    d.line([(7, 7), (10, 4)], fill=brillo, width=2)
    return contorno(im)


def vapor(i):
    n = 24
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    r = 4 + i * 2
    a = 230 - i * 45
    for (cx, cy, k) in ((12, 14, 1.0), (8, 10, 0.7), (16, 9, 0.6)):
        rr = r * k
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=(240, 240, 245, a))
    d.ellipse([10 - r * 0.4, 11 - r * 0.4, 10 + r * 0.2, 11 + r * 0.2], fill=(255, 255, 255, min(255, a + 20)))
    return im


def chispa(i):
    n = 16
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    random.seed(30 + i)
    c = n // 2
    for k in range(4 + i):
        a = random.uniform(0, math.tau)
        l = random.uniform(3, 7)
        d.line([(c, c), (c + math.cos(a) * l, c + math.sin(a) * l)], fill=(255, 230, 90, 255), width=1)
    d.rectangle([c - 1, c - 1, c + 1, c + 1], fill=(255, 255, 255, 255))
    return im


def humo(i):
    n = 32
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    r = 5 + i * 2.5
    a = 200 - i * 40
    for (cx, cy, k) in ((16, 18, 1.0), (11, 14, 0.75), (21, 13, 0.7)):
        rr = r * k
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=(70, 66, 72, a))
    return im


def lienzo(w=32, h=32):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def cuadro():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rectangle([4, 6, 27, 25], fill=(140, 92, 50, 255))
    d.rectangle([6, 8, 25, 23], fill=(120, 190, 240, 255))
    d.polygon([(6, 23), (12, 14), (17, 20), (21, 15), (25, 23)], fill=(70, 140, 80, 255))
    d.ellipse([19, 9, 23, 13], fill=(255, 220, 80, 255))
    return contorno(im)


def pizarra():
    im = lienzo(40, 28); d = ImageDraw.Draw(im)
    d.rectangle([1, 2, 38, 25], fill=(170, 120, 70, 255))
    d.rectangle([3, 4, 36, 23], fill=(40, 80, 60, 255))
    d.line([(6, 9), (18, 9)], fill=(240, 240, 240, 255))
    d.line([(6, 13), (24, 13)], fill=(240, 240, 240, 255))
    d.text((24, 14), "", fill=(255, 255, 255, 255))
    d.line([(26, 16), (32, 10)], fill=(255, 230, 120, 255))   # flecha de clase
    d.line([(6, 18), (14, 18)], fill=(240, 200, 220, 255))
    d.rectangle([14, 23, 24, 25], fill=(120, 80, 50, 255))    # bandeja
    return contorno(im)


def letrero_salida():
    im = lienzo(28, 14); d = ImageDraw.Draw(im)
    d.rectangle([1, 1, 26, 12], fill=(40, 170, 80, 255))
    d.rectangle([3, 3, 24, 10], fill=(60, 200, 100, 255))
    d.rectangle([5, 5, 8, 9], fill=(255, 255, 255, 255))      # monito
    d.polygon([(11, 7), (21, 7), (21, 5), (24, 7), (21, 9), (21, 7)], fill=(255, 255, 255, 255))
    return contorno(im)


def extintor():
    im = lienzo(14, 26); d = ImageDraw.Draw(im)
    d.rounded_rectangle([3, 7, 10, 24], radius=3, fill=(220, 40, 40, 255))
    d.rectangle([4, 9, 5, 22], fill=(255, 120, 120, 255))
    d.rectangle([5, 3, 8, 7], fill=(40, 40, 46, 255))
    d.line([(8, 4), (12, 8), (12, 14)], fill=(40, 40, 46, 255), width=1)
    d.rectangle([4, 14, 9, 17], fill=(250, 250, 250, 255))
    return contorno(im)


def reloj():
    im = lienzo(18, 18); d = ImageDraw.Draw(im)
    d.ellipse([1, 1, 16, 16], fill=(40, 40, 46, 255))
    d.ellipse([3, 3, 14, 14], fill=(250, 248, 240, 255))
    d.line([(8, 8), (8, 4)], fill=(30, 30, 30, 255)); d.line([(8, 8), (11, 9)], fill=(200, 40, 40, 255))
    return contorno(im)


def tablero():
    im = lienzo(32, 24); d = ImageDraw.Draw(im)
    d.rectangle([1, 1, 30, 22], fill=(150, 110, 70, 255))
    d.rectangle([3, 3, 28, 20], fill=(200, 160, 110, 255))
    for (x, y, c) in ((5, 5, (250, 240, 120)), (14, 4, (250, 250, 250)), (21, 6, (150, 220, 250)), (7, 12, (250, 180, 200)), (17, 12, (250, 250, 250))):
        d.rectangle([x, y, x + 6, y + 6], fill=(*c, 255))
        d.point((x + 3, y), fill=(220, 40, 40, 255))
    return contorno(im)


def ventana():
    im = lienzo(32, 20); d = ImageDraw.Draw(im)
    d.rectangle([1, 1, 30, 18], fill=(200, 205, 215, 255))
    d.rectangle([3, 3, 15, 16], fill=(150, 210, 250, 255))
    d.rectangle([17, 3, 28, 16], fill=(150, 210, 250, 255))
    d.line([(5, 14), (11, 5)], fill=(230, 245, 255, 255), width=2)
    d.line([(19, 14), (24, 7)], fill=(230, 245, 255, 255), width=1)
    return contorno(im)


def ventilador():
    n = 64
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = n // 2
    for k in range(4):
        a = k * math.pi / 2
        pts = []
        for t in range(0, 11):
            ang = a + (t / 10) * 0.55
            r = 6 + t * 2.4
            pts.append((c + math.cos(ang) * r, c + math.sin(ang) * r))
        for t in range(10, -1, -1):
            ang = a - 0.12 + (t / 10) * 0.3
            r = 6 + t * 2.4
            pts.append((c + math.cos(ang) * r, c + math.sin(ang) * r))
        d.polygon(pts, fill=(70, 62, 58, 255))
    d.ellipse([c - 7, c - 7, c + 7, c + 7], fill=(50, 46, 44, 255))
    d.ellipse([c - 3, c - 3, c + 3, c + 3], fill=(150, 146, 140, 255))
    return im


def polvo(i):
    n = 8
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    r = 1 + (i % 2)
    d.ellipse([4 - r, 4 - r, 4 + r, 4 + r], fill=(200, 190, 175, 230))
    return im


def confeti(i):
    colores = [(255, 80, 80), (255, 210, 60), (80, 200, 120), (90, 160, 255)]
    im = Image.new("RGBA", (6, 4), (*colores[i % 4], 255))
    ImageDraw.Draw(im).line([(0, 0), (5, 0)], fill=(255, 255, 255, 200))
    return im


def flecha_guia():
    im = Image.new("RGBA", (40, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(2, 10), (22, 10), (22, 2), (38, 16), (22, 30), (22, 22), (2, 22)], fill=K)
    d.polygon([(5, 13), (25, 13), (25, 8), (34, 16), (25, 24), (25, 19), (5, 19)], fill=(255, 210, 40, 255))
    d.line([(6, 14), (24, 14)], fill=(255, 245, 190, 255))
    return im


def nube(i):
    random.seed(70 + i)
    w, h = 72 + i * 12, 30 + i * 4
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for _ in range(7):
        r = random.randint(9, 15)
        x = random.randint(r, w - r); y = random.randint(h - r * 2, h - r)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(250, 252, 255, 235))
    d.rectangle([8, h - 8, w - 8, h - 2], fill=(250, 252, 255, 235))
    for y in range(h - 7, h):
        d.line([(8, y), (w - 8, y)], fill=(220, 228, 240, 235))
    return im


def pajaro(i):
    im = Image.new("RGBA", (12, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if i == 0:
        d.line([(0, 2), (6, 5), (11, 2)], fill=(40, 40, 50, 255), width=1)
    else:
        d.line([(0, 6), (6, 4), (11, 6)], fill=(40, 40, 50, 255), width=1)
    d.point((6, 5), fill=(40, 40, 50, 255))
    return im


def alfombra(c1, c2):
    w, h = 72, 44
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, w - 1, h - 1], fill=(*c2, 230))
    d.rectangle([3, 3, w - 4, h - 4], fill=(*c1, 230))
    d.rectangle([8, 8, w - 9, h - 9], outline=(*c2, 230), width=2)
    for x in range(14, w - 14, 10):
        d.point((x, h // 2), fill=(*c2, 230)); d.point((x + 5, h // 2 - 4), fill=(*c2, 230)); d.point((x + 5, h // 2 + 4), fill=(*c2, 230))
    for x in range(0, w, 3):   # flecos
        d.point((x, 0), fill=(240, 230, 200, 230)); d.point((x, h - 1), fill=(240, 230, 200, 230))
    return im


def franjas_peligro():
    w, h = 64, 12
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for x in range(-12, w, 12):
        d.polygon([(x, h), (x + 6, h), (x + 12, 0), (x + 6, 0)], fill=(250, 200, 30, 220))
        d.polygon([(x + 6, h), (x + 12, h), (x + 18, 0), (x + 12, 0)], fill=(30, 30, 34, 220))
    return im


def main():
    for i in range(4):
        ex.escribir(f"Mejoras/confeti_{i}", confeti(i), 32)
    ex.escribir("Mejoras/flecha_guia", flecha_guia(), 32)
    for i in range(3):
        ex.escribir(f"Mejoras/nube_{i}", nube(i), 32)
    for i in range(2):
        ex.escribir(f"Mejoras/pajaro_{i}", pajaro(i), 32)
    ex.escribir("Mejoras/alfombra_roja", alfombra((150, 40, 46), (100, 24, 30)), 32)
    ex.escribir("Mejoras/alfombra_azul", alfombra((40, 70, 140), (24, 44, 96)), 32)
    ex.escribir("Mejoras/franjas_peligro", franjas_peligro(), 32)
    ex.escribir("Mejoras/ventilador", ventilador(), 32)
    for i in range(2):
        ex.escribir(f"Mejoras/polvo_{i}", polvo(i), 32)
    ex.escribir("Mejoras/casco_obra", casco((250, 196, 30, 255), (190, 130, 20, 255), (255, 240, 160, 255)), 32)
    ex.escribir("Mejoras/casco_rojo", casco((220, 50, 40, 255), (150, 30, 30, 255), (255, 150, 140, 255)), 32)
    for i in range(4):
        ex.escribir(f"Mejoras/vapor_{i}", vapor(i), 32)
        ex.escribir(f"Mejoras/humo_{i}", humo(i), 32)
    for i in range(3):
        ex.escribir(f"Mejoras/chispa_{i}", chispa(i), 32)
    for f in (cuadro, pizarra, letrero_salida, extintor, reloj, tablero, ventana):
        ex.escribir("Pared/" + f.__name__, f(), 32, abajo=True)


if __name__ == "__main__":
    main()
