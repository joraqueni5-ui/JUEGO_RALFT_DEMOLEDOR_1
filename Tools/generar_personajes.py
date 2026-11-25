"""Personajes elegibles (carreras de Unifranz), hechos por código a partir de la animación original del muñeco.

Lee los .aseprite de Assets/personaje (caminar abajo, espalda, derecha y martillo, 4 cuadros cada uno), y para cada
carrera cambia colores (pelo, ropa, pantalón, piel, lentes) y agrega detalles (pelo largo, gorro de chef, corbata).
Así cada personaje conserva exactamente los mismos movimientos que el original.

Salida: Assets/Resources/Extras/Personajes/<id>/{abajo,arriba,derecha,martillo}_<0..3>.png (+ .meta)
        Tools/sprites_out/personajes.png (hoja de revisión)
Uso: python Tools/generar_personajes.py
"""
import importlib.util
import os
import sys
from collections import deque

from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(RAIZ, "Tools"))
import aseprite_tool as at  # noqa: E402

spec = importlib.util.spec_from_file_location("exportar_extras", os.path.join(RAIZ, "Tools", "exportar_extras.py"))
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)

ARCHIVOS = {"abajo": "Movimiento Abajo", "arriba": "Movimiento Espalda1", "derecha": "Movimiento Derecha", "martillo": "Movimiento martillo"}

NEGRO = (0, 0, 0)
PIEL = (238, 195, 154)
CAMISA = (80, 58, 6)          # marrón de la camisa
CAMISA2 = (135, 106, 28)
CAMISA3 = (61, 43, 2)
CAMISA4 = (75, 43, 0)
CAMISA5 = (138, 111, 48)
CAMISA6 = (97, 43, 2)
BLANCO = (255, 255, 255)
CHALECO = (48, 58, 49)        # verde oscuro
OSCURO = (47, 48, 41)         # pantalón / sombras
GRIS = (89, 86, 82)           # zapatos
LENTE = (157, 216, 212)
LENTE2 = (203, 219, 252)

