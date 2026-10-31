"""Exporta el arte .aseprite del proyecto a sprites PNG listos para Unity.

- Demoledor: caminar izquierda/derecha, salto y golpe.
- Muñeco (Félix): caminar de frente, caminar de espaldas y martillo.
- Nivel: fondo y suelo.
(El edificio alto se dibuja aparte: Assets/Sprites/Edificio.)
"""
import os
import random
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from aseprite_tool import parse, frame_image  # noqa: E402
from unity_assets import ROOT, write_png  # noqa: E402

SRC = r"C:\Users\ALEJANDRO\Desktop\Nueva carpeta (23)"
ARTE = os.path.join(ROOT, "Assets", "Arte")

PPU_EDIFICIO = 80   # escala del fondo y el suelo
PPU_PERSONAJE = 100


def buscar_aseprite(carpeta, inicio):
    d = os.path.join(SRC, carpeta)
    for f in sorted(os.listdir(d)):
        if f.lower().startswith(inicio.lower()):
            return os.path.join(d, f)
    raise FileNotFoundError(inicio)


def cargar(carpeta, inicio):
    return parse(buscar_aseprite(carpeta, inicio))


def recortar_todos(img, caja):
    return img.crop(caja)


def exportar_personaje(nombre, doc, caja, destino, prefijo):
    """Exporta los frames de una animación usando una caja común (alinea los pies)."""
    frames = []
    for i in range(len(doc["frames"])):
        img = frame_image(doc, i).crop(caja)
        frames.append(img)
    for i, img in enumerate(frames):
        write_png(os.path.join(destino, f"{prefijo}_{i}.png"), img, PPU_PERSONAJE, "abajo")
    return len(frames)


def ancho_alto_contenido(doc):
    """Caja que abarca todos los frames de la animación."""
    caja = None
    for i in range(len(doc["frames"])):
        b = frame_image(doc, i).getbbox()
        if b is None:
            continue
        caja = b if caja is None else (min(caja[0], b[0]), min(caja[1], b[1]),
                                       max(caja[2], b[2]), max(caja[3], b[3]))
    return caja


def generar_demoledor():
    print("== Demoledor")
    destino = os.path.join(ARTE, "Demoledor")
    os.makedirs(destino, exist_ok=True)
    for archivo, prefijo in [("animaciion_caminata_izquierda", "caminar_izq"),
                             ("animacion_caminata_derecha", "caminar_der"),
                             ("animacion_salto", "salto"),
                             ("demoledor_golpe", "golpe")]:
        doc = cargar("demoledor", archivo)
        caja = ancho_alto_contenido(doc)
        # Se recorta solo por abajo para que los pies queden en el pivote.
        caja_final = (0, 0, doc["width"], caja[3])
        n = exportar_personaje(archivo, doc, caja_final, destino, prefijo)
        print(f"   {prefijo}: {n} frames {doc['width']}x{caja_final[3]}")


def generar_muneco():
    print("== Muñeco (Félix)")
    destino = os.path.join(ARTE, "Muneco")
    os.makedirs(destino, exist_ok=True)
    for archivo, prefijo in [("Movimiento Abajo", "caminar"),
                             ("Movimiento Espalda1", "espalda"),
                             ("Movimiento martillo", "martillo")]:
        doc = cargar("mueñeco", archivo)
        n = exportar_personaje(archivo, doc, (0, 0, 52, doc["height"]), destino, prefijo)
        print(f"   {prefijo}: {n} frames 52x{doc['height']}")


def generar_nivel():
    print("== Nivel")
    destino = os.path.join(ARTE, "Nivel")
    os.makedirs(destino, exist_ok=True)

    # Fondo: cielo con siluetas de edificios lejanos.
    # 960x540 px a 80 ppu = 12 x 6.75 unidades (cubre el encuadre de la cámara).
    ANCHO, ALTO = 960, 540
    HORIZONTE = 446  # fila donde termina el cielo (= linea del suelo)
    fondo = Image.new("RGBA", (ANCHO, ALTO), (0, 0, 0, 0))
    d = ImageDraw.Draw(fondo)
    for y in range(ALTO):
        t = min(1.0, y / float(HORIZONTE))
        color = (int(104 + t * 100), int(138 + t * 64), int(186 + t * 22), 255)
        d.line([(0, y), (ANCHO, y)], fill=color)

    rnd = random.Random(7)
    x = -20
    while x < ANCHO:
        ancho = rnd.randint(46, 104)
        alto = rnd.randint(70, 150)
        base = HORIZONTE + rnd.randint(0, 8)
        d.rectangle([x, base - alto, x + ancho, base], fill=(150, 164, 184, 255))
        d.rectangle([x, base - alto, x + ancho, base - alto + 4], fill=(168, 180, 198, 255))
        for wy in range(base - alto + 10, base - 8, 16):
            for wx in range(x + 8, x + ancho - 8, 13):
                if rnd.random() < 0.6:
                    d.rectangle([wx, wy, wx + 6, wy + 9], fill=(190, 202, 216, 255))
        x += ancho + rnd.randint(6, 26)

    # Nubes suaves.
    for _ in range(7):
        cx = rnd.randint(0, ANCHO)
        cy = rnd.randint(30, 200)
        for i in range(rnd.randint(3, 6)):
            r = rnd.randint(14, 30)
            d.ellipse([cx + i * 22 - r, cy - r // 2, cx + i * 22 + r, cy + r // 2],
                      fill=(232, 240, 248, 90))
    write_png(os.path.join(destino, "fondo.png"), fondo, PPU_EDIFICIO, "centro", filtro=1, verbose=False)
    print("   fondo.png")

    # Suelo: banqueta + banqueta baja + asfalto (16 x 1.2 unidades, tapa el borde inferior).
    ancho_suelo, alto_suelo = 1280, 96
    suelo = Image.new("RGBA", (ancho_suelo, alto_suelo), (0, 0, 0, 0))
    d = ImageDraw.Draw(suelo)
    d.rectangle([0, 0, ancho_suelo, 10], fill=(178, 183, 188, 255))
    d.rectangle([0, 10, ancho_suelo, 16], fill=(143, 148, 154, 255))
    d.rectangle([0, 16, ancho_suelo, alto_suelo], fill=(88, 92, 98, 255))
    for x in range(0, ancho_suelo, 64):
        d.line([(x, 0), (x, 10)], fill=(150, 156, 162, 255), width=1)
    rnd = random.Random(3)
    for _ in range(900):
        x = rnd.randint(0, ancho_suelo - 1)
        y = rnd.randint(18, alto_suelo - 1)
        c = rnd.randint(70, 110)
        d.point((x, y), fill=(c, c + 2, c + 6, 255))
    d.line([(0, 16), (ancho_suelo, 16)], fill=(60, 64, 70, 255), width=1)
    write_png(os.path.join(destino, "suelo.png"), suelo, PPU_EDIFICIO, "centro", verbose=False)
    print("   suelo.png")


def main():
    generar_demoledor()
    generar_muneco()
    generar_nivel()
    print("\nListo.")


if __name__ == "__main__":
    main()
