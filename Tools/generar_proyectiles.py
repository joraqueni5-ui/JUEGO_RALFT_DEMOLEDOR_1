"""Proyectiles del demoledor con más detalle (dibujados por código). Reemplaza los PNG en su lugar, con el mismo
tamaño y nombre, así ArteNiveles y los .meta siguen igual:
  Sprites/Efectos/bloque_escombro_grande/bloque_escombro_grande_<1..8>.png (+ _hoja)   32x32  bloque de hormigón con fierros
  Sprites/Efectos/roca_lluvia_<1..3>/roca_lluvia_<n>_<1..8>.png (+ _hoja)               24x24  rocas con grietas y brillo
  Sprites/Efectos/escombro_ladrillo/escombro_ladrillo_<1..4>.png (+ _hoja)               16x16  ladrillos con mortero
Uso: python Tools/generar_proyectiles.py
"""
import math
import os
import random

from PIL import Image, ImageDraw

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EF = os.path.join(RAIZ, "Assets", "Sprites", "Efectos")
K = (22, 18, 26, 255)


def contorno(im):
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
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 128:
                    po[x, y] = K
                    break
    return out


def rotar(base, grados, lado):
    """Rota en alta resolución y reduce con vecino más cercano (sin difuminar los píxeles)."""
    grande = base.resize((base.width * 4, base.height * 4), Image.NEAREST)
    r = grande.rotate(grados, resample=Image.NEAREST, expand=False)
    r = r.resize((lado, lado), Image.NEAREST)
    return contorno(r)


def bloque():
    rnd = random.Random(3)
    L = 30
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pts = [(4, 6), (12, 3), (24, 4), (27, 12), (26, 22), (18, 27), (6, 25), (3, 15)]
    d.polygon(pts, fill=(150, 146, 140, 255))
    # caras con luz y sombra
    d.polygon([(4, 6), (12, 3), (24, 4), (16, 10), (6, 11)], fill=(186, 182, 174, 255))
    d.polygon([(26, 22), (18, 27), (6, 25), (14, 20), (24, 17)], fill=(112, 108, 104, 255))
    # textura de hormigón (puntitos)
    px = im.load()
    for _ in range(90):
        x, y = rnd.randrange(L), rnd.randrange(L)
        if px[x, y][3]:
            c = px[x, y]
            k = rnd.choice([-18, -10, 12])
            px[x, y] = (max(0, min(255, c[0] + k)), max(0, min(255, c[1] + k)), max(0, min(255, c[2] + k)), 255)
    # ladrillos pegados
    for (x, y) in [(8, 13), (15, 13), (11, 17)]:
        d.rectangle([x, y, x + 5, y + 2], fill=(170, 70, 44, 255))
        d.line([x, y, x + 5, y], fill=(206, 104, 70, 255))
    # grieta
    d.line([(10, 5), (13, 9), (12, 12), (16, 15)], fill=(70, 66, 64, 255))
    # fierros (varillas oxidadas que asoman)
    for (a, b) in [((24, 6), (30 - 1, 1)), ((5, 22), (1, 27)), ((20, 24), (22, 29))]:
        d.line([a, b], fill=(120, 66, 40, 255), width=1)
        d.point(b, fill=(160, 96, 60, 255))
    return im


def roca(semilla, tono):
    rnd = random.Random(semilla)
    L = 22
    im = Image.new("RGBA", (L, L), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pts = []
    for i in range(9):
        a = i / 9 * math.tau
        r = rnd.uniform(7, 10)
        pts.append((11 + math.cos(a) * r, 11 + math.sin(a) * r))
    base, luz, sombra = tono
    d.polygon(pts, fill=base)
    d.polygon([pts[5], pts[6], pts[7], (11, 11)], fill=luz)
    d.polygon([pts[1], pts[2], pts[3], (11, 11)], fill=sombra)
    px = im.load()
    for _ in range(40):
        x, y = rnd.randrange(L), rnd.randrange(L)
        if px[x, y][3]:
            c = px[x, y]
            k = rnd.choice([-16, 14])
            px[x, y] = (max(0, min(255, c[0] + k)), max(0, min(255, c[1] + k)), max(0, min(255, c[2] + k)), 255)
    d.line([(8, 7), (11, 11), (10, 15)], fill=(60, 56, 52, 255))
    d.point((7, 8), fill=(240, 236, 226, 255))
    d.point((8, 7), fill=(240, 236, 226, 255))
    return im


def ladrillo():
    im = Image.new("RGBA", (14, 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 13, 7], fill=(176, 74, 46, 255))
    d.line([0, 0, 13, 0], fill=(214, 112, 76, 255))
    d.line([0, 7, 13, 7], fill=(122, 46, 28, 255))
    for (x, y) in [(3, 3), (8, 2), (11, 5), (5, 5)]:
        d.point((x, y), fill=(140, 56, 34, 255))
    d.line([0, 4, 3, 4], fill=(210, 200, 186, 255))   # resto de mortero
    d.line([10, 1, 13, 1], fill=(210, 200, 186, 255))
    return im


def guardar_serie(carpeta, nombre, cuadros):
    for i, c in enumerate(cuadros, 1):
        c.save(os.path.join(EF, carpeta, f"{nombre}_{i}.png"))
    hoja = Image.new("RGBA", (sum(c.width for c in cuadros), cuadros[0].height), (0, 0, 0, 0))
    x = 0
    for c in cuadros:
        hoja.paste(c, (x, 0))
        x += c.width
    ruta = os.path.join(EF, carpeta, f"{nombre}_hoja.png")
    if os.path.exists(ruta):
        hoja.save(ruta)


def centrar(im, lado):
    out = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
    out.paste(im, ((lado - im.width) // 2, (lado - im.height) // 2))
    return out


def main():
    b = centrar(bloque(), 32)
    guardar_serie("bloque_escombro_grande", "bloque_escombro_grande", [rotar(b, -45 * i, 32) for i in range(8)])
    tonos = [((150, 140, 126, 255), (186, 176, 160, 255), (110, 102, 92, 255)),
             ((132, 132, 140, 255), (170, 170, 178, 255), (96, 96, 106, 255)),
             ((156, 128, 100, 255), (192, 164, 134, 255), (116, 92, 70, 255))]
    for n in range(3):
        r = centrar(roca(10 + n, tonos[n]), 24)
        guardar_serie(f"roca_lluvia_{n + 1}", f"roca_lluvia_{n + 1}", [rotar(r, -45 * i, 24) for i in range(8)])
    l = centrar(ladrillo(), 16)
    guardar_serie("escombro_ladrillo", "escombro_ladrillo", [rotar(l, -45 * i, 16) for i in range(4)])
    print("proyectiles actualizados")


if __name__ == "__main__":
    main()
