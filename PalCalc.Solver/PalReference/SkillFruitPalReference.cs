using PalCalc.Model;
using PalCalc.Solver.PalReference.Properties;
using PalCalc.Solver.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.Solver.PalReference
{
    /// <summary>
    /// Represents a pal which has been taught active skills directly by feeding it skill fruits.
    /// Teaching is instantaneous and doesn't affect what the pal can pass on through breeding.
    /// </summary>
    public class SkillFruitPalReference : IPalReference
    {
        public IPalReference Input { get; }
        public List<ActiveSkill> TaughtSkills { get; }

        public SkillFruitPalReference(IPalReference input, IEnumerable<ActiveSkill> taughtSkills)
        {
            if (input is SkillFruitPalReference sfpr)
            {
                Input = sfpr.Input;
                TaughtSkills = [.. sfpr.TaughtSkills, .. taughtSkills.Except(sfpr.TaughtSkills)];
            }
            else
            {
                Input = input;
                TaughtSkills = taughtSkills.ToList();
            }

            ActualActiveSkills = [.. Input.ActualActiveSkills, .. TaughtSkills.Except(Input.ActualActiveSkills)];
        }

        public Pal Pal => Input.Pal;

        public List<PassiveSkill> EffectivePassives => Input.EffectivePassives;
        public int EffectivePassivesHash => Input.EffectivePassivesHash;
        public List<PassiveSkill> ActualPassives => Input.ActualPassives;

        public List<ActiveSkill> InheritedActiveSkills => Input.InheritedActiveSkills;
        public ActiveSkillSet InheritableActiveSkills => Input.InheritableActiveSkills;
        public List<ActiveSkill> ActualActiveSkills { get; }

        public IV_Set IVs => Input.IVs;
        public PalGender Gender => Input.Gender;
        public float TimeFactor => Input.TimeFactor;

        public ulong RequiredPalMask => Input.RequiredPalMask;

        public IPalRefLocation Location => SkillFruitRefLocation.Instance;

        public TimeSpan BreedingEffort => Input.BreedingEffort;
        public TimeSpan SelfBreedingEffort => Input.SelfBreedingEffort;
        public int TotalCost => Input.TotalCost;
        public int NumTotalBreedingSteps => Input.NumTotalBreedingSteps;
        public int NumTotalEggs => Input.NumTotalEggs;
        public int NumTotalWildPals => Input.NumTotalWildPals;

        public bool IsOutdated { get; set; }

        public IPalReference WithGuaranteedGender(PalDB db, PalGender gender, bool useReverser) =>
            new SkillFruitPalReference(Input.WithGuaranteedGender(db, gender, useReverser), TaughtSkills);

        public override string ToString() => $"Skill fruits on {{{Input}}} : {string.Join("; ", TaughtSkills.Select(s => s.Name))}";

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || (obj is SkillFruitPalReference && obj.GetHashCode() == GetHashCode());

        private static readonly int TypeHash = nameof(SkillFruitPalReference).GetHashCode();

        private int hashCode;

        public override int GetHashCode()
        {
            if (hashCode != 0) return hashCode;

            var result = HashCode.Combine(
                TypeHash,
                Input.GetHashCode(),
                TaughtSkills.Select(s => s.InternalName).SetHash()
            );

            return hashCode = result == 0 ? 1 : result;
        }
    }
}
