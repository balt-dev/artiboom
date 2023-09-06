#!python

from PIL import Image
import numpy as np
from glob import glob
import os

input(f"Running in {os.getcwd()}, Ctrl+C to cance!l\n")

with Image.open("pal_old.png") as old:
    old = np.array(old.convert("RGB"), dtype=np.uint8).reshape(-1, 3)

with Image.open("pal_new.png") as new:
    new = np.array(new.convert("RGB"), dtype=np.uint8).reshape(-1, 3)

colormap = tuple(zip(old, new))

print(colormap)

for image in glob("**/*.png", recursive=True):
    if "pal_" in image: continue
    with Image.open(image) as im:
        arr = np.array(im.convert("RGBA"), dtype=np.uint8)
        for old, new in colormap:
            arr[..., :3][np.all(arr[..., :3] == old, axis = -1)] = new
        Image.fromarray(arr).save(image)
