"""Sonidos de los movimientos especiales del demoledor (embestida y pisotón) y del multijugador.

Uso: python Tools/generar_sonidos_especiales.py   (escribe en Assets/Resources/SonidosExtra/)
"""
import numpy as np
from generar_sonidos_extra import SR, t, ruido, lp, hp, bp, env, fade, tono, guardar


def embestida_carga():
    """Toma de aire y zumbido que sube: el demoledor agacha la cabeza y arranca."""
    d = 0.9
    tt = t(d)
    f = 70 + 160 * (tt / d) ** 1.6
    cuerpo = tono(f, d, "saw") * 0.5 + tono(f * 0.5, d, "sin") * 0.6
    cuerpo = lp(cuerpo, 900) * np.linspace(0.2, 1, len(tt))
    soplo = bp(ruido(len(tt)), 300, 2400) * np.linspace(0.1, 0.9, len(tt)) ** 2 * 0.6
    return cuerpo + soplo


def embestida_correr():
    """Ráfaga de viento con pisadas rápidas mientras embiste."""
    d = 0.7
    n = len(t(d))
    viento = bp(ruido(n), 200, 1800) * env(n, 0.05, 0.4, 0.2) * 0.8
    golpes = np.zeros(n)
    for i in range(5):
        p = int((0.05 + i * 0.12) * SR)
        g = lp(ruido(int(0.08 * SR)), 180) * env(int(0.08 * SR), 0.002, 0.03)
        golpes[p:p + len(g)] += g[: n - p] * 1.4
    return viento + golpes


def choque_muro():
    """Se estrella contra la pared: golpe grave, cascotes y un 'boing' de mareo."""
    d = 1.1
    n = len(t(d))
    golpe = lp(ruido(n), 140) * env(n, 0.002, 0.18) * 2.2
    grave = tono(np.linspace(85, 40, n), d, "sin") * env(n, 0.002, 0.25) * 1.2
    cascotes = hp(ruido(n), 1800) * env(n, 0.01, 0.12) * 0.35
    boing_f = 420 + 80 * np.sin(2 * np.pi * 7 * t(d))
    boing = tono(boing_f, d, "tri") * np.concatenate([np.zeros(int(0.25 * SR)), env(n - int(0.25 * SR), 0.01, 0.3)]) * 0.25
    return golpe + grave + cascotes + boing


def pisoton_salto():
    """Gruñido corto de esfuerzo al saltar."""
    d = 0.5
    tt = t(d)
    f = 120 + 60 * np.sin(np.pi * tt / d)
    voz = lp(tono(f, d, "saw"), 700) * env(len(tt), 0.03, 0.2, 0.1)
    aire = bp(ruido(len(tt)), 400, 1500) * env(len(tt), 0.05, 0.15) * 0.3
    return voz + aire


def pisoton_impacto():
    """Terremoto: boom muy grave, crujido del piso y temblor que se apaga."""
    d = 1.6
    n = len(t(d))
    boom = tono(np.linspace(60, 28, n), d, "sin") * env(n, 0.002, 0.45) * 1.6
    golpe = lp(ruido(n), 220) * env(n, 0.001, 0.12) * 2.0
    crujido = bp(ruido(n), 900, 4000) * env(n, 0.004, 0.08) * 0.5
    temblor = lp(ruido(n), 60) * env(n, 0.05, 0.6, 0.2) * 0.8
    return boom + golpe + crujido + temblor


def red_conectado():
    """Dos notas que suben: tu compañero se unió."""
    a = tono(660, 0.12, "tri") * env(len(t(0.12)), 0.005, 0.08)
    b = tono(990, 0.22, "tri") * env(len(t(0.22)), 0.005, 0.15)
    return np.concatenate([a, b]) * 0.7


if __name__ == "__main__":
    for nombre, f in {
        "embestida_carga": embestida_carga, "embestida_correr": embestida_correr, "choque_muro": choque_muro,
        "pisoton_salto": pisoton_salto, "pisoton_impacto": pisoton_impacto, "red_conectado": red_conectado,
    }.items():
        guardar(nombre, fade(f()))
