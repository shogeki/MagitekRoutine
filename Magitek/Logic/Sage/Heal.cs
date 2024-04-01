﻿using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Sage;
using Magitek.Toggles;
using Magitek.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Sage;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Sage
{
    internal static class Heal
    {
        public static int AoeNeedHealing => PartyManager.NumMembers > 4 ? SageSettings.Instance.AoeNeedHealingFullParty : SageSettings.Instance.AoeNeedHealingLightParty;

        public static bool IsEukrasiaReady()
        {
            return Core.Me.HasAura(Auras.Eukrasia, true) || Spells.Eukrasia.IsKnownAndReady();
        }

        public static async Task<bool> UseEukrasia(uint spellId = 24291, GameObject targetObject = null)
        {
            if (Core.Me.HasAura(Auras.Eukrasia, true))
                return true;
            if (!SageSettings.Instance.Eukrasia)
                return false;
            if (!IsEukrasiaReady())
                return false;
            if (!await Spells.Eukrasia.Cast(Core.Me))
                return false;
            if (!await Coroutine.Wait(2500, () => Core.Me.HasAura(Auras.Eukrasia, true)))
                return false;
            var target = targetObject == null ? Core.Me : targetObject;
            return await Coroutine.Wait(2500, () => ActionManager.CanCast(spellId, target));
        }
        private static async Task<bool> UseZoe()
        {
            if (Core.Me.HasAura(Auras.Zoe))
                return true;

            if (!Spells.Zoe.IsKnownAndReady())
                return false;

            if (!await Spells.Zoe.Cast(Core.Me))
                return false;

            return await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.Zoe));
        }

        private static readonly List<uint> HealingBuffAoEAuras = new List<uint> {
            Auras.EukrasianPrognosis,
            Auras.Kerachole,
            Auras.Panhaimatinon,
            Auras.PhysisII,
            Auras.Holos
        };

        private static readonly List<uint> HealingBuffSingleAuras = new List<uint> {
            Auras.EukrasianDiagnosis,
            Auras.Taurochole,
            Auras.Haimatinon
        };

        public static bool UseAoEHealingBuff(IEnumerable<Character> wantHealTargets)
        {
            if (!SageSettings.Instance.HealingBuffsLimitAtOnce)
                return true;

            if (!wantHealTargets.Any())
                return true;

            var nAuras = wantHealTargets.Select(c => c.CountAuras(HealingBuffAoEAuras, SageSettings.Instance.HealingBuffsOnlyMine)).Max();

            if (nAuras >= SageSettings.Instance.HealingBuffsMaxAtOnce)
            {
                if (nAuras >= SageSettings.Instance.HealingBuffsMaxUnderHp)
                    return false;

                var nUnderHp = wantHealTargets.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.HealingBuffsMoreHpHealthPercentage).Count();
                if (nUnderHp >= SageSettings.Instance.HealingBuffsMoreHpNeedHealing)
                    return true;

                return false;
            }

            return true;
        }

        public static bool NeedAoEHealing()
        {
            var targets = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.AoEHealHealthPercent);

            var needAoEHealing = targets.Count() >= AoeNeedHealing;

            if (needAoEHealing)
                return true;

            return false;
        }

        public static async Task<bool> Diagnosis()
        {
            if (!SageSettings.Instance.Diagnosis)
                return false;

            if (SageSettings.Instance.DiagnosisOnlyBelowXAddersgall && Addersgall > SageSettings.Instance.DiagnosisOnlyAddersgallValue)
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var DiagnosisTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealthPercent < SageSettings.Instance.DiagnosisHpPercent || r.HasAura(Auras.Doom));

                if (DiagnosisTarget == null)
                    return false;

                return await Spells.Diagnosis.Heal(DiagnosisTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.DiagnosisHpPercent)
                return false;

            return await Spells.Diagnosis.Heal(Core.Me);
        }

        public static async Task<bool> EukrasianDiagnosis()
        {
            if (!SageSettings.Instance.EukrasianDiagnosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var target = Group.CastableAlliesWithin30.FirstOrDefault(CanEukrasianDiagnosis);

                if (target == null)
                    return false;

                if (SageSettings.Instance.Zoe && SageSettings.Instance.ZoeEukrasianDiagnosis && !SageSettings.Instance.OnlyZoePneuma)
                    if (SageSettings.Instance.ZoeHealer && target.IsHealer()
                        || SageSettings.Instance.ZoeTank && target.IsTank(SageSettings.Instance.ZoeMainTank))
                        if (target.CurrentHealthPercent <= SageSettings.Instance.ZoeHealthPercent)
                            await UseZoe(); // intentionally ignore failures

                if (!await UseEukrasia(targetObject: target))
                    return false;

                return await Spells.EukrasianDiagnosis.HealAura(target, Auras.EukrasianDiagnosis);

                bool CanEukrasianDiagnosis(Character unit)
                {
                    if (unit == null)
                        return false;

                    if (unit.CurrentHealthPercent > SageSettings.Instance.EukrasianDiagnosisHpPercent)
                        return false;

                    if (unit.HasAura(Auras.EukrasianDiagnosis))
                        return false;

                    if (unit.HasAura(Auras.Galvanize))
                        return false;

                    if (!SageSettings.Instance.EukrasianDiagnosisOnlyHealer && !SageSettings.Instance.EukrasianDiagnosisOnlyTank)
                        return true;

                    if (SageSettings.Instance.EukrasianDiagnosisOnlyHealer && unit.IsHealer())
                        return true;

                    return SageSettings.Instance.EukrasianDiagnosisOnlyTank && unit.IsTank(SageSettings.Instance.EukrasianDiagnosisOnlyMainTank);
                }
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.EukrasianDiagnosisHpPercent || Core.Me.HasAura(Auras.EukrasianDiagnosis))
                return false;

            if (!await UseEukrasia())
                return false;

            return await Spells.EukrasianDiagnosis.HealAura(Core.Me, Auras.EukrasianDiagnosis);
        }

        public static async Task<bool> ForceEukrasianDiagnosis()
        {

            if (!SageSettings.Instance.ForceEukrasianDiagnosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            var target = Core.Me.CurrentTarget;

            if (!await UseEukrasia(Spells.EukrasianDiagnosis.Id, targetObject: target))
                return false;

            if (!await Spells.EukrasianDiagnosis.HealAura(target, Auras.EukrasianDiagnosis))
                return false;

            SageSettings.Instance.ForceEukrasianDiagnosis = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> Prognosis()
        {
            if (!SageSettings.Instance.Prognosis)
                return false;

            if (!Spells.Prognosis.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.PrognosisOnlyBelowXAddersgall && Addersgall > SageSettings.Instance.PrognosisOnlyAddersgallValue)
                return false;

            if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PrognosisHpPercent) < AoeNeedHealing)
                return false;

            return await Spells.Prognosis.Heal(Core.Me);
        }

        public static async Task<bool> EukrasianPrognosis()
        {
            if (!SageSettings.Instance.EukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            var targets = Group.CastableAlliesWithin15.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.EukrasianPrognosisHealthPercent &&
                                                                !r.HasAura(Auras.EukrasianDiagnosis) &&
                                                                !r.HasAura(Auras.EukrasianPrognosis) &&
                                                                !r.HasAura(Auras.Galvanize));

            var needEukrasianPrognosis = targets.Count() >= AoeNeedHealing;

            if (!needEukrasianPrognosis)
                return false;

            if (!UseAoEHealingBuff(targets))
                return false;

            if (SageSettings.Instance.Zoe && SageSettings.Instance.ZoeEukrasianPrognosis && !SageSettings.Instance.OnlyZoePneuma)
                if (SageSettings.Instance.ZoeHealer && targets.Any(r => r.IsHealer())
                    || SageSettings.Instance.ZoeTank && targets.Any(r => r.IsTank(SageSettings.Instance.ZoeMainTank)))
                    if (targets.Any(r => r.CurrentHealthPercent <= SageSettings.Instance.ZoeHealthPercent))
                        await UseZoe(); // intentionally ignore failures

            if (!await UseEukrasia(Spells.EukrasianPrognosis.Id))
                return false;

            return await Spells.EukrasianPrognosis.HealAura(Core.Me, Auras.EukrasianPrognosis);
        }
        public static async Task<bool> ForceEukrasianPrognosis()
        {
            if (!SageSettings.Instance.ForceEukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (!await UseEukrasia(Spells.EukrasianPrognosis.Id))
                return false;

            if (!await Spells.EukrasianPrognosis.HealAura(Core.Me, Auras.EukrasianPrognosis))
                return false;

            SageSettings.Instance.ForceEukrasianPrognosis = false;
            TogglesManager.ResetToggles();
            return true;
        }
        public static async Task<bool> Physis()
        {
            if (!SageSettings.Instance.Physis)
                return false;

            var spell = Spells.PhysisII;
            uint aura = Auras.PhysisII;

            if (Core.Me.ClassLevel < 60)
            {
                spell = Spells.Physis;
                aura = Auras.Physis;
            }

            if (!spell.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            var targets = Spells.PhysisII.IsKnown()
                ? Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.PhysisHpPercent && !r.HasAura(aura))
                : Group.CastableAlliesWithin15.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.PhysisHpPercent && !r.HasAura(aura));

            if (targets.Count() < AoeNeedHealing)
                return false;

            if (!UseAoEHealingBuff(targets))
                return false;

            return await spell.HealAura(Core.Me, aura);
        }
        public static async Task<bool> Druochole()
        {
            if (!SageSettings.Instance.Druochole)
                return false;

            if (Addersgall == 0)
                return false;

            if (!Spells.Druochole.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var DruocholeTarget = Group.CastableAlliesWithin30.FirstOrDefault(r => r.CurrentHealthPercent <= SageSettings.Instance.DruocholeHpPercent);

                if (DruocholeTarget == null)
                    return false;

                return await Spells.Druochole.Heal(DruocholeTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.DruocholeHpPercent)
                return false;

            return await Spells.Druochole.Heal(Core.Me);
        }
        public static async Task<bool> Ixochole()
        {
            if (!SageSettings.Instance.Ixochole)
                return false;

            if (Addersgall == 0)
                return false;

            if (!Spells.Ixochole.IsKnownAndReady())
                return false;

            if (Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.IxocholeHpPercent) < AoeNeedHealing)
                return false;

            return await Spells.Ixochole.Heal(Core.Me);
        }
        public static async Task<bool> Pepsis()
        {
            if (!SageSettings.Instance.Pepsis)
                return false;

            if (!Spells.Pepsis.IsKnownAndReady())
                return false;

            var needPepsis = Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PepsisHpPercent &&
                                                                     (r.HasAura(Auras.EukrasianPrognosis, true) || r.HasAura(Auras.EukrasianDiagnosis, true))) >= AoeNeedHealing;

            if (!needPepsis)
                return false;

            return await Spells.Pepsis.Heal(Core.Me);

        }
        public static async Task<bool> PepsisEukrasianPrognosis()
        {
            if (!SageSettings.Instance.PepsisEukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (!Spells.Pepsis.IsKnownAndReady())
                return false;

            var needPepsis = Group.CastableAlliesWithin15.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PepsisEukrasianPrognosisHealthPercent) >= AoeNeedHealing;

            if (!needPepsis)
                return false;

            if (!await UseEukrasianPrognosisIfNeeded(Group.CastableAlliesWithin15.Count(), Spells.Pepsis, Core.Me))
                return false;

            return await Spells.Pepsis.Heal(Core.Me);
        }
        public static async Task<bool> ForcePepsisEukrasianPrognosis()
        {
            if (!SageSettings.Instance.ForcePepsisEukrasianPrognosis)
                return false;

            if (!IsEukrasiaReady())
                return false;

            if (!Spells.Pepsis.IsKnownAndReady())
                return false;

            if (!await UseEukrasianPrognosisIfNeeded(Group.CastableAlliesWithin15.Count(), Spells.Pepsis, Core.Me))
                return false;

            if (!await Spells.Pepsis.Heal(Core.Me))
                return false;

            SageSettings.Instance.ForcePepsisEukrasianPrognosis = false;
            TogglesManager.ResetToggles();
            return true;
        }

        private static async Task<bool> UseEukrasianPrognosisIfNeeded(int NeedShields, SpellData forSpell, Character target)
        {
            var needPrognosis = Group.CastableAlliesWithin15.Count(r => r.HasAura(Auras.EukrasianPrognosis, true) || r.HasAura(Auras.EukrasianDiagnosis, true)) < NeedShields;

            if (needPrognosis)
            {
                if (!await UseEukrasia(Spells.EukrasianPrognosis.Id))
                    return false;

                if (!await Spells.EukrasianPrognosis.Cast(Core.Me))
                    return false;

                if (!await Coroutine.Wait(1000, () => Core.Me.HasAura(Auras.EukrasianPrognosis, true)))
                    return false;

                if (!await Coroutine.Wait(1000, () => SpellDataExtensions.CanCast(forSpell, target)))
                    return false;
            }

            return true;
        }

        public static async Task<bool> Taurochole()
        {
            if (!SageSettings.Instance.Taurochole)
                return false;

            if (Addersgall == 0)
                return false;

            if (Core.Me.HasAura(Auras.Kerachole))
                return false;

            if (!Spells.Taurochole.IsKnownAndReady())
                return false;

            if (SageSettings.Instance.DisableSingleHealWhenNeedAoeHealing && NeedAoEHealing())
                return false;

            if (Globals.InParty)
            {
                var taurocholeCandidates = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent < SageSettings.Instance.TaurocholeHpPercent
                                                                              && !r.HasAura(Auras.Taurochole)
                                                                              && !r.HasAura(Auras.Kerachole));

                if (SageSettings.Instance.TaurocholeTankOnly)
                    taurocholeCandidates = taurocholeCandidates.Where(r => r.IsTank(SageSettings.Instance.TaurocholeMainTankOnly) || r.CurrentHealthPercent <= SageSettings.Instance.TaurocholeOthersHpPercent);

                var taurocholeTarget = taurocholeCandidates.FirstOrDefault();

                if (taurocholeTarget == null)
                    return false;

                return await Spells.Taurochole.HealAura(taurocholeTarget, Auras.Taurochole);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.TaurocholeHpPercent)
                return false;

            return await Spells.Taurochole.HealAura(Core.Me, Auras.Taurochole);
        }
        public static async Task<bool> Haima()
        {
            if (!SageSettings.Instance.Haima)
                return false;

            if (!Spells.Haima.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                if (SageSettings.Instance.FightLogic_Haima && FightLogic.EnemyHasAnyTankbusterLogic())
                    return false;

                var haimaCandidates = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent < SageSettings.Instance.HaimaHpPercent
                                                                     && !r.HasAura(Auras.Weakness)
                                                                     && !r.HasAura(Auras.Haimatinon)
                                                                     && !r.HasAura(Auras.Panhaimatinon));

                if (SageSettings.Instance.HaimaTankForBuff)
                    haimaCandidates = haimaCandidates.Where(r => r.IsTank(SageSettings.Instance.HaimaMainTankForBuff));

                var haimaTarget = haimaCandidates.FirstOrDefault();

                if (haimaTarget == null)
                    return false;

                return await Spells.Haima.CastAura(haimaTarget, Auras.Haimatinon);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.HaimaHpPercent)
                return false;

            return await Spells.Haima.CastAura(Core.Me, Auras.Haimatinon);
        }
        public static async Task<bool> ForceHaima()
        {
            if (!SageSettings.Instance.ForceHaima)
                return false;

            if (!Spells.Haima.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                var haimaCandidates = Group.CastableAlliesWithin30.Where(r => !r.HasAura(Auras.Weakness));

                if (SageSettings.Instance.HaimaTankForBuff)
                    haimaCandidates = haimaCandidates.Where(r => r.IsTank(SageSettings.Instance.HaimaMainTankForBuff));

                var haimaTarget = haimaCandidates.FirstOrDefault();

                if (haimaTarget == null)
                    return false;

                if (!await Spells.Haima.CastAura(haimaTarget, Auras.Haimatinon))
                    return false;
            }
            else
            {
                if (!await Spells.Haima.Cast(Core.Me))
                    return false;
            }

            SageSettings.Instance.ForceHaima = false;
            TogglesManager.ResetToggles();
            return true;
        }
        public static async Task<bool> Panhaima()
        {
            if (!SageSettings.Instance.Panhaima)
                return false;

            if (!Spells.Panhaima.IsKnownAndReady())
                return false;

            if (Globals.InParty)
            {
                if (SageSettings.Instance.FightLogic_Panhaima && FightLogic.EnemyHasAnyAoeLogic())
                    return false;

                var targets = Group.CastableAlliesWithin30.Where(CanPanhaima);

                if (targets.Count() < AoeNeedHealing)
                    return false;

                if (SageSettings.Instance.PanhaimaOnlyWithTank && !targets.Any(r => r.IsTank(SageSettings.Instance.PanhaimaOnlyWithMainTank)))
                    return false;

                if (!UseAoEHealingBuff(targets))
                    return false;

                return await Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.PanhaimaHpPercent)
                return false;

            return await Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon);

            bool CanPanhaima(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > SageSettings.Instance.PanhaimaHpPercent)
                    return false;

                if (unit.HasAura(Auras.Panhaimatinon))
                    return false;
                //Range is now 30y
                return unit.Distance(Core.Me) <= 30;
            }
        }
        public static async Task<bool> ForcePanhaima()
        {
            if (!SageSettings.Instance.ForcePanhaima)
                return false;

            if (!Spells.Panhaima.IsKnownAndReady())
                return false;

            if (!await Spells.Panhaima.CastAura(Core.Me, Auras.Panhaimatinon))
                return false;

            SageSettings.Instance.ForcePanhaima = false;
            TogglesManager.ResetToggles();
            return true;
        }
        public static async Task<bool> Egeiro()
        {
            return await Roles.Healer.Raise(
                Spells.Egeiro,
                SageSettings.Instance.SwiftcastRes,
                SageSettings.Instance.SlowcastRes,
                SageSettings.Instance.ResOutOfCombat
            );
        }
        public static async Task<bool> Pneuma()
        {
            if (!SageSettings.Instance.Pneuma)
                return false;

            if (SageSettings.Instance.OnlyZoePneuma)
                return false;

            if (!Spells.Pneuma.IsKnownAndReady())
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Globals.InParty)
            {
                var pneumaTarget = Group.CastableAlliesWithin25.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PneumaHpPercent) >= AoeNeedHealing;

                if (!pneumaTarget)
                    return false;

                return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.PneumaHpPercent)
                return false;

            return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ZoePneuma()
        {
            if (!SageSettings.Instance.Pneuma)
                return false;

            if (SageSettings.Instance.Zoe)
            {
                if (!SageSettings.Instance.ZoePneuma)
                    return false;
            }
            else if (!SageSettings.Instance.OnlyZoePneuma)
                return false;

            if (!Spells.Pneuma.IsKnownAndReady())
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Globals.InParty)
            {
                var pneumaTarget = Group.CastableAlliesWithin25.Count(r => r.CurrentHealthPercent <= SageSettings.Instance.PneumaHpPercent) >= AoeNeedHealing;

                if (!pneumaTarget)
                    return false;

                if (!await UseZoe())
                    return false;

                return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
            }

            if (Core.Me.CurrentHealthPercent > SageSettings.Instance.PneumaHpPercent)
                return false;

            if (!await UseZoe())
                return false;

            if (!await Coroutine.Wait(1000, () => ActionManager.CanCast(Spells.Pneuma.Id, Core.Me.CurrentTarget)))
                return false;

            return await Spells.Pneuma.Heal(Core.Me.CurrentTarget);
        }


        public static async Task<bool> ForceZoePneuma()
        {
            if (!SageSettings.Instance.ForceZoePneuma)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (!Spells.Pneuma.IsKnownAndReady())
                return false;

            if (!await UseZoe())
                return false;

            if (!await Coroutine.Wait(1000, () => ActionManager.CanCast(Spells.Pneuma.Id, Core.Me.CurrentTarget)))
                return false;

            if (!await Spells.Pneuma.Heal(Core.Me.CurrentTarget))
                return false;

            SageSettings.Instance.ForceZoePneuma = false;
            TogglesManager.ResetToggles();
            return true;
        }

        public static async Task<bool> Kerachole()
        {
            if (!SageSettings.Instance.Kerachole)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.Kerachole.IsKnownAndReady())
                return false;

            if (Addersgall == 0)
                return false;

            if (Globals.InParty)
            {
                var targets = Group.CastableAlliesWithin30.Where(CanKerachole).ToList();

                if (targets.Count < AoeNeedHealing)
                    return false;

                if (SageSettings.Instance.KeracholeOnlyWithTank && !Group.CastableAlliesWithin30.Any(r => r.IsTank(SageSettings.Instance.KeracholeOnlyWithMainTank)))
                    return false;

                if (!UseAoEHealingBuff(targets))
                    return false;

                return await Spells.Kerachole.CastAura(Core.Me, Auras.Kerachole);
            }

            if (!CanKerachole(Core.Me))
                return false;

            return await Spells.Kerachole.CastAura(Core.Me, Auras.Kerachole);

            bool CanKerachole(Character unit)
            {
                if (unit == null)
                    return false;

                if (unit.CurrentHealthPercent > SageSettings.Instance.KeracholeHealthPercent)
                    return false;

                if (unit.HasAura(Auras.Kerachole))
                    return false;

                return unit.Distance(Core.Me) <= Spells.Kerachole.Radius;
            }
        }
        public static async Task<bool> Holos()
        {
            if (!SageSettings.Instance.Holos)
                return false;

            if (Core.Me.ClassLevel < Spells.Holos.LevelAcquired)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Globals.PartyInCombat)
                return false;

            if (!Spells.Holos.IsKnownAndReady())
                return false;

            var targets = Group.CastableAlliesWithin30.Where(r => r.CurrentHealthPercent <= SageSettings.Instance.HolosHealthPercent
                                                             && !r.HasAura(Auras.Holos));

            if (targets.Count() < AoeNeedHealing)
                return false;

            if (SageSettings.Instance.HolosTankOnly && !targets.Any(r => r.IsTank(SageSettings.Instance.HolosMainTankOnly)))
                return false;

            if (!UseAoEHealingBuff(targets))
                return false;

            return await Spells.Holos.HealAura(Core.Me, Auras.Holos);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return Healer.ForceLimitBreak(Spells.HealingWind, Spells.BreathoftheEarth, Spells.TechneMakre, Spells.Dosis);
        }
    }
}
