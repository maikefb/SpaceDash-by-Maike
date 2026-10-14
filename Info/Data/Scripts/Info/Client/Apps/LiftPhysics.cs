using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace LcdMod.Client.Apps
{
    internal static class LiftPhysics
    {
        public const int DIR_FORWARD = 0;
        public const int DIR_BACKWARD = 1;
        public const int DIR_LEFT = 2;
        public const int DIR_RIGHT = 3;
        public const int DIR_UP = 4;
        public const int DIR_DOWN = 5;

        public static int DirIndex(Base6Directions.Direction dir)
        {
            switch (dir)
            {
                case Base6Directions.Direction.Forward: return DIR_FORWARD;
                case Base6Directions.Direction.Backward: return DIR_BACKWARD;
                case Base6Directions.Direction.Left: return DIR_LEFT;
                case Base6Directions.Direction.Right: return DIR_RIGHT;
                case Base6Directions.Direction.Up: return DIR_UP;
                case Base6Directions.Direction.Down: return DIR_DOWN;
                default: return -1;
            }
        }

        public static bool AccumulateMaxThrust(IList<IMyThrust> thrusts, double[] max6, double[] cur6)
        {
            bool any = false;
            for (int i = 0; i < thrusts.Count; i++)
            {
                var thr = thrusts[i];
                if (thr == null) continue;
                if (!thr.Enabled) continue;
                if (!thr.IsFunctional) continue;
                if (!thr.IsWorking) continue;

                int idx = DirIndex(Base6Directions.GetOppositeDirection(thr.Orientation.Forward));
                if (idx < 0) continue;

                double max = thr.MaxEffectiveThrust;
                max6[idx] += max;
                if (cur6 != null) cur6[idx] += thr.CurrentThrust;
                if (max > 0d) any = true;
            }

            return any;
        }

        public static IMyCockpit ResolveCockpit(IMyCubeBlock block)
        {
            var cockpit = block as IMyCockpit;
            if (cockpit != null) return cockpit;

            var myGrid = block?.CubeGrid as Sandbox.Game.Entities.MyCubeGrid;
            return myGrid?.MainCockpit as IMyCockpit;
        }

        public static bool TryGetNaturalGravity(IMyCubeBlock block, out Vector3D gravity)
        {
            gravity = Vector3D.Zero;

            var cockpit = ResolveCockpit(block);
            if (cockpit != null)
                gravity = cockpit.GetNaturalGravity();
            else if (block?.CubeGrid != null)
                gravity = block.CubeGrid.NaturalGravity;

            return gravity.LengthSquared() > 1e-6;
        }

        public static bool TryGetShipMass(IMyCubeBlock block, out double mass)
        {
            mass = 0d;

            var cockpit = ResolveCockpit(block);
            if (cockpit != null)
                mass = cockpit.CalculateShipMass().PhysicalMass;

            if (mass <= 0d)
            {
                var grid = block?.CubeGrid;
                if (grid != null && grid.Physics != null)
                    mass = grid.Physics.Mass;
            }

            return mass > 0d;
        }

        public static double AvailableForce(double[] max6, Vector3D worldDirection, MatrixD gridMatrix)
        {
            if (max6 == null || max6.Length < 6 || worldDirection.LengthSquared() <= 1e-8)
                return 0d;

            worldDirection.Normalize();

            double desiredForward = Vector3D.Dot(worldDirection, gridMatrix.Forward);
            double desiredRight = Vector3D.Dot(worldDirection, gridMatrix.Right);
            double desiredUp = Vector3D.Dot(worldDirection, gridMatrix.Up);

            double availableForwardAxis = AxisForce(max6, desiredForward >= 0d ? DIR_FORWARD : DIR_BACKWARD);
            double availableRightAxis = AxisForce(max6, desiredRight >= 0d ? DIR_RIGHT : DIR_LEFT);
            double availableUpAxis = AxisForce(max6, desiredUp >= 0d ? DIR_UP : DIR_DOWN);

            // Resultant force along a requested direction is limited by the weakest axis contribution.
            const double eps = 1e-6;
            double lambdaMax = double.PositiveInfinity;

            if (Math.Abs(desiredForward) > eps)
                lambdaMax = Math.Min(lambdaMax, availableForwardAxis / Math.Abs(desiredForward));
            if (Math.Abs(desiredRight) > eps)
                lambdaMax = Math.Min(lambdaMax, availableRightAxis / Math.Abs(desiredRight));
            if (Math.Abs(desiredUp) > eps)
                lambdaMax = Math.Min(lambdaMax, availableUpAxis / Math.Abs(desiredUp));

            return double.IsInfinity(lambdaMax) ? 0d : Math.Max(0d, lambdaMax);
        }

        static double AxisForce(double[] max6, int index)
        {
            if (max6 == null || index < 0 || index >= max6.Length)
                return 0d;

            return Math.Max(0d, max6[index]);
        }
    }
}
