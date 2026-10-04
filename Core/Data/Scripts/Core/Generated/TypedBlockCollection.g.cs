// Mantido como fonte estática e recortado às coleções que as telas usam.
// Cada bloco adicionado passa por um cast por lista, sem a hierarquia completa de tipos do mod original.
namespace LcdMod.Client.GridData
{
    public sealed class TypedBlockCollection
    {
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::VRage.Game.ModAPI.IMyCubeBlock> All = new global::LcdMod.Common.Mvvm.ObservableList<global::VRage.Game.ModAPI.IMyCubeBlock>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyTerminalBlock> TerminalBlocks = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyTerminalBlock>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyBatteryBlock> BatteryBlocks = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyBatteryBlock>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyBeacon> Beacons = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyBeacon>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyGasTank> GasTanks = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyGasTank>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyJumpDrive> JumpDrives = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyJumpDrive>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyLaserAntenna> LaserAntennas = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyLaserAntenna>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyRadioAntenna> RadioAntennas = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyRadioAntenna>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyThrust> Thrusts = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyThrust>();
        public readonly global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyUserControllableGun> UserControllableGuns = new global::LcdMod.Common.Mvvm.ObservableList<global::Sandbox.ModAPI.IMyUserControllableGun>();

        public int Count => All.Count;

        public void Add(global::VRage.Game.ModAPI.IMyCubeBlock block)
        {
            All.Add(block);
            var terminalBlock = block as global::Sandbox.ModAPI.IMyTerminalBlock;
            if (terminalBlock == null)
                return;

            TerminalBlocks.Add(terminalBlock);
            var batteryBlocks = block as global::Sandbox.ModAPI.IMyBatteryBlock;
            if (batteryBlocks != null)
                BatteryBlocks.Add(batteryBlocks);
            var beacons = block as global::Sandbox.ModAPI.IMyBeacon;
            if (beacons != null)
                Beacons.Add(beacons);
            var gasTanks = block as global::Sandbox.ModAPI.IMyGasTank;
            if (gasTanks != null)
                GasTanks.Add(gasTanks);
            var jumpDrives = block as global::Sandbox.ModAPI.IMyJumpDrive;
            if (jumpDrives != null)
                JumpDrives.Add(jumpDrives);
            var laserAntennas = block as global::Sandbox.ModAPI.IMyLaserAntenna;
            if (laserAntennas != null)
                LaserAntennas.Add(laserAntennas);
            var radioAntennas = block as global::Sandbox.ModAPI.IMyRadioAntenna;
            if (radioAntennas != null)
                RadioAntennas.Add(radioAntennas);
            var thrusts = block as global::Sandbox.ModAPI.IMyThrust;
            if (thrusts != null)
                Thrusts.Add(thrusts);
            var userControllableGuns = block as global::Sandbox.ModAPI.IMyUserControllableGun;
            if (userControllableGuns != null)
                UserControllableGuns.Add(userControllableGuns);
        }

        public bool Remove(global::VRage.Game.ModAPI.IMyCubeBlock block)
        {
            if (!All.Remove(block))
                return false;

            var terminalBlock = block as global::Sandbox.ModAPI.IMyTerminalBlock;
            if (terminalBlock == null)
                return true;

            TerminalBlocks.Remove(terminalBlock);
            var batteryBlocks = block as global::Sandbox.ModAPI.IMyBatteryBlock;
            if (batteryBlocks != null)
                BatteryBlocks.Remove(batteryBlocks);
            var beacons = block as global::Sandbox.ModAPI.IMyBeacon;
            if (beacons != null)
                Beacons.Remove(beacons);
            var gasTanks = block as global::Sandbox.ModAPI.IMyGasTank;
            if (gasTanks != null)
                GasTanks.Remove(gasTanks);
            var jumpDrives = block as global::Sandbox.ModAPI.IMyJumpDrive;
            if (jumpDrives != null)
                JumpDrives.Remove(jumpDrives);
            var laserAntennas = block as global::Sandbox.ModAPI.IMyLaserAntenna;
            if (laserAntennas != null)
                LaserAntennas.Remove(laserAntennas);
            var radioAntennas = block as global::Sandbox.ModAPI.IMyRadioAntenna;
            if (radioAntennas != null)
                RadioAntennas.Remove(radioAntennas);
            var thrusts = block as global::Sandbox.ModAPI.IMyThrust;
            if (thrusts != null)
                Thrusts.Remove(thrusts);
            var userControllableGuns = block as global::Sandbox.ModAPI.IMyUserControllableGun;
            if (userControllableGuns != null)
                UserControllableGuns.Remove(userControllableGuns);
            return true;
        }

        public void Clear()
        {
            All.Clear();
            TerminalBlocks.Clear();
            BatteryBlocks.Clear();
            Beacons.Clear();
            GasTanks.Clear();
            JumpDrives.Clear();
            LaserAntennas.Clear();
            RadioAntennas.Clear();
            Thrusts.Clear();
            UserControllableGuns.Clear();
        }
    }
}
