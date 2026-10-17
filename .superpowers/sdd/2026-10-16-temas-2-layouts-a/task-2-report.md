# Task 2 — Painel de Energia em Mínimo e Compacto — Relatório

## Status: DONE_WITH_CONCERNS

(Sem compilador na máquina; e uma preocupação de chrome fora do arquivo permitido — ver "Preocupações".)

## O que foi implementado

Só `Info/Data/Scripts/Info/Client/Apps/EnergyGeneralApp.cs` (+81 linhas, LF, sem CR):

- `using LcdMod.Client.Gui.Styling;` para `BareLayout.Tile`.
- Constantes: `ICON_POWER = "IconEnergy"`, `ICON_BATTERY = "Battery"`, `MINIMAL_HERO_FONT_MUL = 3f`, `RING_TRACK_MIX = 0.15f`, `USED_WARN_RATIO = 0.75f`, `USED_ERROR_RATIO = 0.9f`; campo `readonly List<BareLayout.Tile> _tiles`, limpo por frame.
- `GetSprites`: após `PrepareFrame()` (mantido: `Palette` é usada por `BatteryStatusColor`) e `_sprites.Clear()`, entram `if (IsMinimal) { DrawMinimal(_sprites); return _sprites; }` / `if (IsCompact) { … }` antes do ramo `Host.Frame != null`. O app não chama `Host.AddBackground` (o `EnergySurfaceScriptBase.GetSprites` já faz isso), então o padrão foi adaptado sem essa linha.
- `DrawMinimal`: área `Host.ViewBox`; trilho `Color.Lerp(Host.BackgroundColor, fg, 0.15f)`; `valid = HasData && ProducedMw > EPSILON_MW`; fração = `Clamp(ConsumedMw / ProducedMw, 0, 1)` quando `valid`, senão 0; cor: `> 0.9` `GetErrorColor()`, `> 0.75` `GetWarningColor()`, senão `GetHeaderColor()`; sem `valid` a cor é `fg`. Centro: `%` (fonte ×3) ou `NotAvailable` quando produção é 0; sem dados, anel vazio sem texto. Linha auxiliar `"P " + MegaWattsToString(ProducedMw) + "  C " + MegaWattsToString(ConsumedMw) + "  " + PowerText(NetMw, true, float.MaxValue)` via `Bare.Text(..., 1f, ...)`: abaixo do anel, ou à direita (anel na metade esquerda) quando `BareLayout.IsWide(view)`; some inteira se não couber.
- `DrawCompact`: sem dados → `Host.DrawMessage(sprites, NotAvailable, "Warning", GetWarningColor())`. Senão tiles 2×2: Produção (`IconEnergy`, `fg`), Consumo (`IconEnergy`, `fg`), Carga (`Battery`, `PercentageToString(StoredRatio)` com `BatteryStatusColor(BatteryStatus)`, ou `NotAvailable` em `fg` sem baterias), Saldo (`IconEnergy`, `PowerText(NetMw, true, …)` com sinal, `GetHeaderColor()` se `NetMw >= 0` senão `GetErrorColor()`). `Bare.Tiles(sprites, Host.ViewBox, _tiles, 2, 2, fg, accent, Theme)`.

## Divergências em relação ao brief

- Fração do anel calculada como `ConsumedMw / ProducedMw` (texto do brief) em vez de `Snapshot.UsedRatio`: no serviço, `UsedRatio = consumed / available` (capacidade disponível, não produção), então não é "consumo ÷ produção" e não permite o caso "N/A quando `ProducedMw == 0`" do Review Focus 1. Os limiares de cor (0,75 / 0,9) aplicam-se à mesma fração.
- Cor "verde" do saldo ≥ 0: usei `GetHeaderColor()` (acento), conforme decisão 5 da spec (ok = HeaderColor); negativo = `GetErrorColor()`.
- Mínimo com `ProducedMw == 0` mas com dados: anel vazio em `fg` e centro "N/A" (Review Focus 1); linha auxiliar ainda aparece.