# id, nombre, carrera, mujer, pelo, cambios de color, extras
PERSONAJES = [
    ("sistemas", "Diego", "Ing. de Sistemas", False, None, {}, []),
    ("medicina", "Valeria", "Medicina", True, (92, 52, 28),
     {CAMISA: (236, 240, 246), CAMISA2: (205, 212, 224), CAMISA3: (170, 180, 196), CAMISA4: (170, 180, 196), CAMISA5: (205, 212, 224), CAMISA6: (170, 180, 196),
      CHALECO: (64, 150, 168), OSCURO: (52, 112, 128), LENTE: PIEL, LENTE2: PIEL}, ["estetoscopio"]),
    ("administracion", "Camila", "Administración", True, (128, 46, 30),
     {CAMISA: (40, 52, 96), CAMISA2: (64, 80, 132), CAMISA3: (26, 34, 66), CAMISA4: (26, 34, 66), CAMISA5: (64, 80, 132), CAMISA6: (26, 34, 66),
      BLANCO: (246, 214, 222), CHALECO: (246, 214, 222), OSCURO: (44, 44, 56), LENTE: PIEL, LENTE2: PIEL}, []),
    ("gastronomia", "Lucía", "Gastronomía", True, (214, 168, 74),
     {CAMISA: (240, 240, 236), CAMISA2: (210, 210, 204), CAMISA3: (180, 180, 172), CAMISA4: (180, 180, 172), CAMISA5: (210, 210, 204), CAMISA6: (180, 180, 172),
      CHALECO: (232, 232, 226), BLANCO: (200, 60, 50), OSCURO: (40, 40, 44), LENTE: PIEL, LENTE2: PIEL, PIEL: (214, 160, 116)}, ["gorro"]),
    ("derecho", "Mateo", "Derecho", False, (40, 28, 20),
     {CAMISA: (34, 36, 44), CAMISA2: (60, 62, 74), CAMISA3: (20, 20, 26), CAMISA4: (20, 20, 26), CAMISA5: (60, 62, 74), CAMISA6: (20, 20, 26),
      BLANCO: (240, 240, 240), CHALECO: (170, 30, 40), OSCURO: (30, 30, 36), PIEL: (198, 142, 100)}, []),
    ("arquitectura", "Sofía", "Arquitectura", True, (34, 30, 40),
     {CAMISA: (242, 120, 24), CAMISA2: (255, 160, 60), CAMISA3: (190, 84, 10), CAMISA4: (190, 84, 10), CAMISA5: (255, 160, 60), CAMISA6: (190, 84, 10),
      BLANCO: (236, 236, 90), CHALECO: (60, 90, 140), OSCURO: (52, 60, 90), LENTE: PIEL, LENTE2: PIEL, PIEL: (176, 120, 84)}, []),

    ("docente", "Prof. Ramiro", "Docente de Física", False, (186, 186, 192),
     {CAMISA: (120, 84, 48), CAMISA2: (150, 110, 70), CAMISA3: (90, 60, 32), CAMISA4: (90, 60, 32), CAMISA5: (150, 110, 70), CAMISA6: (90, 60, 32),
      BLANCO: (232, 226, 210), CHALECO: (60, 90, 70), OSCURO: (70, 62, 54), PIEL: (226, 178, 136)}, ["corbata"]),
    ("decana", "Dra. Elena", "Decana", True, (200, 200, 208),
     {CAMISA: (110, 40, 70), CAMISA2: (150, 60, 100), CAMISA3: (80, 26, 50), CAMISA4: (80, 26, 50), CAMISA5: (150, 60, 100), CAMISA6: (80, 26, 50),
      BLANCO: (240, 230, 236), CHALECO: (240, 230, 236), OSCURO: (50, 44, 56), PIEL: (230, 186, 150)}, []),
    ("seguridad", "Don Carlos", "Seguridad", False, (30, 26, 24),
     {CAMISA: (40, 70, 130), CAMISA2: (70, 104, 170), CAMISA3: (26, 46, 92), CAMISA4: (26, 46, 92), CAMISA5: (70, 104, 170), CAMISA6: (26, 46, 92),
      BLANCO: (220, 200, 90), CHALECO: (24, 36, 70), OSCURO: (24, 30, 50), PIEL: (190, 136, 96), LENTE: PIEL, LENTE2: PIEL}, ["gorra"]),
    ("mantenimiento", "Doña Rosa", "Mantenimiento", True, (60, 34, 26),
     {CAMISA: (52, 130, 92), CAMISA2: (80, 170, 122), CAMISA3: (32, 92, 62), CAMISA4: (32, 92, 62), CAMISA5: (80, 170, 122), CAMISA6: (32, 92, 62),
      BLANCO: (240, 200, 60), CHALECO: (40, 104, 74), OSCURO: (40, 60, 52), PIEL: (196, 140, 100), LENTE: PIEL, LENTE2: PIEL}, []),
    ("odontologia", "Ana", "Odontología", True, (24, 22, 30),
     {CAMISA: (120, 80, 170), CAMISA2: (156, 116, 206), CAMISA3: (86, 54, 130), CAMISA4: (86, 54, 130), CAMISA5: (156, 116, 206), CAMISA6: (86, 54, 130),
      BLANCO: (236, 230, 246), CHALECO: (120, 80, 170), OSCURO: (86, 54, 130), LENTE: PIEL, LENTE2: PIEL}, []),
    ("comunicacion", "Bruno", "Comunicación", False, (110, 70, 40),
     {CAMISA: (196, 48, 52), CAMISA2: (230, 84, 84), CAMISA3: (140, 30, 36), CAMISA4: (140, 30, 36), CAMISA5: (230, 84, 84), CAMISA6: (140, 30, 36),
      BLANCO: (250, 250, 250), CHALECO: (196, 48, 52), OSCURO: (60, 66, 96), LENTE: PIEL, LENTE2: PIEL, PIEL: (214, 160, 116)}, []),
]


def cargar():
    series = {}
    for clave, archivo in ARCHIVOS.items():
        doc = at.parse(os.path.join(RAIZ, "Assets", "personaje", archivo + ".aseprite"))
        series[clave] = [at.frame_image(doc, i).crop(at.frame_image(doc, i).getbbox()) for i in range(len(doc["frames"]))]
    return series


def mascara_pelo(im):
    """Píxeles negros interiores conectados con la parte de arriba de la cabeza (el pelo; el contorno no)."""
    w, h = im.size
    px = im.load()
    limite = int(h * 0.42)

    def opaco(x, y):
        return 0 <= x < w and 0 <= y < h and px[x, y][3] > 0

    def interior_negro(x, y):
        p = px[x, y]
        if p[3] == 0 or p[:3] != NEGRO:
            return False
        return all(opaco(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))

    semillas = [(x, y) for y in range(min(limite, 12)) for x in range(w) if interior_negro(x, y)]
    vistos = set(semillas)
    cola = deque(semillas)
    while cola:
        x, y = cola.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if (nx, ny) in vistos or not (0 <= nx < w and 0 <= ny < limite):
                continue
            if interior_negro(nx, ny):
                vistos.add((nx, ny))
                cola.append((nx, ny))
    return vistos


