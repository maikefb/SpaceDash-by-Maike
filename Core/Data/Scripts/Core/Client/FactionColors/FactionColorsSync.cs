using System;
using System.Collections.Generic;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Common.Config.Components;
using LcdMod.Common.FactionColors;
using LcdMod.Common.Networking;
using Sandbox.ModAPI;

namespace LcdMod.Client.FactionColors
{
    /// <summary>
    /// Lado cliente do switch "Sincronizar cores". Publica a paleta com debounce por facção (15 frames após a
    /// última mudança, no máximo 60). Cada publicação parte da paleta atual da facção e troca só o que mudou:
    /// as três cores do mod vindas do seletor, ou fonte e fundo vindos da superfície. Ao receber, só grava a
    /// variável e redesenha: fonte e fundo nativos são escritos pelo servidor, que o jogo replica.
    /// </summary>
    public static class FactionColorsSync
    {
        const long DEBOUNCE_FRAMES = 15;
        const long MAX_DELAY_FRAMES = 60;

        sealed class Pending
        {
            public NetworkPackageFactionColors Packet;
            public long DueFrame;
            public long DeadlineFrame;
        }

        static readonly Dictionary<long, Pending> PendingByFaction = new Dictionary<long, Pending>();
        static readonly List<long> Due = new List<long>();

        static long Frame => MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;

        /// <summary>Cores do mod alteradas no terminal: fonte e fundo vêm da paleta atual (ou da superfície se ainda não há paleta).</summary>
        public static void PublishModColors(IMyTerminalBlock block, ColorConfigComponent component, IMyTextSurface surface)
        {
            Publish(block, component, surface, false);
        }

        /// <summary>Fonte ou fundo alterados no terminal: as cores do mod vêm da paleta atual (ou do componente se ainda não há paleta).</summary>
        public static void PublishNativeColors(IMyTerminalBlock block, ColorConfigComponent component, IMyTextSurface surface)
        {
            Publish(block, component, surface, true);
        }

        static void Publish(IMyTerminalBlock block, ColorConfigComponent component, IMyTextSurface surface, bool nativeChanged)
        {
            long factionId = FactionColorsStore.FactionIdOf(block);
            if (factionId == 0L || component == null || surface == null || !IsLocalPlayerMember(factionId))
            {
                LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] publish recusado: facção=" + factionId + " membro=" + (factionId != 0L && IsLocalPlayerMember(factionId)));
                return;
            }

            FactionPalette palette;
            bool hasPalette = FactionColorsStore.TryGet(factionId, out palette);
            if (!hasPalette)
                palette = new FactionPalette();

            if (!hasPalette || !nativeChanged)
            {
                palette.Header = component.HeaderColor.Get(() => component.ResolveHeaderColor(block));
                palette.Warning = component.WarningColor.Get(() => component.ResolveWarningColor());
                palette.Error = component.ErrorColor.Get(() => component.ResolveErrorColor());
            }
            if (!hasPalette || nativeChanged)
            {
                palette.Foreground = surface.ScriptForegroundColor;
                palette.Background = surface.ScriptBackgroundColor;
            }

