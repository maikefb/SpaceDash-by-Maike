using System.Collections.Generic;
using System;
using System.Linq;
using Generated;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Client.ScreenAreas;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Terminal.Controls;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using LcdMod.Client.Utility;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

using LcdMod.Common.Config.Generation;
namespace LcdMod.Client.SurfaceScripts
{
    
    [MyTextSurfaceScript(ID, TITLE)]
    public partial class RenderProxySurfaceScript : SurfaceScriptBase,
        IReferenceBlockSelection
    {
        public const string ID = ID_PREFIX + "RenderProxy";
        public const string TITLE = MOD_PREFIX + "RenderProxy";

        List<MySprite> _sprites = new List<MySprite>();
        RenderProxyConfigComponent RenderProxyComponent => Config.GetComponent<RenderProxyConfigComponent>();
        BlockReferenceConfigComponent RenderProxyReferenceComponent =>
            Config.GetComponent<BlockReferenceConfigComponent>(RENDER_PROXY_REFERENCE);
        protected override bool ClipToBounds => true;

        protected override string DefaultTitle => TITLE;


        static readonly List<IMySlimBlock> AutoCascadeBlocks = new List<IMySlimBlock>();
        static readonly HashSet<long> ActiveRotationCascadeHosts = new HashSet<long>();

        static int CompareByDistance(ProxyAutoTarget a, ProxyAutoTarget b)
        {
            int distance = a.DistanceFromStart.CompareTo(b.DistanceFromStart);
            return distance != 0 ? distance : a.Block.EntityId.CompareTo(b.Block.EntityId);
        }

        SurfaceScriptBase _parent;
        SurfaceTssInstances _parentInstances;
        SurfaceScriptBase _registeredParent;
        SurfaceScriptBase _renderEventParent;
        long _registeredProxyKey;
        long _resolvedReferenceBlockId;
        int _resolvedHostRotationIndex = -1;
        float _lastOffsetX;
        float _lastOffsetY;
        bool _hostScriptUnsupported;
        bool _hostResolutionUnsupported;
        long _rebindBlockedUntilFrame = long.MinValue;
        long _lastObservedReferenceId;
        float _lastObservedOffsetX;
        float _lastObservedOffsetY;
        bool _hasObservedConfig;
        int _initialAutoAdjustAttempts;
        bool _wasActive;

        const int INITIAL_AUTO_ADJUST_MAX_ATTEMPTS = 10;

        public override IApp App
        {
            get
            {
                if (!IsParentAlive(_parent))
                    return null;

                try
                {
                    return _parent.App;
                }
                catch
                {
                    return null;
                }
            }
        }

        public RenderProxySurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block,
            size)
        {
        }

        public bool IsReferenceBlockCandidate(IMyTerminalBlock block) => IsBasicReferenceBlockCandidate(block);

        public bool TryGetReferenceBlockCandidates(List<IMyTerminalBlock> blocks) => false;

        bool IsBasicReferenceBlockCandidate(IMyTerminalBlock block)
        {
            if (!(block is IMyTextPanel) || block.MarkedForClose || block.Equals(Block))
                return false;

            return !ConfigManager.GetAppsForBlock(block).Any(a => a is RenderProxySurfaceScript);
        }

        bool HasMatchingResolution(IMyTerminalBlock block)
        {
            var panel = block as IMyTextPanel;
            if (panel == null)
                return false;

            return SameResolution(panel.TextureSize, Surface.TextureSize) &&
                   SameResolution(panel.SurfaceSize, Surface.SurfaceSize);
        }

        bool HasMatchingResolution(SurfaceScriptBase parent)
        {
            return parent != null &&
                   SameResolution(parent.TextureSize, Surface.TextureSize) &&
                   SameResolution(parent.SurfaceSize, Surface.SurfaceSize);
        }

        static bool SameResolution(Vector2 a, Vector2 b)
        {
            return Math.Abs(a.X - b.X) < 0.5f &&
                   Math.Abs(a.Y - b.Y) < 0.5f;
        }

        public bool CanApplyProxyAutoOffset()
        {
            if (Config == null)
                return false;

            sbyte x;
            sbyte y;
            ProxyAutoContext context;
            return TryCalculateAutoOffset(out x, out y, out context);
        }

        public void ApplyProxyAutoOffset()
        {
            TryApplyProxyAutoOffset(true);
        }

