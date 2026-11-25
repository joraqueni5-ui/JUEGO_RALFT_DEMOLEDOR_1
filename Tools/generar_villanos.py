"""Variantes del demoledor (mismos movimientos, otra vestimenta), hechas por código desde los .aseprite originales:

  blanco      EL INGENIERO: camisa y pantalón blancos, pelo canoso.
  demoledora  LA DEMOLEDORA: camisa fucsia, pantalón morado, pelo largo rojizo, sin bigote y con labios rojos.
  robot       ROBO-DEMOLEDOR: piel metálica, cuerpo de acero, visor rojo y antena.

Salida: Assets/Resources/Extras/Villanos/<id>/{caminar_0..5, golpe_0..5, salto_0..3}.png (+ .meta)
        Tools/sprites_out/villanos.png (hoja de revisión)
Uso: python Tools/generar_villanos.py
"""
import colorsys
import os
import sys

from PIL import Image, ImageDraw

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(RAIZ, "Tools"))
import aseprite_tool as at  # noqa: E402
import exportar_extras as ex  # noqa: E402

ORIGEN = os.path.join(RAIZ, "Assets", "Animaciones", "Enemigo")
ANIMS = {"caminar": "animacion_caminata_derecha", "golpe": "demoledor_golpe", "salto": "animacion_salto"}


def hsv(p):
    r, g, b, a = p
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return h * 360, s, v


def rgb(h, s, v, a=255):
    r, g, b = colorsys.hsv_to_rgb((h % 360) / 360, max(0, min(1, s)), max(0, min(1, v)))
    return (int(r * 255), int(g * 255), int(b * 255), a)


def clase(p):
    """Qué parte es el píxel: camisa, jean, piel, marron (pelo, bigote, botas, cinturón), oscuro, otro."""
    if p[3] < 40:
        return None
    h, s, v = hsv(p)
    if v < 0.13:
        return "oscuro"
    if 195 <= h <= 235 and s > 0.35:
        return "camisa" if v > 0.62 else "jean"
    if 12 <= h <= 40 and s > 0.3 and v > 0.62:
        return "piel"
    if (h <= 40 or h >= 340) and v < 0.62 and s > 0.25:
        return "marron"
    return "otro"


def componentes(im, tipo):
    """Grupos conectados (8 vecinos) de píxeles de una clase."""
    w, hh = im.size
    px = im.load()
    vistos = set()
    grupos = []
    for y in range(hh):
        for x in range(w):
            if (x, y) in vistos or clase(px[x, y]) != tipo:
                continue
            pila = [(x, y)]
            vistos.add((x, y))
            g = []
            while pila:
                cx, cy = pila.pop()
                g.append((cx, cy))
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < w and 0 <= ny < hh and (nx, ny) not in vistos and clase(px[nx, ny]) == tipo:
                            vistos.add((nx, ny))
                            pila.append((nx, ny))
            grupos.append(g)
    return grupos


def partes(im):
    """(pelo, bigote, caja de la cara): el pelo es el grupo marrón más grande de la mitad de arriba; el bigote, lo marrón de
    la cara (debajo del pelo) que tiene piel arriba y abajo, más su contorno oscuro."""
    caja = im.getbbox()
    if caja is None:
        return set(), set(), None
    mitad = caja[1] + (caja[3] - caja[1]) * 0.5
    grupos = [g for g in componentes(im, "marron") if sum(y for _, y in g) / len(g) < mitad]
    if not grupos:
        return set(), set(), None
    pelo = set(max(grupos, key=len))
    xs = [x for x, _ in pelo]; ys = [y for _, y in pelo]
    cara = (min(xs) - 2, int(sum(ys) / len(ys)), max(xs) + 6, max(ys) + 18)
    px = im.load()
    w, hh = im.size
    bigote = set()
    for y in range(cara[1], min(hh, cara[3])):
        for x in range(max(0, cara[0]), min(w, cara[2])):
            if (x, y) in pelo or clase(px[x, y]) != "marron":
                continue
            arriba = any(clase(px[x, y - k]) == "piel" for k in range(1, 7) if y - k >= 0)
            abajo = any(clase(px[x, y + k]) == "piel" for k in range(1, 7) if y + k < hh)
            if arriba and abajo:
                bigote.add((x, y))
    # De lo encontrado, el bigote es el grupo más grande (los otros son las cejas y sombras de la nariz).
    if bigote:
        resto = set(bigote)
        mejor = set()
        while resto:
            semilla = resto.pop()
            g = {semilla}
            pila = [semilla]
            while pila:
                cx, cy = pila.pop()
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        q = (cx + dx, cy + dy)
                        if q in resto:
                            resto.discard(q)
                            g.add(q)
                            pila.append(q)
            if len(g) > len(mejor):
                mejor = g
        bigote = mejor
    # Contorno oscuro pegado al bigote (si no, queda dibujado su borde).
    borde = set()
    for (x, y) in bigote:
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                q = (x + dx, y + dy)
                if q in bigote or not (0 <= q[0] < w and 0 <= q[1] < hh):
                    continue
                if clase(px[q]) == "oscuro" and any(clase(px[q[0], q[1] - k]) == "piel" for k in range(1, 4) if q[1] - k >= 0):
                    borde.add(q)
    return pelo, bigote | borde, cara


