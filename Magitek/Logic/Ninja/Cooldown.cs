﻿using ff14bot;
using Magitek.Extensions;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using NinjaRoutine = Magitek.Utilities.Routines.Ninja;

namespace Magitek.Logic.Ninja
{
    internal static class Cooldown
    {
        public static async Task<bool> Mug()
        {

            if (Core.Me.ClassLevel < 15)
                return false;

            if (!Spells.Mug.IsKnown())
                return false;

            if (Combat.CombatTime.ElapsedMilliseconds < Spells.SpinningEdge.AdjustedCooldown.TotalMilliseconds * NinjaRoutine.OpenerBurstAfterGCD - 770)
                return false;

            if (MagitekActionResourceManager.Ninja.NinkiGauge + 40 > 100)
                return false;

            return await Spells.Mug.Cast(Core.Me.CurrentTarget);

        }

        public static async Task<bool> TrickAttack()
        {

            if (Core.Me.ClassLevel < 18)
                return false;

            if (!Spells.TrickAttack.IsKnown())
                return false;

            if (Spells.Mug.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Core.Me.ClassLevel >= 80 && Spells.Bunshin.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Spells.SpinningEdge.Cooldown.TotalMilliseconds >= 800)
                return false;

            return await Spells.TrickAttack.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> DreamWithinaDream()
        {

            if (Core.Me.ClassLevel < 56)
                return false;

            if (!Spells.DreamWithinaDream.IsKnown())
                return false;

            if (Spells.TrickAttack.Cooldown == new TimeSpan(0, 0, 0))
                return false;

            if (Casting.SpellCastHistory.First().Spell == Spells.TrickAttack && Spells.SpinningEdge.Cooldown.TotalMilliseconds < 800)
                return false;

            return await Spells.DreamWithinaDream.Cast(Core.Me.CurrentTarget);
        }
    }
}
