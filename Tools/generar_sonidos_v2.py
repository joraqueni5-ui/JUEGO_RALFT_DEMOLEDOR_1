"""Sonidos de la versión 2: peligros de cada piso, jefe final, logros e historia.

Uso: python Tools/generar_sonidos_v2.py   (escribe en Assets/Resources/SonidosExtra/)
"""
import numpy as np
from generar_sonidos_extra import SR, t, ruido, lp, hp, bp, env, tono, guardar


def ton(fr, m, forma):
    return tono(fr, (m + 2) / SR, forma)[:m]


def seg(d):
    return len(t(d))


# ------------------------------------------------------------------ peligros
def gota_cae():
    """Plip de una gota cayendo en un charco."""
    d = 0.35
    tt = t(d)
    f = 1400 * np.exp(-tt * 9) + 500
    plip = tono(f, d, "sin") * env(len(tt), 0.001, 0.08)
    agua = bp(ruido(len(tt)), 1500, 5000) * env(len(tt), 0.001, 0.04) * 0.25
    return plip + agua


def resbalon():
    """Silbato que baja (como en los dibujos animados) y un chirrido de zapatilla."""
    d = 0.8
    tt = t(d)
    f = 1500 * np.exp(-tt * 2.2) + 250
    silbato = tono(f, d, "sin") * env(len(tt), 0.01, 0.6, 0.5) * 0.7
    chirrido = tono(2600 + 300 * np.sin(2 * np.pi * 40 * tt), d, "saw") * env(len(tt), 0.002, 0.08) * 0.15
    return silbato + lp(chirrido, 4000)


def chispazo():
    """Crepitar eléctrico con zumbido."""
    d = 0.6
    n = seg(d)
    tt = t(d)
    crep = hp(ruido(n), 2500) * (np.random.rand(n) > 0.82) * 1.0
    crep = crep * env(n, 0.001, 0.35)
    zumbido = tono(120, d, "saw") * 0.35 + tono(240, d, "sq") * 0.12
    zumbido = lp(zumbido, 1800) * env(n, 0.002, 0.3)
    estallido = bp(ruido(n), 800, 6000) * env(n, 0.0005, 0.03) * 1.4
    return crep + zumbido + estallido


def apagon():
    """Se corta la luz: zumbido que se apaga y un clac."""
    d = 1.3
    n = seg(d)
    tt = t(d)
    f = 120 * np.exp(-tt * 1.6) + 30
    zumbido = (tono(f, d, "saw") * 0.6 + tono(f * 2, d, "sin") * 0.3)
    zumbido = lp(zumbido, 1200) * np.clip(1 - tt / d, 0, 1) ** 1.5
    clac = bp(ruido(n), 500, 3000) * env(n, 0.0005, 0.04) * 1.5
    return zumbido + clac


def luz_vuelve():
    """Vuelve la luz: clic y zumbido de fluorescentes que suben."""
    d = 1.0
    n = seg(d)
    tt = t(d)
    clic = bp(ruido(n), 1000, 5000) * env(n, 0.0005, 0.02) * 1.3
    f = 60 + 60 * np.clip(tt / 0.4, 0, 1)
    zumbido = lp(tono(f, d, "saw") * 0.4 + tono(f * 2, d, "sin") * 0.3, 1500) * env(n, 0.05, 0.5, 0.3)
    parpadeo = (np.sin(2 * np.pi * 14 * tt) > 0) * 0.4 + 0.6
    return clic + zumbido * parpadeo


def rafaga():
    """Ráfaga de viento que crece y se va."""
    d = 2.0
    n = seg(d)
    tt = t(d)
    forma = np.sin(np.pi * tt / d) ** 2
    viento = bp(ruido(n), 250, 1600) * forma
    silbido = tono(700 + 250 * np.sin(2 * np.pi * 0.7 * tt), d, "sin") * forma * 0.08
    return viento + silbido