## Chaves resx

Nenhuma nova. Reutilizadas (todas existentes em `MyTexts.resx` e `MyTexts.pt-BR.resx`): `LcdMod_Energy_Production`, `LcdMod_Energy_Consumption`, `LcdMod_Energy_Charge`, `LcdMod_Energy_Balance`, `LcdMod_NotAvailable`.

## Texturas

- `IconEnergy` (vanilla; já usada em `EnergyChartApp`, `GasApp`).
- `Battery` (`Core/Data/Textures.sbc` linha 51, `Subtype="Battery"`). `SD_Icon_Battery` não existe — não usada.

## Verificação

- `check.ps1` não rodou (sem `dotnet`/DLLs do jogo). Em vez disso:
  - Grep de cada membro: `EnergySnapshot.ProducedMw/ConsumedMw/NetMw/StoredRatio/BatteryStatus/HasData/HasBatteries/EPSILON_MW/Empty`; `EnergyAppBase.Snapshot/Loc/NotAvailable/PowerText(float,bool,float)/BatteryStatusColor`; `App.IsMinimal/IsCompact/Bare/Theme/GetHeaderColor/GetWarningColor/GetErrorColor/GetForegroundColor`; `IAppHost.ViewBox/BackgroundColor/DrawMessage(sprites, message, icon, color, scale = 1f)`; `BareLayout.Margin/LineHeight/Ring/Text/Tiles/IsWide/Tile`; `FormatingHelper.MegaWattsToString/PercentageToString`.
  - Sobrecarga: `PowerText(s.NetMw, true, float.MaxValue)` resolve para a versão `(float, bool, float)` da base, não para a `(float, bool, RectangleF)` desta classe.
  - C# 6 apenas (sem tuplas, `out var`, pattern matching, LINQ). Releitura do diff: sem variáveis sem uso, sem comentários novos, sem TODO. Layout cheio intocado.
  - `git ls-files --eol`: `i/lf w/lf`; diff sem `\r`.
- Não executado: `.\check.ps1`, teste no jogo.

## Arquivos alterados

- `Info/Data/Scripts/Info/Client/Apps/EnergyGeneralApp.cs`

Nenhum comando git executado (o orquestrador commita).

## Preocupações

1. **Chrome do painel em Mínimo/Compacto (fora do meu arquivo).** `EnergySurfaceScriptBase.DrawTitle` chama `_app.DrawChrome(frame)` sempre que `OwnsTitle && HasData` (sem `Frame`), e `EnergyAppBase.DrawChrome` desenha `EnergyCardRenderer.DrawPanel` + barra de título. Nos temas novos isso viola a regra 1 da spec (nada em volta) e o anel/tiles desenham sobre `Host.ViewBox`, por baixo/cruzando a moldura e a barra de título. Mudança proposta (um dos dois): em `EnergySurfaceScriptBase.DrawTitle`, incluir `ThemeChrome.IsBare(ThemeKind)` na condição que cai em `base.DrawTitle(frame)`; ou em `EnergyAppBase.DrawChrome`, `if (IsBare) return Body.Y;` logo após `PrepareFrame()`. O mesmo vale para `EnergyCardApp` (também `OwnsTitle`). Não editei por estar fora do escopo permitido.
2. Sem compilação local: risco residual que só `check.ps1` pega.
3. `"P "`/`"C "` da linha auxiliar são literais do brief, não localizados (funcionam em en e pt-BR: Production/Produção, Consumption/Consumo).
4. Na prática `DrawCompact` sem dados quase nunca roda: `EnergySurfaceScriptBase.GetSprites` só chama `_app.GetSprites()` com `HasData && HasVisibleItems()`; a guarda fica como defesa do padrão do plano.

## Skills

Nenhuma invocada.
