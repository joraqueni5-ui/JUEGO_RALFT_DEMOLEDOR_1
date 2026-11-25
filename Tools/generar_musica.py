"""Música chiptune generada por código: un tema por piso, el jefe final, la historia y el final del sueño.

Cada pista es un bucle exacto (16 compases, A A' B A) con bajo, batería, melodía, arpegio y acordes. La melodía sale
de una semilla propia, así cada piso tiene su tema. Se guardan en OGG (Vorbis) en streaming.

Salida: Assets/Resources/MusicaExtra/<nombre>.ogg (+ .meta)
Uso: python Tools/generar_musica.py
"""
import hashlib
import os
import subprocess
import tempfile
import wave

import numpy as np
from scipy import signal

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(RAIZ, "Assets", "Resources", "MusicaExtra")
SR = 32000

META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 2
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.6
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: 1
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

MAYOR = [0, 2, 4, 5, 7, 9, 11]
MENOR = [0, 2, 3, 5, 7, 8, 10]
ARMONICA = [0, 2, 3, 5, 7, 8, 11]


def frec(midi):
    return 440.0 * 2 ** ((midi - 69) / 12.0)


def onda(forma, f, n, ciclo=0.5, vibrato=0.0):
    tt = np.arange(n) / SR
    ff = f * (1 + vibrato * np.sin(2 * np.pi * 5.5 * tt) * np.clip(tt / 0.15, 0, 1))
    fase = np.cumsum(ff) / SR
    p = fase % 1.0
    if forma == "pulso":
        return np.where(p < ciclo, 1.0, -1.0)
    if forma == "tri":
        return 2 * np.abs(2 * p - 1) - 1
    if forma == "saw":
        return 2 * p - 1
    return np.sin(2 * np.pi * fase)


def adsr(n, a=0.005, d=0.08, s=0.6, r=0.04):
    e = np.ones(n) * s
    na, nd, nr = int(a * SR), int(d * SR), int(r * SR)
    na = max(1, min(na, n)); e[:na] = np.linspace(0, 1, na)
    if na < n:
        k = min(nd, n - na)
        e[na:na + k] = np.linspace(1, s, k) if k > 0 else e[na:na + k]
    nr = min(nr, n)
    if nr > 0:
        e[-nr:] *= np.linspace(1, 0, nr)
    return e


