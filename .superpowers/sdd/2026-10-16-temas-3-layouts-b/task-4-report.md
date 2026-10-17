# Task 4 — Esteiras: layouts Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; `.resx` não editado por regra de concorrência — chaves listadas abaixo para o orquestrador.)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs`:

- `using LcdMod.Client.Gui.ControlsTemplates.Progress;` (para `DonutSegment`).
- Constantes `MINIMAL_HERO_FONT_MUL = 3f`, `RING_TRACK_MIX = 0.15f`, `SEPARATOR = " · "`.
- Campos reutilizados: `readonly DonutSegment[] _segments = new DonutSegment[3]`, `readonly List<BareLayout.KvRow> _rows`, `readonly Dictionary<string, string> _labels` (cache de rótulos em caixa normal; o `_loc`/`Loc` da Task 3 continua em MAIÚSCULAS só para o rodapé do layout cheio).
- `GetSprites`: ramos `IsMinimal` → `DrawMinimal` e `IsCompact` → `DrawCompact` logo após `sprites.Clear()`, antes de `GetMapArea()`/layout cheio. O fundo já vem do surface script (`ConveyorMap.cs:70` chama `AddBackground`; `DrawTitle` retorna cedo em temas bare), então o app não repete `AddBackground`. Os ramos novos usam `Host.ViewBox` inteiro (ignoram `FooterHeight`/`ContentTop`).
- `DrawMinimal`: `intact = _blocks.Count - Damaged`; `valid = BlueprintTotal > 0`; fatias `(intact/BlueprintTotal, accent)`, `(Damaged/BlueprintTotal, warn)`, `(Destroyed/BlueprintTotal, erro)` (todas 0 quando `BlueprintTotal == 0`); trilho `Lerp(BackgroundColor, fg, 0.15f)`; centro `%` íntegro com `fontMul 3f`, cor por estado (`DamageColor`: erro se há destruídos, aviso se há danificados, senão acento); legenda linha 1 = três números com cores separados por `" · "` (`DrawCounts`: mede a string inteira com `MeasureText(FontScale)`, some inteira se não cabe em largura/altura, senão desenha cinco `AddText` lado a lado — íntegros em fg, danificados em aviso se > 0, destruídos em erro se > 0); linha 2 = `string.Format(Label("BlocksNetworks"), _blocks.Count, NetworkCount())` via `Bare.Text(fontMul 1f)`. `IsWide` → anel na metade esquerda e as duas linhas à direita (mesma geometria de `DefenseDashboardApp.DrawMinimal`); senão anel em cima e linhas centradas embaixo; se não há altura para as linhas, só o anel.
- `NetworkCount()`: conta `_rootSize[i] > 0` para `i < _nodes.Count` (conforme brief).
- `DrawCompact`: `_blocks.Count == 0` → `Host.DrawMessage(sprites, Label("NoConveyors"), "Warning", GetWarningColor())`. Senão `Bare.Kv(ViewBox, _rows, fg, Theme)` com: Planta (`BlueprintTotal`, só se `HasBlueprint`); Íntegros (`intact · %`, cor `DamageColor`); Danificados (`Damaged · TrimName(WorstDamagedName) %pior`, aviso; some se 0); Destruídos (erro; some se 0 ou sem planta); Desligados · Gargalos (`_alerts · _bottlenecks`; erro se desligados > 0, aviso se gargalos > 0, senão fg).
- `Label(key)`: cache em `_labels` de `LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_" + key)` sem `ToUpperInvariant`.

Nada alterado no layout cheio, em `Rebuild`/`CollectDamage` ou no rodapé da Task 3.

## Chaves resx (não editadas aqui — a aplicar em `Core/Data/Localization/MyTexts.resx` e `MyTexts.pt-BR.resx`)

| Chave | en | pt-BR | Situação |
|---|---|---|---|
| `LcdMod_ConveyorMap_BlocksNetworks` | `{0} blocks · {1} networks` | `{0} blocos · {1} redes` | nova (brief desta task) |
| `LcdMod_ConveyorMap_OffBottlenecks` | `Off · Bottlenecks` | `Desligados · Gargalos` | nova; rótulo da última linha do Compacto (uma chave evita concatenar dois rótulos por frame) |
| `LcdMod_ConveyorMap_Intact` | `Intact` | `Íntegros` | pedida pela Task 3; agora usada |
| `LcdMod_ConveyorMap_Blueprint` | `Blueprint` | `Planta` | pedida pela Task 3; agora usada |
| `LcdMod_ConveyorMap_Damaged` | `Damaged` | `Danificados` | pedida pela Task 3; usada também aqui |
| `LcdMod_ConveyorMap_Destroyed` | `Destroyed` | `Destruídos` | pedida pela Task 3; usada também aqui |
| `LcdMod_ConveyorMap_Alerts` | `Off` | `Desligados` | pedida pela Task 3; não usada pelos ramos novos (substituída por `_OffBottlenecks`), continua no rodapé |

