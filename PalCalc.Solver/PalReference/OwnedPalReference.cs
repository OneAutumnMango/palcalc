using PalCalc.Model;
using PalCalc.Solver.PalReference.Properties;
using PalCalc.Solver.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver.PalReference
{
    public class OwnedPalReference : IPalReference
    {
        PalInstance instance;

        /// <param name="effectivePassives">The list of passives held by the `instance`, filtered/re-mapped based on desired passives. (.ToDedicatedPassives())</param>
        /// <param name="useCurrentPalLevel">Limit the pal to the skills it knows now instead of assuming it gets raised to <paramref name="maxPalLevel"/>.</param>
        public OwnedPalReference(PalInstance instance, List<PassiveSkill> effectivePassives, IV_Set effectiveIVs, int maxPalLevel, bool useCurrentPalLevel = false)
        {
            this.instance = instance;

            EffectivePassives = effectivePassives;
            EffectivePassivesHash = EffectivePassives.Select(p => p.InternalNameHash).SetHash();
            TimeFactor = instance.PassiveSkills.ToTimeFactor();

            ActualPassives = instance.PassiveSkills;

            IVs = effectiveIVs;

            Gender = instance.Gender;

            // Initialize inherited active skills based on what this pal species can learn
            MaxPalLevel = maxPalLevel;
            UseCurrentPalLevel = useCurrentPalLevel;

            var skillLevel = useCurrentPalLevel ? Math.Min(instance.Level, maxPalLevel) : maxPalLevel;
            InheritedActiveSkills = ActiveSkillInheritance.NaturalSkillsOf(instance.Pal, skillLevel);
            InheritableActiveSkills = ActiveSkillInheritance.NaturalSkillMaskOf(instance.Pal, skillLevel);

            ActualActiveSkills = instance.ActiveSkills ?? [];
        }

        public PalInstance UnderlyingInstance => instance;

        public Pal Pal => instance.Pal;

        public List<PassiveSkill> EffectivePassives { get; private set; }

        public int EffectivePassivesHash { get; }

        public List<ActiveSkill> InheritedActiveSkills { get; private set; }

        public ActiveSkillSet InheritableActiveSkills { get; private set; }

        public int MaxPalLevel { get; }

        public bool UseCurrentPalLevel { get; }

        public List<ActiveSkill> ActualActiveSkills { get; }

        public List<PassiveSkill> ActualPassives { get; }

        public float TimeFactor { get; }

        public IV_Set IVs { get; }

        // (Make this private-settable for use by WithGuaranteedGender when gender-reversers are enabled)
        public PalGender Gender { get; private set; }

        public IPalRefLocation Location => new OwnedRefLocation() { OwnerId = instance.OwnerPlayerId, Location = instance.Location };

        public TimeSpan BreedingEffort => TimeSpan.Zero;
        public TimeSpan SelfBreedingEffort => TimeSpan.Zero;

        public int TotalCost => 0;

        public int NumTotalBreedingSteps => 0;

        public int NumTotalEggs => 0;

        public int NumTotalWildPals => 0;

        public bool IsOutdated { get; set; }

        private OwnedPalReference cachedFemaleRef, cachedMaleRef, cachedWildcardRef, cachedOppositeWildcardRef;

        private OwnedPalReference MakeGuaranteedGenderImpl(PalGender gender)
        {
            var res = new OwnedPalReference(instance, EffectivePassives, IVs, MaxPalLevel, UseCurrentPalLevel);
            res.Gender = gender;
            res.InheritedActiveSkills = InheritedActiveSkills;
            res.InheritableActiveSkills = InheritableActiveSkills;
            return res;
        }

        public IPalReference WithGuaranteedGender(PalDB db, PalGender gender, bool useReverser)
        {
            if (gender != Gender)
            {
                if (!useReverser)
                    throw new Exception("Cannot force a gender change for owned pals without a gender reverser");

                switch (gender)
                {
                    case PalGender.FEMALE: return cachedFemaleRef ??= MakeGuaranteedGenderImpl(gender);
                    case PalGender.MALE: return cachedMaleRef ??= MakeGuaranteedGenderImpl(gender);
                    case PalGender.OPPOSITE_WILDCARD: return cachedOppositeWildcardRef ??= MakeGuaranteedGenderImpl(gender);
                    case PalGender.WILDCARD: return cachedWildcardRef ??= MakeGuaranteedGenderImpl(gender);
                }
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            var asOwned = obj as OwnedPalReference;
            if (ReferenceEquals(asOwned, null)) return false;
            if (ReferenceEquals(this, asOwned)) return true;

            return GetHashCode() == asOwned.GetHashCode();
        }

        public override string ToString() => $"Owned {Gender} {Pal.Name} w/ ({EffectivePassives.PassiveSkillListToString()}) in {Location}";

        private static readonly int TypeHash = nameof(OwnedPalReference).GetHashCode();

        private int hashCode;

        public override int GetHashCode()
        {
            if (hashCode != 0) return hashCode;

            var result = HashCode.Combine(TypeHash, UnderlyingInstance.GetHashCode());
            return hashCode = result == 0 ? 1 : result;
        }
    }
}