def oscurecer(c, k):
    return tuple(max(0, int(v * k)) for v in c)


def pelo_largo(im, pelo, vista, original, factor=0.26):
    """Agrega pelo largo hasta los hombros (de frente/lado) o por la espalda (de espaldas).
    La forma del pelo se mide en 'original' (antes de cambiar colores, cuando el pelo todavía es negro)."""
    w, h = im.size
    px = im.load()
    masc = mascara_pelo(original)
    if not masc:
        return im
    xs = [x for x, _ in masc]
    ys = [y for _, y in masc]
    x0, x1, yfin = min(xs), max(xs), max(ys)
    largo = int(h * factor)
    nuevos = set()
    if vista == "arriba":
        for x in range(x0 + 1, x1):
            for y in range(yfin, min(h - 1, yfin + largo)):
                nuevos.add((x, y))
    else:
        ancho = max(3, (x1 - x0) // 6)
        lados = [range(x0, x0 + ancho)] if vista == "derecha" else [range(x0, x0 + ancho), range(x1 - ancho + 1, x1 + 1)]
        for rango in lados:
            for x in rango:
                for y in range(yfin - 4, min(h - 1, yfin + largo - abs(x - (x0 + x1) // 2) // 4)):
                    nuevos.add((x, y))
    sombra = oscurecer(pelo, 0.7)
    for (x, y) in nuevos:
        if 0 <= x < w and 0 <= y < h:
            px[x, y] = (*sombra, 255) if (x + y) % 5 == 0 else (*pelo, 255)
    # contorno negro alrededor del pelo nuevo que quedó por fuera del cuerpo
    for (x, y) in list(nuevos):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] == 0:
                px[nx, ny] = (0, 0, 0, 255)
    return im


def con_margen(im, m=4):
    out = Image.new("RGBA", (im.width + m * 2, im.height + m * 2), (0, 0, 0, 0))
    out.paste(im, (m, m))
    return out


def gorro(im):
    """Gorro de chef blanco sobre la cabeza."""
    im = im.crop(im.getbbox())
    w, h = im.size
    alto = 10
    out = Image.new("RGBA", (w, h + alto), (0, 0, 0, 0))
    out.paste(im, (0, alto))
    px = out.load()
    filas = [x for x in range(w) if px[x, alto + 6][3] > 0]
    if not filas:
        return out
    cx = (min(filas) + max(filas)) // 2
    ancho = max(10, (max(filas) - min(filas)) * 3 // 4)
    for y in range(alto + 2):
        r = ancho // 2 + (2 if y < 7 else 0)
        for x in range(cx - r, cx + r + 1):
            if 0 <= x < w:
                borde = y == 0 or x in (cx - r, cx + r) or (y == alto + 1)
                px[x, y] = (0, 0, 0, 255) if borde else ((226, 226, 232, 255) if (x - cx) % 4 == 0 else (250, 250, 252, 255))
    return out.crop(out.getbbox())


def estetoscopio(im, vista):
    if vista == "arriba":
        return im
    w, h = im.size
    px = im.load()
    y0 = int(h * 0.47)
    cx = w // 2 if vista != "derecha" else int(w * 0.55)
    for i in range(7):
        for x in (cx - 4 + i // 3, cx + 3 - i // 3):
            y = y0 + i
            if 0 <= x < w and 0 <= y < h and px[x, y][3] > 0:
                px[x, y] = (60, 60, 70, 255)
    if 0 <= cx < w and y0 + 8 < h:
        px[cx, y0 + 8] = (180, 190, 200, 255)
    return im



BOCA = (141, 73, 37)


def blobs(im, colores):
    """Grupos de píxeles conectados con alguno de esos colores. Devuelve listas de (x, y)."""
    w, h = im.size
    px = im.load()
    vistos, grupos = set(), []
    for y in range(h):
        for x in range(w):
            if (x, y) in vistos or px[x, y][3] == 0 or px[x, y][:3] not in colores:
                continue
            g, cola = [], deque([(x, y)])
            vistos.add((x, y))
            while cola:
                cx, cy = cola.popleft()
                g.append((cx, cy))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in vistos and px[nx, ny][3] and px[nx, ny][:3] in colores:
                        vistos.add((nx, ny))
                        cola.append((nx, ny))
            grupos.append(g)
    return grupos


def ojos(original, im, piel, susto=False):
    """Sin lentes: dibuja ojos (blanco y pupila) donde estaban los cristales. Asustado: ojos más grandes, pupila chica."""
    px = im.load()
    for g in blobs(original, {LENTE, LENTE2}):
        if len(g) < 4:
            continue
        xs = [x for x, _ in g]
        ys = [y for _, y in g]
        cx, cy = (min(xs) + max(xs)) // 2, (min(ys) + max(ys)) // 2
        for (x, y) in g:
            px[x, y] = (*piel, 255)
        ancho, alto = (2, 1) if susto else (1, 1)
        for x in range(cx - ancho, cx + ancho + 1):
            for y in range(cy - alto, cy + alto + 1):
                if 0 <= x < im.width and 0 <= y < im.height:
                    px[x, y] = (255, 255, 255, 255)
        if susto:
            px[cx, cy] = (0, 0, 0, 255)
        else:
            for y in range(cy - 1, cy + 2):
                if 0 <= y < im.height:
                    px[cx, y] = (0, 0, 0, 255)
    return im


def boca_susto(original, im):
    """Boca abierta de susto (negra con lengua) y una gota de sudor."""
    px = im.load()
    grupos = blobs(original, {BOCA})
    if not grupos:
        return im
    g = max(grupos, key=len)
    xs = [x for x, _ in g]
    ys = [y for _, y in g]
    x0, x1, y0, y1 = min(xs) + 1, max(xs) - 1, min(ys), max(ys) + 2
    for x in range(x0, x1 + 1):
        for y in range(y0, min(im.height - 1, y1) + 1):
            borde = x in (x0, x1) or y in (y0, y1)
            px[x, y] = (0, 0, 0, 255) if borde else (190, 60, 70, 255)
    # gota de sudor junto a la sien
    gx, gy = min(im.width - 3, max(xs) + 6), max(1, y0 - 12)
    for (x, y) in [(gx, gy), (gx, gy + 1), (gx - 1, gy + 2), (gx, gy + 2), (gx + 1, gy + 2), (gx, gy + 3)]:
        if 0 <= x < im.width and 0 <= y < im.height:
            px[x, y] = (120, 200, 255, 255)
    return im


def corbata(im, vista):
    if vista == "arriba":
        return im
    px = im.load()
    w, h = im.size
    cx = w // 2 if vista == "abajo" else int(w * 0.55)
    for y in range(int(h * 0.5), int(h * 0.62)):
        for x in (cx - 1, cx):
            if 0 <= x < w and px[x, y][3]:
                px[x, y] = (150, 30, 40, 255)
    return im


def gorra(original, im, vista):
    """Gorra de seguridad azul oscuro sobre el pelo, con visera."""
    px = im.load()
    masc = mascara_pelo(original)
    if not masc:
        return im
    ys = [y for _, y in masc]
    top = min(ys)
    for (x, y) in masc:
        if y < top + 7:
            px[x, y] = (34, 44, 88, 255) if (x + y) % 6 else (60, 74, 130, 255)
    if vista != "arriba":
        xs = [x for x, y in masc if y == top + 7] or [x for x, _ in masc]
        for x in range(min(xs) - (0 if vista == "derecha" else 1), max(xs) + 3):
            if 0 <= x < im.width:
                px[x, top + 7] = (0, 0, 0, 255)
                if top + 8 < im.height and px[x, top + 8][3]:
                    pass
    return im

def recolorear(im, mapa, pelo):
    im = im.copy()
    px = im.load()
    masc = mascara_pelo(im) if pelo else set()
    for y in range(im.height):
        for x in range(im.width):
            p = px[x, y]
            if p[3] == 0:
                continue
            if (x, y) in masc:
                px[x, y] = (*(oscurecer(pelo, 0.75) if (x * 3 + y) % 7 == 0 else pelo), 255)
            elif p[:3] in mapa:
                px[x, y] = (*mapa[p[:3]], p[3])
    return im


def base_ref(base, v):
    """La imagen original (antes de recolorear) del mismo tamaño que 'v', para ubicar lentes, boca y pelo."""
    return base


def pegar_abajo(lienzo, im, dx=0, dy=0):
    """Pega 'im' centrado abajo (pies) en 'lienzo', corrido dx, dy píxeles."""
    x = (lienzo.width - im.width) // 2 + dx
    y = lienzo.height - im.height + dy
    lienzo.alpha_composite(im, (max(0, x), max(0, y)))


def intermedio(a, b, clave):
    """Cuadro intermedio entre a y b (pixel art): al caminar, piernas de b con torso y cabeza de a un píxel más arriba
    (el rebote del paso); en el martillazo, b con una estela tenue de a (sensación de golpe rápido)."""
    w, h = max(a.width, b.width), max(a.height, b.height) + 1
    lienzo = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    if clave == "martillo":
        estela = a.copy()
        alfa = estela.getchannel("A").point(lambda v: int(v * 0.35))
        estela.putalpha(alfa)
        pegar_abajo(lienzo, estela)
        pegar_abajo(lienzo, b)
        return lienzo.crop(lienzo.getbbox())
    pegar_abajo(lienzo, b)
    corte = int(a.height * 0.55)   # de la cintura para arriba
    arriba = a.crop((0, 0, a.width, corte))
    x = (w - a.width) // 2
    y = h - a.height - 1
    lienzo.alpha_composite(arriba, (max(0, x), max(0, y)))
    return lienzo.crop(lienzo.getbbox())


def main():
    series = cargar()
    hoja = []
    for pid, nombre, carrera, mujer, pelo, mapa, extras in PERSONAJES:
        fila = []
        finales = {}
        for clave, cuadros in series.items():
            for i, c in enumerate(cuadros):
                vista = "arriba" if clave == "arriba" else ("derecha" if clave in ("derecha", "martillo") else "abajo")
                base = con_margen(c) if mujer else c
                im = recolorear(base, mapa, pelo)
                if mujer:
                    factor = {"gastronomia": 0.12, "administracion": 0.2}.get(pid, 0.26)
                    im = pelo_largo(im, pelo or (20, 16, 14), vista, base, factor)
                sin_lentes = mapa.get(LENTE) == mapa.get(PIEL, PIEL) or mapa.get(LENTE) == PIEL
                piel = mapa.get(PIEL, PIEL)
                variantes = [("", False)] + ([("_susto", True)] if clave in ("abajo", "derecha") else [])
                for sufijo, susto in variantes:
                    v = im.copy()
                    base_v = base
                    if mujer:   # pelo largo agrega margen: el original de referencia también lo lleva
                        pass
                    if sin_lentes and clave != "arriba":
                        v = ojos(base_ref(base, v), v, piel, susto)
                    elif susto:
                        v = ojos(base_ref(base, v), v, piel, True) if False else v
                    if susto:
                        v = boca_susto(base_ref(base, v), v)
                    if "corbata" in extras:
                        v = corbata(v, vista)
                    if "gorra" in extras:
                        v = gorra(base_ref(base, v), v, vista)
                    if "gorro" in extras:
                        v = gorro(v)
                    if "estetoscopio" in extras:
                        v = estetoscopio(v, vista)
                    v = v.crop(v.getbbox())
                    ex.escribir(f"Personajes/{pid}/{clave}{sufijo}_{i}", v, 100, True)
                    if not sufijo:
                        fila.append(v)
                        finales.setdefault(clave, []).append(v)
        # Ciclos de 8 cuadros: cada cuadro original y uno intermedio hacia el siguiente.
        for clave, cuadros in finales.items():
            n = len(cuadros)
            ocho = []
            if clave == "martillo" and n == 4:
                # Martillazo: preparación, golpe sostenido (el impacto se lee mejor) y vuelta.
                ocho = [cuadros[0], cuadros[1], cuadros[1], cuadros[2], cuadros[2], cuadros[2], cuadros[3], cuadros[3]]
            else:
                for i in range(n):
                    ocho.append(cuadros[i])
                    ocho.append(intermedio(cuadros[i], cuadros[(i + 1) % n], clave))
            for k, v in enumerate(ocho):
                ex.escribir(f"Personajes/{pid}/{clave}8_{k}", v, 100, True)
        hoja.append(fila)

    # hoja de revisión
    esc = 3
    W = max(sum(t.width * esc + 6 for t in f) for f in hoja)
    H = sum(max(t.height for t in f) * esc + 10 for f in hoja)
    s = Image.new("RGBA", (W, H), (60, 64, 84, 255))
    y = 0
    for f in hoja:
        x = 0
        for t in f:
            s.alpha_composite(t.resize((t.width * esc, t.height * esc), Image.NEAREST), (x, y))
            x += t.width * esc + 6
        y += max(t.height for t in f) * esc + 10
    os.makedirs(os.path.join(RAIZ, "Tools", "sprites_out"), exist_ok=True)
    s.save(os.path.join(RAIZ, "Tools", "sprites_out", "personajes.png"))
    print("personajes:", [p[0] for p in PERSONAJES])


if __name__ == "__main__":
    main()
