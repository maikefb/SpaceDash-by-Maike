# Task 5 — Energia Preenchida % em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; ver "Verificação". Sem commit: o orquestrador commita.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs`:

- `using LcdMod.Client.Gui.Styling;` (para `BareLayout`).
- Constantes `MINIMAL_HERO_FONT_MUL = 3f`, `MINIMAL_LINE_FONT_MUL = 1f`; campo `readonly List<BareLayout.KvRow> _kvRows` limpo por frame.
- `GetSprites`: logo após `sprites.Clear()`, `if (IsMinimal) { DrawMinimal; return } if (IsCompact) { DrawCompact; return }`, antes de `DrawFooter`/`DrawBatteries`. Sem `AddBackground` no app: o surface script `PowerFilled.cs` já chama `AddBackground` e `DrawTitle` antes de `_app.GetSprites()`. Layout cheio intocado.
- `MinimalCollector()`: primeiro coletor com `HasVisibleItems` (baterias vem primeiro em `BuildCollectors`, logo é o preferido); se nenhum tem itens, devolve `_collectors[0]` para a textura vazia; `null` se não há coletores.
- `CountEntries(collector)`: conta `_entries` cujo `FillableTexture` é a mesma instância do coletor (cada coletor usa uma `static readonly FillableTexture` própria; `PowerCollector` não expõe contagem).
- `DisplayedRatio(collector)`: mesmo arredondamento de `DrawFooterRow` (`Math.Round(AverageCharge*100)/100`).
- `DrawMinimal`: área = `Host.ViewBox`. Em `BareLayout.IsWide(view)` a textura fica na metade esquerda e a linha auxiliar à direita (LEFT, centrada verticalmente); senão a textura ocupa `view.Height - LineHeight(1) - Margin` e a linha fica abaixo (CENTER). `iconSize = min(W,H) - 2*Margin` da área da textura; `DrawFillableTexture(FillableTexture, center, iconSize, ratio, StatusColor, fg, DrawCenterIcon, 0f, CenterIconScale)`. `%` médio via `Bare.Text(fontMul 3f, fg)` centrado no `GetInnerRect` da textura, com `maxWidth/maxHeight` = retângulo interno (some se não cabe). Linha auxiliar = `count + " · " + StatusText` (ou só `count` se `StatusText` vazio), `Bare.Text(fontMul 1f, fg)`. Sem coletor com itens: só a textura vazia (fração 0, preenchimento `fg`), sem textos.
- `DrawCompact`: uma `KvRow` por coletor com itens: `Label = FooterPrefix` (chaves do jogo `HudEnergyGroupBatteries` / `DisplayName_BlockGroup_JumpDrives`, já usadas pelo rodapé), `Icon = FillableTexture.Name` ("Battery"/"JumpDrive"), `Value = %` médio, `ValueColor = StatusColor`, `Bar = AverageCharge`, `BarColor = StatusColor`. Autonomia: para o `BatteryPowerCollector` com `HasRightSideText`, linha extra sem barra com `Label = LcdMod_Frame_Autonomy` (chave existente), `Icon = "IconEnergy"`, `Value = RightSideText` (tempo até cheio/vazio que o rodapé já mostra), `ValueColor = RightSideColor`. Zero linhas: `Host.DrawMessage(sprites, LocHelper.Empty, "Warning", GetWarningColor())` (mesma chave do surface script). Depois `Bare.Kv(sprites, Host.ViewBox, _kvRows, fg, Theme)`.

## Divergências em relação ao brief

- **Tanques H₂/O₂:** o app só tem `BatteryPowerCollector` e `JumpDrivePowerCollector` (`BuildCollectors`); não há coletor de tanques nem dado de gás no app. O Compacto lista baterias e jump drives. Linhas de tanque omitidas (plano: "nenhum dado novo neste plano").
- **Saldo (MW):** o app não expõe entrada/saída líquida (`totalIn/totalOut` são locais de `Collect`). Omitido.
- **Autonomia:** incluída só porque o coletor de baterias já publica `RightSideText` (tempo até cheio/vazio); jump drive não ganha linha de autonomia para não repetir o rótulo.
- `ChargingLabel` do brief: usei `collector.StatusText`, que é exatamente `ChargingLabel`/`DischargingLabel`/`FullLabel` conforme `StatusKind` (já localizado via `MyTexts`), em vez de só "carregando".

## Chaves resx novas

Nenhuma. Usadas só existentes: `LcdMod_Frame_Autonomy`, `LcdMod_Empty` (via `LocHelper.Empty`) e as do jogo via `FooterPrefix`.

## Verificação

- `check.ps1` **não rodou** (sem `dotnet`/DLLs aqui). Em vez disso:
  - Grep de cada membro usado: `App.IsMinimal/IsCompact/Bare/Theme/Host/GetWarningColor` (`AppBase.cs` :59, :238–242, :251); `BareLayout.Margin/LineHeight/Text/Kv/KvRow/IsWide` (assinaturas conferidas em `BareLayout.cs`); `IAppHost.ViewBox/ForegroundColor/DrawMessage(sprites, message, icon, color, scale = 1f)`; `PowerCollector.HasVisibleItems/AverageCharge/FillableTexture/StatusColor/StatusText/FooterPrefix/DrawCenterIcon/CenterIconScale/HasRightSideText/RightSideText/RightSideColor`; `FillableTexture.Name/GetInnerRect`; `PowerEntry.FillableTexture`; `FormatingHelper.PercentageToString(float)/Culture`; `Constants.MOD_PREFIX` (`LcdMod.Common.Helpers`, `using` já presente; `Generated.Constants` não está importado neste arquivo, sem ambiguidade); `LocHelper.Empty/GetLoc`.
  - C# 6: sem tuplas, `is` pattern, `out var`, `default` literal; `collector is BatteryPowerCollector` é o `is` clássico. Sem LINQ. Sem alocação nova por frame além das structs na lista reutilizada.
  - Releitura do `git diff` completo. LF (0 `\r` no arquivo). Sem comentários, sem TODO.
- Não executado: `check.ps1`, `build.ps1`, jogo.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/PowerFilledApp.cs` (+138 linhas)

## Preocupações

- `%` no centro da bateria com `fontMul 3f`: em LCD pequeno o retângulo interno não comporta e o número some (regra 2). Em LCD 1×1 padrão com fonte 1 deve caber; validar no jogo. Se quiser garantir, reduzir para 2,5× seria desvio da decisão 1 (percentual = 3×).
- No jump drive o `CenterIconTexture` ("JumpDriveCore") é desenhado por `DrawFillableTexture` sobre o retângulo interno, atrás do `%`; pode disputar atenção com o número quando o coletor exibido é o de jump drives (sem baterias). Aceitável.
- O separador " · " já é usado em `EnergyGeneralApp`, então a fonte do jogo o renderiza.
- `Bare.Kv` corta linhas que não cabem: em LCD baixo com fonte grande, a linha de autonomia (terceira) pode sumir primeiro, o que é o comportamento desejado.
