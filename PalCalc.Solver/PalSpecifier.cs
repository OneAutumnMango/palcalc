using PalCalc.Model;
using PalCalc.Solver.PalReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver
{
    public class PalSpecifier
    {
        public Pal Pal { get; set; }
        public List<PassiveSkill> RequiredPassives { get; set; } = new List<PassiveSkill>();
        public PalGender RequiredGender { get; set; } = PalGender.WILDCARD;

        public List<PassiveSkill> OptionalPassives { get; set; } = new List<PassiveSkill>();

        // Target active skills that must be inherited through breeding (up to 6)
        private List<ActiveSkill> targetActiveSkills = new List<ActiveSkill>();
        private ActiveSkillSet? targetActiveSkillSet;

        public List<ActiveSkill> TargetActiveSkills
        {
            get => targetActiveSkills;
            set
            {
                targetActiveSkills = value;
                targetActiveSkillSet = null;
            }
        }

        public ActiveSkillSet TargetActiveSkillSet => targetActiveSkillSet ??= ActiveSkillSet.Of(targetActiveSkills);

        public IEnumerable<PassiveSkill> DesiredPassives => RequiredPassives.Concat(OptionalPassives);

        // Owned pal instances which must show up somewhere in the resulting breeding tree
        private List<string> requiredInstanceIds = new List<string>();
        private RequiredPalSet requiredPals;

        public List<string> RequiredInstanceIds
        {
            get => requiredInstanceIds;
            set
            {
                requiredInstanceIds = value ?? new List<string>();
                requiredPals = null;
            }
        }

        public RequiredPalSet RequiredPals => requiredPals ??= new RequiredPalSet(requiredInstanceIds);

        public int IV_HP { get; set; }
        public int IV_Attack { get; set; }
        public int IV_Defense { get; set; }

        public override string ToString() => $"{Pal.Name} with {RequiredPassives.PassiveSkillListToString()}";

        public bool IsSatisfiedBy(IPalReference palRef)
        {
            if (Pal != palRef.Pal) return false;

            if (!RequiredPals.IsSatisfiedBy(palRef)) return false;

            if (RequiredGender != PalGender.WILDCARD && palRef.Gender != PalGender.WILDCARD && palRef.Gender != RequiredGender)
                return false;

            var effectivePassives = palRef.EffectivePassives;
            for (int i = 0; i < RequiredPassives.Count; i++)
                if (!effectivePassives.Contains(RequiredPassives[i])) return false;

            if (IV_HP != 0 && !palRef.IVs.HP.Satisfies(IV_HP)) return false;
            if (IV_Attack != 0 && !palRef.IVs.Attack.Satisfies(IV_Attack)) return false;
            if (IV_Defense != 0 && !palRef.IVs.Defense.Satisfies(IV_Defense)) return false;

            if (targetActiveSkills.Count == 0) return true;

            var actualActiveSkills = palRef.ActualActiveSkills;
            for (int i = 0; i < targetActiveSkills.Count; i++)
                if (!actualActiveSkills.Contains(targetActiveSkills[i])) return false;

            return ActiveSkillInheritance.CanProvide(palRef, TargetActiveSkillSet);
        }

        public void Normalize()
        {
            RequiredPassives = RequiredPassives.Distinct().ToList();
            OptionalPassives = OptionalPassives.Except(RequiredPassives).Distinct().ToList();
            TargetActiveSkills = TargetActiveSkills.Distinct().Take(6).ToList();
            RequiredInstanceIds = RequiredInstanceIds.Distinct().Take(RequiredPalSet.MaxRequiredPals).ToList();
        }

        internal PalSpecifier NormalizedCopy()
        {
            var requiredPassives = RequiredPassives.Distinct().ToList();

            return new PalSpecifier
            {
                Pal = Pal,
                RequiredPassives = requiredPassives,
                RequiredGender = RequiredGender,
                OptionalPassives = OptionalPassives
                    .Except(requiredPassives)
                    .Distinct()
                    .ToList(),
                TargetActiveSkills = TargetActiveSkills.Distinct().Take(6).ToList(),
                RequiredInstanceIds = RequiredInstanceIds.Distinct().Take(RequiredPalSet.MaxRequiredPals).ToList(),
                IV_HP = IV_HP,
                IV_Attack = IV_Attack,
                IV_Defense = IV_Defense,
            };
        }
    }
}
