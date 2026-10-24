#!/usr/bin/env python3
import os
import math
from PIL import Image, ImageDraw

def generate_assets():
    media_dir = os.path.abspath('kodi-addon/service.introskipper/resources/media')
    ring_dir = os.path.join(media_dir, 'ring')
    os.makedirs(ring_dir, exist_ok=True)

    # 1. Circular countdown frames
    size = 128
    stroke_width = 12
    margin = stroke_width + 4
    num_frames = 36

    scale = 4
    ss_size = size * scale
    ss_stroke = stroke_width * scale
    ss_margin = margin * scale
    ss_bbox = [ss_margin, ss_margin, ss_size - ss_margin, ss_size - ss_margin]

    for i in range(num_frames + 1):
        im = Image.new('RGBA', (ss_size, ss_size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(im)

        # Subtle background track ring
        draw.ellipse(ss_bbox, outline=(255, 255, 255, 60), width=ss_stroke)

        # Foreground active countdown arc (clockwise from 12 o'clock / -90 deg)
        if i > 0:
            sweep = (i / num_frames) * 360.0
            start_deg = -90.0
            end_deg = -90.0 + sweep
            draw.arc(ss_bbox, start=start_deg, end=end_deg, fill=(255, 255, 255, 245), width=ss_stroke)

        im_out = im.resize((size, size), Image.Resampling.LANCZOS)
        out_file = os.path.join(ring_dir, f'ring_{i:02d}.png')
        im_out.save(out_file, optimize=True)

    print(f"Generated {num_frames + 1} ring frames in {ring_dir}")

    # 2. Modern rounded pill button textures (400x70 reference, super-sampled)
    btn_w = 400
    btn_h = 70
    r = btn_h // 2

    # Focused button texture (solid dark pill with crisp high-contrast border and subtle glow)
    ss_w = btn_w * scale
    ss_h = btn_h * scale
    ss_r = r * scale

    for name, bg_color, border_color, border_w in [
        ('button_focus.png', (18, 22, 28, 235), (255, 255, 255, 255), 3),
        ('button_nofocus.png', (15, 18, 22, 210), (200, 205, 215, 150), 2)
    ]:
        im_btn = Image.new('RGBA', (ss_w, ss_h), (0, 0, 0, 0))
        draw_btn = ImageDraw.Draw(im_btn)
        draw_btn.rounded_rectangle(
            [scale, scale, ss_w - scale, ss_h - scale],
            radius=ss_r,
            fill=bg_color,
            outline=border_color,
            width=border_w * scale
        )
        btn_out = im_btn.resize((btn_w, btn_h), Image.Resampling.LANCZOS)
        btn_out.save(os.path.join(media_dir, name), optimize=True)

    print(f"Generated button textures in {media_dir}")

if __name__ == '__main__':
    generate_assets()
