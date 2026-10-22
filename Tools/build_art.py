"""Exporta el arte .aseprite del proyecto a sprites PNG listos para Unity.

- Edificio: se corta en una rejilla de 6x4 bloques (uno normal y uno dañado).
- Demoledor: caminar izquierda/derecha, salto y golpe.
- Muñeco (Félix): caminar de frente, caminar de espaldas y martillo.
- Nivel: fondo, suelo e interior del edificio.
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

COLUMNAS = 12
FILAS = 8
PPU_EDIFICIO = 80
PPU_PERSONAJE = 100

# Recuadro con contenido real del edificio dentro del lienzo 640x360.
EDIF_X0, EDIF_Y0, EDIF_ANCHO, EDIF_ALTO = 45, 7, 576, 321


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


def bloque_danado(img, semilla):
    """Devuelve la versión agrietada de un bloque: oscurece, marca grietas y pica una esquina."""
    rnd = random.Random(semilla)
    out = img.convert("RGBA").copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (int(r * 0.80), int(g * 0.80), int(b * 0.83), a)

    ancho, alto = out.size
    # Esquina rota (se quita un trozo irregular).
    esquina = rnd.choice([(0, 0), (ancho, 0), (0, alto), (ancho, alto)])
    sx = 1 if esquina[0] == 0 else -1
    sy = 1 if esquina[1] == 0 else -1
    d = ImageDraw.Draw(out)
    t1 = rnd.randint(int(ancho * 0.16), int(ancho * 0.30))
    t2 = rnd.randint(int(alto * 0.22), int(alto * 0.40))
    d.polygon([esquina,
               (esquina[0] + sx * t1, esquina[1]),
               (esquina[0] + sx * int(t1 * 0.55), esquina[1] + sy * int(t2 * 0.45)),
               (esquina[0], esquina[1] + sy * t2)], fill=(0, 0, 0, 0))

    # Grietas.
    oscuro = (34, 37, 43, 255)
    claro = (215, 222, 226, 255)
    for _ in range(rnd.randint(2, 3)):
        x = rnd.randint(int(ancho * 0.15), int(ancho * 0.85))
        y = rnd.choice([0, alto])
        puntos = [(x, y)]
        pasos = rnd.randint(4, 6)
        dy = (alto / (pasos + 1)) * (1 if y == 0 else -1)
        for _p in range(pasos):
            x = max(2, min(ancho - 2, x + rnd.randint(-8, 8)))
            y = y + dy
            puntos.append((x, y))
        d.line(puntos, fill=oscuro, width=2, joint="curve")
        d.line([(p[0] + 1, p[1]) for p in puntos], fill=claro, width=1, joint="curve")

    # Pequeños huecos.
    for _ in range(rnd.randint(2, 4)):
        cx = rnd.randint(6, ancho - 6)
        cy = rnd.randint(6, alto - 6)
        r = rnd.randint(2, 4)
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=oscuro)
    return out


def generar_edificio():
    doc = cargar("edificio", "edificio")
    img = frame_image(doc, 0)
    ancho_bloque = EDIF_ANCHO // COLUMNAS   # 48
    alto_bloque = EDIF_ALTO // FILAS        # 40
    print(f"== Edificio: rejilla {COLUMNAS}x{FILAS} de {ancho_bloque}x{alto_bloque} px")
    destino = os.path.join(ARTE, "Edificio")
    for fila in range(FILAS):  # fila 0 = abajo
        for col in range(COLUMNAS):
            x = EDIF_X0 + col * ancho_bloque
            y = EDIF_Y0 + (FILAS - 1 - fila) * alto_bloque
            bloque = img.crop((x, y, x + ancho_bloque, y + alto_bloque))
            write_png(os.path.join(destino, f"bloque_{fila}_{col}.png"), bloque,
                      PPU_EDIFICIO, "centro", verbose=False)
            danado = bloque_danado(bloque, semilla=1000 + fila * COLUMNAS + col)
            write_png(os.path.join(destino, f"bloque_{fila}_{col}_danado.png"), danado,
                      PPU_EDIFICIO, "centro", verbose=False)
    print(f"   {FILAS * COLUMNAS * 2} sprites de bloque")

    # Interior oscuro que se ve por los huecos. Se recorta con la silueta del
    # edificio para que no aparezca un rectángulo oscuro sobre el cielo.
    ancho_total = COLUMNAS * ancho_bloque
    alto_total = FILAS * alto_bloque
    interior = Image.new("RGBA", (ancho_total, alto_total), (30, 34, 40, 255))
    d = ImageDraw.Draw(interior)
    for fila in range(FILAS):
        y = fila * alto_bloque
        d.rectangle([0, y, interior.width, y + 8], fill=(20, 23, 28, 255))
        d.rectangle([0, y + 8, interior.width, y + 12], fill=(43, 49, 57, 255))
    for col in range(1, COLUMNAS):
        x = col * ancho_bloque
        d.rectangle([x - 2, 0, x + 2, interior.height], fill=(24, 27, 33, 255))
    silueta = img.crop((EDIF_X0, EDIF_Y0, EDIF_X0 + ancho_total, EDIF_Y0 + alto_total))
    interior.putalpha(silueta.split()[3])
    write_png(os.path.join(ARTE, "Nivel", "interior.png"), interior, PPU_EDIFICIO, "centro", verbose=False)
    print("   interior.png")


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
    generar_edificio()
    generar_demoledor()
    generar_muneco()
    generar_nivel()
    print("\nListo.")


if __name__ == "__main__":
    main()
