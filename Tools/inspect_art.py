"""Inspect the building / windows art: export PNG + print an ASCII luminance map."""
import os
import sys

sys.path.insert(0, "Tools")
from aseprite_tool import parse, frame_image  # noqa: E402

SRC = r"C:\Users\ALEJANDRO\Desktop\Nueva carpeta (23)"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")


def find(sub, stem):
    d = os.path.join(SRC, sub)
    for f in os.listdir(d):
        if f.lower().startswith(stem.lower()):
            return os.path.join(d, f)
    raise FileNotFoundError(stem)


def lum_map(img, cols=100):
    w, h = img.size
    step_x = max(1, w // cols)
    step_y = step_x * 2
    ramp = " .:-=+*#%@"
    lines = []
    for y in range(0, h, step_y):
        row = []
        for x in range(0, w, step_x):
            box = img.crop((x, y, min(x + step_x, w), min(y + step_y, h)))
            px = list(box.getdata())
            opaque = [p for p in px if p[3] > 40]
            if not opaque:
                row.append(" ")
                continue
            l = sum(0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2] for p in opaque) / len(opaque)
            row.append(ramp[min(len(ramp) - 1, int(l / 26))])
        lines.append("".join(row))
    return lines


def main():
    os.makedirs(OUT, exist_ok=True)
    for sub, stem, name in [("edificio", "edificio", "edificio.png"),
                            ("edificio", "ventanas", "ventanas.png")]:
        doc = parse(find(sub, stem))
        img = frame_image(doc, 0)
        img.save(os.path.join(OUT, name))
        print(f"== {name} {img.size} bbox={img.getbbox()} layers={[l['name'] for l in doc['layers']]}")
        filled = sum(1 for p in img.getdata() if p[3] > 40)
        print(f"   opaque px: {filled}")
        for line in lum_map(img):
            print("   |" + line)


if __name__ == "__main__":
    main()
