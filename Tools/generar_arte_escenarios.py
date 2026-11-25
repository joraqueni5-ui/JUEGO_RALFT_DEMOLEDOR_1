"""Arte para dar más vida a los escenarios (pixel art por código):

  Vista/      ventanas con vista a La Paz (Illimani, laderas con casas, teleférico) según la hora de cada piso
  Vida/       monitor con latido, pantallas con código, diapositivas, libros, papel, notas musicales, haz del proyector
  Dano/       grietas y montones de escombros (el edificio se va rompiendo)
  Carteles/   afiches y placas (el texto lo pone el juego encima)
  Ascensor/   puertas del ascensor y el interior para el viaje entre pisos
  Fachada/    montañas, ciudad lejana y cercana, autos y minibús, cabina del teleférico

Salida: Assets/Resources/Extras/  (uso: python Tools/generar_arte_escenarios.py)
"""
import math
import random

from PIL import Image, ImageDraw

import exportar_extras as ex

K = (16, 14, 24, 255)


def lienzo(w, h, fondo=(0, 0, 0, 0)):
    return Image.new("RGBA", (w, h), fondo)


def mezcla(a, b, k):
    return tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(3)) + (255,)


def degradado(d, x0, y0, x1, y1, arriba, abajo):
    for y in range(y0, y1 + 1):
        k = (y - y0) / max(1, y1 - y0)
        d.line([(x0, y), (x1, y)], fill=mezcla(arriba, abajo, k))


# Hora de cada piso: (cielo arriba, cielo abajo, nieve, montaña, casas, luces)
HORAS = {
    0: ((120, 190, 245), (215, 238, 255), (250, 252, 255), (120, 110, 150), (190, 110, 80), False),   # mañana
    1: ((110, 185, 245), (210, 236, 255), (250, 252, 255), (118, 108, 148), (192, 112, 82), False),
    2: ((95, 175, 240), (200, 232, 255), (248, 250, 255), (112, 104, 142), (186, 108, 80), False),    # mediodía
    3: ((110, 170, 230), (240, 226, 200), (255, 250, 240), (120, 100, 130), (196, 112, 78), False),   # tarde
    4: ((120, 160, 215), (246, 214, 165), (255, 240, 220), (125, 98, 122), (200, 110, 74), False),
    5: ((150, 140, 200), (248, 196, 128), (255, 225, 190), (118, 88, 112), (190, 100, 70), False),   # dorada
    6: ((95, 85, 165), (245, 140, 95), (255, 200, 170), (90, 66, 100), (150, 80, 70), True),        # atardecer
    9: ((14, 20, 50), (45, 50, 95), (190, 200, 230), (40, 42, 70), (60, 50, 70), True),             # noche
}


