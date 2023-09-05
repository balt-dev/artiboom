import re
from io import StringIO

with open("Graphics/Sprites.xml", "r") as f:
    content = f.read()

buf = StringIO()

offset = 0, -2

lines = []

for line in content.splitlines():
    if 'hair="' in line:
        if (match := re.search(r'<Frames path=\"(.*?)\" hair=\"(?!x)(.*?)\"/>', line)) is not None:
            hair_string = match.group(2)
            hairs = []
            for hmatch in re.finditer(r'(-?\d+?)(:[^\|\",]+?)?,(-?\d+?)(:[^\|\",]+?)?', hair_string):
                x, y = hmatch.group(1), hmatch.group(3)
                x = int(x) + offset[0]
                y = int(y) + offset[1]
                remade = f"{x}" \
                         f"{'' if hmatch.group(2) is None else hmatch.group(2)}" \
                          "," \
                         f"{y}" \
                         f"{'' if hmatch.group(4) is None else hmatch.group(4)}"
                hairs.append(remade)
            print(line, match)
            line = line[:match.start()] + f"<Frames path=\"{match.group(1)}\" hair=\"{'|'.join(hairs)}\"/>" + line[match.end():]
            print(line)
    lines.append(line + "\n")

with open("Graphics/Sprites.xml", "w") as f:
    f.writelines(lines)