        bool TryApplyProxyAutoOffset(bool cascade)
        {
            sbyte x;
            sbyte y;
            ProxyAutoContext context;
            if (!TryCalculateAutoOffset(out x, out y, out context))
                return false;

            if (RenderProxyComponent.XAxisOffset != x || RenderProxyComponent.YAxisOffset != y)
            {
                RenderProxyComponent.XAxisOffset = x;
                RenderProxyComponent.YAxisOffset = y;

                var terminalBlock = Block as IMyTerminalBlock;
                if (terminalBlock != null)
                    ConfigManager.Sync(terminalBlock);

                if (MyAPIGateway.Session != null)
                    _rebindBlockedUntilFrame = MyAPIGateway.Session.GameplayFrameCounter + 1;

                RenderSprites();
            }

            if (cascade)
                StartProxyAutoCascade(context, x, y);

            return true;
        }

        void ResetSiblingsActiveFlag()
        {
            var instances = Instances.GetInstances(Block as IMyTerminalBlock);
            if (instances == null)
                return;

            foreach (var sibling in instances.GetInstances())
            {
                var proxy = sibling as RenderProxySurfaceScript;
                if (proxy != null && proxy != this)
                    proxy._wasActive = false;
            }
        }

        void HandleBecameActiveInstance()
        {
            _initialAutoAdjustAttempts = 0;
            ScheduleInitialAutoAdjust();
        }

        void ScheduleInitialAutoAdjust()
        {
            if (!RenderProxyComponent.EnableAutoAdjust)
                return;

            if (_initialAutoAdjustAttempts >= INITIAL_AUTO_ADJUST_MAX_ATTEMPTS)
                return;

            _initialAutoAdjustAttempts++;
            LcdModClientComponent.RunNextFrame.Add(TryInitialAutoAdjust);
        }

        void TryInitialAutoAdjust()
        {
            if (Block == null || Block.MarkedForClose)
                return;

            if (!RenderProxyComponent.EnableAutoAdjust)
                return;

            if (!TryApplyProxyAutoOffset(false))
                ScheduleInitialAutoAdjust();
        }

        bool TryCalculateAutoOffset(out sbyte x, out sbyte y, out ProxyAutoContext context)
        {
            x = 0;
            y = 0;
            return TryGetAutoContext(out context) &&
                   TryCalculateAutoOffsetFor(Block as IMyTextPanel, context, out x, out y);
        }

        bool TryGetAutoContext(out ProxyAutoContext context)
        {
            context = null;

            IMyTextPanel hostBlock;
            int hostRotationIndex;
            SurfaceScriptBase host;
            if (!TryGetConfiguredHost(out hostBlock, out hostRotationIndex, out host))
                return false;

            if (hostBlock.BlockDefinition.SubtypeName != Block.BlockDefinition.SubtypeName)
                return false;

            if (Block.CubeGrid == null || hostBlock.CubeGrid == null || Block.CubeGrid != hostBlock.CubeGrid)
                return false;

            if (!TryCreateProxyAutoContext(hostBlock, hostRotationIndex, out context))
                return false;

            context.TextureSize = Surface.TextureSize;
            return true;
        }

        bool TryCalculateAutoOffsetFor(IMyTextPanel panel, ProxyAutoContext context, out sbyte x, out sbyte y)
        {
            x = 0;
            y = 0;

            if (!IsAutoCascadeCandidatePanel(panel, context))
                return false;

            ScreenFrame panelFrame;
            return TryGetScreenGridFrame(panel, context.HostRotationIndex, out panelFrame) &&
                   TryCalculateOffsetFromPanelFrame(panelFrame, context, out x, out y);
        }

