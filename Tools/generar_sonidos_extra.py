"""Sonidos extra del juego, sintetizados por código (WAV 44,1 kHz, mono, 16 bits).

Genera en Assets/Resources/SonidosExtra/ (con su .meta):
  Demoledor: alarma_aparicion, rugido_demoledor, paso_demoledor, gruñido_cerca, empujon_demoledor, golpe_fuerte_jugador
  Mini villanos: mini_llamada, mini_golpe, mini_mordida, mini_vencido, topadora_motor_bucle
  Potenciadores: poder_turbo, poder_martillo, poder_congelar, poder_casco, casco_roto, congelado
  Ambiente (bucles): ambiente_universidad, ambiente_terraza, ambiente_fachada, ambiente_menu

Uso: python Tools/generar_sonidos_extra.py
"""
import hashlib
import os
import wave

import numpy as np
from scipy import signal

SR = 44100
RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(RAIZ, "Assets", "Resources", "SonidosExtra")
rng = np.random.default_rng(1234)

META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: {load}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: {comp}
    quality: {calidad}
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid(ruta):
    rel = os.path.relpath(ruta, RAIZ).replace("\\", "/").lower()
    return hashlib.md5(rel.encode("utf-8")).hexdigest()


def t(seg):
    return np.arange(int(SR * seg)) / SR


def ruido(n):
    return rng.uniform(-1, 1, n)


def lp(x, fc, orden=2):
    b, a = signal.butter(orden, min(fc, SR / 2 - 100) / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def hp(x, fc, orden=2):
    b, a = signal.butter(orden, fc / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def bp(x, f1, f2, orden=2):
    b, a = signal.butter(orden, [f1 / (SR / 2), f2 / (SR / 2)], "band")
    return signal.lfilter(b, a, x)


def env(n, ataque=0.005, caida=0.2, sostener=0.0):
    e = np.ones(n)
    a = max(1, int(ataque * SR))
    e[:a] = np.linspace(0, 1, a)
    s = int(sostener * SR)
    resto = n - a - s
    if resto > 0:
        e[a + s:] = np.exp(-np.arange(resto) / (caida * SR))
    return e


def fade(x, ini=0.005, fin=0.02):
    x = x.copy()
    a, b = int(ini * SR), int(fin * SR)
    if a: x[:a] *= np.linspace(0, 1, a)
    if b: x[-b:] *= np.linspace(1, 0, b)
    return x


def tono(frec, seg, forma="sin"):
    fase = 2 * np.pi * np.cumsum(np.broadcast_to(frec, (int(SR * seg),))) / SR
    if forma == "sin": return np.sin(fase)
    if forma == "saw": return 2 * ((fase / (2 * np.pi)) % 1) - 1
    if forma == "sq": return np.sign(np.sin(fase))
    if forma == "tri": return 2 * np.abs(2 * ((fase / (2 * np.pi)) % 1) - 1) - 1


def norm(x, pico_db=-3.0):
    m = np.max(np.abs(x)) or 1
    return x / m * 10 ** (pico_db / 20)


def bucle(x, cruce=1.0):
    """Hace el final empalmable con el principio (fundido cruzado)."""
    n = int(cruce * SR)
    cabeza, cola = x[:n], x[-n:]
    r = np.linspace(0, 1, n)
    mezcla = cola * np.cos(r * np.pi / 2) + cabeza * np.sin(r * np.pi / 2)
    return np.concatenate([mezcla, x[n:-n]])


def guardar(nombre, x, ambiente=False, pico=-3.0):
    os.makedirs(OUT, exist_ok=True)
    x = norm(x, pico)
    ruta = os.path.join(OUT, nombre + ".wav")
    with wave.open(ruta, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype("<i2").tobytes())
    # Ambientes largos: comprimidos y en streaming. Efectos: descomprimidos en memoria.
    meta = META.format(guid=guid(ruta), load=2 if ambiente else 0, comp=1 if ambiente else 2, calidad=0.7 if ambiente else 1)
    with open(ruta + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)
    print(f"  {nombre}.wav  {len(x) / SR:.2f} s")


# ------------------------------------------------------------------ demoledor
def alarma_aparicion():
    d = 1.8
    tt = t(d)
    f = np.where((tt * 2.5) % 1 < 0.5, 660, 880)
    f = lp(f.astype(float), 30)
    x = tono(f, d, "sq") * 0.5 + tono(f * 1.01, d, "saw") * 0.3
    x = lp(x, 3500) * env(len(tt), 0.01, 0.9, 0.9)
    return x


def rugido_demoledor():
    d = 1.6
    tt = t(d)
    f0 = 85 + 30 * np.sin(np.pi * tt / d) - 20 * tt
    vib = 1 + 0.04 * np.sin(2 * np.pi * 7 * tt) + 0.03 * ruido(len(tt))
    base = tono(f0 * vib, d, "saw")
    voz = bp(base, 250, 900) * 1.6 + bp(base, 900, 2200) * 0.6 + lp(base, 250)
    aspero = bp(ruido(len(tt)), 300, 2500) * (0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 23 * tt)))
    x = voz + aspero * 0.6
    x = np.tanh(x * 2.2)
    return x * env(len(tt), 0.12, 0.45, 0.6)