def vista(piso):
    """Ventana de 52 × 34 con marco de aluminio: cielo, Illimani nevado, laderas con casas y el teleférico."""
    w, h = 52, 34
    im = lienzo(w, h)
    d = ImageDraw.Draw(im)
    cielo_a, cielo_b, nieve, monte, casas, luces = HORAS[piso]
    gx0, gy0, gx1, gy1 = 3, 3, w - 4, h - 4
    degradado(d, gx0, gy0, gx1, gy1, cielo_a, cielo_b)
    rng = random.Random(100 + piso)
    if piso in (6, 9):
        for _ in range(12 if piso == 9 else 4):
            d.point((rng.randint(gx0, gx1), rng.randint(gy0, gy0 + 8)), fill=(255, 255, 230, 255))
    if piso == 6:
        d.ellipse([gx1 - 14, gy1 - 14, gx1 - 4, gy1 - 4], fill=(255, 190, 110, 255))   # sol que se pone
    # Más alto el piso: el horizonte más abajo (se ve más cielo y las casas más chicas).
    horizonte = gy1 - 6 - piso * 2 if piso <= 6 else gy1 - 14
    horizonte = max(gy0 + 10, min(gy1 - 4, horizonte + 10))
    # Illimani: tres picos nevados.
    base = horizonte - 2
    picos = [(gx0 + 12, base - 13), (gx0 + 20, base - 16), (gx0 + 28, base - 12)]
    d.polygon([(gx0, base + 2), picos[0], (gx0 + 16, base - 9), picos[1], (gx0 + 24, base - 10), picos[2], (gx1, base + 2)], fill=monte + (255,))
    for (px, py) in picos:
        d.polygon([(px - 4, py + 5), (px, py), (px + 4, py + 5), (px + 1, py + 4), (px - 1, py + 6)], fill=nieve + (255,))
    # Laderas con casas de ladrillo (más chicas cuanto más alto el piso).
    tam = max(1, 3 - piso // 3)
    for fila in range(4):
        y = horizonte + fila * (tam + 1)
        for x in range(gx0, gx1, tam + 1 + rng.randint(0, 1)):
            if y > gy1:
                break
            c = casas if rng.random() < 0.8 else (230, 220, 200)
            d.rectangle([x, y, x + tam, y + tam], fill=mezcla(c, (0, 0, 0), rng.random() * 0.25))
            if luces and rng.random() < 0.3:
                d.point((x + tam // 2, y + tam // 2), fill=(255, 220, 120, 255))
    # Cable del teleférico cruzando.
    cy0, cy1 = gy0 + 8, gy0 + 14
    d.line([(gx0, cy0), (gx1, cy1)], fill=(60, 60, 70, 255))
    # Marco de aluminio y travesaño.
    d.rectangle([0, 0, w - 1, h - 1], outline=(170, 178, 190, 255), width=3)
    d.rectangle([1, 1, w - 2, h - 2], outline=(220, 226, 235, 255), width=1)
    d.line([(w // 2, 2), (w // 2, h - 3)], fill=(190, 198, 210, 255), width=2)
    d.line([(gx0 + 3, gy0 + 2), (gx0 + 9, gy0 + 2)], fill=(255, 255, 255, 150))   # reflejo
    d.rectangle([0, h - 3, w - 1, h - 1], fill=(150, 156, 168, 255))              # repisa
    return im


def cabina():
    im = lienzo(6, 7)
    d = ImageDraw.Draw(im)
    d.line([(3, 0), (3, 2)], fill=(50, 50, 60, 255))
    d.rectangle([0, 2, 5, 6], fill=(230, 60, 50, 255))
    d.rectangle([1, 3, 4, 4], fill=(200, 230, 255, 255))
    return im


# ------------------------------------------------------------------ vida
def ecg(i):
    im = lienzo(16, 12)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, 15, 11], 2, fill=(40, 44, 54, 255), outline=(20, 20, 26, 255))
    d.rectangle([2, 2, 13, 8], fill=(10, 30, 20, 255))
    # Línea con un latido que avanza.
    pts = []
    for x in range(2, 14):
        fase = (x - 2 - i * 1.5) % 12
        y = 6
        if 4 <= fase < 5: y = 3
        elif 5 <= fase < 6: y = 8
        elif 6 <= fase < 7: y = 5
        pts.append((x, y))
    d.line(pts, fill=(80, 255, 120, 255))
    d.point((13, 10), fill=(255, 80, 80, 255) if i % 4 < 2 else (80, 20, 20, 255))
    return im


def ecg_plano():
    im = lienzo(16, 12)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, 15, 11], 2, fill=(40, 44, 54, 255), outline=(20, 20, 26, 255))
    d.rectangle([2, 2, 13, 8], fill=(30, 10, 10, 255))
    d.line([(2, 6), (13, 6)], fill=(255, 70, 70, 255))
    return im


def codigo(i):
    im = lienzo(14, 10)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 13, 9], fill=(12, 16, 30, 255), outline=(30, 34, 50, 255))
    rng = random.Random(i)
    colores = [(120, 220, 255), (255, 200, 90), (140, 255, 140), (230, 130, 255)]
    for fila in range(4):
        y = 2 + fila * 2
        x = 2 + (rng.randint(0, 2) if fila % 2 else 0)
        largo = rng.randint(3, 9)
        d.line([(x, y), (min(11, x + largo), y)], fill=colores[(fila + i) % 4] + (255,))
    return im


def diapositiva(i):
    im = lienzo(20, 12)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 19, 11], fill=(240, 244, 250, 255), outline=(40, 40, 50, 255))
    d.rectangle([1, 1, 18, 3], fill=(240, 120, 30, 255))   # franja naranja
    if i == 0:
        for k, alto in enumerate([3, 5, 4, 7]):
            d.rectangle([3 + k * 4, 10 - alto, 5 + k * 4, 10], fill=[(60, 140, 230), (240, 120, 30), (90, 190, 90), (230, 80, 80)][k] + (255,))
    elif i == 1:
        d.ellipse([3, 4, 9, 10], fill=(240, 120, 30, 255)); d.pieslice([3, 4, 9, 10], 0, 120, fill=(60, 140, 230, 255))
        for k in range(3): d.line([(11, 5 + k * 2), (17, 5 + k * 2)], fill=(80, 80, 90, 255))
    else:
        for k in range(3): d.line([(3, 5 + k * 2), (16 - k * 2, 5 + k * 2)], fill=(80, 80, 90, 255))
    return im