# ------------------------------------------------------------------ jefe final
def jefe_transforma():
    """Rugido que sube, temblor y un acorde de órgano."""
    d = 2.6
    n = seg(d)
    tt = t(d)
    f = 70 + 90 * (tt / d) ** 1.4
    rugido = lp(tono(f, d, "saw") * 0.6 + tono(f * 1.5, d, "saw") * 0.3, 900) * env(n, 0.3, 1.6, 1.0)
    rugido *= 1 + 0.3 * np.sin(2 * np.pi * 23 * tt)
    temblor = lp(ruido(n), 120) * env(n, 0.1, 2.0, 1.0) * 1.2
    acorde = np.zeros(n)
    for fr in (130.8, 155.6, 196.0, 261.6):   # do menor
        acorde += tono(fr, d, "sq") * 0.08
    acorde = lp(acorde, 2200) * np.clip((tt - 1.2) / 0.3, 0, 1) * env(n, 0.001, 2.6, 1.0)
    return rugido + temblor + acorde


def jefe_lanza():
    """Silbido de algo pesado que vuela."""
    d = 0.7
    n = seg(d)
    tt = t(d)
    f = 400 + 900 * (tt / d)
    soplo = bp(ruido(n), 300, 2200) * np.sin(np.pi * tt / d) * 0.8
    tonito = tono(f, d, "sin") * np.sin(np.pi * tt / d) * 0.15
    return soplo + tonito


def desinflar():
    """Globo que se desinfla (el jefe vuelve a su tamaño)."""
    d = 1.4
    n = seg(d)
    tt = t(d)
    f = 520 * np.exp(-tt * 1.2) + 90 + 40 * np.sin(2 * np.pi * 9 * tt)
    pedorreta = lp(tono(f, d, "saw"), 1800) * env(n, 0.01, 1.2, 0.8) * 0.7
    aire = bp(ruido(n), 600, 3000) * env(n, 0.02, 1.1, 0.7) * 0.25
    return pedorreta + aire


# ------------------------------------------------------------------ logros
def logro():
    """Arpegio brillante de logro."""
    d = 1.1
    n = seg(d)
    x = np.zeros(n)
    notas = [523.3, 659.3, 784.0, 1046.5]
    for i, fr in enumerate(notas):
        p = int(i * 0.09 * SR)
        m = n - p
        s = (ton(fr, m, "sq") * 0.25 + ton(fr * 2, m, "sin") * 0.2) * env(m, 0.002, 0.45)
        x[p:] += lp(s, 5000)
    brillo = hp(ruido(n), 6000) * env(n, 0.3, 0.5) * 0.06
    return x + brillo


def desbloqueo():
    """Destello mágico: campanitas que suben."""
    d = 1.3
    n = seg(d)
    x = np.zeros(n)
    for i in range(8):
        fr = 880 * 2 ** (i / 6)
        p = int(i * 0.06 * SR)
        m = n - p
        x[p:] += ton(fr, m, "sin") * env(m, 0.001, 0.35) * 0.3
    return x


# ------------------------------------------------------------------ historia
def despertador():
    """Despertador de campanas sonando."""
    d = 1.8
    n = seg(d)
    tt = t(d)
    golpeteo = (np.sin(2 * np.pi * 22 * tt) > 0).astype(float)
    campana = tono(2100, d, "sin") * 0.5 + tono(3150, d, "sin") * 0.3 + tono(4300, d, "sin") * 0.15
    x = campana * golpeteo
    x = lp(x, 7000) * env(n, 0.005, 1.7, 1.6)
    return x


def puf_sueno():
    """Brillo de sueño: escala mágica que baja con reverberación."""
    d = 1.8
    n = seg(d)
    x = np.zeros(n)
    for i in range(10):
        fr = 1760 * 2 ** (-i / 7)
        p = int(i * 0.07 * SR)
        m = n - p
        x[p:] += (ton(fr, m, "tri") * 0.2) * env(m, 0.002, 0.6)
    eco = np.zeros(n)
    retardo = int(0.18 * SR)
    eco[retardo:] = x[:-retardo] * 0.4
    return x + eco + hp(ruido(n), 5000) * env(n, 0.2, 1.2) * 0.05


def ronquido():
    """Ronquido de dibujo animado (inspira y silba)."""
    d = 2.0
    n = seg(d)
    tt = t(d)
    inspira = lp(ruido(n), 400) * (1 + np.sin(2 * np.pi * 30 * tt)) * np.clip(np.sin(np.pi * tt / 1.1), 0, 1) * (tt < 1.1)
    silbido = tono(900 - 300 * ((tt - 1.1) / 0.9), d, "sin") * np.clip(np.sin(np.pi * (tt - 1.1) / 0.9), 0, 1) * (tt >= 1.1) * 0.3
    return inspira * 0.8 + silbido


