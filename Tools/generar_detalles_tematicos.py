"""Detalles temáticos por piso (32x32, vistos desde arriba): lo que deja cada carrera en el suelo.

  Planta baja:            maceta, folleto, paraguas, termo
  Gastronomía/Enfermería: olla, cucharon, botiquin, venda, tabla_picar
  Informática:            teclado, mouse_pc, cable, laptop, disco
  Aulas 300-500:          tiza, borrador, examen, calculadora, regla, audifonos
  Terraza/Biblioteca:     pila_libros, hoja_seca, maceta, lentes
Salida: Assets/Resources/Extras/Detalles/<nombre>.png  (uso: python Tools/generar_detalles_tematicos.py)
"""
import math
import random
from PIL import Image, ImageDraw
from generar_ira_y_detalles import lienzo, contorno, ex


def maceta():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.ellipse([8, 9, 24, 25], fill=(170, 86, 50, 255))
    d.ellipse([10, 11, 22, 23], fill=(90, 60, 40, 255))
    for a in range(0, 360, 45):
        x = 16 + math.cos(math.radians(a)) * 8; y = 17 + math.sin(math.radians(a)) * 8
        d.ellipse([x - 4, y - 3, x + 4, y + 3], fill=(60, 150, 70, 255))
    d.ellipse([12, 13, 20, 21], fill=(90, 180, 90, 255))
    return contorno(im)


def folleto():
    im = lienzo(); d = ImageDraw.Draw(im)
    h = Image.new("RGBA", (14, 18), (250, 220, 70, 255)); hd = ImageDraw.Draw(h)
    hd.rectangle([2, 2, 11, 6], fill=(220, 60, 50, 255))
    for y in (9, 12, 15): hd.line([2, y, 11, y], fill=(90, 70, 40, 255))
    im.alpha_composite(h.rotate(-20, expand=True, resample=Image.NEAREST), (8, 6))
    return contorno(im, (60, 50, 40, 200))


def paraguas():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.pieslice([5, 5, 27, 27], 0, 360, fill=(60, 60, 150, 255))
    for a in range(0, 360, 60):
        d.line([16, 16, 16 + math.cos(math.radians(a)) * 11, 16 + math.sin(math.radians(a)) * 11], fill=(40, 40, 110, 255))
    d.ellipse([14, 14, 18, 18], fill=(220, 220, 230, 255))
    return contorno(im)


def termo():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([8, 13, 25, 19], radius=3, fill=(40, 150, 140, 255))
    d.rectangle([22, 13, 25, 19], fill=(30, 40, 50, 255))
    d.line([10, 14, 20, 14], fill=(140, 220, 210, 255))
    return contorno(im)


def olla():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.ellipse([7, 8, 25, 26], fill=(170, 176, 186, 255))
    d.ellipse([9, 10, 23, 24], fill=(120, 126, 136, 255))
    d.ellipse([12, 13, 20, 21], fill=(200, 120, 60, 255))   # algo cocinándose
    d.rectangle([3, 15, 7, 18], fill=(40, 40, 46, 255)); d.rectangle([25, 15, 29, 18], fill=(40, 40, 46, 255))
    return contorno(im)


def cucharon():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.line([6, 24, 19, 11], fill=(190, 196, 206, 255), width=2)
    d.ellipse([17, 5, 27, 15], fill=(200, 206, 216, 255)); d.ellipse([19, 7, 25, 13], fill=(150, 156, 166, 255))
    return contorno(im)


def botiquin():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([7, 9, 25, 24], radius=2, fill=(240, 240, 240, 255))
    d.rectangle([14, 11, 18, 22], fill=(220, 40, 50, 255)); d.rectangle([10, 15, 22, 18], fill=(220, 40, 50, 255))
    d.rectangle([13, 6, 19, 9], fill=(120, 120, 130, 255))
    return contorno(im)


def venda():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.ellipse([6, 10, 16, 20], fill=(246, 240, 228, 255)); d.ellipse([9, 13, 13, 17], fill=(210, 200, 190, 255))
    d.line([15, 16, 27, 20], fill=(246, 240, 228, 255), width=3)
    return contorno(im, (90, 80, 80, 200))


def tabla_picar():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([6, 8, 26, 24], radius=3, fill=(196, 150, 96, 255))
    d.ellipse([22, 14, 25, 17], fill=(120, 80, 50, 255))
    for x, c in ((10, (220, 60, 50)), (14, (90, 170, 70)), (12, (240, 160, 40))):
        d.ellipse([x, 12 + (x % 3), x + 3, 15 + (x % 3)], fill=(*c, 255))
    return contorno(im)


def teclado():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([4, 11, 28, 22], radius=2, fill=(50, 54, 64, 255))
    for y in (13, 16, 19):
        for x in range(6, 26, 3):
            d.rectangle([x, y, x + 1, y + 1], fill=(170, 176, 190, 255))
    d.rectangle([11, 19, 20, 20], fill=(170, 176, 190, 255))
    return contorno(im)


def mouse_pc():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.ellipse([11, 9, 21, 24], fill=(220, 224, 232, 255)); d.line([16, 9, 16, 15], fill=(140, 144, 156, 255))
    d.line([16, 9, 16, 3], fill=(40, 40, 46, 255)); d.line([16, 3, 24, 2], fill=(40, 40, 46, 255))
    return contorno(im)