def paso_demoledor():
    d = 0.22
    tt = t(d)
    golpe = np.sin(2 * np.pi * (70 - 120 * tt) * tt) * env(len(tt), 0.002, 0.07)
    pol = lp(ruido(len(tt)), 900) * env(len(tt), 0.001, 0.03) * 0.6
    return golpe + pol


def grunido_cerca():
    d = 0.55
    tt = t(d)
    f0 = 110 - 40 * tt
    base = tono(f0 * (1 + 0.05 * np.sin(2 * np.pi * 9 * tt)), d, "saw")
    x = np.tanh((bp(base, 200, 1100) * 2 + bp(ruido(len(tt)), 400, 1800) * 0.4) * 2)
    return x * env(len(tt), 0.04, 0.18, 0.2)


def empujon_demoledor():
    d = 0.6
    tt = t(d)
    corte = 400 + 3000 * np.exp(-tt * 7)
    whoosh = np.zeros(len(tt))
    r = ruido(len(tt))
    for i in range(0, len(tt), 512):
        whoosh[i:i + 512] = bp(r[i:i + 512], 200, max(300, corte[i]))
    whoosh *= np.sin(np.pi * np.clip(tt / 0.25, 0, 1)) * (tt < 0.25)
    thud_t = np.clip(tt - 0.2, 0, None)
    thud = np.sin(2 * np.pi * (90 - 60 * thud_t) * thud_t) * np.exp(-thud_t / 0.12) * (tt >= 0.2)
    crunch = lp(ruido(len(tt)), 2500) * np.exp(-thud_t / 0.05) * (tt >= 0.2) * 0.7
    return whoosh * 0.8 + thud * 1.2 + crunch


def golpe_fuerte_jugador():
    d = 0.45
    tt = t(d)
    x = np.sin(2 * np.pi * (180 - 250 * tt) * tt) * env(len(tt), 0.001, 0.12)
    x += hp(ruido(len(tt)), 1500) * env(len(tt), 0.001, 0.04) * 0.6
    x += tono(900 - 600 * tt, d, "sq") * env(len(tt), 0.001, 0.08) * 0.15   # "¡ay!" arcade
    return np.tanh(x * 1.5)


# ------------------------------------------------------------------ mini villanos
def mini_llamada():
    d = 0.5
    tt = t(d)
    f = np.where(tt < 0.22, 1400 + 900 * (tt / 0.22), 1100 + 1300 * ((tt - 0.25) / 0.25))
    x = tono(f, d) * ((tt < 0.2) | (tt > 0.26))
    return lp(x, 5000) * env(len(tt), 0.01, 0.25, 0.25)


def mini_golpe():
    d = 0.2
    tt = t(d)
    x = sum(np.sin(2 * np.pi * f * tt) * a for f, a in [(1800, 1), (2900, 0.6), (4100, 0.3)])
    return x * env(len(tt), 0.001, 0.05) + lp(ruido(len(tt)), 3000) * env(len(tt), 0.001, 0.015) * 0.5


