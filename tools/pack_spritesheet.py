#!/usr/bin/env python3
"""
pack_spritesheet.py — pack a folder of frame PNGs into a sprite-sheet PNG
                     plus a metadata JSON that the engine consumes.

USAGE
    python tools/pack_spritesheet.py <frames_folder> <output_basename> [--columns N] [--fps N]

ARGUMENTS
    frames_folder    Path to a folder containing frame PNGs. Files are sorted
                     alphanumerically — name them Frame_01.png, Frame_02.png,
                     ... or 01.png, 02.png to control ordering. All frames
                     must have the same dimensions.

    output_basename  Path + base name for the output, WITHOUT extension.
                     The script writes <basename>.png and <basename>.json
                     side by side.

OPTIONS
    --columns N      Number of columns in the grid (default: ceil(sqrt(N)),
                     which makes the sheet roughly square).
    --fps N          Playback frame rate (default: 24).

EXAMPLE
    python tools/pack_spritesheet.py \\
        ~/Downloads/menu_frames \\
        Assets/Art/UI/MainMenuBackground_sheet \\
        --columns 6 --fps 24

    Result:
        Assets/Art/UI/MainMenuBackground_sheet.png
        Assets/Art/UI/MainMenuBackground_sheet.json

REQUIREMENTS
    pip install pillow
"""

import argparse
import json
import math
import os
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("ERROR: Pillow is not installed. Run: pip install pillow", file=sys.stderr)
    sys.exit(1)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("frames_folder", help="Folder containing frame PNGs")
    p.add_argument("output_basename", help="Output base path (no extension)")
    p.add_argument("--columns", type=int, default=None, help="Grid columns (default: roughly square)")
    p.add_argument("--fps", type=int, default=24, help="Playback FPS (default: 24)")
    args = p.parse_args()

    folder = Path(args.frames_folder)
    if not folder.is_dir():
        print(f"ERROR: {folder} is not a directory", file=sys.stderr)
        sys.exit(1)

    frame_paths = sorted(folder.glob("*.png"))
    if not frame_paths:
        print(f"ERROR: no .png files in {folder}", file=sys.stderr)
        sys.exit(1)

    print(f"Found {len(frame_paths)} frames in {folder}")

    # Load all frames and verify dimensions match
    frames = [Image.open(p) for p in frame_paths]
    fw, fh = frames[0].size
    for f, path in zip(frames, frame_paths):
        if f.size != (fw, fh):
            print(f"ERROR: frame {path.name} is {f.size}, expected {(fw, fh)}", file=sys.stderr)
            sys.exit(1)

    n = len(frames)
    columns = args.columns or math.ceil(math.sqrt(n))
    rows    = math.ceil(n / columns)

    sheet_w = columns * fw
    sheet_h = rows    * fh
    print(f"Sheet: {columns} cols x {rows} rows, {sheet_w}x{sheet_h}px, {n} frames @ {args.fps}fps")

    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))
    for i, frame in enumerate(frames):
        col = i % columns
        row = i // columns
        sheet.paste(frame, (col * fw, row * fh))

    out_base = Path(args.output_basename)
    out_base.parent.mkdir(parents=True, exist_ok=True)

    sheet_path = out_base.with_suffix(".png")
    json_path  = out_base.with_suffix(".json")

    sheet.save(sheet_path, optimize=True)
    json_path.write_text(json.dumps({
        "columns":     columns,
        "rows":        rows,
        "frame_count": n,
        "fps":         args.fps
    }, indent=2))

    print(f"Wrote {sheet_path}")
    print(f"Wrote {json_path}")


if __name__ == "__main__":
    main()