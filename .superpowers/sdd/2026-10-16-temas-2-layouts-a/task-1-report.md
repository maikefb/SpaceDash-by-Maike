# Task 1 — Capacidade de Elevação em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Concerns por não ter compilador na máquina e por duas divergências pequenas em relação ao padrão do plano; ver abaixo.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/LiftCapacityApp.cs`:

- Entrada em `GetSprites`: logo após `sprites.Clear()`, `if (IsMinimal) { DrawMinimal(sprites); return sprites; } if (IsCompact) { DrawCompact(sprites); return sprites; }`, antes do ramo `Host.Frame != null`. Layout cheio intocado.
- Helpers privados `StateColor(fg, accent)` e `StateWord()` (mesma lógica já usada três vezes no layout cheio; o layout cheio não foi alterado para usá-los).
- `DrawMinimal`: `Bare.Ring(area, fraction, fill, track, center, stateColor, 3f)` com `track = Color.Lerp(BackgroundColor, fg, 0.15f)`; `valid = HasGravity && MaxMassKg > 0` → fração `Clamp(Usage)`, fill `stateColor`, centro `PercentageToString(Usage)`; senão fração 0, fill `fg`, sem texto central. Abaixo do anel (ou à direita, em `BareLayout.IsWide(ViewBox)`, com o anel na metade esquerda) a palavra de estado `Loc("Safe"|"Warn"|"Crit"|"NoGravity")` em maiúsculas via `ThemeChrome.LabelText(LcdTheme.Futuristic, …)` com `Bare.Text(fontMul 1.2f, stateColor)`. Sem `HasData`, só o anel vazio (palavra nula → `Bare.Text` não desenha).
- `DrawCompact`: sem `HasData` → `Host.DrawMessage(sprites, LocHelper.GetLoc(MOD_PREFIX + "NotAvailable"), "Warning", GetWarningColor())`. Senão `_tiles` (`readonly List<BareLayout.Tile>`, limpa por frame) com 4 tiles e `Bare.Tiles(sprites, ViewBox, _tiles, 2, 2, fg, accent, Theme)`:
  1. `IconTemperature` / `Loc("LiftUsage")` / % (ou N/A quando `!HasGravity` ou `MaxMassKg == 0`) em `stateColor`.
  2. `IconEnergy` / `Loc("CanCarry")` / `MassToString(CanCarryKg)` (N/A sem gravidade).
  3. `IconEnergy` / `Loc("ShipMass")` / `MassToString(ShipMassKg)` (N/A se 0).
  4. `ShieldIcon` / `Loc("SafeMax")` / `MassToString(SafeMaxKg)` (N/A sem gravidade).
- Constantes novas: `ICON_SAFE = "ShieldIcon"`, `MINIMAL_STATE_FONT_MUL = 1.2f`, `MINIMAL_HERO_FONT_MUL = 3f`.

## Chaves resx

Nenhuma nova. Todas já existem em `MyTexts.resx` e `MyTexts.pt-BR.resx`: `LcdMod_Lift_LiftUsage`, `LcdMod_Lift_CanCarry`, `LcdMod_Lift_ShipMass`, `LcdMod_Lift_SafeMax`, `LcdMod_Lift_Safe`, `LcdMod_Lift_Warn`, `LcdMod_Lift_Crit`, `LcdMod_Lift_NoGravity`, `LcdMod_NotAvailable` (confirmado por grep).

## Texturas

- `IconTemperature`, `IconEnergy`: vanilla, já usadas pelo app (`ICON_GRAVITY`, `ICON_MASS`); `IconTemperature` também aparece em `Core/Data/Texture-Remaps.sbc:66`.
- `ShieldIcon`: `Core/Data/Textures.sbc:14`. O brief pedia `SD_Icon_ShieldIcon`, que **não existe** (nenhum `SD_Icon_*` no .sbc nem no código); usei `ShieldIcon`.
- `SD_Icon_Gauge` não existe; ficou `IconTemperature` como o brief previa.
- `Warning`: `Core/Data/Textures.sbc:7` (usado por `DrawMessage`).

## Divergências em relação ao plano/brief

1. **`Host.AddBackground(_sprites)` não foi adicionado** em `GetSprites`: este app não chama o fundo hoje — o host `Info/.../SurfaceScripts/LiftCapacity.cs` já faz `AddBackground` + `DrawTitle` antes de `_app.GetSprites()` (e `DrawTitle` já retorna cedo em temas bare). Adicionar seria um segundo retângulo de fundo por frame.
2. **Fonte da palavra de estado = 1.2f** (brief, Step 1) em vez de 1f (Global Constraint da linha auxiliar). Trocar é uma constante (`MINIMAL_STATE_FONT_MUL`).
3. `HasData == false` em Compacto: o host nem chama o app nesse caso (`AddEmptySprites`), mas a guarda com `DrawMessage` está lá conforme o padrão.

## Verificação

- Sem `dotnet`/DLLs do jogo: `check.ps1` não rodou.
- Grep de cada membro usado: `IsMinimal`/`IsCompact`/`Bare`/`Theme`/`GetHeaderColor`/`GetWarningColor`/`GetErrorColor` em `AppBase.cs`; `IAppHost.DrawMessage(sprites, message, icon, color, scale = 1f)`; `BareLayout.Ring/Text/Tiles/Margin/LineHeight/IsWide` e `BareLayout.Tile { Icon, Label, Value, ValueColor }`; `ThemeChrome.LabelText(LcdTheme, string)`; `LcdTheme` em `LcdMod.Common.Config.Components` (já em `using`); `FormatingHelper.PercentageToString(float)` / `MassToString(double)` e `MathHelper.Clamp(double…)` já usados no arquivo.
- Releitura do diff: C# 6 apenas (sem tuplas, `out var`, pattern matching, local functions); sem LINQ; sem variável sem uso; sem comentários/TODO.
- Line endings: 0 `\r` no arquivo (LF).
- Review Focus 1 (divisão por zero): `MaxMassKg == 0` → fração 0, fill `fg`, centro/tile de uso = N/A. Review Focus 2 (LCD largo): anel na metade esquerda, texto à esquerda da metade direita a partir de `ringArea.Right + Margin`, largura limitada a `view.Right - left`.

## Preocupações

- Compilação não verificada localmente.
- Em LCD muito baixo (altura < linha + margem) a área do anel fica com altura ≤ 0 e `Ring` não desenha nada; a palavra de estado ainda aparece se couber. Aceitável pela regra 2 (some em vez de encolher), mas vale conferir no jogo.
- `StateColor`/`StateWord` duplicam expressões que o layout cheio já tem inline; não refatorei o layout cheio para respeitar "não mexa no layout cheio".
