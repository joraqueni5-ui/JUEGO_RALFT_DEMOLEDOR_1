"""Controles táctiles con el estilo del juego (pixel art, azul noche + amarillo, contorno oscuro).

Salida: Assets/Resources/Extras/Controles/
  base_joystick, palanca           palanca analógica (la base aparece donde apoyas el pulgar)
  boton, boton_apretado            botón redondo azul (pausa, mapa, flechas)
  boton_accion, boton_accion_apretado   botón grande amarillo (martillo)
  icono_martillo, icono_pausa, icono_mapa, icono_flecha, icono_saltar
Uso: python Tools/generar_controles_movil.py
"""
import math
from PIL import Image, ImageDraw
import exportar_extras as ex

NOCHE = (11, 19, 48)
AZUL = (36, 64, 122)
AZUL_C = (60, 111, 209)
AMARILLO = (255, 194, 26)
AMARILLO_C = (255, 226, 122)
NARANJA = (255, 122, 26)
CONTORNO = (10, 15, 36)
CREMA = (255, 244, 220)


def circulo(d, c, r, color):
    d.ellipse([c - r, c - r, c + r, c + r], fill=color)


def base_joystick():
    n = 128
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = n // 2
    circulo(d, c, 62, (*CONTORNO, 230))
    circulo(d, c, 59, (*AMARILLO, 235))
    circulo(d, c, 55, (*CONTORNO, 230))
    circulo(d, c, 53, (*NOCHE, 150))
    circulo(d, c, 30, (*AZUL, 90))
    for k in range(4):   # cheurones en las 4 direcciones
        a = k * math.pi / 2
        ux, uy = math.cos(a), -math.sin(a)
        px, py = -uy, ux
        p = (c + ux * 44, c + uy * 44)
        pts = [(p[0] + ux * 6, p[1] + uy * 6), (p[0] - ux * 2 + px * 8, p[1] - uy * 2 + py * 8),
               (p[0] - ux * 2 - px * 8, p[1] - uy * 2 - py * 8)]
        d.polygon(pts, fill=(*AMARILLO_C, 220))
    return im


def palanca():
    n = 64
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = n // 2
    circulo(d, c, 30, (*CONTORNO, 255))
    circulo(d, c, 27, (*NARANJA, 255))
    d.ellipse([c - 27, c - 27, c + 27, c + 20], fill=(*AMARILLO, 255))
    d.ellipse([c - 18, c - 22, c + 4, c - 8], fill=(*AMARILLO_C, 255))
    d.rectangle([c - 13, c - 18, c - 8, c - 14], fill=(255, 255, 255, 255))
    return im


def boton(cuerpo, claro, apretado=False):
    n = 96
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = n // 2
    caida = 3 if apretado else 7
    # sombra / costado
    d.ellipse([4, 4 + caida, n - 5, n - 5], fill=(*CONTORNO, 255))
    d.ellipse([7, 7 + caida, n - 8, n - 8], fill=(int(cuerpo[0] * 0.55), int(cuerpo[1] * 0.55), int(cuerpo[2] * 0.55), 255))
    # cara
    top = 4 + (caida - 3 if apretado else 0)
    d.ellipse([4, top, n - 5, n - 5 - caida], fill=(*CONTORNO, 255))
    d.ellipse([7, top + 3, n - 8, n - 8 - caida], fill=(*cuerpo, 255))
    d.ellipse([12, top + 7, n - 26, top + 30], fill=(*claro, 255))   # brillo
    d.ellipse([7, top + 3, n - 8, n - 8 - caida], outline=(*AMARILLO, 255) if cuerpo != AMARILLO else (*CREMA, 255), width=3)
    return im


