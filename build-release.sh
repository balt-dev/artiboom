#!/bin/sh
rm release.zip
mkdir release
cp Graphics release/Graphics -r
cp Dialog release/Dialog -r
cp Audio release/Audio -r
mkdir release/bin
cp bin/artiboom.dll release/bin/artiboom.dll -r
cp everest.yaml release/everest.yaml -r
cd release
7z a ../release.zip * -sdel
cd ..
rm release -r