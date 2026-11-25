"""Efectos y piezas extra dibujados por código (sin modelo de imagen).

Genera en Tools/sprites_out/fx/:
  onda_empujon_0..3.png   (48x48) onda del empujón del demoledor
  humo_poof_0..3.png      (32x32) nube al vencer a un mini villano
  alerta_0..1.png         (12x18) "!" sobre el demoledor antes de empujar
  hielo_0.png             (32x32) cristales sobre un enemigo congelado
"""
import math, os, random
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "sprites_out", "fx")
os.makedirs(OUT, exist_ok=True)
K = (22, 18, 28, 255)

def onda():
    for i in range(4):
        im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
        px = im.load()
        r = 7 + i * 5.5
        grosor = 3.2 - i * 0.6
        alfa = [255, 230, 170, 100][i]
        for y in range(48):
            for x in range(48):
                d = math.hypot(x + 0.5 - 24, (y + 0.5 - 24) * 1.35)
                if abs(d - r) < grosor:
                    c = (255, 236, 150, alfa) if d < r else (255, 160, 40, alfa)
                    px[x, y] = c
                elif abs(d - r) < grosor + 1.0 and i < 3:
                    px[x, y] = (120, 60, 20, int(alfa * 0.8))
        # chispas
        rnd = random.Random(10 + i)
        for _ in range(6 - i):
            a = rnd.random() * math.tau
            rr = r + 2 + rnd.random() * 3
            x, y = int(24 + math.cos(a) * rr), int(24 + math.sin(a) * rr / 1.35)
            if 0 <= x < 47 and 0 <= y < 47:
                px[x, y] = (255, 255, 230, alfa); px[x + 1, y] = (255, 210, 90, alfa)
        im.save(os.path.join(OUT, f"onda_empujon_{i}.png"))

def poof():
    base = [(10, 18, 6), (20, 17, 6), (15, 12, 7), (14, 20, 5), (22, 12, 4)]
    for i in range(4):
        im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        esc = 0.7 + i * 0.25
        alfa = [255, 255, 200, 120][i]
        for (x, y, r) in base:
            cx, cy = 16 + (x - 16) * esc, 16 + (y - 16) * esc - i
            rr = r * esc
            d.ellipse([cx - rr - 1, cy - rr - 1, cx + rr + 1, cy + rr + 1], fill=(40, 34, 48, alfa))
        for (x, y, r) in base:
            cx, cy = 16 + (x - 16) * esc, 16 + (y - 16) * esc - i
            rr = r * esc
            d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=(206, 204, 214, alfa))
            d.ellipse([cx - rr * 0.6, cy - rr * 0.8, cx + rr * 0.3, cy - rr * 0.1], fill=(246, 246, 250, alfa))
        if i < 2:
            for (sx, sy) in [(4, 6), (27, 8), (6, 26), (26, 25)]:
                d.point((sx + (i * 2 if sx > 16 else -i * 2), sy), fill=(255, 220, 80, 255))
        im.save(os.path.join(OUT, f"humo_poof_{i}.png"))

def alerta():
    filas = ["..kkkk..", ".kyyyyk.", ".kyyyyk.", ".kyyyyk.", ".kyyyyk.", ".kyyyyk.", "..kyyk..", "..kyyk..",
             "..kyyk..", "...kk...", "........", "..kkkk..", ".kyyyyk.", ".kyyyyk.", "..kkkk.."]
    for i, col in enumerate([(255, 214, 40, 255), (255, 70, 40, 255)]):
        im = Image.new("RGBA", (12, 18), (0, 0, 0, 0))
        px = im.load()
        for y, f in enumerate(filas):
            for x, ch in enumerate(f):
                if ch == "k": px[x + 2, y + 1] = K
                elif ch == "y": px[x + 2, y + 1] = col
        im.save(os.path.join(OUT, f"alerta_{i}.png"))

def hielo():
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for (x, y, h) in [(6, 22, 10), (12, 24, 14), (19, 23, 12), (25, 24, 9), (9, 14, 7), (22, 15, 8)]:
        pts = [(x - 3, y), (x, y - h), (x + 3, y)]
        d.polygon(pts, fill=(150, 220, 255, 200), outline=(40, 90, 150, 255))
        d.line([(x - 1, y - 2), (x, y - h + 2)], fill=(240, 252, 255, 230))
    im.save(os.path.join(OUT, "hielo_0.png"))

if __name__ == "__main__":
    onda(); poof(); alerta(); hielo()
    print("ok", sorted(os.listdir(OUT)))
