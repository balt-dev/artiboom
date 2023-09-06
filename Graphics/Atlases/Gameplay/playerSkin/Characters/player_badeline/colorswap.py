#!python

from PIL import Image
import numpy as np
from glob import glob
import os

input(f"Running in {os.getcwd()}, Ctrl+C to cancel")

colormap = (
    ((0x70, 0x23, 0x3c), (0x1f, 0x00, 0x00)),
    ((0x56, 0x13, 0x29), (0x17, 0x00, 0x00)),
    ((0xb6, 0xb4, 0xb7), (0x91, 0x25, 0x25)),
    ((0x45, 0x28, 0x3c), (0x33, 0x04, 0x04))
)

for image in glob("**/*.png", recursive=True):
    with Image.open(image) as im:
        arr = np.array(im.convert("RGBA"), dtype=np.uint8)
        for old, new in colormap:
            arr[..., :3][np.all(arr[..., :3] == old, axis = -1)] = new
        Image.fromarray(arr).save(image)
