import re
import sys

path = sys.argv[1]
text = open(path, encoding="utf-8").read()
pattern = re.compile(r'///\s*<summary>(?P<doc>[^<]*)</summary>\s*\n\s*public const string (?P<name>\w+) = "(?P<value>[^"]*)";')
rows = []
total = 0
for m in pattern.finditer(text):
    total += 1
    doc = m.group("doc").strip()
    final = re.search(r"SD_Icon_\w+", doc)
    if final is None:
        continue
    if m.group("value") != final.group(0):
        rows.append((m.group("name"), m.group("value"), final.group(0)))

print("| Constante | Fallback atual | Nome final |")
print("|---|---|---|")
for name, value, final in rows:
    print("| `BareIcons.%s` | `%s` | `%s` |" % (name, value, final))
print()
print("Total: %d pendentes de %d constantes." % (len(rows), total))