Já existentes e reutilizadas: `LcdMod_ConveyorMap_NoConveyors`, `LcdMod_NotAvailable`.

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs do jogo). Feito em vez disso:
  - Releitura do diff completo (`git diff` do arquivo): C# 6 (`out` com declaração prévia, sem tuplas/pattern matching/`out var`), sem LINQ, sem comentários/TODO, nenhuma variável sem uso. Alocações por frame: só as strings de valor (`ToString`/concatenação), como nos demais apps do padrão; `_segments`, `_rows`, `_labels` reutilizados.
  - Membros conferidos por leitura: `BareLayout.Ring(List<MySprite>, RectangleF, IReadOnlyList<DonutSegment>, Color, string, Color, float)` (`BareLayout.cs:108`; `DonutSegment[]` implementa `IReadOnlyList<>`), `Bare.Text`, `Bare.Kv`, `Bare.Margin`, `Bare.LineHeight`, `BareLayout.IsWide`, `BareLayout.KvRow` (`Label/Icon/Value/ValueColor/Bar/BarColor`); `DonutSegment(float, Color)` (`DonutPanel.cs:12`, namespace `LcdMod.Client.Gui.ControlsTemplates.Progress`); `IsMinimal/IsCompact/Theme/Bare/GetHeaderColor/GetWarningColor/GetErrorColor` (`AppBase.cs:227-258`); `MeasureText(string, float)` e `AddText` (já usados no arquivo; `FontScale` da classe = `Host.FontScale`, igual ao `FontScale * 1f` que `Bare` usa para medir); `IAppHost.DrawMessage(sprites, message, icon, color, scale = 1f)` (`IAppHost.cs:38`); `FormatingHelper.PercentageToString(float)` e `TrimName(string, int = 8)` (`FormatingHelper.cs:266,289`).
  - Surface script: `ConveyorMap.cs:70-71` já chama `AddBackground` e `DrawTitle` (que retorna em temas bare, `SurfaceScriptBase.cs:687`).
  - Line endings: arquivo continua LF (`git ls-files --eol` = `i/lf w/lf`; 0 bytes `\r`).
- Não executado: `.\check.ps1`, `.\build.ps1`, teste no jogo (LCD 1×3 largo; `BlueprintTotal == 0`; sem esteiras em Compacto).

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/ConveyorMapApp.cs` (único; sem git).

## Preocupações

- `NetworkCount()` segue o brief (`_rootSize[i] > 0`) e por isso conta também nós isolados (redes de tamanho 1), enquanto o rodapé do layout cheio mostra `_networkCount` (só redes com ≥ 2 nós). Se quiser o mesmo número do rodapé, trocar `NetworkCount()` por `_networkCount` na linha 2 do Mínimo.
- `BlueprintTotal == 0` só ocorre com `_blocks.Count == 0`; nesse caso o Mínimo desenha o anel vazio sem texto central (padrão "sem dados" do Plano 2) em vez de "N/A" — o "N/A" fica só no Compacto (inalcançável na prática, pois zero blocos cai em `DrawMessage`).
- `_tooLarge` é ignorado nos temas novos: os contadores (`Damaged`, `Destroyed`, `_alerts`, `_bottlenecks`) continuam válidos porque `CollectDamage`/`BuildNodes` rodam antes do retorno antecipado de `Rebuild`; só o layout de mapa é pulado.
- Até o orquestrador adicionar as chaves novas ao resx, `LocHelper.GetLoc` devolve o fallback para chave ausente; `string.Format` com `_BlocksNetworks` ausente não lança (a string de fallback não tem `{0}`), só mostra o texto errado.
- `_labels` não é limpo em `LayoutChanged` (idem `_loc` da Task 3); troca de idioma em jogo mantém o texto antigo até recriar o app.

## Skills

Nenhuma invocada.
