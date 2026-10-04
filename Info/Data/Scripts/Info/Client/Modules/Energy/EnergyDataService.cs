using System;
using System.Collections.Generic;
using LcdMod.Client.GridData;
using LcdMod.Common.Helpers;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace LcdMod.Client.Modules.Energy
{
    /// <summary>Amostra a energia do escopo a cada 50 frames (0,83 s) e guarda 50 amostras, como o PowerGraph original.</summary>
    public sealed class EnergyDataService
    {
        const long SAMPLE_INTERVAL_FRAMES = 50L;
        const float STATUS_ENTER_MW = 0.001f;
        const float STATUS_EXIT_MW = 0.0002f;
        const double MAX_ETA_SECONDS = 366d * 86400d;

        /// <summary>No criativo o jogo enche 1/8 da capacidade por segundo: 3600 / 8 MW por MWh, antes da eficiência de recarga.</summary>
        const float CREATIVE_FILL_MW_PER_MWH = 450f;

        /// <summary>Teto da carga e da energia armazenada exibidas enquanto alguma bateria ainda tem espaço: arredonda para 99 %.</summary>
        const float ROOM_LEFT_DISPLAY_RATIO = 0.994f;

        /// <summary>
        ///     Amostras seguidas toleradas por bateria no corte de recarga do jogo: cheia em Auto, ela zera a entrada até o próprio
        ///     tick de 100 frames (duas amostras) e fornece sozinha; uma descarga real passa disso.
        /// </summary>
        const int FULL_DIP_SAMPLES = 2;

        readonly LinkedTypedBlockSourceSet<IMyTerminalBlock> _terminalSources =
            new LinkedTypedBlockSourceSet<IMyTerminalBlock>(blocks => blocks.TerminalBlocks);
        readonly EnergySnapshot _snapshot = new EnergySnapshot();
        readonly long _phase;
        long _lastSampleFrame = -SAMPLE_INTERVAL_FRAMES;

        /// <summary>Amostras seguidas de cada bateria no corte de recarga, por EntityId; os dois mapas trocam a cada amostra.</summary>
        Dictionary<long, int> _dipRuns = new Dictionary<long, int>();
        Dictionary<long, int> _nextDipRuns = new Dictionary<long, int>();

        /// <summary><paramref name="stagger"/> define a fase das amostras, para serviços distintos não amostrarem no mesmo frame.</summary>
        internal EnergyDataService(GridLogic requester, GridLinkTypeEnum linkType, int stagger)
        {
            Requester = requester;
            LinkType = linkType;
            _phase = stagger % SAMPLE_INTERVAL_FRAMES;
        }

        public GridLogic Requester { get; private set; }
        public GridLinkTypeEnum LinkType { get; private set; }
        public EnergySnapshot Latest => _snapshot;
        public int CaptureCount { get; private set; }
        public long ReleasedFrame { get; private set; }
        public bool HasCaptures => CaptureCount > 0;

        /// <summary>O Bind é idempotente: refaz as fontes só quando a lógica do grid ou o vínculo mudaram.</summary>
        internal void AddCapture(GridLogic requester)
        {
            if (requester != null)
                Requester = requester;
            if (CaptureCount == 0)
                _dipRuns.Clear();
            CaptureCount++;
            ReleasedFrame = 0L;
            _terminalSources.Bind(Requester, LinkType);
        }

        internal void Release()
        {
            if (CaptureCount > 0)
                CaptureCount--;
            if (CaptureCount == 0)
            {
                ReleasedFrame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
                _terminalSources.Unbind();
            }
        }

        internal void Update(long gameplayFrame)
        {
            if (!HasCaptures || Requester == null || !Requester.IsAlive)
                return;
            if (gameplayFrame - _lastSampleFrame < SAMPLE_INTERVAL_FRAMES)
                return;

            // A primeira amostra sai já; as seguintes caem nos frames da fase deste serviço.
            _lastSampleFrame = gameplayFrame + ((_phase - gameplayFrame) % SAMPLE_INTERVAL_FRAMES + SAMPLE_INTERVAL_FRAMES) % SAMPLE_INTERVAL_FRAMES;
            try
            {
                Sample();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        internal void Dispose()
        {
            _terminalSources.Dispose();
        }

        void Sample()
        {
            var s = _snapshot;
            for (int i = 0; i < EnergySnapshot.KIND_COUNT; i++)
                s.Kinds[i] = new EnergyKindStats();

            bool creative = MyAPIGateway.Session != null && MyAPIGateway.Session.CreativeMode;
            int units = 0;
            var batteryModes = EnergyBatteryMode.None;
            bool roomToCharge = false;
            _nextDipRuns.Clear();
            float produced = 0f, maxProduced = 0f, batteryIn = 0f, stored = 0f, capacity = 0f, storeRate = 0f, drainable = 0f, fillable = 0f, drainRate = 0f, fillRate = 0f;

            var sources = _terminalSources.Sources;
            for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
            {
                var source = sources[sourceIndex];
                for (int blockIndex = 0; blockIndex < source.Count; blockIndex++)
                {
                    var block = source[blockIndex];
                    var producer = block as IMyPowerProducer;
                    if (producer == null || block.Closed || block.MarkedForClose)
                        continue;

                    var battery = block as IMyBatteryBlock;
                    var definition = (block as MyCubeBlock)?.BlockDefinition;
                    var stats = s.Kinds[(int)Classify(block, battery, definition)];
                    var producerDefinition = definition as MyPowerProducerDefinition;
                    bool working = block.IsWorking;
                    float currentOutput = producer.CurrentOutput;
                    // Reatores e motores devolvem o nominal pela ModAPI mesmo parados; o jogo só dá saída máxima a quem funciona.
                    float maxOutput = working ? producer.MaxOutput : 0f;
                    float nominal = producerDefinition != null ? producerDefinition.MaxPowerOutput : producer.MaxOutput;

                    units++;
                    stats.Total++;
                    if (producer.Enabled)
                        stats.Enabled++;
                    if (working)
                    {
                        stats.Working++;
                        stats.WorkingDefinedOutputMw += nominal;
                    }

                    if (stats.RepresentativeDefinition == null)
                        stats.RepresentativeDefinition = definition;
                    stats.CurrentOutputMw += currentOutput;
                    stats.MaxOutputMw += maxOutput;
                    stats.DefinedOutputMw += nominal;

                    if (battery == null)
                    {
                        produced += currentOutput;
                        maxProduced += maxOutput;
                        continue;
                    }

                    var mode = battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Recharge ? EnergyBatteryMode.Recharge
                        : battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Discharge ? EnergyBatteryMode.Discharge
                        : EnergyBatteryMode.Auto;
                    batteryModes = batteryModes == EnergyBatteryMode.None || batteryModes == mode ? mode : EnergyBatteryMode.Mixed;
                    bool active = producer.Enabled && block.IsFunctional;
                    bool canCharge = active && mode != EnergyBatteryMode.Discharge;

                    // Como o jogo (MyBatteryBlock.UpdateInternal): fora do criativo só o excedente da entrada sobre a saída é
                    // guardado, com a eficiência de recarga (0,8 nas baterias comuns, 0,9 nas Prototech), e o déficit sai do banco sem perda. No
                    // criativo a bateria que pode carregar enche num ritmo fixo, sem depender da entrada, e nada descarrega.
                    var batteryDefinition = definition as MyBatteryBlockDefinition;
                    float recharge = batteryDefinition != null ? batteryDefinition.RechargeMultiplier : 1f;
                    float currentInput = battery.CurrentInput;
                    float flow = currentInput - currentOutput;
                    float rate = !(creative || FollowsCreativeRules(block)) ? (flow > 0f ? flow * recharge : flow)
                        : canCharge && battery.CurrentStoredPower < battery.MaxStoredPower ? battery.MaxStoredPower * recharge * CREATIVE_FILL_MW_PER_MWH : 0f;
                    if (rate < 0f && mode == EnergyBatteryMode.Auto && currentInput <= EnergySnapshot.EPSILON_MW &&
                        battery.CurrentStoredPower >= battery.MaxStoredPower * EnergySnapshot.FULL_RATIO)
                    {
                        int run;
                        _dipRuns.TryGetValue(block.EntityId, out run);
                        _nextDipRuns[block.EntityId] = ++run;
                        if (run <= FULL_DIP_SAMPLES)
                            rate = 0f;
                    }

                    batteryIn += currentInput;
                    storeRate += rate;
                    stored += battery.CurrentStoredPower;
                    capacity += battery.MaxStoredPower;
                    if (canCharge && battery.CurrentStoredPower < battery.MaxStoredPower * EnergySnapshot.FULL_RATIO)
                        roomToCharge = true;

                    // A previsão só conta o que pode fluir, energia e taxa do mesmo conjunto: em Recarregar a bateria não
                    // fornece, em Descarregar não recebe e desligada ou danificada não faz nenhum dos dois.
                    if (active && mode != EnergyBatteryMode.Recharge)
                    {
                        drainable += battery.CurrentStoredPower;
                        drainRate += rate;
                    }

                    if (canCharge)
                    {
                        fillable += battery.MaxStoredPower - battery.CurrentStoredPower;
                        fillRate += rate;
                    }
                }
            }

            var dipRuns = _dipRuns;
            _dipRuns = _nextDipRuns;
            _nextDipRuns = dipRuns;

            var batteries = s.Kinds[(int)EnergyKind.Battery];
            float consumed = Math.Max(0f, produced + batteries.CurrentOutputMw - batteryIn);
            float available = maxProduced + batteries.MaxOutputMw;
            float storedRatio = capacity > EnergySnapshot.EPSILON_MW ? MathHelper.Clamp(stored / capacity, 0f, 1f) : 0f;
            var status = ResolveBatteryStatus(s.BatteryStatus, batteries.Total, storeRate, storedRatio, roomToCharge);
            float eta = ResolveEta(status, drainable, -drainRate, fillable, fillRate);

            s.UnitsTotal = units;
            s.ProducedMw = produced;
            s.MaxProducedMw = maxProduced;
            s.BatteryInputMw = batteryIn;
            s.StoredMwh = roomToCharge ? Math.Min(stored, capacity * ROOM_LEFT_DISPLAY_RATIO) : stored;
            s.CapacityMwh = capacity;
            s.ConsumedMw = consumed;
            s.NetMw = status == EnergyBatteryStatus.Full ? 0f : storeRate;
            s.UsedRatio = available > EnergySnapshot.EPSILON_MW ? MathHelper.Clamp(consumed / available, 0f, 1f) : 0f;
            s.StoredRatio = roomToCharge ? Math.Min(storedRatio, ROOM_LEFT_DISPLAY_RATIO) : storedRatio;
            s.BatteryStatus = status;
            s.BatteryMode = batteryModes;
            s.EtaSeconds = eta;
            s.BatteryEmptySoon = status == EnergyBatteryStatus.Discharging && eta >= 0f && eta < EnergySnapshot.EMPTY_SOON_SECONDS;
            for (int i = 0; i < EnergySnapshot.KIND_COUNT; i++)
                s.Kinds[i].State = ResolveState((EnergyKind)i, s.Kinds[i], s);
            s.Push(produced, consumed);
            s.Version++;
        }

        static EnergyKind Classify(IMyTerminalBlock block, IMyBatteryBlock battery, MyCubeBlockDefinition definition)
        {
            if (battery != null)
                return EnergyKind.Battery;
            if (block is IMyReactor)
                return EnergyKind.Reactor;
            if (block is IMySolarPanel)
                return EnergyKind.Solar;
            if (block is IMyWindTurbine)
                return EnergyKind.Wind;
            if (definition is MyHydrogenEngineDefinition)
                return EnergyKind.Hydrogen;
            return EnergyKind.Other;
        }

        /// <summary>
        ///     Pela variação da energia armazenada. Histerese: entra em carga/descarga acima de ENTER e só volta a ocioso abaixo
        ///     de EXIT, sem trocar de sinal. As quedas curtas do ciclo de recarga do jogo já chegam zeradas por bateria
        ///     (<see cref="FULL_DIP_SAMPLES"/>). "Completo" só vale quando nenhuma bateria que pode carregar tem espaço
        ///     (<paramref name="roomToCharge"/>): uma bateria pequena carregando num banco grande cheio não some.
        /// </summary>
        static EnergyBatteryStatus ResolveBatteryStatus(EnergyBatteryStatus previous, int batteries, float netMw, float storedRatio, bool roomToCharge)
        {
            if (batteries == 0)
                return EnergyBatteryStatus.None;
            if (storedRatio >= EnergySnapshot.FULL_RATIO && !roomToCharge && netMw > -STATUS_ENTER_MW)
                return EnergyBatteryStatus.Full;
            if (netMw > STATUS_ENTER_MW)
                return EnergyBatteryStatus.Charging;
            if (netMw < -STATUS_ENTER_MW)
                return EnergyBatteryStatus.Discharging;
            if (Math.Abs(netMw) < STATUS_EXIT_MW)
                return EnergyBatteryStatus.Idle;
            if (previous == EnergyBatteryStatus.Charging && netMw > 0f)
                return previous;
            if (previous == EnergyBatteryStatus.Discharging && netMw < 0f)
                return previous;
            return EnergyBatteryStatus.Idle;
        }

        /// <summary>Tempo até cheia ou vazia: energia e taxa só das baterias que podem fluir naquele sentido.</summary>
        static float ResolveEta(EnergyBatteryStatus status, float drainableMwh, float drainRateMw, float fillableMwh, float fillRateMw)
        {
            float remaining;
            float rate;
            switch (status)
            {
                case EnergyBatteryStatus.Charging:
                    remaining = fillableMwh;
                    rate = fillRateMw;
                    break;
                case EnergyBatteryStatus.Discharging:
                    remaining = drainableMwh;
                    rate = drainRateMw;
                    break;
                default:
                    return EnergySnapshot.NO_ETA;
            }

            if (rate <= EnergySnapshot.EPSILON_MW || remaining <= 0f)
                return EnergySnapshot.NO_ETA;

            double seconds = remaining / rate * 3600d;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                return EnergySnapshot.NO_ETA;
            return (float)Math.Min(seconds, MAX_ETA_SECONDS);
        }

        static EnergyUnitState ResolveState(EnergyKind kind, EnergyKindStats stats, EnergySnapshot s)
        {
            if (stats.Total == 0)
                return EnergyUnitState.NoUnits;
            if (stats.Working == 0)
                return stats.Enabled == 0 ? EnergyUnitState.Offline : EnergyUnitState.Inoperative;
            if (stats.Working < stats.Total)
                return EnergyUnitState.Warning;

            switch (kind)
            {
                case EnergyKind.Reactor:
                case EnergyKind.Hydrogen:
                    return stats.LoadRatio >= EnergySnapshot.ALERT_LOAD_RATIO ? EnergyUnitState.Warning : EnergyUnitState.Online;
                case EnergyKind.Battery:
                {
                    bool discharging = s.BatteryStatus == EnergyBatteryStatus.Discharging;
                    return discharging && (s.StoredRatio < EnergySnapshot.LOW_CHARGE_RATIO || s.BatteryEmptySoon)
                        ? EnergyUnitState.Warning
                        : EnergyUnitState.Online;
                }
                case EnergyKind.Solar:
                case EnergyKind.Wind:
                    // Como o emissivo do jogo: em operação sem sol ou sem vento é atenção.
                    return stats.MaxOutputMw <= EnergySnapshot.EPSILON_MW ? EnergyUnitState.Warning : EnergyUnitState.Online;
                default:
                    return EnergyUnitState.Online;
            }
        }

        /// <summary>
        ///     Como MyTerminalBlock.IsCreativeModeEnabled em sobrevivência: bloco de NPC, construído por NPC, num grid gerado por
        ///     NPC segue a regra do criativo. ponytail: IsSurvivalModeForced não está na ModAPI e fica de fora.
        /// </summary>
        static bool FollowsCreativeRules(IMyTerminalBlock block)
        {
            return block.CubeGrid.IsNpcSpawnedGrid && IsNpc(block.OwnerId) && (block.SlimBlock.BuiltBy == block.OwnerId || IsNpc(block.SlimBlock.BuiltBy));
        }

        /// <summary>
        ///     Pela facção, como Sync.Players.IdentityIsNpc. ponytail: sem facção cai no teste por SteamId, que num cliente de
        ///     servidor dedicado também vale para jogador offline.
        /// </summary>
        static bool IsNpc(long identityId)
        {
            if (identityId == 0)
                return false;
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(identityId);
            return faction != null ? faction.IsEveryoneNpc() : MyAPIGateway.Players.TryGetSteamId(identityId) == 0;
        }
    }
}