def recolorear(im, tabla, sin_bigote=False, pelo=None):
    out = im.copy()
    px = im.load()
    po = out.load()
    pelos, bigotes, cara = partes(im)
    w, hh = im.size
    for y in range(hh):
        for x in range(w):
            p = px[x, y]
            c = clase(p)
            if c is None:
                continue
            h, s, v = hsv(p)
            if (x, y) in pelos and pelo:
                po[x, y] = rgb(pelo[0], pelo[1], v * pelo[2], p[3])
                continue
            if (x, y) in bigotes and sin_bigote:
                continue   # se pinta después con el color de la piel vecina
            dentro_cara = cara is not None and cara[0] <= x <= cara[2] and cara[1] <= y <= cara[3]
            f = tabla.get("pelo" if (x, y) in pelos else ("cara_" + c if dentro_cara and ("cara_" + c) in tabla else c))
            if f is not None:
                po[x, y] = f(h, s, v, p[3])
    if sin_bigote:
        for (x, y) in sorted(bigotes, key=lambda q: q[1]):
            for k in range(1, 8):
                if y - k >= 0 and (x, y - k) not in bigotes and clase(px[x, y - k]) == "piel":
                    po[x, y] = po[x, y - k]
                    break
    return out, bigotes, pelos, cara


def camisa_blanca(h, s, v, a):
    return rgb(210, 0.06, 0.62 + v * 0.38, a)


def jean_blanco(h, s, v, a):
    return rgb(40, 0.12, 0.55 + v * 0.45, a)


def pelo_canoso(h, s, v, a):
    return rgb(220, 0.05, 0.45 + v * 0.6, a)


def blanco(im):
    out, _, _, _ = recolorear(im, {"camisa": camisa_blanca, "jean": jean_blanco}, pelo=(220, 0.06, 1.6))
    return out