def icono_martillo():
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    mango = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    md = ImageDraw.Draw(mango)
    md.rectangle([19, 18, 29, 47], fill=(*CONTORNO, 255))
    md.rectangle([21, 20, 27, 45], fill=(170, 110, 60, 255))
    md.rectangle([21, 20, 22, 45], fill=(210, 150, 90, 255))
    md.rectangle([3, 3, 45, 22], fill=(*CONTORNO, 255))
    md.rectangle([5, 5, 43, 20], fill=(196, 204, 220, 255))
    md.rectangle([5, 5, 43, 8], fill=(240, 244, 255, 255))
    md.rectangle([38, 5, 43, 20], fill=(140, 148, 166, 255))
    return mango.rotate(-30, resample=Image.NEAREST, expand=False, center=(24, 26))


def icono_pausa():
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for x in (12, 27):
        d.rectangle([x, 10, x + 9, 38], fill=(*CONTORNO, 255))
        d.rectangle([x + 2, 12, x + 7, 36], fill=(*CREMA, 255))
    return im


def icono_mapa():
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pts = [(6, 12), (17, 8), (30, 12), (42, 8), (42, 38), (30, 42), (17, 38), (6, 42)]
    d.polygon(pts, fill=(*CONTORNO, 255))
    d.polygon([(9, 14), (17, 11), (17, 36), (9, 39)], fill=(*CREMA, 255))
    d.polygon([(19, 11), (29, 14), (29, 39), (19, 36)], fill=(230, 216, 180, 255))
    d.polygon([(31, 14), (39, 11), (39, 36), (31, 39)], fill=(*CREMA, 255))
    d.ellipse([21, 18, 27, 24], fill=(226, 59, 46, 255))
    d.line([(24, 24), (24, 29)], fill=(226, 59, 46, 255), width=2)
    return im


def icono_flecha():
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(24, 6), (42, 26), (31, 26), (31, 42), (17, 42), (17, 26), (6, 26)], fill=(*CONTORNO, 255))
    d.polygon([(24, 10), (37, 24), (28, 24), (28, 39), (20, 39), (20, 24), (11, 24)], fill=(*CREMA, 255))
    return im


def icono_saltar():
    im = icono_flecha()
    d = ImageDraw.Draw(im)
    for x in (8, 38):
        d.line([(x, 30), (x, 40)], fill=(*AMARILLO, 255), width=3)
    return im


def icono_furia():
    """Rayo de la furia."""
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pts = [(28, 3), (10, 27), (21, 27), (16, 45), (38, 19), (26, 19), (32, 3)]
    d.polygon(pts, fill=(*CONTORNO, 255))
    pts2 = [(28, 7), (14, 25), (24, 25), (20, 39), (34, 21), (23, 21), (29, 7)]
    d.polygon(pts2, fill=(*AMARILLO, 255))
    d.line([(27, 9), (17, 23)], fill=(*CREMA, 255), width=2)
    return im


def disco_relleno():
    """Disco blanco para la carga de la furia (el juego lo tiñe y lo llena en círculo)."""
    n = 96
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([7, 7, n - 8, n - 15], fill=(255, 255, 255, 255))
    return im


def main():
    ex.escribir("Controles/icono_furia", icono_furia(), 32)
    ex.escribir("Controles/disco_relleno", disco_relleno(), 32)
    ex.escribir("Controles/base_joystick", base_joystick(), 32)
    ex.escribir("Controles/palanca", palanca(), 32)
    ex.escribir("Controles/boton", boton(AZUL, AZUL_C), 32)
    ex.escribir("Controles/boton_apretado", boton(AZUL, AZUL_C, True), 32)
    ex.escribir("Controles/boton_accion", boton(AMARILLO, AMARILLO_C), 32)
    ex.escribir("Controles/boton_accion_apretado", boton(AMARILLO, AMARILLO_C, True), 32)
    ex.escribir("Controles/icono_martillo", icono_martillo(), 32)
    ex.escribir("Controles/icono_pausa", icono_pausa(), 32)
    ex.escribir("Controles/icono_mapa", icono_mapa(), 32)
    ex.escribir("Controles/icono_flecha", icono_flecha(), 32)
    ex.escribir("Controles/icono_saltar", icono_saltar(), 32)


if __name__ == "__main__":
    main()
