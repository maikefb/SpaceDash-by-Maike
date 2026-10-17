"""Gera FrameLayouts.g.cs e Textures-Frames.sbc a partir dos coordenadas_<Tema>.md das molduras.

Uso: python -I -X utf8 tools/FrameLayouts/gen.py [--src <pasta v2>] [--check-langs]
"""
import argparse
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
THEMES = ("Futuristic", "Clean", "Cafe")
ID_PREFIX = "LcdInfo_"
# Telas que voltaram ao chrome desenhado por código: o .md do designer continua com elas, o mod não.
SKIP = {"LcdInfo_InGameClockDashboard", "LcdInfo_Radar"}
# Cabeçalho "GRID / Telemetria da grid em tempo real" saiu das molduras: o .md do designer continua com ele, o mod não.
SKIP_LABELS = {"GRID", "Telemetria da grid", "Telemetria", "da grid", "em tempo real"}

# Texto de referência (PT) -> chave resx. "=texto" é literal, "@Chave" é chave do jogo (não validada no resx do mod).
LABEL_KEYS = {
    "SpaceDash": "=SpaceDash",
    "SpaceDash by Maike": "=SpaceDash by Maike",
    "0 %": "=0 %", "25 %": "=25 %", "50 %": "=50 %", "75 %": "=75 %", "100 %": "=100 %",
    "Painel de energia": "LcdMod_EnergyGeneral",
    "Gestão dinâmica de energia": "LcdMod_Frame_EnergySubtitle",
    "Produção total": "LcdMod_Energy_Production",
    "Consumo total": "LcdMod_Energy_Consumption",
    "Saldo": "LcdMod_Energy_Balance",
    "Baterias": "@DisplayName_BlockGroup_Batteries",
    "Painéis solares": "@DisplayName_BlockGroup_SolarPanels",
    "Painéis": "@DisplayName_BlockGroup_SolarPanels",
    "Turbinas eólicas": "@DisplayName_BlockGroup_WindTurbines",
    "Turbinas": "@DisplayName_BlockGroup_WindTurbines",
    "Reatores": "@DisplayName_BlockGroup_Reactors",
    "Motores H2": "@DisplayName_BlockGroup_HydrogenEngines",
    "Motores H₂": "@DisplayName_BlockGroup_HydrogenEngines",
    "Motores": "@DisplayName_BlockGroup_HydrogenEngines",
    "Armazenado": "LcdMod_Energy_Stored",
    "Carga": "LcdMod_Energy_Charge",
    "Modo": "LcdMod_Energy_Mode",
    "Entrada": "LcdMod_Energy_Input",
    "Saída": "LcdMod_Energy_Output",
    "Estado": "@TerminalStatus",
    "Saída atual": "LcdMod_Frame_CurrentOutput",
    "Saída máxima": "LcdMod_Energy_MaxOutput",
    "Utilização": "LcdMod_Energy_Load",
    "Tempo de jogo": "LcdMod_ClockDashboard_PlayTime",
    "Uptime": "LcdMod_ClockDashboard_ServerUptime",
    "Painel de defesa": "LcdMod_DefenseDashboard",
    "Escudo e armamento": "LcdMod_Frame_DefenseSubtitle",
    "Escudo": "LcdMod_DefenseDashboard_ShieldFallback",
    "Gráfico de energia": "LcdMod_EnergyChart",
    "Produção e consumo · 1 h": "LcdMod_Frame_ChartSubtitle",
    "Produção ━━  Consumo ╌╌": "LcdMod_Frame_ChartLegend",
    "Produção": "LcdMod_Gas_Production",
    "Consumo": "LcdMod_Frame_Consumption",
    "Pico 1 h": "LcdMod_Frame_Peak1h",
    "Autonomia": "LcdMod_Frame_Autonomy",
    "Alinhamento de acoplagem": "LcdMod_DockingAlignment",
    "Conector de referência": "LcdMod_Frame_DockingSubtitle",
    "Distância": "LcdMod_Frame_Distance",
    "Pitch": "@BlockPropertyTitle_ProjectionRotationX",
    "Yaw": "@BlockPropertyTitle_ProjectionRotationY",
    "Roll": "@BlockPropertyTitle_ProjectionRotationZ",
    "Sistema de gás": "LcdMod_GasDashboard",
    "Gestão dinâmica de gás": "LcdMod_Frame_GasSubtitle",
    "H2 armazenado": "LcdMod_Gas_H2Stored",
    "Nível": "LcdMod_Gas_Level",
    "Capacidade": "LcdMod_Gas_Capacity",
    "O2 armazenado": "LcdMod_Gas_O2Stored",
    "Geradores": "LcdMod_Gas_Generators",
    "Ativos": "LcdMod_Gas_Active",
    "Reserva de gelo": "LcdMod_Gas_IceReserve",
    "Saída de H2": "LcdMod_Gas_H2Output",
    "Vazão": "LcdMod_Gas_Flow",
    "Uso": "LcdMod_Gas_Usage",
    "Saída de O2": "LcdMod_Gas_O2Output",
    "Estoque": "LcdMod_Gas_Stockpile",
    "Total armazenado": "LcdMod_Gas_TotalStored",
    "Limite de reserva": "LcdMod_Frame_ReserveLimit",
    "Reabastecimento": "LcdMod_Gas_Refill",
    "Nível de disparo": "LcdMod_Frame_TriggerLevel",
    "Estado do sistema": "LcdMod_Gas_SystemStatus",
    "Sistemas": "LcdMod_Frame_Systems",
    "Armazenamento": "LcdMod_Frame_Storage",
    "Automação": "LcdMod_Frame_Automation",
    "Capacidade de elevação": "LcdMod_LiftCapacity",
    "Massa, empuxo e margem": "LcdMod_Frame_LiftSubtitle",
    "Gravidade": "LcdMod_Lift_Gravity",
    "Massa da nave": "LcdMod_Lift_ShipMass",
    "Empuxo de elevação": "LcdMod_Lift_LiftThrust",
    "Uso de elevação": "LcdMod_Lift_LiftUsage",
    "Massa máxima": "LcdMod_Lift_MaxMass",
    "Máx. seguro": "LcdMod_Lift_SafeMax",
    "Pode carregar": "LcdMod_Lift_CanCarry",
}

