using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using AstroUtils = Magitek.Utilities.Routines.Astrologian;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal static class Buff
    {
        public static int AoeThreshold => PartyManager.NumMembers > 4 ? AstrologianSettings.Instance.AoeNeedHealingFullParty : AstrologianSettings.Instance.AoeNeedHealingLightParty;

        public static async Task<bool> Swiftcast()
        {
            if (await Spells.Swiftcast.CastAura(Core.Me, Auras.Swiftcast))
            {
                return await Coroutine.Wait(15000, () => Core.Me.HasAura(Auras.Swiftcast, true, 7000));
            }

            return false;
        }

        public static async Task<bool> LucidDreaming()
        {
            return await Roles.Healer.LucidDreaming(AstrologianSettings.Instance.LucidDreaming, AstrologianSettings.Instance.LucidDreamingManaPercent);
        }

        public static async Task<bool> Lightspeed()
        {
            if (!AstrologianSettings.Instance.Lightspeed)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Combat.CombatTotalTimeLeft <= 25)
                return false;

            if (!Spells.Lightspeed.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                if (AstrologianSettings.Instance.FightLogic_Lightspeed && FightLogic.EnemyIsCastingBigAoe() && !Spells.NeutralSect.IsKnownAndReady() && !Spells.Macrocosmos.IsKnownAndReady())
                    return await FightLogic.DoAndBuffer(Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed));

                if (AstrologianSettings.Instance.LightspeedTankOnly && Group.CastableTanks.All(r => r.CurrentHealthPercent >= AstrologianSettings.Instance.LightspeedHealthPercent))
                    return false;

                if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.LightspeedHealthPercent) > Heals.AoeThreshold)
                    return await Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed);
            }


            return await Spells.Lightspeed.CastAura(Core.Me, Auras.Lightspeed);
        }

        public static async Task<bool> Divination()
        {
            if (!AstrologianSettings.Instance.Divination)

                return false;


            if (!Spells.Divination.IsKnownAndReady())
                return false;


            // Added check to see if more than configured allies are around

            var divinationTargets = Group.CastableAlliesWithin30.Count(r => r.IsAlive);

            if (divinationTargets >= AstrologianSettings.Instance.DivinationAllies)
                return await Spells.Divination.CastAura(Core.Me, Auras.Divination);

            return false;
        }

        public static async Task<bool> Synastry()
        {
            if (!AstrologianSettings.Instance.Synastry)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!Spells.Synastry.IsKnownAndReady())
                return false;

            if (Casting.LastSpell == Spells.Synastry)
                return false;

            if (Core.Me.HasAura(Auras.SynastrySource))
                return false;

            if (Group.CastableAlliesWithin30.Count(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.SynastryHealthPercent) < AstrologianSettings.Instance.SynastryAmountOfPeople)
                return false;

            GameObject target = AstrologianSettings.Instance.SynastryTankOnly
                ? Group.CastableTanks.FirstOrDefault(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.SynastryHealthPercent
                && r.IsTank())
                : Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealthPercent <= AstrologianSettings.Instance.SynastryHealthPercent);

            if (target == null)
                return false;

            return await Spells.Synastry.CastAura(target, Auras.SynastryDestination);
        }

        public static async Task<bool> NeutralSect()
        {
            if (!AstrologianSettings.Instance.NeutralSect)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.NeutralSect.IsKnownAndReady())
                return false;

            if (AstrologianSettings.Instance.FightLogic_NeutralSectAspectedHelios && FightLogic.EnemyIsCastingBigAoe() && (AstrologianSettings.Instance.FightLogic_Macrocosmos && !Spells.Macrocosmos.IsKnownAndReady()) && !Core.Me.HasAnyAura(AstroUtils.ScholarAndSageShieldsNotToOverwrite))
                return await FightLogic.DoAndBuffer(Spells.NeutralSect.CastAura(Core.Me, Auras.NeutralSect));

            var neutral = Group.CastableAlliesWithin15.Count(r => r.CurrentHealth > 0
            && r.CurrentHealthPercent <= AstrologianSettings.Instance.NeutralSectHealthPercent);

            if (neutral < AoeThreshold)
                return false;

            return await Spells.NeutralSect.CastAura(Core.Me, Auras.NeutralSect);
        }
    }
}