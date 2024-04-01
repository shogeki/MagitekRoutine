using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Dancer;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Dancer
{
    internal static class SingleTarget
    {

        public static async Task<bool> FanDance()
        {
            if (!DancerSettings.Instance.FanDance1)
                return false;

            if (ActionResourceManager.Dancer.FourFoldFeathers < 4 && !Core.Me.HasAura(Auras.Devilment) && Core.Me.ClassLevel >= 62)
                return false;

            if (DancerSettings.Instance.UseRangeAndFacingChecks)
            {
                if (Core.Me.HasAura(Auras.StandardStep) || Core.Me.HasAura(Auras.TechnicalStep))
                    return false;

                if (!GameSettingsManager.FaceTargetOnAction && !Core.Me.CurrentTarget.InView())
                    return false;
            }

            return await Spells.FanDance.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fountainfall()
        {
            if (!Core.Me.HasAura(Auras.FlourishingFlow) && !Core.Me.HasAura(Auras.SilkenFlow)) return false;

            if (DancerSettings.Instance.UseRangeAndFacingChecks)
            {
                if (Core.Me.HasAura(Auras.StandardStep) || Core.Me.HasAura(Auras.TechnicalStep)) return false;
                //if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.Fountainfall.Range) return false;
                if (!GameSettingsManager.FaceTargetOnAction && !Core.Me.CurrentTarget.InView()) return false;
            }

            return await Spells.Fountainfall.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ReverseCascade()
        {
            if (!Core.Me.HasAura(Auras.FlourishingSymmetry) && !Core.Me.HasAura(Auras.SilkenSymmetry)) return false;

            if (DancerSettings.Instance.UseRangeAndFacingChecks)
            {
                if (Core.Me.HasAura(Auras.StandardStep) || Core.Me.HasAura(Auras.TechnicalStep)) return false;
                //if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.ReverseCascade.Range) return false;
                if (!GameSettingsManager.FaceTargetOnAction && !Core.Me.CurrentTarget.InView()) return false;
            }

            return await Spells.ReverseCascade.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fountain()
        {
            if (ActionManager.LastSpell != Spells.Cascade) return false;

            if (DancerSettings.Instance.UseRangeAndFacingChecks)
            {
                if (Core.Me.HasAura(Auras.StandardStep) || Core.Me.HasAura(Auras.TechnicalStep)) return false;
                //if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.Fountain.Range) return false;
                if (!GameSettingsManager.FaceTargetOnAction && !Core.Me.CurrentTarget.InView()) return false;
            }

            return await Spells.Fountain.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Cascade()
        {
            //if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.Cascade.Range) return false;

            if (DancerSettings.Instance.UseRangeAndFacingChecks)
            {
                if (Core.Me.HasAura(Auras.StandardStep) || Core.Me.HasAura(Auras.TechnicalStep)) return false;
                //if (Core.Me.CurrentTarget.Distance(Core.Me) > Spells.Cascade.Range) return false;
                if (!GameSettingsManager.FaceTargetOnAction && !Core.Me.CurrentTarget.InView()) return false;
            }

            return await Spells.Cascade.Cast(Core.Me.CurrentTarget);
        }
    }
}