def cable():
    im = lienzo(); d = ImageDraw.Draw(im)
    pts = [(2, 20), (8, 14), (14, 18), (20, 10), (26, 15), (30, 12)]
    d.line(pts, fill=(30, 30, 36, 255), width=2)
    d.rectangle([26, 10, 30, 14], fill=(200, 200, 210, 255))
    return contorno(im, (80, 80, 90, 160))


def laptop():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([5, 7, 27, 18], radius=1, fill=(60, 64, 76, 255)); d.rectangle([7, 9, 25, 16], fill=(70, 150, 220, 255))
    d.line([9, 11, 18, 11], fill=(200, 240, 255, 255)); d.line([9, 13, 15, 13], fill=(140, 255, 160, 255))
    d.rounded_rectangle([4, 18, 28, 25], radius=1, fill=(150, 156, 168, 255))
    for x in range(7, 26, 3): d.point((x, 21), fill=(90, 96, 110, 255))
    return contorno(im)


def disco():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.ellipse([8, 8, 24, 24], fill=(200, 210, 230, 255)); d.arc([9, 9, 23, 23], 200, 260, fill=(255, 140, 220, 255))
    d.arc([10, 10, 22, 22], 20, 80, fill=(120, 230, 255, 255)); d.ellipse([14, 14, 18, 18], fill=(0, 0, 0, 0))
    return contorno(im)


def tiza():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.line([8, 18, 18, 14], fill=(250, 250, 250, 255), width=2); d.line([14, 22, 22, 22], fill=(250, 200, 220, 255), width=2)
    for _ in range(10):
        d.point((random.randint(6, 24), random.randint(12, 26)), fill=(240, 240, 240, 140))
    return contorno(im, (80, 80, 90, 160))


def borrador():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([8, 11, 24, 20], radius=2, fill=(150, 100, 60, 255)); d.rectangle([8, 18, 24, 21], fill=(80, 80, 90, 255))
    return contorno(im)


def examen():
    im = lienzo()
    h = Image.new("RGBA", (15, 19), (250, 250, 246, 255)); hd = ImageDraw.Draw(h)
    for y in range(4, 17, 3): hd.line([2, y, 12, y], fill=(150, 160, 190, 255))
    hd.text((8, 0), "", fill=(0, 0, 0, 255))
    hd.ellipse([9, 1, 14, 6], outline=(220, 40, 40, 255))
    hd.line([3, 2, 6, 2], fill=(40, 40, 120, 255))
    im.alpha_composite(h.rotate(12, expand=True, resample=Image.NEAREST), (8, 5))
    return contorno(im, (60, 60, 70, 180))


def calculadora():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.rounded_rectangle([10, 6, 22, 26], radius=2, fill=(50, 54, 64, 255)); d.rectangle([12, 8, 20, 12], fill=(170, 210, 150, 255))
    for y in (15, 18, 21, 24):
        for x in (12, 15, 18):
            d.rectangle([x, y, x + 1, y + 1], fill=(220, 220, 228, 255))
    return contorno(im)


def regla():
    im = lienzo(); d = ImageDraw.Draw(im)
    r = Image.new("RGBA", (26, 6), (240, 210, 80, 255)); rd = ImageDraw.Draw(r)
    for x in range(1, 26, 2): rd.line([x, 0, x, 2 if x % 4 else 3], fill=(70, 60, 30, 255))
    im.alpha_composite(r.rotate(30, expand=True, resample=Image.NEAREST), (4, 6))
    return contorno(im)


def audifonos():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.arc([7, 7, 25, 25], 180, 360, fill=(30, 30, 36, 255), width=2)
    d.rounded_rectangle([5, 14, 10, 22], radius=2, fill=(220, 60, 90, 255)); d.rounded_rectangle([22, 14, 27, 22], radius=2, fill=(220, 60, 90, 255))
    return contorno(im)


def pila_libros():
    im = lienzo(); d = ImageDraw.Draw(im)
    for i, c in enumerate([(60, 100, 170), (170, 60, 60), (70, 140, 80), (200, 150, 50)]):
        y = 20 - i * 4
        d.rectangle([7 + i, y, 25 - i % 2, y + 4], fill=(*c, 255))
        d.line([8 + i, y + 2, 24 - i % 2, y + 2], fill=(240, 236, 220, 255))
    return contorno(im)


def hoja_seca():
    im = lienzo(); d = ImageDraw.Draw(im)
    for (x, y, c) in ((10, 12, (200, 120, 40)), (17, 17, (180, 80, 30)), (13, 20, (220, 170, 60))):
        d.ellipse([x, y, x + 7, y + 4], fill=(*c, 255)); d.line([x, y + 2, x + 7, y + 2], fill=(120, 70, 30, 255))
    return contorno(im, (70, 50, 30, 180))


def lentes():
    im = lienzo(); d = ImageDraw.Draw(im)
    d.ellipse([6, 12, 14, 19], outline=(30, 30, 36, 255), width=2); d.ellipse([18, 12, 26, 19], outline=(30, 30, 36, 255), width=2)
    d.line([14, 14, 18, 14], fill=(30, 30, 36, 255)); d.point((9, 14), fill=(200, 230, 255, 255)); d.point((21, 14), fill=(200, 230, 255, 255))
    return im


TODOS = [maceta, folleto, paraguas, termo, olla, cucharon, botiquin, venda, tabla_picar, teclado, mouse_pc, cable, laptop,
         disco, tiza, borrador, examen, calculadora, regla, audifonos, pila_libros, hoja_seca, lentes]

if __name__ == "__main__":
    random.seed(11)
    for f in TODOS:
        ex.escribir("Detalles/" + f.__name__, f(), 32)
    print(len(TODOS), "detalles")
