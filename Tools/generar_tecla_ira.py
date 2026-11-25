"""Teclas grandes (K / L) y brillo radial para el aviso de FURIA."""
import math
from PIL import Image, ImageDraw
import exportar_extras as ex

def redondeado(d, caja, r, color):
    d.rounded_rectangle(caja, r, fill=color)

def tecla(presionada=False):
    # 48x52 px en arte pixelado: base oscura + cara superior clara (se ve 3D).
    w, h = 48, 52
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    alto = 4 if presionada else 9          # cuánto se ve el costado
    top = (h - 44) - (0 if presionada else 0)
    off = 5 if presionada else 0
    redondeado(d, (0, 6 + off, w - 1, h - 1), 8, (12, 14, 26, 255))            # contorno/sombra
    redondeado(d, (2, 7 + off, w - 3, h - 3), 7, (70, 74, 96, 255))            # costado
    redondeado(d, (2, 2 + off, w - 3, h - 3 - alto), 7, (12, 14, 26, 255))     # contorno cara
    redondeado(d, (4, 4 + off, w - 5, h - 5 - alto), 6, (236, 238, 246, 255))  # cara
    redondeado(d, (7, 6 + off, w - 8, h - 13 - alto), 5, (250, 251, 255, 255)) # brillo central
    d.line((8, 6 + off, w - 10, 6 + off), fill=(255, 255, 255, 255))
    return im

def brillo():
    n = 64
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    px = im.load()
    c = (n - 1) / 2
    for y in range(n):
        for x in range(n):
            r = math.hypot(x - c, y - c) / c
            a = max(0.0, 1 - r) ** 2
            px[x, y] = (255, 255, 255, int(255 * a))
    return im

def main():
    ex.escribir("Ira/tecla", tecla(False), 32)
    ex.escribir("Ira/tecla_presionada", tecla(True), 32)
    ex.escribir("Ira/brillo_suave", brillo(), 32)

if __name__ == "__main__":
    main()