def mini_mordida():
    d = 0.3
    tt = t(d)
    pulsos = (np.sin(2 * np.pi * 28 * tt) > 0.6).astype(float)
    x = bp(ruido(len(tt)), 1500, 6000) * pulsos
    return x * env(len(tt), 0.005, 0.2, 0.05)


def mini_vencido():
    d = 0.5
    tt = t(d)
    poof = lp(ruido(len(tt)), 1800) * env(len(tt), 0.002, 0.08)
    blip = tono(900 * np.exp(-tt * 3.5), d, "sq") * env(len(tt), 0.01, 0.2) * 0.35
    return lp(poof + blip, 6000)


def topadora_motor_bucle():
    d = 2.0
    tt = t(d)
    f = 34 + 2 * np.sin(2 * np.pi * 1 * tt)
    pist = (np.sin(2 * np.pi * np.cumsum(f) / SR) > 0.3).astype(float)
    x = lp(pist * 0.8 + ruido(len(tt)) * 0.3, 500) + lp(tono(f * 2, d, "saw"), 300) * 0.4
    return bucle(x, 0.4)


# ------------------------------------------------------------------ potenciadores
def poder_turbo():
    d = 0.55
    tt = t(d)
    x = tono(300 + 1600 * tt / d, d, "saw") * 0.4 + bp(ruido(len(tt)), 800, 5000) * 0.3
    return lp(x, 6000) * env(len(tt), 0.01, 0.2, 0.3)


def poder_martillo():
    d = 0.8
    tt = t(d)
    x = sum(np.sin(2 * np.pi * f * tt) * a * np.exp(-tt / dd) for f, a, dd in [(1320, 1, 0.4), (1980, 0.5, 0.3), (2640, 0.4, 0.2), (3960, 0.2, 0.1)])
    x += hp(ruido(len(tt)), 4000) * env(len(tt), 0.001, 0.03) * 0.4
    return x


def poder_congelar():
    d = 1.0
    tt = t(d)
    x = np.zeros(len(tt))
    for i, f in enumerate([1568, 2093, 2637, 3136, 4186]):
        ini = i * 0.07
        tl = np.clip(tt - ini, 0, None)
        x += np.sin(2 * np.pi * f * tl) * np.exp(-tl / 0.25) * (tt >= ini)
    x += hp(ruido(len(tt)), 6000) * env(len(tt), 0.05, 0.3) * 0.25
    return x


def poder_casco():
    d = 0.45
    tt = t(d)
    x = sum(np.sin(2 * np.pi * f * tt) * a for f, a in [(520, 1), (1180, 0.6), (2210, 0.3)]) * env(len(tt), 0.001, 0.12)
    return x + tono(660, d, "sq") * env(len(tt), 0.08, 0.1) * 0.15 * (tt > 0.12)


def casco_roto():
    d = 0.6
    tt = t(d)
    x = hp(ruido(len(tt)), 2000) * env(len(tt), 0.001, 0.1)
    x += sum(np.sin(2 * np.pi * f * tt) for f in (740, 1530)) * env(len(tt), 0.001, 0.15) * 0.5
    return x


def congelado():
    d = 0.7
    tt = t(d)
    cr = (rng.random(len(tt)) < 0.004).astype(float)
    cr = bp(cr, 3000, 9000) * 8
    return (cr + hp(ruido(len(tt)), 5000) * 0.15) * env(len(tt), 0.01, 0.3)


# ------------------------------------------------------------------ ambientes (bucles)
def murmullo(n):
    """Murmullo de gente lejana: ruido con formantes de voz que cambian despacio."""
    x = np.zeros(n)
    for _ in range(6):
        f1 = rng.uniform(300, 800)
        f2 = f1 * rng.uniform(1.8, 3.2)
        voz = bp(ruido(n), f1 * 0.8, f1 * 1.2) + 0.5 * bp(ruido(n), f2 * 0.85, f2 * 1.15)
        sil = np.clip(lp(ruido(n), 3) * 6 + 0.2, 0, 1)    # frases con pausas
        x += voz * sil
    return lp(x, 1800)


