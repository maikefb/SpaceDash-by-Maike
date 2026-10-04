// Mantido como fonte estática. Pacote 1: sincronização da configuração das telas. Pacote 2: lista de sobreviventes. Pacote 3: paleta de cores da facção. Pacotes 4 e 5: pedido e entrega de imagem da Moldura digital.
// No servidor o pacote só é aceito (e retransmitido pelo NetworkManager) se o remetente tiver acesso ao bloco.
namespace Generated
{
    public partial class NetworkManager
    {
        partial void DispatchGeneratedCallbacks(ReceivedPacketEventArgs args, ref bool isKnown)
        {
            switch (args.PacketId)
            {
                case 1:
                {
                    isKnown = true;
                    if (!args.IsFromServer && args.DataLength > global::LcdMod.Common.Helpers.Constants.MAX_CONFIG_PACKET_BYTES)
                        return;
                    var config = args.UnWrap<global::LcdMod.Common.Networking.NetworkPackageSyncComponentConfig>();
                    bool isServer = global::Sandbox.ModAPI.MyAPIGateway.Session.IsServer;
                    if (!args.IsFromServer && isServer && global::LcdMod.Server.LcdModServerComponent.Instance != null)
                        args.SetResolved(global::LcdMod.Server.LcdModServerComponent.Instance.HandleSyncConfig(config, args.SenderId));
                    if (!global::Sandbox.ModAPI.MyAPIGateway.Utilities.IsDedicated &&
                        global::LcdMod.Client.LcdModClientComponent.Instance != null &&
                        (!isServer || args.IsResolved))
                        global::LcdMod.Client.LcdModClientComponent.Instance.HandleSyncConfig(config);
                    return;
                }
                case 2:
                {
                    isKnown = true;
                    var survivors = args.UnWrap<global::LcdMod.Common.Survivors.NetworkPackageSurvivors>();
                    if (!args.IsFromServer && global::Sandbox.ModAPI.MyAPIGateway.Session.IsServer)
                    {
                        var server = global::LcdMod.Server.LcdModServerComponent.Instance;
                        if (server != null && server.Survivors != null)
                            server.Survivors.SendTo(args.SenderId);
                        return;
                    }

                    if (args.IsFromServer && survivors != null)
                        global::LcdMod.Client.Modules.Survivors.SurvivorsClient.Apply(survivors.Entries);
                    return;
                }
                case 3:
                {
                    isKnown = true;
                    var colors = args.UnWrap<global::LcdMod.Common.FactionColors.NetworkPackageFactionColors>();
                    bool isServer = global::Sandbox.ModAPI.MyAPIGateway.Session.IsServer;
                    if (!args.IsFromServer && isServer && global::LcdMod.Server.LcdModServerComponent.Instance != null)
                        args.SetResolved(global::LcdMod.Server.LcdModServerComponent.Instance.HandleFactionColors(colors, args.SenderId));
                    if (!global::Sandbox.ModAPI.MyAPIGateway.Utilities.IsDedicated && (!isServer || args.IsResolved))
                        global::LcdMod.Client.FactionColors.FactionColorsSync.Receive(colors);
                    return;
                }
                case 4:
                {
                    isKnown = true;
                    var request = args.UnWrap<global::LcdMod.Common.Networking.NetworkPackageRequestTexture>();
                    if (!args.IsFromServer && global::Sandbox.ModAPI.MyAPIGateway.Session.IsServer && global::LcdMod.Server.LcdModServerComponent.Instance != null)
                        global::LcdMod.Server.LcdModServerComponent.Instance.HandleRequestTexture(request, args.SenderId);
                    else if (args.IsFromServer && !global::Sandbox.ModAPI.MyAPIGateway.Utilities.IsDedicated && global::LcdMod.Client.LcdModClientComponent.Instance != null)
                        global::LcdMod.Client.LcdModClientComponent.Instance.HandleRequestTexture(request);
                    return;
                }
                case 5:
                {
                    isKnown = true;
                    var texture = args.UnWrap<global::LcdMod.Common.Networking.NetworkPackageSyncTexture>();
                    if (!args.IsFromServer && global::Sandbox.ModAPI.MyAPIGateway.Session.IsServer && global::LcdMod.Server.LcdModServerComponent.Instance != null)
                        global::LcdMod.Server.LcdModServerComponent.Instance.HandleSyncTexture(texture, args.SenderId);
                    else if (args.IsFromServer && !global::Sandbox.ModAPI.MyAPIGateway.Utilities.IsDedicated && global::LcdMod.Client.LcdModClientComponent.Instance != null)
                        global::LcdMod.Client.LcdModClientComponent.Instance.HandleSyncTexture(texture);
                    return;
                }

                default:
                    return;
            }
        }
    }
}
