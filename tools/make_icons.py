#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 mod 需要的两个图标（无第三方依赖，纯 zlib 手写 PNG）。"""
import math, os, struct, zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "src", "SpaceStore", "images"))


def write_png(path, w, h, px):
    raw = b"".join(b"\x00" + b"".join(bytes(p) for p in row) for row in px)
    idat = zlib.compress(raw, 9)

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data +
                struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) +
                chunk(b"IDAT", idat) + chunk(b"IEND", b""))


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def coin(nx, ny):
    """返回 (r,g,b,a)，nx/ny 为 -1..1 的归一化坐标。"""
    d = math.hypot(nx, ny)
    if d > 0.98:
        return 0, 0, 0, 0
    if d > 0.88:
        return 0x8A, 0x5A, 0x12, 255
    t = min(1.0, max(0.0, (ny + 1) / 2))
    base = lerp((0xF7, 0xD9, 0x6B), (0xD1, 0x93, 0x1E), t)
    if 0.60 < d <= 0.70:
        return 0xB4, 0x7A, 0x14, 255
    if d <= 0.60:
        # 高光
        hl = math.hypot(nx + 0.28, ny + 0.30)
        if hl < 0.22:
            return lerp(base, (0xFF, 0xF6, 0xCE), 0.75) + (255,)
    return base + (255,)


def icon(nx, ny):
    # 圆角方形底 + 金币
    r = 0.92
    ax, ay = abs(nx), abs(ny)
    if ax > r or ay > r:
        return 0, 0, 0, 0
    if ax > r - 0.12 and ay > r - 0.12:
        if math.hypot(ax - (r - 0.12), ay - (r - 0.12)) > 0.12:
            return 0, 0, 0, 0
    if math.hypot(nx, ny) < 0.62:
        return coin(nx / 0.62, ny / 0.62)
    # 背景渐变
    t = (ny + 1) / 2
    bg = lerp((0x27, 0x4E, 0x7A), (0x16, 0x2F, 0x4C), t)
    if ax > r - 0.10 or ay > r - 0.10:
        return 0xE8, 0xB4, 0x3C, 255
    return bg + (255,)


def render(size, fn, ss=4):
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            acc = [0, 0, 0, 0]
            for sy in range(ss):
                for sx in range(ss):
                    nx = ((x + (sx + 0.5) / ss) / size) * 2 - 1
                    ny = ((y + (sy + 0.5) / ss) / size) * 2 - 1
                    r, g, b, a = fn(nx, ny)
                    acc[0] += r * a
                    acc[1] += g * a
                    acc[2] += b * a
                    acc[3] += a
            n = ss * ss
            if acc[3] == 0:
                row.append((0, 0, 0, 0))
            else:
                row.append((int(acc[0] / acc[3]), int(acc[1] / acc[3]),
                            int(acc[2] / acc[3]), int(acc[3] / n)))
        rows.append(row)
    return rows


def main():
    os.makedirs(OUT, exist_ok=True)
    write_png(os.path.join(OUT, "coin.png"), 64, 64, render(64, coin))
    write_png(os.path.join(OUT, "icon_space_store.png"), 64, 64, render(64, icon))
    print("written:", os.listdir(OUT))


if __name__ == "__main__":
    main()
