"""Minimal Aseprite (.aseprite/.ase) reader + slicer for the RALFT project.

Used to inspect the .aseprite art and export PNG frames / block slices
so Unity can import them as regular sprites.
"""
import struct
import zlib
import sys
import os
from PIL import Image


def parse(path):
    with open(path, "rb") as f:
        data = f.read()
    (filesize, magic, nframes, width, height, depth, flags) = struct.unpack_from("<IHHHHHI", data, 0)
    assert magic == 0xA5E0, f"{path}: not an aseprite file"
    bpp = {32: 4, 24: 3, 16: 2, 8: 1}[depth]
    off = 128
    layers = []
    frames = []
    tags = []
    while off < len(data) and len(frames) < nframes:
        (fbytes, fmagic, old_chunks, duration, _res, new_chunks) = struct.unpack_from("<IHHHHI", data, off)
        assert fmagic == 0xF1FA, f"{path}: bad frame magic"
        chunk_count = new_chunks if new_chunks else old_chunks
        p = off + 16
        cels = {}
        for _ in range(chunk_count):
            (csize, ctype) = struct.unpack_from("<IH", data, p)
            body = p + 6
            if ctype == 0x2004:  # layer
                (lflags, ltype, child, dw, dh, blend, opacity) = struct.unpack_from("<HHHHHHB", data, body)
                name_len = struct.unpack_from("<H", data, body + 16)[0]
                name = data[body + 18:body + 18 + name_len].decode("utf-8", "replace")
                layers.append({"index": len(layers), "name": name, "flags": lflags,
                               "type": ltype, "opacity": opacity, "blend": blend,
                               "visible": bool(lflags & 1)})
            elif ctype == 0x2005:  # cel
                (li, cx, cy, cop, ctype2) = struct.unpack_from("<HhhBH", data, body)
                cel = {"x": cx, "y": cy, "type": ctype2, "opacity": cop}
                if ctype2 == 2:
                    (cw, ch) = struct.unpack_from("<HH", data, body + 16)
                    raw = zlib.decompress(data[body + 20:p + csize])
                    cel["img"] = (cw, ch, raw)
                cels[li] = cel
            elif ctype == 0x2018:  # tags
                (ntags,) = struct.unpack_from("<H", data, body)
                q = body + 8
                for _t in range(ntags):
                    (f0, f1, direction, repeat) = struct.unpack_from("<HHBH", data, q)
                    nlen = struct.unpack_from("<H", data, q + 9)[0]
                    tname = data[q + 11:q + 11 + nlen].decode("utf-8", "replace")
                    tags.append({"name": tname, "from": f0, "to": f1, "dir": direction})
                    q += 11 + nlen + 6
            p += csize
        frames.append({"duration": duration, "cels": cels})
        off += fbytes
    return {"width": width, "height": height, "depth": depth, "bpp": bpp,
            "layers": layers, "frames": frames, "tags": tags}


def frame_image(doc, idx):
    w, h = doc["width"], doc["height"]
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for li, cel in sorted(doc["frames"][idx]["cels"].items()):
        if li < len(doc["layers"]) and not doc["layers"][li]["visible"]:
            continue
        if "img" not in cel or doc["bpp"] != 4:
            continue
        cw, ch, raw = cel["img"]
        img = Image.frombytes("RGBA", (cw, ch), raw)
        layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        layer.paste(img, (cel["x"], cel["y"]), img)
        out = Image.alpha_composite(out, layer)
    return out


def info(path):
    doc = parse(path)
    print(f"{os.path.basename(path)}: {doc['width']}x{doc['height']} frames={len(doc['frames'])} "
          f"depth={doc['depth']}")
    for l in doc["layers"]:
        print(f"   layer {l['index']}: '{l['name']}' visible={l['visible']} type={l['type']}")
    if doc["tags"]:
        for t in doc["tags"]:
            print(f"   tag '{t['name']}' frames {t['from']}..{t['to']}")
    for i, fr in enumerate(doc["frames"]):
        img = frame_image(doc, i)
        bbox = img.getbbox()
        cels = {li: (c["x"], c["y"]) + (c["img"][0], c["img"][1]) for li, c in fr["cels"].items() if "img" in c}
        print(f"   frame {i}: dur={fr['duration']}ms bbox={bbox} celRects={cels}")


if __name__ == "__main__":
    base = r"C:\Users\ALEJANDRO\Desktop\Nueva carpeta (23)"
    if len(sys.argv) > 1:
        for p in sys.argv[1:]:
            info(p)
    else:
        for root, _dirs, files in os.walk(base):
            for f in sorted(files):
                if f.lower().endswith((".aseprite", ".ase")):
                    info(os.path.join(root, f))