def lp(x, fc):
    b, a = signal.butter(2, min(fc, SR / 2 - 100) / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def hp(x, fc):
    b, a = signal.butter(2, fc / (SR / 2), "high")
    return signal.lfilter(b, a, x)


class Cancion:
    def __init__(self, p):
        self.p = p
        self.rng = np.random.RandomState(p["semilla"])
        self.paso = 60.0 / p["bpm"] / 4          # semicorchea
        self.compases = 16
        self.n = int(round(self.compases * 16 * self.paso * SR))
        cola = int(2.0 * SR)
        self.L = np.zeros(self.n + cola)
        self.R = np.zeros(self.n + cola)

    def pos(self, paso):
        return int(round(paso * self.paso * SR))

    def poner(self, x, inicio, vol, pan=0.0):
        a = inicio
        b = min(len(self.L), a + len(x))
        x = x[: b - a]
        self.L[a:b] += x * vol * (1 - max(0, pan))
        self.R[a:b] += x * vol * (1 + min(0, pan))

    # -------------------------------------------------------------- armonía
    def escala(self):
        return self.p["escala"]

    def grado(self, g, octava):
        esc = self.escala()
        o, i = divmod(g, 7)
        return self.p["tonica"] + 12 * (octava + o) + esc[i]

    def acorde(self, compas):
        prog = self.p["prog"] if (compas // 4) != 2 else self.p["prog_b"]
        return prog[compas % 4]

    # -------------------------------------------------------------- capas
    def bajo(self):
        estilo = self.p["bajo"]
        for c in range(self.compases):
            g = self.acorde(c)
            raiz = self.grado(g, 2)
            quinta = self.grado(g + 4, 2)
            if estilo == "corcheas":
                notas = [(i * 2, raiz if i % 2 == 0 else raiz + 12, 2) for i in range(8)]
            elif estilo == "caminante":
                notas = [(0, raiz, 4), (4, quinta, 4), (8, raiz + 12, 4), (12, quinta, 4)]
            elif estilo == "galope":
                notas = []
                for b in range(4):
                    notas += [(b * 4, raiz, 1), (b * 4 + 2, raiz, 1), (b * 4 + 3, raiz + (12 if b % 2 else 7), 1)]
            elif estilo == "suave":
                notas = [(0, raiz, 6), (6, quinta, 2), (8, raiz, 6), (14, quinta, 2)]
            else:  # saltarin
                notas = [(0, raiz, 2), (3, raiz + 12, 1), (4, quinta, 2), (6, raiz, 2), (8, raiz, 2), (11, raiz + 12, 1), (12, quinta, 2), (14, raiz + 7, 2)]
            for paso, m, largo in notas:
                n = int(largo * self.paso * SR * 0.92)
                x = onda("tri", frec(m), n) * adsr(n, 0.003, 0.05, 0.85, 0.02)
                self.poner(x, self.pos(c * 16 + paso), self.p.get("vol_bajo", 0.55))

    def bateria(self):
        estilo = self.p["bateria"]
        if estilo == "ninguna":
            return
        bombo = self.bombo()
        caja = self.caja()
        plato = self.plato()
        for c in range(self.compases):
            final_frase = c % 4 == 3
            for s in range(16):
                p = self.pos(c * 16 + s)
                if estilo == "rock":
                    if s in (0, 8, 10) or (s == 6 and c % 2): self.poner(bombo, p, 0.9)
                    if s in (4, 12): self.poner(caja, p, 0.55)
                    if s % 2 == 0: self.poner(plato, p, 0.16 if s % 4 else 0.22)
                elif estilo == "rapido":
                    if s % 4 == 0: self.poner(bombo, p, 0.9)
                    if s in (4, 12): self.poner(caja, p, 0.6)
                    self.poner(plato, p, 0.12 if s % 2 else 0.2)
                elif estilo == "suave":
                    if s in (0, 10): self.poner(bombo, p, 0.6)
                    if s in (4, 12): self.poner(caja, p, 0.25)
                    if s % 4 == 2: self.poner(plato, p, 0.1)
                else:  # bailable
                    if s % 4 == 0: self.poner(bombo, p, 0.85)
                    if s in (4, 12): self.poner(caja, p, 0.45)
                    if s % 4 == 2: self.poner(plato, p, 0.22)
                if final_frase and s >= 12 and estilo != "suave":
                    self.poner(caja, p, 0.35)

    def bombo(self):
        n = int(0.18 * SR)
        tt = np.arange(n) / SR
        f = 55 + 110 * np.exp(-tt * 35)
        return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-tt * 18)

    def caja(self):
        n = int(0.16 * SR)
        tt = np.arange(n) / SR
        r = hp(self.rng.uniform(-1, 1, n), 1200) * np.exp(-tt * 22)
        cuerpo = np.sin(2 * np.pi * 190 * tt) * np.exp(-tt * 30) * 0.5
        return r + cuerpo

    def plato(self):
        n = int(0.05 * SR)
        tt = np.arange(n) / SR
        return hp(self.rng.uniform(-1, 1, n), 7000) * np.exp(-tt * 70)

    def motivo(self, rng, compases=2):
        """Ritmo y alturas de una frase: (paso, grado, largo)."""
        ritmos = self.p["ritmos"]
        notas = []
        g = rng.choice([0, 2, 4, 7])
        for c in range(compases):
            ritmo = ritmos[rng.randint(len(ritmos))]
            for i, (paso, largo) in enumerate(ritmo):
                salto = rng.choice([-2, -1, -1, 1, 1, 2, 0, 3, -3])
                g = int(np.clip(g + salto, -2, 11))
                notas.append((c * 16 + paso, g, largo))
        return notas

    def melodia(self):
        rng = np.random.RandomState(self.p["semilla"] + 1)
        a = self.motivo(rng)
        a2 = self.motivo(rng)
        b = self.motivo(rng)
        b2 = self.motivo(rng)
        # A A' B A: cada letra son 4 compases (dos motivos de 2).
        frases = [a + [(p + 32, g, l) for p, g, l in a2],
                  a + [(p + 32, g + (1 if i % 3 == 0 else 0), l) for i, (p, g, l) in enumerate(a2)],
                  b + [(p + 32, g, l) for p, g, l in b2],
                  a + [(p + 32, g, l) for p, g, l in a2]]
        forma = self.p.get("lead", "pulso")
        ciclo = self.p.get("ciclo", 0.25)
        oct_mel = self.p.get("octava_melodia", 5)
        for f, frase in enumerate(frases):
            for paso, g, largo in frase:
                compas = f * 4 + paso // 16
                # Notas largas en tiempos fuertes: al acorde más cercano.
                if paso % 8 == 0:
                    raiz = self.acorde(compas)
                    tonos = [raiz, raiz + 2, raiz + 4, raiz + 7]
                    g = min(tonos, key=lambda x: abs(x - g))
                m = self.grado(g, oct_mel)
                n = int(largo * self.paso * SR * 0.9)
                x = onda(forma, frec(m), n, ciclo, vibrato=0.006 if largo >= 4 else 0.0)
                x = x * adsr(n, 0.004, 0.08, 0.55, 0.03)
                self.poner(lp(x, 5200), self.pos(f * 64 + paso), self.p.get("vol_melodia", 0.32), 0.25)
                # eco suave
                self.poner(lp(x, 3000), self.pos(f * 64 + paso + 3), self.p.get("vol_melodia", 0.32) * 0.25, -0.35)

    def arpegio(self):
        if not self.p.get("arpegio"):
            return
        for c in range(self.compases):
            g = self.acorde(c)
            tonos = [self.grado(g, 4), self.grado(g + 2, 4), self.grado(g + 4, 4), self.grado(g + 7, 4)]
            orden = [0, 1, 2, 3, 2, 1] if self.p["arpegio"] == "vaiven" else [0, 1, 2, 3]
            for s in range(16):
                m = tonos[orden[s % len(orden)]]
                n = int(self.paso * SR * 0.8)
                x = onda("pulso", frec(m), n, 0.125) * adsr(n, 0.002, 0.04, 0.3, 0.02)
                self.poner(lp(x, 4200), self.pos(c * 16 + s), self.p.get("vol_arpegio", 0.12), -0.3)

    def acordes(self):
        if not self.p.get("colchon"):
            return
        for c in range(self.compases):
            g = self.acorde(c)
            n = int(16 * self.paso * SR)
            x = np.zeros(n)
            for k in (0, 2, 4):
                x += onda("pulso", frec(self.grado(g + k, 4)), n, 0.5)
            x = lp(x, 1400) * adsr(n, 0.08, 0.3, 0.7, 0.1)
            self.poner(x, self.pos(c * 16), self.p.get("vol_colchon", 0.05))

    def render(self):
        self.bajo()
        self.bateria()
        self.melodia()
        self.arpegio()
        self.acordes()
        L, R = self.L, self.R
        # Bucle exacto: la cola se suma al principio.
        cola = len(L) - self.n
        L[:cola] += L[self.n:]
        R[:cola] += R[self.n:]
        est = np.stack([L[: self.n], R[: self.n]], axis=1)
        est = np.tanh(est * 1.2)
        est /= np.max(np.abs(est)) or 1
        return est * 10 ** (-3 / 20)


# Ritmos de 1 compás: (paso de semicorchea, largo en semicorcheas)
R_ALEGRE = [[(0, 2), (2, 2), (4, 4), (8, 2), (10, 2), (12, 4)], [(0, 3), (3, 1), (4, 2), (6, 2), (8, 8)],
            [(0, 2), (2, 1), (3, 1), (4, 2), (6, 2), (8, 4), (12, 2), (14, 2)]]
R_SALTARIN = [[(0, 1), (2, 1), (4, 1), (6, 2), (8, 1), (10, 1), (12, 4)], [(0, 2), (3, 2), (6, 2), (8, 2), (11, 1), (12, 4)],
              [(0, 1), (1, 1), (2, 2), (4, 4), (10, 2), (12, 2), (14, 2)]]
R_TECNO = [[(0, 2), (2, 2), (4, 2), (6, 2), (8, 2), (10, 2), (12, 2), (14, 2)], [(0, 3), (3, 3), (6, 2), (8, 3), (11, 3), (14, 2)],
           [(0, 4), (4, 2), (6, 2), (8, 4), (12, 4)]]
R_LENTO = [[(0, 6), (6, 2), (8, 8)], [(0, 4), (4, 4), (8, 4), (12, 4)], [(0, 8), (8, 4), (12, 4)]]
R_EPICO = [[(0, 3), (3, 3), (6, 2), (8, 3), (11, 3), (14, 2)], [(0, 2), (2, 2), (4, 2), (6, 2), (8, 8)],
           [(0, 1), (1, 1), (2, 1), (3, 1), (4, 4), (8, 4), (12, 4)]]

PISTAS = {
    # Planta baja: alegre, do mayor.
    "musica_piso_0": dict(semilla=10, bpm=120, tonica=48, escala=MAYOR, prog=[0, 4, 5, 3], prog_b=[3, 4, 2, 5],
                          bajo="corcheas", bateria="rock", ritmos=R_ALEGRE, colchon=True),
    # Gastronomía y Enfermería: saltarín, fa mayor.
    "musica_piso_1": dict(semilla=21, bpm=128, tonica=53, escala=MAYOR, prog=[0, 3, 4, 0], prog_b=[5, 3, 1, 4],
                          bajo="saltarin", bateria="bailable", ritmos=R_SALTARIN, ciclo=0.5, octava_melodia=4),
    # Informática: tecno con arpegios, la menor.
    "musica_piso_2": dict(semilla=32, bpm=134, tonica=45, escala=MENOR, prog=[0, 5, 2, 6], prog_b=[3, 4, 0, 4],
                          bajo="corcheas", bateria="rapido", ritmos=R_TECNO, arpegio="sube", lead="saw", vol_melodia=0.22),
    # Aulas 300: sol mayor, marcha.
    "musica_piso_3": dict(semilla=43, bpm=122, tonica=43, escala=MAYOR, prog=[0, 5, 3, 4], prog_b=[1, 4, 0, 5],
                          bajo="caminante", bateria="rock", ritmos=R_ALEGRE, arpegio="vaiven", ciclo=0.125),
    # Aulas 400: re menor, más tenso.
    "musica_piso_4": dict(semilla=54, bpm=128, tonica=50, escala=MENOR, prog=[0, 6, 5, 4], prog_b=[3, 0, 6, 4],
                          bajo="galope", bateria="rock", ritmos=R_EPICO, colchon=True),
    # Aulas 500: mi menor, rápido.
    "musica_piso_5": dict(semilla=65, bpm=140, tonica=40, escala=ARMONICA, prog=[0, 5, 3, 4], prog_b=[5, 6, 4, 4],
                          bajo="galope", bateria="rapido", ritmos=R_EPICO, arpegio="sube", ciclo=0.25),
    # Terraza y Biblioteca: si bemol mayor, aireado.
    "musica_piso_6": dict(semilla=76, bpm=112, tonica=46, escala=MAYOR, prog=[0, 2, 3, 4], prog_b=[5, 3, 0, 4],
                          bajo="suave", bateria="suave", ritmos=R_LENTO, arpegio="vaiven", lead="tri", vol_melodia=0.42, colchon=True),
    # Jefe final: do menor armónica, a toda máquina.
    "musica_jefe_final": dict(semilla=87, bpm=152, tonica=48, escala=ARMONICA, prog=[0, 5, 6, 4], prog_b=[3, 4, 5, 4],
                              bajo="galope", bateria="rapido", ritmos=R_EPICO, arpegio="sube", lead="saw", vol_melodia=0.26, colchon=True),
    # Supervivencia: la mi menor, rápido y oscuro.
    "musica_supervivencia": dict(semilla=121, bpm=146, tonica=45, escala=ARMONICA, prog=[0, 6, 5, 4], prog_b=[3, 5, 6, 4],
                                 bajo="galope", bateria="rapido", ritmos=R_EPICO, arpegio="vaiven", lead="pulso", ciclo=0.25, vol_melodia=0.28, colchon=True),
    # Ascensor: bossa tranquila (música de ascensor).
    "musica_ascensor": dict(semilla=133, bpm=96, tonica=53, escala=MAYOR, prog=[0, 5, 1, 4], prog_b=[3, 2, 1, 4],
                            bajo="suave", bateria="suave", ritmos=R_LENTO, arpegio="vaiven", lead="tri", vol_melodia=0.42, colchon=True),
    # Historia: torpe y simpática (staccato), re mayor.
    "musica_historia": dict(semilla=98, bpm=104, tonica=50, escala=MAYOR, prog=[0, 3, 4, 0], prog_b=[5, 1, 4, 4],
                            bajo="caminante", bateria="suave", ritmos=R_SALTARIN, ciclo=0.5, octava_melodia=4, vol_melodia=0.36),
    # Final del sueño: fa mayor, tranquilo y feliz.
    "musica_final_sueno": dict(semilla=109, bpm=92, tonica=53, escala=MAYOR, prog=[0, 4, 5, 3], prog_b=[3, 0, 1, 4],
                               bajo="suave", bateria="ninguna", ritmos=R_LENTO, arpegio="vaiven", lead="tri", vol_melodia=0.45, colchon=True),
}


def guid(ruta):
    rel = os.path.relpath(ruta, RAIZ).replace("\\", "/")
    return hashlib.md5(("musica_v2:" + rel).encode()).hexdigest()


def guardar_ogg(nombre, est):
    os.makedirs(OUT, exist_ok=True)
    ruta = os.path.join(OUT, nombre + ".ogg")
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        wav = tmp.name
    with wave.open(wav, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(est, -1, 1) * 32767).astype("<i2").tobytes())
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "3", ruta], check=True)
    os.remove(wav)
    with open(ruta + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(META.format(guid=guid(ruta)))
    carpeta_meta = OUT + ".meta"
    if not os.path.exists(carpeta_meta):
        with open(carpeta_meta, "w", encoding="utf-8", newline="\n") as f:
            f.write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
                    % hashlib.md5(b"carpeta:MusicaExtra").hexdigest())
    print(f"  {nombre}.ogg  {len(est) / SR:.1f} s  {os.path.getsize(ruta) // 1024} KB")


if __name__ == "__main__":
    for nombre, p in PISTAS.items():
        guardar_ogg(nombre, Cancion(p).render())
