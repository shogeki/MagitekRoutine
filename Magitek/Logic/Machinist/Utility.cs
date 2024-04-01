﻿using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Machinist;
using Magitek.Toggles;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Machinist
{
    internal static class Utility
    {
        public static async Task<bool> Tactician()
        {

            if (!MachinistSettings.Instance.ForceTactician)
                return false;

            if (!await Spells.Tactician.Cast(Core.Me))
                return false;

            MachinistSettings.Instance.ForceTactician = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> Dismantle()
        {

            if (!MachinistSettings.Instance.ForceDismantle)
                return false;

            if (!await Spells.Dismantle.Cast(Core.Me.CurrentTarget))
                return false;

            MachinistSettings.Instance.ForceDismantle = false;
            TogglesManager.ResetToggles();
            return true;
        }

    }
}