def eventos(x, sonido, cantidad, vol):
    n = len(x)
    for _ in range(cantidad):
        s = sonido()
        i = rng.integers(0, n - len(s) - 1)
        x[i:i + len(s)] += s * vol * rng.uniform(0.6, 1.0)
    return x


def puerta():
    d = 0.4
    tt = t(d)
    return lp(np.sin(2 * np.pi * (60 + 30 * rng.random()) * tt) * env(len(tt), 0.002, 0.08) + ruido(len(tt)) * env(len(tt), 0.001, 0.02) * 0.4, 1200)


def pasos_lejanos():
    out = np.zeros(int(SR * 2.4))
    for k in range(6):
        tt = t(0.08)
        p = lp(ruido(len(tt)), 1500 + 500 * (k % 2)) * env(len(tt), 0.001, 0.02)
        i = int(k * 0.4 * SR)
        out[i:i + len(p)] += p
    return out


def pajaro():
    d = rng.uniform(0.25, 0.5)
    tt = t(d)
    f = rng.uniform(2800, 4200) + 600 * np.sin(2 * np.pi * rng.uniform(12, 25) * tt)
    return tono(f, d) * env(len(tt), 0.01, 0.08, d * 0.3) * (np.sin(2 * np.pi * 8 * tt) > -0.2)


def bocina():
    d = 0.6
    tt = t(d)
    return lp(tono(rng.choice([392, 440, 330]), d, "saw") + tono(rng.choice([494, 523]), d, "saw"), 1500) * env(len(tt), 0.02, 0.1, 0.4)


def clank():
    d = 0.7
    tt = t(d)
    return sum(np.sin(2 * np.pi * f * tt) * np.exp(-tt / 0.15) for f in rng.uniform(300, 1400, 3)) + lp(ruido(len(tt)), 2500) * env(len(tt), 0.001, 0.02)


def viento(n, fuerza=1.0):
    r = ruido(n)
    mod = np.clip(lp(ruido(n), 0.5) * 8 + 0.6, 0.15, 1.3)
    a = bp(r, 150, 900) * mod
    b = bp(ruido(n), 900, 2600) * np.clip(mod - 0.5, 0, 1) * 0.5    # silbido en ráfagas
    return (a + b) * fuerza


def capa_amenaza_bucle():
    """Capa de música que sube cuando el demoledor está cerca: latido grave, bombo y un zumbido oscuro (8 s, 90 BPM)."""
    d = 8.0
    n = int(SR * d)
    tt = np.arange(n) / SR
    x = np.zeros(n)
    beat = 60 / 90
    for k in range(int(d / beat)):
        for off, amp in ((0.0, 1.0), (0.18, 0.6)):          # latido: tum-tum
            i0 = int((k * beat + off) * SR)
            tl = np.arange(int(0.35 * SR)) / SR
            golpe = np.sin(2 * np.pi * (55 + 40 * np.exp(-tl * 30)) * tl) * np.exp(-tl / 0.09) * amp
            x[i0:i0 + len(golpe)] += golpe[:max(0, n - i0)]
    for k in range(int(d / (beat / 2))):                     # hi-hat metálico suave en corcheas
        i0 = int(k * beat / 2 * SR)
        tl = np.arange(int(0.05 * SR)) / SR
        h = hp(ruido(len(tl)), 7000) * np.exp(-tl / 0.012) * (0.25 if k % 2 else 0.12)
        x[i0:i0 + len(h)] += h[:max(0, n - i0)]
    dron = (tono(np.full(n, 41.25), d, "saw") + tono(np.full(n, 61.75), d, "saw") * 0.6 + tono(np.full(n, 43.625), d, "saw") * 0.4)
    dron = lp(dron, 260) * (0.55 + 0.45 * np.sin(2 * np.pi * tt / d) ** 2)
    return x * 0.9 + dron * 0.35


