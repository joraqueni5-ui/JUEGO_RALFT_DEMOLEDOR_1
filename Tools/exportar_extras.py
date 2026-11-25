"""Copia el arte extra (Tools/sprites_out) a Assets/Resources/Extras con sus .meta de Unity.

Primero se generan las piezas:
  (los PixelSpec de Tools/sprites_spec se dibujan a PNG en Tools/sprites_out/<x>)
  python Tools/generar_extras_fx.py
  python Tools/generar_tema_menu.py
Luego: python Tools/exportar_extras.py

El código los carga con Resources.Load<Sprite>("Extras/...") (ver Assets/Scripts/Extras/ArteExtra.cs).
"""
import hashlib
import json
import os
import shutil

from PIL import Image, ImageDraw

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(RAIZ, "Tools", "sprites_out")
DST = os.path.join(RAIZ, "Assets", "Resources", "Extras")

sys_meta = os.path.join(RAIZ, "Tools", "unity_assets.py")
import importlib.util
spec = importlib.util.spec_from_file_location("unity_assets", sys_meta)
ua = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ua)

CARPETA_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid(ruta):
    rel = os.path.relpath(ruta, RAIZ).replace("\\", "/").lower()
    return hashlib.md5(rel.encode("utf-8")).hexdigest()


def meta_carpetas(ruta):
    """Escribe el .meta de cada carpeta nueva bajo Assets (GUID estable)."""
    actual = os.path.dirname(ruta)
    while os.path.relpath(actual, RAIZ).replace("\\", "/").startswith("Assets/"):
        m = actual + ".meta"
        if not os.path.exists(m):
            with open(m, "w", encoding="utf-8", newline="\n") as f:
                f.write(CARPETA_META.format(guid=guid(actual)))
        actual = os.path.dirname(actual)


def escribir(ruta_rel, imagen, ppu=32, abajo=False, borde=None):
    ruta = os.path.join(DST, ruta_rel + ".png")
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    imagen.save(ruta)
    alineacion, px, py = (7, 0.5, 0.0) if abajo else (0, 0.5, 0.5)
    meta = ua.PNG_META.format(guid=guid(ruta), ppu=ppu, alignment=alineacion, pivot_x=px, pivot_y=py, filter=0)
    if borde:
        x, y, z, w = borde
        meta = meta.replace("spriteBorder: {x: 0, y: 0, z: 0, w: 0}", f"spriteBorder: {{x: {x}, y: {y}, z: {z}, w: {w}}}")
        meta = meta.replace("spriteMeshType: 0", "spriteMeshType: 0")
    with open(ruta + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)
    meta_carpetas(ruta)
    return ruta


def cuadros(nombre, variante):
    """Devuelve {frame: Image} con todos los cuadros (los repetidos apuntan al archivo reutilizado)."""
    base = os.path.join(SRC, nombre)
    meta = json.load(open(os.path.join(base, "codeart-meta.json")))
    res = {}
    for f in meta.get("pixelspec", {}).get("frames", []):
        p = os.path.join(base, variante, f["file"])
        if os.path.exists(p):
            res[f["name"]] = Image.open(p).convert("RGBA")
    if not res:   # sin frames: una sola imagen
        p = os.path.join(base, variante, "frames")
        for a in sorted(os.listdir(p)):
            res[os.path.splitext(a)[0]] = Image.open(os.path.join(p, a)).convert("RGBA")
    return res


def con_sombra(im):
    """Agrega la sombra suave que tienen los poderes del juego."""
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    caja = im.getbbox()
    d = ImageDraw.Draw(out)
    if caja:
        cx = (caja[0] + caja[2]) / 2
        y = min(im.height - 3, caja[3] + 1)
        d.ellipse([cx - 9, y - 2, cx + 9, y + 2], fill=(20, 18, 30, 110))
    out.alpha_composite(im)
    return out


def main():
    for sub in ("Mini", "Poderes", "Efectos", "Tema"):   # Personajes y Menu los generan sus propios scripts
        if os.path.isdir(os.path.join(DST, sub)):
            shutil.rmtree(os.path.join(DST, sub))
    n = 0
    personajes = [("mini_aprendiz", ["naranja", "rojo"], True), ("mini_termita", ["cobre", "toxica"], False),
                  ("mini_topadora", ["amarilla", "blindada"], True)]
    for nombre, variantes, abajo in personajes:
        corto = nombre.replace("mini_", "")
        for v in variantes:
            for cuadro, im in cuadros(nombre, v).items():
                escribir(f"Mini/{corto}_{v}/{cuadro}", im, 32, abajo); n += 1

    for nombre in ["poder_turbo", "poder_martillo", "poder_reloj", "poder_casco"]:
        base = os.path.join(SRC, nombre, "base", "frames") if os.path.isdir(os.path.join(SRC, nombre, "base")) else None
        if base is None:
            sub = [d for d in os.listdir(os.path.join(SRC, nombre)) if os.path.isdir(os.path.join(SRC, nombre, d))][0]
            base = os.path.join(SRC, nombre, sub, "frames")
        archivo = sorted(os.listdir(base))[0]
        escribir(f"Poderes/{nombre}", con_sombra(Image.open(os.path.join(base, archivo)).convert("RGBA")), 32, True); n += 1

    fx = os.path.join(SRC, "fx")
    for a in sorted(os.listdir(fx)):
        escribir("Efectos/" + os.path.splitext(a)[0], Image.open(os.path.join(fx, a)).convert("RGBA"), 32); n += 1

    tema = os.path.join(SRC, "tema")
    bordes = {"boton_serio": (3, 5, 4, 3), "marco_serio": (3, 5, 4, 3), "panel_serio": (6, 6, 6, 6), "burbuja": (5, 5, 5, 5)}
    for a in sorted(os.listdir(tema)):
        nombre = os.path.splitext(a)[0]
        escribir("Tema/" + nombre, Image.open(os.path.join(tema, a)).convert("RGBA"), 100, False, bordes.get(nombre)); n += 1
    print(n, "sprites en", os.path.relpath(DST, RAIZ))


if __name__ == "__main__":
    main()