def libro(i):
    c = [(200, 60, 60), (60, 120, 200), (70, 160, 90), (230, 170, 50)][i]
    im = lienzo(6, 8)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 5, 7], fill=c + (255,), outline=K)
    d.line([(1, 1), (1, 6)], fill=mezcla(c, (255, 255, 255), 0.4))
    d.rectangle([2, 2, 4, 3], fill=(250, 240, 210, 255))
    return im


def papel():
    im = lienzo(6, 8)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 5, 7], fill=(250, 250, 245, 255), outline=(170, 170, 180, 255))
    for y in (2, 4, 6): d.line([(1, y), (4, y)], fill=(150, 160, 190, 255))
    return im


def nota(i):
    im = lienzo(7, 9)
    d = ImageDraw.Draw(im)
    c = (255, 220, 90, 255) if i == 0 else (130, 220, 255, 255)
    d.ellipse([0, 5, 3, 8], fill=c)
    d.line([(3, 6), (3, 0)], fill=c, width=1)
    if i == 0: d.line([(3, 0), (6, 2)], fill=c)
    else:
        d.ellipse([3, 4, 6, 7], fill=c); d.line([(6, 5), (6, 0)], fill=c); d.line([(3, 0), (6, 0)], fill=c)
    return im


def haz():
    im = lienzo(44, 24)
    d = ImageDraw.Draw(im)
    for k in range(10):
        a = 90 - k * 8
        d.polygon([(0, 12), (43, 12 - 2 - k), (43, 12 + 2 + k)], fill=(255, 250, 210, max(6, a // 4)))
    return im


# ------------------------------------------------------------------ daño
def grieta(i):
    rng = random.Random(200 + i)
    im = lienzo(28, 28)
    d = ImageDraw.Draw(im)
    for rama in range(3 + i):
        x, y = 14, 14
        ang = rng.uniform(0, math.tau)
        pts = [(x, y)]
        for paso in range(4 + i):
            ang += rng.uniform(-0.6, 0.6)
            x += math.cos(ang) * rng.uniform(2, 4)
            y += math.sin(ang) * rng.uniform(2, 4)
            pts.append((x, y))
        d.line(pts, fill=(30, 24, 26, 230), width=2 if rama == 0 else 1)
        d.line([(p[0] + 1, p[1]) for p in pts], fill=(255, 255, 255, 50))
    return im


def escombros(i):
    rng = random.Random(300 + i)
    im = lienzo(30, 18)
    d = ImageDraw.Draw(im)
    for _ in range(14 + i * 3):
        x = rng.randint(2, 26); y = rng.randint(6, 15)
        s = rng.randint(2, 4)
        c = rng.choice([(150, 140, 130), (120, 112, 104), (180, 170, 160), (200, 120, 90)])
        d.polygon([(x, y), (x + s, y - rng.randint(1, s)), (x + s + 1, y + 1), (x + 1, y + s // 2 + 1)], fill=c + (255,), outline=(60, 54, 50, 255))
    d.line([(6, 9), (20, 7)], fill=(160, 120, 70, 255), width=2)   # tabla
    return im


# ------------------------------------------------------------------ carteles
def cartel(i):
    colores = [(240, 120, 30), (60, 120, 210), (70, 170, 90), (210, 60, 60)]
    c = colores[i]
    im = lienzo(34, 42)
    d = ImageDraw.Draw(im)
    d.rectangle([1, 2, 33, 41], fill=(0, 0, 0, 60))
    d.rectangle([0, 0, 32, 40], fill=(250, 246, 232, 255), outline=(70, 60, 50, 255))
    d.rectangle([1, 1, 31, 9], fill=c + (255,))
    d.ellipse([14, -1, 18, 3], fill=(220, 40, 40, 255))   # chinche
    d.polygon([(26, 40), (32, 34), (32, 40)], fill=(225, 218, 200, 255))   # esquina doblada
    return im


def letrero():
    im = lienzo(110, 22)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, 109, 21], 3, fill=(240, 120, 30, 255), outline=(120, 50, 10, 255))
    d.rounded_rectangle([2, 2, 107, 19], 2, outline=(255, 190, 120, 255))
    return im


def placa():
    im = lienzo(56, 18)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 55, 17], fill=(30, 40, 70, 255), outline=(240, 120, 30, 255))
    d.rectangle([1, 1, 54, 2], fill=(240, 120, 30, 255))
    return im


# ------------------------------------------------------------------ ascensor
def puerta():
    im = lienzo(16, 34)
    d = ImageDraw.Draw(im)
    degradado(d, 0, 0, 15, 33, (200, 206, 216), (150, 156, 168))
    d.rectangle([0, 0, 15, 33], outline=(90, 96, 108, 255))
    for y in range(4, 32, 6): d.line([(2, y), (13, y)], fill=(215, 220, 230, 255))
    return im


def interior():
    w, h = 480, 270
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    degradado(d, 0, 0, w, 210, (150, 158, 172), (110, 116, 130))
    for x in range(0, w, 40):
        d.line([(x, 0), (x, 210)], fill=(130, 138, 152, 255))
        d.line([(x + 2, 0), (x + 2, 210)], fill=(175, 182, 196, 255))
    d.rectangle([0, 210, w, h], fill=(110, 76, 50, 255))
    for x in range(0, w, 24): d.line([(x, 210), (x - 20, h)], fill=(95, 64, 42, 255))
    d.rectangle([0, 150, w, 158], fill=(190, 160, 80, 255))      # pasamanos
    d.rectangle([0, 158, w, 160], fill=(130, 100, 40, 255))
    # Panel de botones.
    d.rectangle([400, 50, 450, 180], fill=(60, 64, 74, 255), outline=(30, 30, 36, 255))
    for fila in range(4):
        for col in range(2):
            d.ellipse([408 + col * 22, 112 + fila * 16, 420 + col * 22, 124 + fila * 16], fill=(210, 210, 200, 255), outline=(40, 40, 40, 255))
    # Visor del número (el juego escribe el número encima).
    d.rectangle([380, 14, 470, 44], fill=(20, 10, 10, 255), outline=(240, 120, 30, 255), width=2)
    # Espejo con reflejo.
    d.rectangle([30, 30, 170, 140], fill=(170, 190, 210, 255), outline=(90, 96, 108, 255), width=3)
    d.line([(50, 40), (90, 120)], fill=(220, 235, 250, 255), width=3)
    # Luces del techo.
    for x in (120, 240, 360): d.ellipse([x - 30, -10, x + 30, 8], fill=(255, 250, 220, 255))
    return im


def puerta_grande():
    w, h = 240, 270
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    degradado(d, 0, 0, w, h, (196, 202, 212), (140, 146, 158))
    d.rectangle([0, 0, w - 1, h - 1], outline=(80, 86, 98, 255), width=3)
    for y in range(20, h, 30): d.line([(10, y), (w - 10, y)], fill=(212, 218, 228, 255))
    return im


# ------------------------------------------------------------------ fachada
def montanas():
    w, h = 320, 110
    im = lienzo(w, h)
    d = ImageDraw.Draw(im)
    rng = random.Random(5)
    pts = [(0, h)]
    x = 0
    while x <= w:
        pts.append((x, rng.randint(55, 80)))
        x += rng.randint(14, 26)
    pts.append((w, h))
    d.polygon(pts, fill=(110, 104, 140, 255))
    # Illimani al centro.
    cx = 170
    d.polygon([(cx - 90, h), (cx - 40, 30), (cx - 20, 40), (cx, 14), (cx + 22, 36), (cx + 42, 24), (cx + 100, h)], fill=(126, 118, 156, 255))
    for (px, py) in ((cx - 40, 30), (cx, 14), (cx + 42, 24)):
        d.polygon([(px - 12, py + 14), (px, py), (px + 12, py + 14), (px + 4, py + 11), (px - 3, py + 16)], fill=(248, 250, 255, 255))
    return im


def ciudad(lejos):
    w, h = 320, 70 if lejos else 90
    im = lienzo(w, h)
    d = ImageDraw.Draw(im)
    rng = random.Random(11 if lejos else 12)
    if lejos:
        d.polygon([(0, h), (0, 30), (80, 18), (160, 34), (240, 20), (320, 28), (320, h)], fill=(150, 110, 100, 255))
        for _ in range(420):
            x = rng.randint(0, w - 3); y = rng.randint(24, h - 3)
            d.rectangle([x, y, x + 2, y + 2], fill=mezcla((190, 110, 80), (230, 220, 200), rng.random() * 0.4))
    else:
        x = 0
        while x < w:
            bw = rng.randint(14, 30); bh = rng.randint(30, 80)
            c = rng.choice([(90, 96, 120), (110, 112, 136), (130, 120, 130), (100, 90, 110)])
            d.rectangle([x, h - bh, x + bw, h], fill=c + (255,))
            for yy in range(h - bh + 4, h - 4, 7):
                for xx in range(x + 3, x + bw - 3, 6):
                    if rng.random() < 0.7:
                        d.rectangle([xx, yy, xx + 2, yy + 3], fill=(255, 220, 130, 255) if rng.random() < 0.35 else (70, 80, 100, 255))
            x += bw + rng.randint(1, 4)
    return im


def auto(i):
    if i == 3:   # minibús paceño
        im = lienzo(34, 16)
        d = ImageDraw.Draw(im)
        d.rounded_rectangle([0, 2, 33, 12], 3, fill=(245, 245, 240, 255), outline=K)
        d.rectangle([1, 7, 32, 8], fill=(40, 130, 200, 255))
        for x in range(4, 30, 6): d.rectangle([x, 4, x + 4, 6], fill=(150, 200, 240, 255))
        d.rectangle([2, 3, 28, 3], fill=(255, 210, 60, 255))
        for x in (6, 26): d.ellipse([x - 3, 10, x + 3, 15], fill=(30, 30, 34, 255))
        return im
    c = [(220, 50, 50), (60, 110, 210), (240, 200, 60)][i]
    im = lienzo(26, 14)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 5, 25, 11], 3, fill=c + (255,), outline=K)
    d.polygon([(6, 5), (9, 1), (18, 1), (21, 5)], fill=mezcla(c, (0, 0, 0), 0.2), outline=K)
    d.polygon([(8, 5), (10, 2), (13, 2), (13, 5)], fill=(170, 210, 240, 255))
    d.polygon([(14, 5), (14, 2), (17, 2), (19, 5)], fill=(170, 210, 240, 255))
    for x in (6, 20): d.ellipse([x - 3, 8, x + 3, 13], fill=(30, 30, 34, 255))
    d.point((24, 7), fill=(255, 240, 160, 255))
    return im