# ------------------------------------------------------------------ voces (balbuceo tipo videojuego) y gritos
FORMANTES = {"a": (800, 1200), "e": (450, 1900), "i": (300, 2300), "o": (500, 900), "u": (350, 800)}
VOCES = {"hombre": (135, 0.0), "mujer": (225, 0.0), "mayor": (115, 0.06), "villano": (82, 0.03), "mini": (430, 0.0)}


def silaba(f0, vocal, vibrato=0.0, d=0.1):
    tt = t(d)
    f = f0 * (1 + 0.06 * np.sin(2 * np.pi * 6 * tt) * (vibrato > 0) + vibrato * np.sin(2 * np.pi * 9 * tt)) * (1 + 0.08 * (tt / d))
    fuente = tono(f, d, "saw") + 0.15 * ruido(len(tt))
    f1, f2 = FORMANTES[vocal]
    x = bp(fuente, f1 * 0.8, f1 * 1.25) * 1.2 + bp(fuente, f2 * 0.85, f2 * 1.15) * 0.7 + lp(fuente, 300) * 0.4
    return x * env(len(tt), 0.008, d * 0.45, d * 0.35)


def voces():
    res = {}
    for tipo, (f0, vib) in VOCES.items():
        for k, vocal in enumerate("aeiou"):
            res[f"voz_{tipo}_{k}"] = silaba(f0 * rng.uniform(0.96, 1.04), vocal, vib, 0.11 if tipo != "mini" else 0.07)
    return res


def grito(f0=320, d=0.8):
    tt = t(d)
    curva = f0 * (1 + 0.5 * np.sin(np.pi * tt / d)) * (1 + 0.03 * np.sin(2 * np.pi * 7 * tt))
    fuente = tono(curva, d, "saw") + 0.3 * ruido(len(tt))
    x = bp(fuente, 700, 1300) * 1.3 + bp(fuente, 1600, 2600) * 0.6
    return np.tanh(x * 2) * env(len(tt), 0.03, d * 0.5, d * 0.4)


def grito_multitud():
    d = 2.2
    n = int(SR * d)
    x = np.zeros(n)
    for _ in range(9):
        g = grito(rng.uniform(220, 520), rng.uniform(0.6, 1.3))
        i = int(rng.uniform(0, 0.6) * SR)
        x[i:i + len(g)] += g[:n - i] * rng.uniform(0.4, 1.0)
    pasos = np.zeros(n)
    for k in range(40):   # gente corriendo
        tl = t(0.05)
        p = lp(ruido(len(tl)), 1800) * env(len(tl), 0.001, 0.015)
        i = int(rng.uniform(0.2, d - 0.1) * SR)
        pasos[i:i + len(p)] += p[:n - i] * 0.5
    return x + pasos


# ------------------------------------------------------------------ furia del estudiante
def ira_lista():
    d = 0.9
    tt = t(d)
    x = np.zeros(len(tt))
    for i, f in enumerate([523, 659, 784, 1047]):
        ini = i * 0.09
        tl = np.clip(tt - ini, 0, None)
        x += (tono(np.full(len(tt), f), d, "tri") * np.exp(-tl / 0.35)) * (tt >= ini)
    return x + hp(ruido(len(tt)), 6000) * env(len(tt), 0.02, 0.3) * 0.15


def ira_activar():
    d = 2.0
    tt = t(d)
    golpe = np.sin(2 * np.pi * (60 - 30 * tt) * tt) * np.exp(-tt / 0.35) * 1.4
    coro = np.zeros(len(tt))
    for f in (110, 165, 220, 277, 330):   # acorde heroico que sube
        coro += tono(f * (1 + 0.5 * np.clip(tt / 1.2, 0, 1)), d, "saw") * 0.25
    coro = lp(coro, 2500) * np.clip(tt / 0.5, 0, 1) * np.exp(-np.clip(tt - 1.2, 0, None) / 0.3)
    whoosh = bp(ruido(len(tt)), 300, 4000) * np.sin(np.pi * np.clip(tt / 1.0, 0, 1)) * 0.6
    return np.tanh((golpe + coro + whoosh) * 1.3)


