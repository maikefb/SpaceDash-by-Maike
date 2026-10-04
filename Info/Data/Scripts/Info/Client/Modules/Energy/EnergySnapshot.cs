using Sandbox.Definitions;
using VRageMath;

namespace LcdMod.Client.Modules.Energy
{
    public enum EnergyKind
    {
        Battery = 0,
        Solar = 1,
        Wind = 2,
        Reactor = 3,
        Hydrogen = 4,
        Other = 5
    }

    public enum EnergyUnitState
    {
        NoUnits,

        /// <summary>Todas desligadas.</summary>
        Offline,

        /// <summary>Ligadas, mas nenhuma funcionando: bateria vazia, reator ou motor sem combustível, bloco danificado.</summary>
        Inoperative,

        Warning,
        Online
    }

    public enum EnergyBatteryStatus
    {
        None,
        Idle,
        Charging,
        Discharging,
        Full
    }

    public enum EnergyBatteryMode
    {
        None,
        Auto,
        Recharge,
        Discharge,
        Mixed
    }

    public sealed class EnergyKindStats
    {
        public int Total { get; internal set; }
        public int Enabled { get; internal set; }
        public int Working { get; internal set; }
        public float CurrentOutputMw { get; internal set; }
        public float MaxOutputMw { get; internal set; }
        public float DefinedOutputMw { get; internal set; }
        public float WorkingDefinedOutputMw { get; internal set; }
        public EnergyUnitState State { get; internal set; }
        public MyCubeBlockDefinition RepresentativeDefinition { get; internal set; }

        public float LoadRatio => MaxOutputMw > EnergySnapshot.EPSILON_MW ? MathHelper.Clamp(CurrentOutputMw / MaxOutputMw, 0f, 1f) : 0f;

        /// <summary>
        ///     Quanto do nominal das unidades em operação está disponível agora: exposição ao sol e ao vento. Passa de 1 quando o
        ///     clima ou a posição da turbina dão mais que o nominal.
        /// </summary>
        public float AvailabilityRatio => WorkingDefinedOutputMw > EnergySnapshot.EPSILON_MW ? MathHelper.Max(0f, MaxOutputMw / WorkingDefinedOutputMw) : 0f;
    }

    /// <summary>Uma instância mutável por serviço; <see cref="Version"/> muda a cada amostra.</summary>
    public sealed class EnergySnapshot
    {
        public const int HISTORY_LENGTH = 50;
        public const int KIND_COUNT = 6;
        public const float EPSILON_MW = 1e-6f;
        public const float NO_ETA = -1f;

        /// <summary>Limiares compartilhados entre a amostragem (estado) e o desenho (cor).</summary>
        public const float LOW_CHARGE_RATIO = 0.25f;
        public const float FULL_RATIO = 0.995f;
        public const float ALERT_LOAD_RATIO = 0.9f;
        public const float EMPTY_SOON_SECONDS = 600f;

        public static readonly EnergySnapshot Empty = new EnergySnapshot();

        public readonly EnergyKindStats[] Kinds = new EnergyKindStats[KIND_COUNT];
        public readonly float[] ProducedHistory = new float[HISTORY_LENGTH];
        public readonly float[] UsedHistory = new float[HISTORY_LENGTH];

        public EnergySnapshot()
        {
            for (int i = 0; i < KIND_COUNT; i++)
                Kinds[i] = new EnergyKindStats();

            EtaSeconds = NO_ETA;
        }

        public long Version { get; internal set; }
        public int UnitsTotal { get; internal set; }
        public float ProducedMw { get; internal set; }
        public float MaxProducedMw { get; internal set; }
        public float BatteryInputMw { get; internal set; }
        public float StoredMwh { get; internal set; }
        public float CapacityMwh { get; internal set; }
        public float ConsumedMw { get; internal set; }

        /// <summary>
        ///     Variação da energia armazenada, pela regra do jogo: o excedente da entrada sobre a saída de cada bateria entra
        ///     com a eficiência de recarga, o déficit sai sem perda. Positivo carregando, negativo cobrindo déficit, zero quando
        ///     o banco está cheio.
        /// </summary>
        public float NetMw { get; internal set; }

        /// <summary>
        ///     Consumo sobre a capacidade disponível agora (máximo dos geradores mais o das baterias). Produção acompanha o
        ///     consumo no jogo, então consumo sobre produção ficaria sempre perto de 100 %.
        /// </summary>
        public float UsedRatio { get; internal set; }

        public float StoredRatio { get; internal set; }
        public EnergyBatteryStatus BatteryStatus { get; internal set; }
        public EnergyBatteryMode BatteryMode { get; internal set; }

        /// <summary>Segundos até cheia ou vazia; <see cref="NO_ETA"/> sem estimativa.</summary>
        public float EtaSeconds { get; internal set; }

        /// <summary>Descarregando e vazia em menos de <see cref="EMPTY_SOON_SECONDS"/>.</summary>
        public bool BatteryEmptySoon { get; internal set; }

        /// <summary>Índice da amostra mais recente nos anéis de histórico, sempre cheios após a primeira amostra.</summary>
        public int HistoryHead { get; internal set; }

        public bool HasData => Version > 0L;
        public bool HasBatteries => Kinds[(int)EnergyKind.Battery].Total > 0;

        /// <summary>A primeira amostra preenche o anel inteiro, como no PowerGraph original.</summary>
        internal void Push(float produced, float used)
        {
            if (Version == 0L)
            {
                for (int i = 0; i < HISTORY_LENGTH; i++)
                {
                    ProducedHistory[i] = produced;
                    UsedHistory[i] = used;
                }

                HistoryHead = HISTORY_LENGTH - 1;
                return;
            }

            HistoryHead = (HistoryHead + 1) % HISTORY_LENGTH;
            ProducedHistory[HistoryHead] = produced;
            UsedHistory[HistoryHead] = used;
        }
    }
}
