﻿using ff14bot;
using ff14bot.Managers;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Samurai;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using SamuraiRoutine = Magitek.Utilities.Routines.Samurai;

namespace Magitek.Logic.Samurai
{
    internal static class Utility
    {

        public static async Task<bool> Hagakure()
        {
            if (!SamuraiSettings.Instance.UseHagakure)
                return false;

            if (Core.Me.HasAura(Auras.MeikyoShisui))
                return false;

            if (!Core.Me.CurrentTarget.HasAura(Auras.Higanbana, true))
                return false;

            if (SamuraiRoutine.SenCount != 1)
                return false;

            //Don't use mid combo
            if (ActionManager.LastSpell == Spells.Hakaze || ActionManager.LastSpell == Spells.Shifu || ActionManager.LastSpell == Spells.Jinpu || ActionManager.LastSpell == Spells.Fuga
                || Casting.LastSpell == Spells.Hakaze || Casting.LastSpell == Spells.Shifu || Casting.LastSpell == Spells.Jinpu || Casting.LastSpell == Spells.Fuga)
                return false;

            if (!SamuraiRoutine.isReadyFillerRotation)
                return false;

            if (!await Spells.Hagakure.Cast(Core.Me))
                return false;

            // If 4GCD Filler, we need to replay again the 2GCD filler, Otherwise, let's stop it
            if (SamuraiSettings.Instance.SamuraiFillerStrategy.Equals(SamuraiFillerStrategy.FourGCD))
                SamuraiRoutine.InitializeFillerVar(false, true); //Replay 2GCD Filler
            else
                SamuraiRoutine.InitializeFillerVar(false, false); //End Filler

            return true;
        }

        public static async Task<bool> TrueNorth()
        {
            if (SamuraiSettings.Instance.EnemyIsOmni || !SamuraiSettings.Instance.UseTrueNorth)
                return false;

            if (Casting.LastSpell == Spells.TrueNorth)
                return false;

            if (Core.Me.HasAura(Auras.TrueNorth))
                return false;

            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 10 + x.CombatReach) >= SamuraiSettings.Instance.AoeEnemies)
                return false;

            if (Spells.TrueThrust.Cooldown.TotalMilliseconds > Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset + 100)
                return false;

            if (ActionManager.LastSpell == Spells.Shifu)
            {
                if (Core.Me.CurrentTarget.IsFlanking)
                    return false;

                return await Spells.TrueNorth.CastAura(Core.Me, Auras.TrueNorth);
            }

            if (ActionManager.LastSpell == Spells.Jinpu)
            {
                if (Core.Me.CurrentTarget.IsBehind)
                    return false;

                return await Spells.TrueNorth.CastAura(Core.Me, Auras.TrueNorth);
            }

            return false;
        }

    }
}