def ira_bucle():
    d = 2.0
    tt = t(d)
    hum = tono(np.full(len(tt), 55.0), d, "saw") * 0.5 + tono(np.full(len(tt), 110.5), d, "saw") * 0.3
    chispa = (rng.random(len(tt)) < 0.003).astype(float)
    chispa = bp(chispa, 3000, 9000) * 6
    x = lp(hum, 400) * (0.8 + 0.2 * np.sin(2 * np.pi * 4 * tt)) + chispa * 0.4
    return bucle(x, 0.3)


def ira_onda():
    d = 0.7
    tt = t(d)
    x = np.sin(2 * np.pi * (90 - 50 * tt) * tt) * np.exp(-tt / 0.15) * 1.2
    x += bp(ruido(len(tt)), 400, 5000) * np.exp(-tt / 0.08)
    x += tono(np.full(len(tt), 880.0), d, "sq") * np.exp(-tt / 0.1) * 0.1
    return np.tanh(x * 1.4)


def ambiente_universidad():
    n = int(SR * 24)
    tono_sala = lp(ruido(n), 220) * 0.6 + np.sin(2 * np.pi * 60 * np.arange(n) / SR) * 0.02   # aire acondicionado
    x = tono_sala + murmullo(n) * 0.35
    x = eventos(x, puerta, 4, 0.5)
    x = eventos(x, pasos_lejanos, 3, 0.25)
    return bucle(x, 1.5)


def ambiente_terraza():
    n = int(SR * 24)
    x = viento(n, 1.0) + lp(ruido(n), 120) * 0.3
    x = eventos(x, pajaro, 14, 0.18)
    x = eventos(x, bocina, 2, 0.08)
    return bucle(x, 1.5)


def ambiente_fachada():
    n = int(SR * 24)
    trafico = lp(ruido(n), 300) * (0.7 + 0.3 * np.sin(2 * np.pi * 0.07 * np.arange(n) / SR))
    x = trafico + viento(n, 0.8)
    x = eventos(x, bocina, 4, 0.12)
    x = eventos(x, pajaro, 4, 0.1)
    return bucle(x, 1.5)


def ambiente_menu():
    n = int(SR * 20)
    x = viento(n, 0.9) + lp(ruido(n), 90) * 0.4
    x = eventos(x, clank, 5, 0.12)
    return bucle(x, 1.5)


if __name__ == "__main__":
    efectos = {
        "alarma_aparicion": alarma_aparicion, "rugido_demoledor": rugido_demoledor, "paso_demoledor": paso_demoledor,
        "grunido_cerca": grunido_cerca, "empujon_demoledor": empujon_demoledor, "golpe_fuerte_jugador": golpe_fuerte_jugador,
        "mini_llamada": mini_llamada, "mini_golpe": mini_golpe, "mini_mordida": mini_mordida, "mini_vencido": mini_vencido,
        "topadora_motor_bucle": topadora_motor_bucle,
        "poder_turbo": poder_turbo, "poder_martillo": poder_martillo, "poder_congelar": poder_congelar,
        "poder_casco": poder_casco, "casco_roto": casco_roto, "congelado": congelado,
        "capa_amenaza_bucle": capa_amenaza_bucle,
        "ira_lista": ira_lista, "ira_activar": ira_activar, "ira_bucle": ira_bucle, "ira_onda": ira_onda,
    }
    for nombre, f in efectos.items():
        guardar(nombre, f() if nombre.endswith("_bucle") else fade(f()))
    for nombre, x in voces().items():
        guardar(nombre, fade(x, 0.003, 0.01), pico=-6.0)
    guardar("grito_multitud", fade(grito_multitud()))
    guardar("grito_susto", fade(grito(360, 0.7)))
    for nombre, f in {"ambiente_universidad": ambiente_universidad, "ambiente_terraza": ambiente_terraza,
                      "ambiente_fachada": ambiente_fachada, "ambiente_menu": ambiente_menu}.items():
        guardar(nombre, f(), ambiente=True, pico=-6.0)