def demoledora(im):
    tabla = {
        "camisa": lambda h, s, v, a: rgb(320, 0.75, v, a),
        "jean": lambda h, s, v, a: rgb(275, 0.6, v * 1.05, a),
    }
    out, bigotes, pelos, cara = recolorear(im, tabla, sin_bigote=True, pelo=(8, 0.75, 1.15))
    d = ImageDraw.Draw(out)
    px = out.load()
    w, hh = out.size
    # Labios rojos donde estaba el bigote.
    if bigotes:
        xs = sorted(x for x, _ in bigotes)
        ys = [y for _, y in bigotes]
        cx = xs[len(xs) // 2]
        yb = max(ys)
        d.line([(cx - 2, yb), (cx + 2, yb)], fill=(205, 30, 60, 255))
        d.point((cx, yb - 1), fill=(235, 70, 100, 255))
    # Pelo largo: desde los costados del pelo hacia abajo hasta los hombros (detrás: solo en píxeles vacíos).
    if pelos:
        xs = [x for x, _ in pelos]; ys = [y for _, y in pelos]
        x0, x1 = min(xs), max(xs)
        base = max(ys) - 2
        largo = 24
        ancho = max(2, (x1 - x0) // 4)
        for x in range(x0 - 2, x1 + 3):
            borde = min(x - x0, x1 - x)
            if borde > ancho or not (0 <= x < w):
                continue
            for y in range(base, min(hh, base + largo - max(0, borde) * 2)):
                if px[x, y][3] < 40:
                    k = (y - base) / largo
                    px[x, y] = rgb(8, 0.75, 0.42 - 0.12 * k)
        # Moño fucsia arriba de la cabeza.
        top = min(ys)
        cx = (x0 + x1) // 2
        d.polygon([(cx - 6, top - 1), (cx, top + 2), (cx - 6, top + 5)], fill=(240, 60, 150, 255), outline=(20, 10, 20, 255))
        d.polygon([(cx + 6, top - 1), (cx, top + 2), (cx + 6, top + 5)], fill=(240, 60, 150, 255), outline=(20, 10, 20, 255))
        d.rectangle([cx - 1, top + 1, cx + 1, top + 3], fill=(200, 30, 110, 255))
    return out


def robot(im):
    tabla = {
        "piel": lambda h, s, v, a: rgb(205, 0.12, 0.45 + v * 0.45, a),
        "camisa": lambda h, s, v, a: rgb(210, 0.25, v * 0.75, a),
        "jean": lambda h, s, v, a: rgb(215, 0.2, v * 0.8 + 0.08, a),
        "marron": lambda h, s, v, a: rgb(210, 0.15, v * 0.7 + 0.08, a),
        "pelo": lambda h, s, v, a: rgb(215, 0.2, v * 0.5 + 0.12, a),
    }
    out, bigotes, pelos, cara = recolorear(im, tabla)
    d = ImageDraw.Draw(out)
    if pelos:
        xs = [x for x, _ in pelos]; ys = [y for _, y in pelos]
        x0, x1 = min(xs), max(xs)
        cx = (x0 + x1) // 2
        top = min(ys)
        # Visor rojo a la altura de los ojos (justo arriba del bigote).
        yv = (min(y for _, y in bigotes) - 4) if bigotes else max(ys) + 3
        fila = [x for x in range(max(0, x0 - 2), min(im.width, x1 + 6)) if clase(im.getpixel((x, yv))) in ("piel", "oscuro", "marron")]
        if fila:
            d.rectangle([min(fila) + 1, yv - 1, max(fila) - 1, yv + 1], fill=(255, 40, 40, 255))
            d.line([(min(fila) + 2, yv - 1), (max(fila) - 2, yv - 1)], fill=(255, 170, 160, 255))
        # Antena.
        d.line([(cx, top - 9), (cx, top)], fill=(40, 44, 54, 255), width=2)
        d.ellipse([cx - 3, top - 13, cx + 3, top - 7], fill=(255, 60, 60, 255), outline=(30, 20, 20, 255))
    return out


VARIANTES = {"blanco": blanco, "demoledora": demoledora, "robot": robot}


def main():
    hoja = []
    for nombre, archivo in ANIMS.items():
        doc = at.parse(os.path.join(ORIGEN, archivo + ".aseprite"))
        for i in range(len(doc["frames"])):
            base = at.frame_image(doc, i).convert("RGBA")
            caja = base.getbbox()
            for vid, f in VARIANTES.items():
                im = f(base)
                # Recorte común: el del original más un margen para el pelo largo, el moño y la antena.
                x0, y0, x1, y1 = caja
                x0 = max(0, x0 - 4); y0 = max(0, y0 - 16); x1 = min(im.width, x1 + 4); y1 = min(im.height, y1)
                im = im.crop((x0, y0, x1, y1))
                ex.escribir(f"Villanos/{vid}/{nombre}_{i}", im, 100)
                if nombre == "golpe" and i == 2 or nombre == "caminar" and i == 0:
                    hoja.append(im)
    W = sum(i.width * 3 + 10 for i in hoja)
    s = Image.new("RGBA", (W, 420), (90, 90, 100, 255))
    x = 0
    for i in hoja:
        s.alpha_composite(i.resize((i.width * 3, i.height * 3), Image.NEAREST), (x, 0))
        x += i.width * 3 + 10
    os.makedirs(os.path.join(RAIZ, "Tools", "sprites_out"), exist_ok=True)
    s.save(os.path.join(RAIZ, "Tools", "sprites_out", "villanos.png"))
    print("Villanos listos")


if __name__ == "__main__":
    main()
