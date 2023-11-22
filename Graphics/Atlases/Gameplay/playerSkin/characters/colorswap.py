#!/usr/bin/env python

from PIL import Image
import numpy as np
from glob import glob
import os

raise Exception("DO NOT OVERWRITE THE BADELINE SPRITES!!!")

input(f"Running in {os.getcwd()}, Ctrl+C to cance!l\n")

with Image.open("pal.png") as old:
    colormap = np.array(old.convert("RGB"), dtype=np.uint8).swapaxes(1, 0)

print(colormap)

for image in glob("player/**/*.png", recursive=True):
    if "pal_" in image: continue
    with Image.open(image) as im:
        arr = np.array(im.convert("RGBA"), dtype=np.uint8)
        for old, new in colormap:
            arr[..., :3][np.all(arr[..., :3] == old, axis = -1)] = new
        Image.fromarray(arr).save(image.replace("player", "player_badeline"))