def cabina_grande():
    im = lienzo(12, 14)
    d = ImageDraw.Draw(im)
    d.line([(6, 0), (6, 4)], fill=(50, 50, 60, 255), width=2)
    d.rounded_rectangle([0, 4, 11, 13], 2, fill=(40, 160, 90, 255), outline=K)
    d.rectangle([2, 6, 9, 9], fill=(200, 230, 255, 255))
    return im


def main():
    for p in HORAS:
        ex.escribir(f"Vista/ventana_{p}", vista(p), 32, abajo=True)
    ex.escribir("Vista/cabina", cabina(), 32)
    for i in range(8):
        ex.escribir(f"Vida/ecg_{i}", ecg(i), 32)
    ex.escribir("Vida/ecg_plano", ecg_plano(), 32)
    for i in range(4):
        ex.escribir(f"Vida/codigo_{i}", codigo(i), 32)
        ex.escribir(f"Vida/libro_{i}", libro(i), 32)
        ex.escribir(f"Dano/grieta_{i}", grieta(i), 32)
        ex.escribir(f"Carteles/cartel_{i}", cartel(i), 32, abajo=True)
        ex.escribir(f"Fachada/auto_{i}", auto(i), 32, abajo=True)
    for i in range(3):
        ex.escribir(f"Vida/diapo_{i}", diapositiva(i), 32)
        ex.escribir(f"Dano/escombros_{i}", escombros(i), 32)
    for i in range(2):
        ex.escribir(f"Vida/nota_{i}", nota(i), 32)
    ex.escribir("Vida/papel", papel(), 32)
    ex.escribir("Vida/haz", haz(), 32)
    ex.escribir("Carteles/letrero", letrero(), 32, abajo=True)
    ex.escribir("Carteles/placa", placa(), 32, abajo=True)
    ex.escribir("Ascensor/puerta", puerta(), 32)
    ex.escribir("Ascensor/interior", interior(), 100)
    ex.escribir("Ascensor/puerta_grande", puerta_grande(), 100)
    ex.escribir("Fachada/montanas", montanas(), 32, abajo=True)
    ex.escribir("Fachada/ciudad_lejos", ciudad(True), 32, abajo=True)
    ex.escribir("Fachada/ciudad_cerca", ciudad(False), 32, abajo=True)
    ex.escribir("Fachada/cabina", cabina_grande(), 32)
    print("Arte de escenarios listo")


if __name__ == "__main__":
    main()
