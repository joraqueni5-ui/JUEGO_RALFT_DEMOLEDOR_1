"""Fondo del menú como "mini partida": un aula/pasillo visto desde arriba (con el arte del juego) y las piezas que se
mueven encima (demoledor, objetos reparables, grietas). Las piezas las anima en vivo FondoPartidaMenu.cs.

Salida en Assets/Resources/Extras/Menu/:
  piso_menu.png            640x360 (20 x 11,25 baldosas; se ve x3 en 1920x1080)
  demoledor_caminar_<0..5> / demoledor_golpe_<0..5>   (copias del arte del demoledor)
  obj_pupitres3, obj_pupitres2, obj_escritorio, obj_lavamanos, grieta_chica, grieta_mediana, grieta_grande
Uso: python Tools/generar_fondo_menu_juego.py
"""
import importlib.util
import os
import random

from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
A = os.path.join(RAIZ, "Assets")
spec = importlib.util.spec_from_file_location("exportar_extras", os.path.join(RAIZ, "Tools", "exportar_extras.py"))
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)


def img(rel):
    return Image.open(os.path.join(A, rel)).convert("RGBA")


def piso():
    T = 32
    W, H = 20, 12
    im = Image.new("RGBA", (W * T, H * T))
    parquet = img("Sprites/Pisos/Aulas300/suelos/aula_parquet.png")
    pasillo = img("Arte/PisoCenital/suelo2_pasillo.png")
    pared = img("Sprites/Pisos/Comunes/paredes/pared_crema.png")
    ventana = img("Sprites/Pisos/Comunes/decoracion/ventana_pared.png")
    pizarra = [img(f"Sprites/Pisos/Comunes/decoracion/pizarra_decorativa_{p}.png") for p in ("izq", "centro", "centro", "der")]
    puerta = img("Sprites/Pisos/Comunes/paredes/puerta_marco.png")
    for y in range(H):
        for x in range(W):
            if y < 2:
                t = pared
            elif x < 12:
                t = parquet
            else:
                t = pasillo
            im.paste(t, (x * T, y * T))
    # pared superior: ventanas, pizarra y puerta
    for x in (1, 3, 14, 16, 18):
        im.alpha_composite(ventana, (x * T, T))
    for i, p in enumerate(pizarra):
        im.alpha_composite(p, ((5 + i) * T, T))
    im.alpha_composite(puerta, (12 * T, T))
    # tabique entre aula y pasillo (línea de pared baja con hueco de puerta)
    tabique = img("Sprites/Pisos/Comunes/paredes/pared_gris.png").resize((8, T))
    for y in range(2, H):
        if y in (6, 7):
            continue
        im.alpha_composite(tabique, (12 * T - 4, y * T))
    # decoración fija
    deco = {"planta": [(0, 2), (11, 2), (19, 2), (19, 10)], "basurero": [(13, 10)], "extintor": [(12, 2)],
            "banca": [(15, 9), (17, 9)], "reciclaje_azul": [(18, 2)], "reciclaje_verde": [(17, 2)]}
    for nombre, posiciones in deco.items():
        s = img(f"Sprites/Pisos/Comunes/decoracion/{nombre}.png")
        for (x, y) in posiciones:
            im.alpha_composite(s, (x * T, y * T))
    # sombra suave en los bordes
    vel = Image.new("RGBA", im.size, (0, 0, 0, 0))
    px = vel.load()
    for y in range(im.height):
        for x in range(im.width):
            dx = min(x, im.width - 1 - x) / 60
            dy = min(y, im.height - 1 - y) / 50
            a = max(0.0, 1 - min(dx, dy, 1))
            px[x, y] = (6, 8, 18, int(110 * a))
    im.alpha_composite(vel)
    return im.crop((0, 0, 640, 360))


def main():
    ex.escribir("Menu/piso_menu", piso(), 100)
    for i in range(6):
        ex.escribir(f"Menu/demoledor_caminar_{i}", img(f"Arte/Demoledor/caminar_der_{i}.png"), 100, True)
        ex.escribir(f"Menu/demoledor_golpe_{i}", img(f"Arte/Demoledor/golpe_{i}.png"), 100, True)
    objs = {"obj_pupitres3": "reparables/fila_pupitres_3x1", "obj_pupitres2": "reparables/fila_pupitres_2x1",
            "obj_escritorio": "reparables/escritorio_docente", "obj_lavamanos": "reparables/lavamanos",
            "grieta_chica": "grietas/grietas_chica", "grieta_mediana": "grietas/grietas_mediana", "grieta_grande": "grietas/grietas_grande"}
    for n, rel in objs.items():
        ex.escribir("Menu/" + n, img(f"Sprites/Pisos/Comunes/{rel}.png"), 100)
    print("ok")


if __name__ == "__main__":
    main()