ALIGN = {"start": "L", "middle": "C", "end": "R", "—": "L"}


def cells(line):
    return [c.strip() for c in line.strip().strip("|").split("|")]


def num(text):
    return float(text.replace(",", "."))


def parse(md_path, theme):
    screens = []
    screen = None
    mode = None
    for line in md_path.read_text(encoding="utf-8").splitlines():
        m = re.match(r"^## (LcdInfo_\w+)", line)
        if m and m.group(1) in SKIP:
            screen = None
            continue
        if m:
            screen = {"theme": theme, "id": m.group(1), "size": None, "slots": [], "labels": []}
            screens.append(screen)
            mode = "slot"
            continue
        if line.startswith("### "):
            mode = "label"
            continue
        m = re.search(r"· (\d+) × (\d+)", line)
        if m and screen and screen["size"] is None:
            screen["size"] = (int(m.group(1)), int(m.group(2)))
            continue
        if screen is None or not line.startswith("|") or line.startswith("|---") or line.startswith("| Campo") or line.startswith("| Chave"):
            continue
        c = cells(line)
        if mode == "slot":
            kind = "b" if c[1] == "barra" else "t"
            font = 0.0 if c[8] == "—" else num(c[8])
            screen["slots"].append((c[0], kind, num(c[2]), num(c[3]), num(c[4]), num(c[5]), font, ALIGN[c[9]]))
        else:
            ref = c[1]
            if ref in SKIP_LABELS:
                continue
            if ref not in LABEL_KEYS:
                sys.exit(f"rótulo sem chave no mapa: {ref!r} ({theme}/{screen['id']})")
            maxw = 0.0 if c[9] == "—" else num(c[9])
            screen["labels"].append((LABEL_KEYS[ref], num(c[2]), num(c[3]), ALIGN[c[4]], num(c[6]), int(c[7]) >= 700, maxw, ref))
    return screens


def f(value):
    text = f"{value:.2f}".rstrip("0").rstrip(".")
    return (text if text else "0") + "f"


def short_id(lcd_id):
    assert lcd_id.startswith(ID_PREFIX), lcd_id
    name = lcd_id[len(ID_PREFIX):]
    tiny = name.endswith("_Tiny")
    return (name[:-5] if tiny else name), tiny


def emit_cs(screens):
    out = ["// <auto-generated> tools/FrameLayouts/gen.py — não editar à mão.", "using LcdMod.Common.Config.Components;", "",
           "namespace LcdMod.Client.Gui.Frames", "{", "    static partial class FrameLayouts", "    {",
           "        static void Build()", "        {"]
    for s in screens:
        name, tiny = short_id(s["id"])
        texture = f"SD_Frame_{s['theme']}_{name}{'_Tiny' if tiny else ''}"
        out.append(f"            Screen(LcdTheme.{s['theme']}, \"{name}\", {'true' if tiny else 'false'}, \"{texture}\", {f(s['size'][0])}, {f(s['size'][1])});")
        for field, kind, x, y, w, h, font, align in s["slots"]:
            out.append(f"            Slot(\"{field}\", '{kind}', {f(x)}, {f(y)}, {f(w)}, {f(h)}, {f(font)}, '{align}');")
        for key, x, y, align, px, bold, maxw, _ in s["labels"]:
            key = key[1:] if key[0] == "@" else key
            out.append(f"            Label(\"{key}\",{f(x)}, {f(y)}, '{align}', {f(px)}, {'true' if bold else 'false'}, {f(maxw)});")
    out += ["        }", "    }", "}", ""]
    return "\n".join(out)