def torbellino():
    """Remolino: viento que gira (sube y baja) con un silbido."""
    d = 1.6
    n = seg(d)
    tt = t(d)
    giro = 0.5 + 0.5 * np.sin(2 * np.pi * 7 * tt)
    viento = bp(ruido(n), 400, 2600) * giro * env(n, 0.1, 1.4, 1.0)
    silbido = tono(900 + 400 * np.sin(2 * np.pi * 7 * tt), d, "sin") * 0.12 * env(n, 0.1, 1.4, 1.0)
    return viento + silbido


def laser_robot():
    """Carga eléctrica que sube y disparo de láser."""
    d = 1.3
    n = seg(d)
    tt = t(d)
    carga = tono(200 + 1400 * (tt / 0.9) ** 2, d, "sq") * (tt < 0.9) * 0.18
    carga = lp(carga, 4000)
    rayo = (tono(1200 - 600 * np.clip((tt - 0.9) / 0.4, 0, 1), d, "saw") * 0.4 + hp(ruido(n), 3000) * 0.3) * (tt >= 0.9)
    rayo = rayo * np.clip(1 - (tt - 0.9) / 0.4, 0, 1)
    return carga + lp(rayo, 6000)


def oleada():
    """Sirena corta de aviso de oleada."""
    d = 1.6
    tt = t(d)
    f = 500 + 300 * np.abs(np.sin(2 * np.pi * 1.2 * tt))
    return lp(tono(f, d, "saw") * 0.5 + tono(f * 1.5, d, "sq") * 0.15, 3000) * env(len(tt), 0.05, 1.4, 1.2)


def ding():
    """Campanita del ascensor (dos notas)."""
    d = 1.4
    n = seg(d)
    x = np.zeros(n)
    for i, fr in enumerate((1318.5, 1046.5)):
        p = int(i * 0.28 * SR)
        m = n - p
        x[p:] += (ton(fr, m, "sin") * 0.5 + ton(fr * 2.01, m, "sin") * 0.15) * env(m, 0.002, 0.9)
    return x


def latido():
    """Bip del monitor de signos vitales."""
    d = 0.16
    n = seg(d)
    return tono(1000, d, "sin") * env(n, 0.002, 0.12) * 0.6


def libros_caen():
    """Libros que caen al piso: golpes sordos de papel."""
    d = 0.9
    n = seg(d)
    x = np.zeros(n)
    for i in range(4):
        p = int((0.05 + i * 0.16 + np.random.rand() * 0.05) * SR)
        m = min(n - p, int(0.18 * SR))
        golpe = lp(ruido(m), 900) * env(m, 0.001, 0.12) + bp(ruido(m), 1500, 5000) * env(m, 0.001, 0.04) * 0.4
        x[p:p + m] += golpe * (1 - i * 0.15)
    return x


def reparado():
    """Destello de objeto reparado: brillo que sube."""
    d = 0.6
    n = seg(d)
    tt = t(d)
    f = 700 + 900 * (tt / d)
    return (tono(f, d, "sin") * 0.35 + tono(f * 1.5, d, "tri") * 0.15) * env(n, 0.005, 0.5)


SONIDOS = {
    "ding": ding, "latido": latido, "libros_caen": libros_caen, "reparado": reparado,
    "torbellino": torbellino, "laser_robot": laser_robot, "oleada": oleada,
    "gota_cae": gota_cae, "resbalon": resbalon, "chispazo": chispazo, "apagon": apagon, "luz_vuelve": luz_vuelve,
    "rafaga": rafaga, "jefe_transforma": jefe_transforma, "jefe_lanza": jefe_lanza, "desinflar": desinflar,
    "logro": logro, "desbloqueo": desbloqueo, "despertador": despertador, "puf_sueno": puf_sueno, "ronquido": ronquido,
}

if __name__ == "__main__":
    np.random.seed(7)
    for nombre, f in SONIDOS.items():
        guardar(nombre, f())
