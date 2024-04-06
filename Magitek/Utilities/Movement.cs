using Buddy.Coroutines;
using Clio.Common;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Interfaces;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.Pathing.Avoidance;
using Magitek.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseSettings = Magitek.Models.Account.BaseSettings;

namespace Magitek.Utilities
{
    internal static class Movement
    {
        public static IPlayerMover PlayerMover { get; set; }

        public static DateTime lastThrottleStopTime = DateTime.MinValue;
        public static TimeSpan throttleStop = TimeSpan.FromMilliseconds(BaseSettings.Instance.StopThrottle);

        public static DateTime lastThrottleMoveTime = DateTime.MinValue;
        public static TimeSpan throttleMove = TimeSpan.FromMilliseconds(BaseSettings.Instance.MoveThrottle);

        public static DateTime lastThrottleFaceTime = DateTime.MinValue;
        public static TimeSpan throttleFace = TimeSpan.FromMilliseconds(BaseSettings.Instance.FaceThrottle);

        public static void NavigateToUnitLos(GameObject unit, float distance)
        {
            
            if (!BaseSettings.Instance.MagitekMovement)
                return;

            if (RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement))
                return;
                        
            if (unit == null)
                return;

            if (AvoidanceManager.IsRunningOutOfAvoid)
                return;

            NavigationProvider navigationProvider = new AStarNavigator();

            if (navigationProvider != null)
            {
                navigationProvider.ClearStuckInfo();
            }

            if (Core.Me.Distance(unit.Location) <= distance)
            {
                if (!unit.InLineOfSight()
                    && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing))
                {
                    if (DateTime.Now - lastThrottleFaceTime >= throttleFace)
                    {
                        Logger.WriteInfo($@"Trying to Face Target. Throttle: " + BaseSettings.Instance.FaceThrottle + "ms.");
                        Core.Me.Face(unit);
                        lastThrottleFaceTime = DateTime.Now;
                    }
                }

                if (unit.InLineOfSight() && MovementManager.IsMoving)
                {

                    if (DateTime.Now - lastThrottleStopTime >= throttleStop)
                    {
                        Logger.WriteInfo($@"Stopping movement since we are in range and LoS of our target. Throttle: " + BaseSettings.Instance.StopThrottle + "ms.");
                        Navigator.Clear();
                        navigationProvider.ClearStuckInfo();
                        Navigator.PlayerMover.MoveStop();
                        lastThrottleStopTime = DateTime.Now;
                        return;
                    }

                    
                }

            }
            else
            {
                if (AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(unit.Location)))
                {
                    if (DateTime.Now - lastThrottleStopTime >= throttleStop)
                    {
                        Logger.WriteInfo($@"Stopping movement since our destination is in an avoid. Throttle: " + BaseSettings.Instance.StopThrottle + "ms.");
                        Navigator.Clear();
                        navigationProvider.ClearStuckInfo();
                        Navigator.PlayerMover.MoveStop();
                        lastThrottleStopTime = DateTime.Now;
                    }
                    return;
                }

                else if (DateTime.Now - lastThrottleMoveTime >= throttleMove)
                {
                    Logger.WriteInfo($@"NavigateToUnitLos -> " + unit.X + "," + unit.Y + "," + unit.Z + ".  Throttle: " + BaseSettings.Instance.MoveThrottle + "ms.");
                    //Navigator.PlayerMover.MoveTowards(unit.Location);
                    Navigator.MoveTo(new MoveToParameters(unit.Location));
                    lastThrottleMoveTime = DateTime.Now;
                    return;
                }
            }
        }

        public static async Task<bool> Dismount()
        {
            if (!Core.Me.IsMounted)
                return false;

            while (Core.Me.IsMounted)
            {
                ActionManager.Mount();
                await Coroutine.Yield();
            }

            return true;
        }

        /* public static (Vector3, Vector3) FindPositionToCastAoE(Vector3 playerPosition, IEnumerable<Vector3> enemiesPositions, int minTargets, float coneAngleDegrees, double spellRange)
        {
            Vector3 bestPosition;
            Vector3 bestEnemy;
            int maxTargets = 0;
            float coneAngle =  MathEx.ToRadians(coneAngleDegrees);

            foreach (Vector3 enemy in enemiesPositions)
            {
                Vector3 directionToEnemy = enemy - playerPosition;
                for (int i = 0; i < 360; i += 10) // Reduced iteration steps
                {
                    Vector3 rotatedDirection = Core.Me.Face(MathEx.ToRadians(i));
                    List<Vector3> enemiesInCone = enemiesPositions.Where(enemyPos => rotatedDirection. .AngleWith(enemyPos - playerPosition) <= coneAngle).ToList();

                    if (enemiesInCone.Count >= minTargets)
                    {
                        Vector3 position = playerPosition + rotatedDirection * spellRange;
                        int targetsInRange = enemiesInCone.Count(enemyPos => (enemyPos - position).LengthSquared() <= spellRange * spellRange); // Count targets in range using LINQ
                        if (targetsInRange > maxTargets)
                        {
                            maxTargets = targetsInRange;
                            bestPosition = position;
                            bestEnemy = enemy;
                        }
                    }
                }
            }

            return (bestPosition, bestEnemy);
        }
        */
    }
}