def emit_sbc(screens):
    out = ['<?xml version="1.0" encoding="utf-8"?>',
           '<Definitions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">',
           "    <LCDTextures>"]
    for s in screens:
        name, tiny = short_id(s["id"])
        subtype = f"SD_Frame_{s['theme']}_{name}{'_Tiny' if tiny else ''}"
        out += ["        <LCDTextureDefinition>",
                f'            <Id Type="LCDTextureDefinition" Subtype="{subtype}"/>',
                f"            <SpritePath>Textures/Sprites/Frames/{s['theme']}/{s['id']}.dds</SpritePath>",
                "            <Selectable>false</Selectable>",
                "        </LCDTextureDefinition>"]
    out += ["    </LCDTextures>", "</Definitions>", ""]
    return "\n".join(out)


def resx_values(path):
    return {d.get("name"): (d.find("value").text or "") for d in ET.parse(path).getroot().findall("data")}


def validate(screens):
    by_theme = {t: {} for t in THEMES}
    for s in screens:
        by_theme[s["theme"]][s["id"]] = s
        assert s["size"], s["id"]
        dds = ROOT / "Core/Textures/Sprites/Frames" / s["theme"] / f"{s['id']}.dds"
        assert dds.is_file(), dds
    assert all(len(v) == 16 for v in by_theme.values()), {k: len(v) for k, v in by_theme.items()}
    for lcd_id, ref in by_theme["Futuristic"].items():
        ref_fields = {x[0] for x in ref["slots"]}
        for theme in ("Clean", "Cafe"):
            fields = {x[0] for x in by_theme[theme][lcd_id]["slots"]}
            assert fields == ref_fields - {"rodape.estado_link"}, (theme, lcd_id, fields ^ ref_fields)
    en = resx_values(ROOT / "Core/Data/Localization/MyTexts.resx")
    pt = resx_values(ROOT / "Core/Data/Localization/MyTexts.pt-BR.resx")
    for s in screens:
        for key, *_ in s["labels"]:
            if key[0] in "=@":
                continue
            assert key in en and key in pt, key
    return sum(len(s["slots"]) for s in screens), sum(len(s["labels"]) for s in screens)


def default_max_width(width, x, align):
    """Espelho de FrameRenderer.DefaultMaxWidth."""
    if align == "C":
        return min(width * 0.6, 2 * min(x, width - x) - 32)
    if align == "R":
        return x - 16
    return width - x - 16


def check_langs(screens):
    """Estimativa grosseira (largura ≈ chars × px × 0.55) dos rótulos com largura máxima que estouram por idioma."""
    loc_dir = ROOT / "Core/Data/Localization"
    en = resx_values(loc_dir / "MyTexts.resx")
    seen = set()
    for resx in sorted(loc_dir.glob("MyTexts*.resx")):
        values = resx_values(resx)
        culture = resx.stem.replace("MyTexts", "").lstrip(".") or "en"
        for s in screens:
            if s["theme"] != "Futuristic":
                continue
            for key, x, _, align, px, _, maxw, ref in s["labels"]:
                if key[0] in "=@":
                    continue
                if maxw <= 0:
                    maxw = default_max_width(s["size"][0], x, align)
                text = values.get(key) or en.get(key, "")
                width = len(text) * px * 0.55
                if width > maxw and (culture, s["id"], key) not in seen:
                    seen.add((culture, s["id"], key))
                    print(f"{culture:6} {s['id']:32} {key:36} {text!r} ~{width:.0f}px > {maxw:.0f}px")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", default=r"C:\Users\maike\Downloads\SpaceDash_molduras\v2")
    parser.add_argument("--check-langs", action="store_true")
    args = parser.parse_args()
    src = pathlib.Path(args.src)
    screens = []
    for theme in THEMES:
        screens += parse(src / theme / f"coordenadas_{theme}.md", theme)
    slots, labels = validate(screens)
    cs = ROOT / "Core/Data/Scripts/Core/Client/Gui/Frames/FrameLayouts.g.cs"
    cs.parent.mkdir(parents=True, exist_ok=True)
    cs.write_text(emit_cs(screens), encoding="utf-8", newline="\n")
    (ROOT / "Core/Data/Textures-Frames.sbc").write_text(emit_sbc(screens), encoding="utf-8", newline="\n")
    print(f"{len(screens)} telas, {slots} campos, {labels} rótulos")
    if args.check_langs:
        check_langs(screens)


if __name__ == "__main__":
    main()