        static bool TryCalculateOffsetFromPanelFrame(
            ScreenFrame panel,
            ProxyAutoContext context,
            out sbyte x,
            out sbyte y)
        {
            x = 0;
            y = 0;

            if (context == null)
                return false;

            var host = context.Frame;
            if (panel.Right != host.Right || panel.Up != host.Up || panel.Forward != host.Forward)
                return false;

            if (host.StepX <= 0.05d || host.StepY <= 0.05d || panel.StepX <= 0.05d || panel.StepY <= 0.05d)
                return false;

            if (Math.Abs(panel.StepX - host.StepX) > 0.05d || Math.Abs(panel.StepY - host.StepY) > 0.05d)
                return false;

            var delta = panel.Center - host.Center;
            double planeOffset = Dot(delta, host.Forward);
            if (Math.Abs(planeOffset) > 0.05d)
                return false;

            double xScreens = Dot(delta, host.Right) / host.StepX;
            double yScreens = Dot(delta, -host.Up) / host.StepY;

            int xInt;
            int yInt;
            if (!TryRoundCellOffset(xScreens, out xInt) ||
                !TryRoundCellOffset(yScreens, out yInt))
                return false;

            if (xInt < sbyte.MinValue || xInt > sbyte.MaxValue ||
                yInt < sbyte.MinValue || yInt > sbyte.MaxValue)
                return false;

            x = (sbyte)xInt;
            y = (sbyte)yInt;
            return true;
        }

        void StartProxyAutoCascade(ProxyAutoContext context, sbyte startX, sbyte startY)
        {
            if (context == null || context.Grid == null)
                return;

            var targets = CollectProxyAutoCascadeTargets(context, startX, startY);
            if (targets.Count == 0)
                return;

            ScheduleTargets(targets, 0, ApplyProxyAutoCascadeTarget, null);
        }

        List<ProxyAutoTarget> CollectProxyAutoCascadeTargets(ProxyAutoContext context, int startX, int startY)
        {
            var targets = new List<ProxyAutoTarget>();
            AutoCascadeBlocks.Clear();
            context.Grid.GetBlocks(AutoCascadeBlocks);

            int minX = Math.Min(0, startX);
            int maxX = Math.Max(0, startX);
            int minY = Math.Min(0, startY);
            int maxY = Math.Max(0, startY);

            for (int i = 0; i < AutoCascadeBlocks.Count; i++)
            {
                var panel = AutoCascadeBlocks[i].FatBlock as IMyTextPanel;
                if (panel == null ||
                    panel.EntityId == Block.EntityId ||
                    panel.EntityId == context.HostId ||
                    !IsAutoCascadeEditable(panel))
                {
                    continue;
                }

                sbyte x;
                sbyte y;
                if (!TryCalculateAutoOffsetFor(panel, context, out x, out y))
                    continue;

                if (x == 0 && y == 0)
                    continue;

                if (x < minX || x > maxX || y < minY || y > maxY)
                    continue;

                targets.Add(new ProxyAutoTarget
                {
                    Block = panel,
                    X = x,
                    Y = y,
                    HostId = context.HostId,
                    DistanceFromStart = Math.Abs(x - startX) + Math.Abs(y - startY),
                    RotationIndex = context.HostRotationIndex
                });
            }

            AutoCascadeBlocks.Clear();
            targets.Sort(CompareByDistance);
            return targets;
        }

        static void ScheduleTargets(List<ProxyAutoTarget> targets, int index, Action<ProxyAutoTarget> apply, Action done)
        {
            if (index >= targets.Count)
            {
                if (done != null)
                    done();
                return;
            }

            LcdModClientComponent.RunNextFrame.Add(() =>
            {
                apply(targets[index]);
                ScheduleTargets(targets, index + 1, apply, done);
            });
        }

        static void ApplyProxyAutoCascadeTarget(ProxyAutoTarget target)
        {
            if (target == null || !IsAutoCascadeEditable(target.Block))
                return;

            ConfigureProxyPanel(target.Block, target.X, target.Y, target.HostId, target.RotationIndex);
        }

        static void ConfigureProxyPanel(IMyTextPanel panel, sbyte x, sbyte y, long hostId, int hostRotationIndex)
        {
            if (panel == null)
                return;

            var providerConfig = ConfigManager.GetConfigForBlock(panel) ??
                                 ConfigManager.TryLoad(panel) ??
                                 ConfigManager.CreateSettings(panel);
            if (!providerConfig.CanWrite)
                return;

            providerConfig.EnsureSurfaceApp(0, AppType.RenderProxy);
            var surface = providerConfig.GetSurfaceConfig(0);
            if (!providerConfig.CanWriteConfig(surface))
                return;
            var reference = surface?.TryGet<BlockReferenceConfigComponent>(RENDER_PROXY_REFERENCE);
            var proxyConfig = surface?.TryGetComponent<RenderProxyConfigComponent>();
            if (reference == null || proxyConfig == null)
                return;

            reference.EntityId = hostId;
            proxyConfig.XAxisOffset = x;
            proxyConfig.YAxisOffset = y;

            ConfigManager.Sync(panel, providerConfig);

            bool rotationChanged = SetSelectedTextPanelRotationIndex(panel, hostRotationIndex);
            EnsureProxyScript(panel, rotationChanged);
        }

