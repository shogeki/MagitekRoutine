﻿using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.Bard;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.Bard;
using Magitek.Utilities;
using System.Threading.Tasks;
using BardRoutine = Magitek.Utilities.Routines.Bard;

namespace Magitek.Rotations
{
    public static class Bard
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < BardSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            //Openers.OpenerCheck();

            if (Core.Me.HasTarget && Core.Me.CurrentTarget.CanAttack)
                return false;

            if (Globals.OnPvpMap)
                return false;

            if (WorldManager.InSanctuary)
                return false;

            return await PhysicalDps.Peloton(BardSettings.Instance);
        }

        public static async Task<bool> Pull()
        {
            BardRoutine.RefreshVars();

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                {
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20 + Core.Me.CurrentTarget.CombatReach);
                }
            }

            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            return await GambitLogic.Gambit();
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                {
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20 + Core.Me.CurrentTarget.CombatReach);
                }
            }

            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            BardRoutine.RefreshVars();

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (await CustomOpenerLogic.Opener())
                return true;

            //LimitBreak
            if (Aoe.ForceLimitBreak()) return true;

            if (BardRoutine.GlobalCooldown.CanWeave())
            {
                // Utility
                if (await Utility.RepellingShot()) return true;
                if (await Utility.WardensPaean()) return true;
                if (await Utility.NaturesMinne()) return true;
                if (await Utility.Troubadour()) return true;
                if (await PhysicalDps.ArmsLength(BardSettings.Instance)) return true;
                if (await PhysicalDps.SecondWind(BardSettings.Instance)) return true;
                if (await PhysicalDps.Interrupt(BardSettings.Instance)) return true;
                if (await Cooldowns.UsePotion()) return true;

                // Damage
                if (await SingleTarget.LastPossiblePitchPerfectDuringWM()) return true;
                if (await Songs.LetMeSingYouTheSongOfMyPeople()) return true;
                if (await Cooldowns.RagingStrikes()) return true;
                if (await Cooldowns.BattleVoice()) return true;
                if (await Cooldowns.RadiantFinale()) return true;
                if (await Cooldowns.Barrage()) return true;
                if (await SingleTarget.PitchPerfect()) return true;
                if (await Aoe.RainOfDeathDuringMagesBallard()) return true;
                if (await SingleTarget.BloodletterInMagesBallard()) return true;
                if (await SingleTarget.EmpyrealArrow()) return true;
                if (await SingleTarget.Sidewinder()) return true;
                if (await Aoe.RainOfDeath()) return true;
                if (await SingleTarget.Bloodletter()) return true;
            }

            if (await DamageOverTime.IronJawsOnCurrentTarget()) return true;
            if (await DamageOverTime.SnapShotIronJawsOnCurrentTarget()) return true;
            if (await Aoe.BlastArrow()) return true;
            if (await SingleTarget.StraightShotAfterBarrage()) return true;
            if (await DamageOverTime.StormbiteOnCurrentTarget()) return true;
            if (await DamageOverTime.CausticBiteOnCurrentTarget()) return true;
            if (await Aoe.ApexArrow()) return true;
            if (await Aoe.ShadowBite()) return true;
            if (await DamageOverTime.IronJawsOnOffTarget()) return true;
            if (await DamageOverTime.StormbiteOnOffTarget()) return true;
            if (await DamageOverTime.CausticBiteOnOffTarget()) return true;
            if (await Aoe.LadonsBite()) return true;
            if (await SingleTarget.StraightShot()) return true;
            return (await SingleTarget.HeavyShot());

        }

        public static async Task<bool> PvP()
        {
            if (!BaseSettings.Instance.ActivePvpCombatRoutine)
                return await Combat();

            if (await PhysicalDps.Guard(BardSettings.Instance)) return true;
            if (await PhysicalDps.Purify(BardSettings.Instance)) return true;
            if (await PhysicalDps.Recuperate(BardSettings.Instance)) return true;

            if (await Pvp.FinalFantasiaPvp()) return true;

            if (!PhysicalDps.GuardCheck())
            {
                if (await Pvp.SilentNocturnePvp()) return true;
                if (await Pvp.EmpyrealArrow()) return true;
                if (await Pvp.BlastArrow()) return true;
                if (await Pvp.ApexArrow()) return true;
            }

            return (await Pvp.PowerfulShot());
        }
    }
}


