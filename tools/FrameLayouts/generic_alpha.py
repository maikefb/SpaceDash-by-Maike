"""Converte LcdInfo_GenericFrame.png (centro branco opaco) em DDS RGBA8 com o centro transparente, para o jogo desenhar
o conteúdo por trás da borda. PNG RGB/RGBA 8 bits sem paleta, só stdlib.

Uso: python -I -X utf8 tools/FrameLayouts/generic_alpha.py [--src <pasta v2>]
"""
import argparse
import pathlib
import struct
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
THEMES = ("Futuristic", "Clean", "Cafe")
CONTENT = (32, 32, 448, 448)


def read_png(path):
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", path
    pos = 8
    idat = b""
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + length]
        if kind == b"IHDR":
            width, height, depth, color, _, _, interlace = struct.unpack(">IIBBBBB", body)
            assert depth == 8 and color in (2, 6) and interlace == 0, (path, depth, color, interlace)
            channels = 3 if color == 2 else 4
        elif kind == b"IDAT":
            idat += body
        pos += 12 + length
    raw = zlib.decompress(idat)
    stride = width * channels
    rows = []
    prev = bytearray(stride)
    for y in range(height):
        start = y * (stride + 1)
        filt = raw[start]
        line = bytearray(raw[start + 1:start + 1 + stride])
        for i in range(stride):
            a = line[i - channels] if i >= channels else 0
            b = prev[i]
            c = prev[i - channels] if i >= channels else 0
            if filt == 1:
                line[i] = (line[i] + a) & 0xFF
            elif filt == 2:
                line[i] = (line[i] + b) & 0xFF
            elif filt == 3:
                line[i] = (line[i] + (a + b) // 2) & 0xFF
            elif filt == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 0xFF
        rows.append(line)
        prev = line
    return width, height, channels, rows


def write_dds(path, width, height, channels, rows):
    header = struct.pack("<4sIIIIIII44x", b"DDS ", 124, 0x1 | 0x2 | 0x4 | 0x1000 | 0x8, height, width, width * 4, 0, 0)
    header += struct.pack("<IIIIIIII", 32, 0x41, 0, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000)
    header += struct.pack("<IIIII", 0x1000, 0, 0, 0, 0)
    x0, y0, w, h = CONTENT
    out = bytearray()
    for y, line in enumerate(rows):
        for x in range(width):
            r, g, b = line[x * channels], line[x * channels + 1], line[x * channels + 2]
            a = line[x * channels + 3] if channels == 4 else 255
            if x0 <= x < x0 + w and y0 <= y < y0 + h:
                a = 0
            out += bytes((b, g, r, a))
    path.write_bytes(header + out)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", default=r"C:\Users\maike\Downloads\SpaceDash_molduras\v2")
    args = parser.parse_args()
    for theme in THEMES:
        width, height, channels, rows = read_png(pathlib.Path(args.src) / theme / "LcdInfo_GenericFrame.png")
        assert (width, height) == (512, 512), (theme, width, height)
        target = ROOT / "Core/Textures/Sprites/Frames" / theme / "LcdInfo_GenericFrame.dds"
        write_dds(target, width, height, channels, rows)
        print(target, target.stat().st_size)


if __name__ == "__main__":
    main()