        static bool IsAutoCascadeEditable(IMyTextPanel panel)
        {
            if (panel == null || panel.MarkedForClose)
                return false;

            if (panel.ContentType == ContentType.NONE)
                return true;

            var script = panel.Script;
            return string.IsNullOrWhiteSpace(script) ||
                   string.Equals(script, "none", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(script, ID, StringComparison.Ordinal);
        }

        static bool IsAutoCascadeCandidatePanel(IMyTextPanel panel, ProxyAutoContext context)
        {
            return panel != null &&
                   context != null &&
                   !panel.MarkedForClose &&
                   panel.CubeGrid == context.Grid &&
                   panel.BlockDefinition.SubtypeName == context.ScreenSubtype &&
                   SameResolution(panel.TextureSize, context.TextureSize);
        }

        static void ScheduleLinkedProxyRotationUpdate(IMyTerminalBlock hostBlock, int hostRotationIndex)
        {
            var hostPanel = hostBlock as IMyTextPanel;
            if (hostPanel == null || hostPanel.CubeGrid == null)
                return;

            ProxyAutoContext context;
            if (!TryCreateProxyAutoContext(hostPanel, hostRotationIndex, out context))
                return;

            long scheduleKey = GetRotationKey(hostPanel.EntityId, hostRotationIndex);
            if (ActiveRotationCascadeHosts.Contains(scheduleKey))
                return;

            var targets = CollectLinkedProxyRotationTargets(context);
            if (targets.Count == 0)
                return;

            ActiveRotationCascadeHosts.Add(scheduleKey);
            ScheduleTargets(targets, 0, ApplyLinkedProxyRotationTarget,
                () => ActiveRotationCascadeHosts.Remove(scheduleKey));
        }

        static bool TryCreateProxyAutoContext(IMyTextPanel hostPanel, int hostRotationIndex,
            out ProxyAutoContext context)
        {
            context = null;
            if (hostPanel == null)
                return false;

            ScreenFrame hostFrame;
            if (!TryGetScreenGridFrame(hostPanel, hostRotationIndex, out hostFrame))
                return false;

            context = new ProxyAutoContext
            {
                HostId = hostPanel.EntityId,
                HostRotationIndex = hostRotationIndex,
                Grid = hostPanel.CubeGrid,
                ScreenSubtype = hostPanel.BlockDefinition.SubtypeName,
                TextureSize = hostPanel.TextureSize,
                Frame = hostFrame
            };
            return true;
        }

        static List<ProxyAutoTarget> CollectLinkedProxyRotationTargets(ProxyAutoContext context)
        {
            var targets = new List<ProxyAutoTarget>();
            if (context == null || context.Grid == null)
                return targets;

            AutoCascadeBlocks.Clear();
            context.Grid.GetBlocks(AutoCascadeBlocks);

            for (int i = 0; i < AutoCascadeBlocks.Count; i++)
            {
                var panel = AutoCascadeBlocks[i].FatBlock as IMyTextPanel;
                if (panel == null ||
                    panel.EntityId == context.HostId)
                {
                    continue;
                }

                ScreenProviderConfig providerConfig;
                RenderProxyConfigComponent proxyConfig;
                BlockReferenceConfigComponent proxyReference;
                if (!TryGetLinkedProxyConfig(panel, out providerConfig, out proxyConfig, out proxyReference) ||
                    proxyReference.EntityId != context.HostId)
                {
                    continue;
                }

                if (!proxyConfig.EnableAutoAdjust)
                    continue;

                if (SurfaceCollection.GetActiveRotationIndex(panel) == context.HostRotationIndex)
                    continue;

                targets.Add(new ProxyAutoTarget
                {
                    Block = panel,
                    X = proxyConfig.XAxisOffset,
                    Y = proxyConfig.YAxisOffset,
                    HostId = context.HostId,
                    DistanceFromStart = Math.Abs(proxyConfig.XAxisOffset) + Math.Abs(proxyConfig.YAxisOffset),
                    RotationIndex = context.HostRotationIndex
                });
            }

            AutoCascadeBlocks.Clear();
            targets.Sort(CompareByDistance);
            return targets;
        }

        static bool TryGetLinkedProxyConfig(
            IMyTextPanel panel,
            out ScreenProviderConfig providerConfig,
            out RenderProxyConfigComponent proxyConfig,
            out BlockReferenceConfigComponent proxyReference)
        {
            providerConfig = null;
            proxyConfig = null;
            proxyReference = null;

            if (panel == null)
                return false;

            providerConfig = ConfigManager.GetConfigForBlock(panel) ?? ConfigManager.TryLoad(panel);
            if (providerConfig == null)
                return false;

            var surface = providerConfig.GetSurfaceConfig(0);
            if (surface == null || surface.AppTypeId != (int)AppType.RenderProxy)
                return false;

            proxyConfig = surface.TryGetComponent<RenderProxyConfigComponent>();
            proxyReference = surface.TryGet<BlockReferenceConfigComponent>(RENDER_PROXY_REFERENCE);
            return proxyConfig != null && proxyReference != null;
        }

        static void ApplyLinkedProxyRotationTarget(ProxyAutoTarget target)
        {
            if (target == null || target.Block == null || target.Block.MarkedForClose)
                return;

            ScreenProviderConfig providerConfig;
            RenderProxyConfigComponent proxyConfig;
            BlockReferenceConfigComponent proxyReference;
            if (!TryGetLinkedProxyConfig(target.Block, out providerConfig, out proxyConfig, out proxyReference) ||
                proxyReference.EntityId != target.HostId)
            {
                return;
            }

            if (!proxyConfig.EnableAutoAdjust)
                return;

            bool rotationChanged = SetSelectedTextPanelRotationIndex(target.Block, target.RotationIndex);
            EnsureProxyScript(target.Block, rotationChanged);
        }

        static void EnsureProxyScript(IMyTextPanel panel, bool defer)
        {
            if (panel == null)
                return;

            if (defer)
            {
                LcdModClientComponent.RunNextFrame.Add(() => EnsureProxyScript(panel, false));
                return;
            }

            if (panel.MarkedForClose)
                return;

            if (panel.ContentType != ContentType.SCRIPT)
                panel.ContentType = ContentType.SCRIPT;

            if (!string.Equals(panel.Script, ID, StringComparison.Ordinal))
                panel.Script = ID;
        }

        bool TryGetConfiguredHost(
            out IMyTextPanel hostBlock,
            out int hostRotationIndex,
            out SurfaceScriptBase host)
        {
            hostRotationIndex = -1;
            host = null;

            hostBlock = GetReferenceBlock();
            if (hostBlock == null)
                return false;

            hostRotationIndex = SurfaceCollection.GetActiveRotationIndex(hostBlock);
            host = GetActiveParentInstance(Instances.GetInstances(hostBlock), hostRotationIndex);
            return host != null;
        }

        IMyTextPanel GetReferenceBlock()
        {
            long referenceBlockId = RenderProxyReferenceComponent.EntityId;
            IMyEntity entity;
            if (referenceBlockId == 0L || !MyAPIGateway.Entities.TryGetEntityById(referenceBlockId, out entity))
                return null;

            var block = entity as IMyTextPanel;
            if (block == null || block.MarkedForClose || !IsBasicReferenceBlockCandidate(block))
                return null;

            return block;
        }

        static bool TryGetScreenGridFrame(IMyCubeBlock block, int surfaceIndex, out ScreenFrame frame)
        {
            frame = new ScreenFrame { StepX = 1d, StepY = 1d };

            if (block == null || block.CubeGrid == null)
                return false;

            MatrixD screenWorld;
            if (!ScreenAreaGeometry.TryGetScreenWorldMatrix(block, surfaceIndex, out screenWorld))
                return false;

            var inverseGrid = MatrixD.Invert(block.CubeGrid.WorldMatrix);
            frame.Center = Vector3D.Transform(screenWorld.Translation, inverseGrid) / block.CubeGrid.GridSize;

            if (!TryGetGridAxis(Vector3D.TransformNormal(screenWorld.Right, inverseGrid), out frame.Right) ||
                !TryGetGridAxis(Vector3D.TransformNormal(screenWorld.Up, inverseGrid), out frame.Up) ||
                !TryGetGridAxis(Vector3D.TransformNormal(screenWorld.Forward, inverseGrid), out frame.Forward))
            {
                return false;
            }

            frame.StepX = GetBlockSpanInGridCells(block, frame.Right);
            frame.StepY = GetBlockSpanInGridCells(block, frame.Up);

            return frame.StepX > 0.05d && frame.StepY > 0.05d;
        }

        static double GetBlockSpanInGridCells(IMyCubeBlock block, Vector3I axis)
        {
            var slim = block?.SlimBlock;
            if (slim == null)
                return 1d;

            return Math.Abs(Dot(new Vector3D(slim.Max - slim.Min), axis)) + 1d;
        }

        static bool TryGetGridAxis(Vector3D vector, out Vector3I axis)
        {
            axis = Vector3I.Zero;
            if (vector.LengthSquared() <= 1e-12d)
                return false;

            vector.Normalize();
            axis = Base6Directions.GetIntVector(Base6Directions.GetClosestDirection((Vector3)vector));
            return Math.Abs(Dot(vector, axis)) > 0.999d;
        }

        static bool SetSelectedTextPanelRotationIndex(IMyTextPanel panel, int rotationIndex)
        {
            if (panel == null)
                return false;

            var property = panel.GetProperty("Rotate") as ITerminalProperty<float>;
            if (property == null)
                return false;

            rotationIndex = ((rotationIndex % 4) + 4) % 4;
            float degrees = rotationIndex * 90f;
            if (NormalizeRotationIndex(property.GetValue(panel)) == rotationIndex)
                return false;

            property.SetValue(panel, degrees);
            return true;
        }

        static int NormalizeRotationIndex(float degrees)
        {
            int index = (int)Math.Round(degrees / 90f) % 4;
            return index < 0 ? index + 4 : index;
        }

        static bool TryRoundCellOffset(double value, out int rounded)
        {
            rounded = (int)Math.Round(value);
            return Math.Abs(value - rounded) <= 0.05d;
        }

        static double Dot(Vector3D vector, Vector3I axis)
        {
            return vector.X * axis.X + vector.Y * axis.Y + vector.Z * axis.Z;
        }

        sealed class ProxyAutoContext
        {
            public long HostId;
            public int HostRotationIndex;
            public IMyCubeGrid Grid;
            public string ScreenSubtype;
            public Vector2 TextureSize;
            public ScreenFrame Frame;
        }

        struct ScreenFrame
        {
            public Vector3I Right;
            public Vector3I Up;
            public Vector3I Forward;
            public Vector3D Center;
            public double StepX;
            public double StepY;
        }

        sealed class ProxyAutoTarget
        {
            public IMyTextPanel Block;
            public sbyte X;
            public sbyte Y;
            public long HostId;
            public int DistanceFromStart;
            public int RotationIndex;
        }

        bool IsParentAlive(SurfaceScriptBase parent)
        {
            return parent?.Block != null && !parent.Block.MarkedForClose;
        }

        static SurfaceScriptBase GetActiveParentInstance(SurfaceTssInstances parentInstances, int hostRotationIndex)
        {
            if (parentInstances == null)
                return null;

            var parent = parentInstances.GetInstance(hostRotationIndex);
            if (parent is RenderProxySurfaceScript)
                return null;

            return parent;
        }

        bool ShouldRefreshLinkedProxyMapping(IMyTerminalBlock hostBlock, int hostRotationIndex)
        {
            var panel = Block as IMyTextPanel;
            var hostPanel = hostBlock as IMyTextPanel;
            if (panel == null || hostPanel == null)
                return false;

            return RenderProxyComponent.EnableAutoAdjust && SurfaceCollection.GetActiveRotationIndex(panel) != hostRotationIndex;
        }

        long GetProxyRegistrationId() => GetRotationKey(Block.EntityId, RotationOrSurfaceIndex);

        static long GetRotationKey(long entityId, int rotationIndex)
        {
            unchecked
            {
                return (entityId * 397L) ^ rotationIndex;
            }
        }

        Vector2 GetProxyPixelOffset(float xOffset, float yOffset)
        {
            // Offsets are visible-screen pages: cropped LCDs (e.g. Sci-Fi LCD Panel 5x3) center the visible area inside TextureSize.
            return GetVisibleScreenSize() * new Vector2(xOffset, yOffset);
        }

        Vector2 GetVisibleScreenSize()
        {
            var textureSize = Surface.TextureSize;
            var screenSize = Surface.SurfaceSize;
            return Vector2.Min(screenSize.X > 0f && screenSize.Y > 0f ? screenSize : textureSize, textureSize);
        }

        void SyncProxyRegistration()
        {
            if (_registeredParent != null && _registeredParent != _parent)
            {
                _registeredParent.UnregisterProxy(_registeredProxyKey);
                _registeredParent = null;
                _registeredProxyKey = 0L;
                UnsubscribeFromParentRender();
            }

            if (!IsParentAlive(_parent))
            {
                UnsubscribeFromParentRender();
                return;
            }

            float x = RenderProxyComponent.XAxisOffset;
            float y = RenderProxyComponent.YAxisOffset;
            long proxyKey = GetProxyRegistrationId();

            if (_registeredParent == _parent &&
                _registeredProxyKey == proxyKey &&
                _lastOffsetX.Equals(x) &&
                _lastOffsetY.Equals(y))
            {
                SubscribeToParentRender(_parent);
                return;
            }

            if (_registeredParent == _parent && _registeredProxyKey != 0L && _registeredProxyKey != proxyKey)
                _registeredParent?.UnregisterProxy(_registeredProxyKey);

            _parent.RegisterProxy(proxyKey, GetProxyPixelOffset(x, y));
            _registeredParent = _parent;
            _registeredProxyKey = proxyKey;
            _lastOffsetX = x;
            _lastOffsetY = y;
            SubscribeToParentRender(_parent);
        }

        void SubscribeToParentRender(SurfaceScriptBase parent)
        {
            if (_renderEventParent == parent)
                return;

            UnsubscribeFromParentRender();

            _renderEventParent = parent;
            if (_renderEventParent != null)
                _renderEventParent.OnRender += HandleParentRender;
        }

        void UnsubscribeFromParentRender()
        {
            if (_renderEventParent == null)
                return;

            _renderEventParent.OnRender -= HandleParentRender;
            _renderEventParent = null;
        }

        void HandleParentRender(SurfaceScriptBase parent)
        {
            if (parent == null ||
                parent != _registeredParent ||
                !ReferenceEquals(parent, GetActiveParentInstance(_parentInstances, _resolvedHostRotationIndex)))
            {
                return;
            }

            RenderSprites(force: true);
        }

        void ResolveParentFromConfig()
        {
            if (MyAPIGateway.Session != null &&
                MyAPIGateway.Session.GameplayFrameCounter < _rebindBlockedUntilFrame)
            {
                _parent = null;
                return;
            }

            long referenceBlockId = RenderProxyReferenceComponent.EntityId;
            var targetBlock = GetReferenceBlock();
            if (targetBlock == null)
            {
                ClearParent(referenceBlockId);
                return;
            }

            int hostRotationIndex = SurfaceCollection.GetActiveRotationIndex(targetBlock);
            if (ShouldRefreshLinkedProxyMapping(targetBlock, hostRotationIndex))
                ScheduleLinkedProxyRotationUpdate(targetBlock, hostRotationIndex);

            if (_parentInstances == null || _resolvedReferenceBlockId != referenceBlockId)
                _parentInstances = Instances.GetInstances(targetBlock);

            if (_resolvedReferenceBlockId == referenceBlockId &&
                _resolvedHostRotationIndex == hostRotationIndex &&
                _parent != null &&
                ReferenceEquals(_parent, GetActiveParentInstance(_parentInstances, hostRotationIndex)))
            {
                if (!HasMatchingResolution(_parent))
                {
                    _parent = null;
                    _hostScriptUnsupported = false;
                    _hostResolutionUnsupported = true;
                    return;
                }

                _hostScriptUnsupported = false;
                _hostResolutionUnsupported = false;
                return;
            }

            _parent = null;
            _resolvedReferenceBlockId = referenceBlockId;
            _resolvedHostRotationIndex = hostRotationIndex;
            _parentInstances = Instances.GetInstances(targetBlock);
            _hostScriptUnsupported = false;
            _hostResolutionUnsupported = false;

            if (!HasMatchingResolution(targetBlock))
            {
                _hostResolutionUnsupported = true;
                return;
            }

            // Text panels keep one resident script per rotation; proxy to the host's
            // active rotation slot so proxy panels can use independent rotations.
            _parent = GetActiveParentInstance(_parentInstances, hostRotationIndex);

            if (_parent != null)
            {
                _hostScriptUnsupported = false;
                return;
            }

            // Only mark unsupported when we can positively classify the host as unsupported.
            // During script swap/layout transitions there may be no live script for a frame.
            bool hasAnyNonProxyScript = ConfigManager.GetAppsForBlock(targetBlock).Any(a => !(a is RenderProxySurfaceScript));
            _hostScriptUnsupported = !hasAnyNonProxyScript;
        }

        void ClearParent(long resolvedReferenceBlockId)
        {
            _parent = null;
            _parentInstances = null;
            _resolvedReferenceBlockId = resolvedReferenceBlockId;
            _resolvedHostRotationIndex = -1;
            _hostScriptUnsupported = false;
            _hostResolutionUnsupported = false;
        }

        void UpdateRebindCooldown()
        {
            long referenceId = RenderProxyReferenceComponent.EntityId;
            float x = RenderProxyComponent.XAxisOffset;
            float y = RenderProxyComponent.YAxisOffset;

            bool changed = !_hasObservedConfig ||
                           _lastObservedReferenceId != referenceId ||
                           !_lastObservedOffsetX.Equals(x) ||
                           !_lastObservedOffsetY.Equals(y);
            if (!changed || MyAPIGateway.Session == null)
                return;

            _lastObservedReferenceId = referenceId;
            _lastObservedOffsetX = x;
            _lastObservedOffsetY = y;
            _hasObservedConfig = true;
            _rebindBlockedUntilFrame = MyAPIGateway.Session.GameplayFrameCounter + 1;
        }

        public override void SafeRun()
        {
            bool active = ReferenceEquals(Instances.GetActiveInstance(Block as IMyTerminalBlock), this);
            if (active && !_wasActive)
            {
                ResetSiblingsActiveFlag();
                HandleBecameActiveInstance();
            }
            _wasActive = active;

            UpdateRebindCooldown();
            ResolveParentFromConfig();
            SyncProxyRegistration();

            if (IsParentAlive(_registeredParent) && _registeredParent == _parent)
                return;

            RenderSprites();
        }

        public override List<MySprite> GetSprites()
        {
            _sprites.Clear();

            bool proxyingParent = _parent != null;
            if (!proxyingParent)
            {
                AddBackground(_sprites);
                DrawTitle(_sprites);
            }

            if (!(Block is IMyTextPanel))
                return DrawError("RenderProxy_UnsupportedScreen");

            if (_parent == null)
            {
                if (_hostResolutionUnsupported)
                    return DrawWarning("RenderProxy_UnsupportedResolution");

                return _hostScriptUnsupported
                    ? DrawError("RenderProxy_UnsupportedScript")
                    : DrawWarning("RenderProxy_NotLinked");
            }

            if (!IsParentAlive(_parent))
                return DrawError("RenderProxy_UnsupportedScript");

            var offset = GetProxyPixelOffset(RenderProxyComponent.XAxisOffset, RenderProxyComponent.YAxisOffset);
            List<MySprite> parentFrame;
            try
            {
                parentFrame = _parent.GetCachedFrame();
            }
            catch
            {
                return DrawError("RenderProxy_UnsupportedScript");
            }

            foreach (var sprite in parentFrame)
            {
                var remapped = sprite;
                remapped.Position -= offset;
                _sprites.Add(remapped);
            }

            return _sprites;
        }

        List<MySprite> DrawError(string key)
        {
            DrawMessage(_sprites, LocHelper.GetLoc(MOD_PREFIX + key), "Cross", ColorComponent.ResolveErrorColor(), 0.9f);
            return _sprites;
        }

        List<MySprite> DrawWarning(string key)
        {
            DrawMessage(_sprites, LocHelper.GetLoc(MOD_PREFIX + key), "Warning", ColorComponent.ResolveWarningColor(), 0.9f);
            return _sprites;
        }

        protected override List<MySprite> RenderFrame(Func<List<MySprite>> sprites)
        {
            return sprites();
        }

        public override void Dispose()
        {
            UnsubscribeFromParentRender();

            if (_registeredParent != null)
            {
                _registeredParent.UnregisterProxy(_registeredProxyKey);
                _registeredParent = null;
                _registeredProxyKey = 0L;
            }

            _parent = null;
            base.Dispose();
        }
    }
}