            FactionColorsStore.Set(factionId, palette);
            long frame = Frame;
            Pending pending;
            if (!PendingByFaction.TryGetValue(factionId, out pending))
            {
                pending = new Pending { DeadlineFrame = frame + MAX_DELAY_FRAMES };
                PendingByFaction[factionId] = pending;
            }
            pending.Packet = NetworkPackageFactionColors.From(factionId, palette);
            pending.DueFrame = Math.Min(frame + DEBOUNCE_FRAMES, pending.DeadlineFrame);
            LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] paleta pendente facção " + factionId + " nativo=" + nativeChanged + " fg=" + palette.Foreground + " bg=" + palette.Background + " tinhaPaleta=" + hasPalette);
        }

        static bool IsLocalPlayerMember(long factionId)
        {
            var player = MyAPIGateway.Session?.Player;
            var faction = MyAPIGateway.Session?.Factions?.TryGetFactionById(factionId);
            return player != null && faction != null && faction.IsMember(player.IdentityId);
        }

        public static void Flush(bool force)
        {
            if (PendingByFaction.Count == 0)
                return;

            long frame = Frame;
            Due.Clear();
            foreach (var entry in PendingByFaction)
                if (force || frame >= entry.Value.DueFrame)
                    Due.Add(entry.Key);

            foreach (var factionId in Due)
            {
                var packet = PendingByFaction[factionId].Packet;
                PendingByFaction.Remove(factionId);
                Send(packet);
            }
            Due.Clear();
        }

        static void Send(NetworkPackageFactionColors packet)
        {
            var server = LcdModSessionComponent.Server;
            if (server != null)
            {
                bool handled = server.HandleFactionColors(packet, MyAPIGateway.Multiplayer.MyId);
                LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] enviado ao servidor local: aceito=" + handled + " mp=" + MyAPIGateway.Multiplayer.MultiplayerActive);
                if (handled && MyAPIGateway.Multiplayer.MultiplayerActive)
                    LcdModSessionComponent.NetworkManager.TransmitToAllPlayers(packet, MyAPIGateway.Multiplayer.MyId);
                RedrawFaction(packet.FactionId);
                return;
            }

            LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] enviado ao servidor remoto");
            LcdModSessionComponent.NetworkManager.TransmitToServer(packet);
        }

        public static void Receive(NetworkPackageFactionColors packet)
        {
            if (packet == null)
                return;

            FactionColorsStore.Set(packet.FactionId, packet.ToPalette());
            RedrawFaction(packet.FactionId);
        }

        /// <summary>Copia a paleta da facção para o componente e para esta superfície. Falso quando a facção ainda não tem paleta.</summary>
        public static bool TryCopyToComponent(IMyTerminalBlock block, ColorConfigComponent component, IMyTextSurface surface)
        {
            FactionPalette palette;
            if (component == null || !FactionColorsStore.TryGet(FactionColorsStore.FactionIdOf(block), out palette))
                return false;

            component.HeaderColor.Set(palette.Header);
            component.WarningColor.Set(palette.Warning);
            component.ErrorColor.Set(palette.Error);
            if (surface != null)
            {
                if (surface.ScriptForegroundColor != palette.Foreground)
                    surface.ScriptForegroundColor = palette.Foreground;
                if (surface.ScriptBackgroundColor != palette.Background)
                    surface.ScriptBackgroundColor = palette.Background;
            }
            return true;
        }

        /// <summary>Cor da letra ou do fundo alterada no terminal deste bloco (setter vanilla envolvido pelo TerminalManager).</summary>
        public static void OnNativeColorsEdited(IMyTerminalBlock block)
        {
            var provider = block as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
            if (provider == null || provider.SurfaceCount <= 0)
            {
                LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] editado: sem provider/superfície");
                return;
            }

            var component = Config.ConfigManager.GetComponentForCurrentSurface<ColorConfigComponent>(block, LcdMod.Common.Helpers.Constants.COLORS);
            if (component == null || !component.SyncColors)
            {
                // TODO: Claude 03/10/2026 | remover log de diagnóstico do sync de cores após confirmar em jogo
                LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] editado: componente " + (component == null ? "nulo" : "sync=" + component.SyncColors));
                return;
            }

            var multiPanel = block.Components.Get<Sandbox.Game.EntityComponents.MyMultiTextPanelComponent>();
            int index = multiPanel != null ? multiPanel.SelectedPanelIndex : 0;
            if (index < 0 || index >= provider.SurfaceCount)
            {
                LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] editado: índice " + index + " fora de " + provider.SurfaceCount);
                return;
            }
            LcdMod.Common.Helpers.LogHelper.LogInfo("[Sync] editado: publicando fonte/fundo da superfície " + index);

            PublishNativeColors(block, component, (IMyTextSurface)provider.GetSurface(index));
        }

        static void RedrawFaction(long factionId)
        {
            foreach (var screen in SurfaceScriptBase.Instances)
            {
                var block = screen.Block as IMyTerminalBlock;
                if (block == null || screen.Config == null || FactionColorsStore.FactionIdOf(block) != factionId)
                    continue;

                var component = screen.ColorComponent;
                if (component != null && component.SyncColors)
                    screen.RequestRedraw();
            }
        }

        public static void Clear()
        {
            PendingByFaction.Clear();
            FactionColorsStore.ClearCache();
        }
    }
}
