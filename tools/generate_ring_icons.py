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

    # 3. Dialog Card Background (740x460) and Inner Box (660x100 reference)
    card_w, card_h = 740, 460
    card_scale = 2
    im_card = Image.new('RGBA', (card_w * card_scale, card_h * card_scale), (0, 0, 0, 0))
    draw_card = ImageDraw.Draw(im_card)
    draw_card.rounded_rectangle(
        [card_scale, card_scale, (card_w - 1) * card_scale, (card_h - 1) * card_scale],
        radius=22 * card_scale,
        fill=(18, 24, 34, 250),
        outline=(60, 82, 115, 230),
        width=2 * card_scale
    )
    card_out = im_card.resize((card_w, card_h), Image.Resampling.LANCZOS)
    card_out.save(os.path.join(media_dir, 'card_bg.png'), optimize=True)

    box_w, box_h = 660, 100
    im_box = Image.new('RGBA', (box_w * card_scale, box_h * card_scale), (0, 0, 0, 0))
    draw_box = ImageDraw.Draw(im_box)
    draw_box.rounded_rectangle(
        [card_scale, card_scale, (box_w - 1) * card_scale, (box_h - 1) * card_scale],
        radius=12 * card_scale,
        fill=(12, 16, 23, 235),
        outline=(45, 62, 85, 200),
        width=int(1.5 * card_scale)
    )
    box_out = im_box.resize((box_w, box_h), Image.Resampling.LANCZOS)
    box_out.save(os.path.join(media_dir, 'inner_box.png'), optimize=True)

    print(f"Generated dialog textures (card_bg.png, inner_box.png) in {media_dir}")

if __name__ == '__main__':
    generate_assets()
