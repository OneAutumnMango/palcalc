using PalCalc.Model;
using PalCalc.Solver.PalReference;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.Solver
{
    /// <summary>
    /// A parent can only pass down a limited number of active skills to its child
    /// (<see cref="GameConstants.MaxInheritedActiveSkills"/>). Skills the child learns naturally
    /// don't count against that limit.
    /// </summary>
    public static class ActiveSkillInheritance
    {
        private static readonly ConcurrentDictionary<(string, int), List<ActiveSkill>> naturalSkillsByPal = new();

        /// <summary>
        /// The skills this pal species learns on its own by leveling up to at most <paramref name="maxLevel"/>.
        /// </summary>
        public static List<ActiveSkill> NaturalSkillsOf(Pal pal, int maxLevel) =>
            naturalSkillsByPal.GetOrAdd((pal.Name, maxLevel), static key =>
                PalDB.LoadEmbedded().BreedingSkills.Values
                    .SelectMany(ls => ls)
                    .Where(ls => ls.PalName == key.Item1 && ls.Level <= key.Item2)
                    .Select(ls => ls.Skill)
                    .Distinct()
                    .ToList()
            );

        public static bool LearnsNaturally(Pal pal, ActiveSkill skill, int maxLevel) =>
            NaturalSkillsOf(pal, maxLevel).Contains(skill);

        /// <summary>
        /// Whether <paramref name="reference"/> can actually end up with all of <paramref name="required"/>
        /// without any single parent needing to pass down more than the game allows.
        /// </summary>
        public static bool CanProvide(IPalReference reference, IReadOnlyList<ActiveSkill> required) =>
            CanProvide(reference, required, []);

        private static bool CanProvide(
            IPalReference reference,
            IReadOnlyList<ActiveSkill> required,
            Dictionary<(IPalReference, int), bool> memo
        )
        {
            if (required.Count == 0) return true;

            var memoKey = (reference, required.Select(s => s.InternalName).SetHash());
            if (memo.TryGetValue(memoKey, out var cached)) return cached;

            memo[memoKey] = false; // guard against re-entry
            var result = Evaluate(reference, required, memo);
            memo[memoKey] = result;
            return result;
        }

        private static bool Evaluate(
            IPalReference reference,
            IReadOnlyList<ActiveSkill> required,
            Dictionary<(IPalReference, int), bool> memo
        )
        {
            switch (reference)
            {
                case SkillFruitPalReference fruit:
                    return CanProvide(fruit.Input, required.Except(fruit.TaughtSkills).ToList(), memo);

                case SurgeryTablePalReference surgery:
                    return CanProvide(surgery.Input, required, memo);

                case BredPalReference bred:
                    var inherited = required.Where(s => !bred.NaturalActiveSkills.Contains(s)).ToList();
                    if (inherited.Count == 0) return true;
                    if (inherited.Count > GameConstants.MaxInheritedActiveSkills * 2) return false;

                    return TrySplit(bred.Parent1, bred.Parent2, inherited, 0, [], [], memo);

                default:
                    return required.All(s => CanSupply(reference, s));
            }
        }

        private static bool TrySplit(
            IPalReference parent1,
            IPalReference parent2,
            List<ActiveSkill> skills,
            int index,
            List<ActiveSkill> from1,
            List<ActiveSkill> from2,
            Dictionary<(IPalReference, int), bool> memo
        )
        {
            if (index == skills.Count)
                return CanProvide(parent1, from1, memo) && CanProvide(parent2, from2, memo);

            var skill = skills[index];

            if (from1.Count < GameConstants.MaxInheritedActiveSkills && CanSupply(parent1, skill))
            {
                from1.Add(skill);
                if (TrySplit(parent1, parent2, skills, index + 1, from1, from2, memo)) return true;
                from1.RemoveAt(from1.Count - 1);
            }

            if (from2.Count < GameConstants.MaxInheritedActiveSkills && CanSupply(parent2, skill))
            {
                from2.Add(skill);
                if (TrySplit(parent1, parent2, skills, index + 1, from1, from2, memo)) return true;
                from2.RemoveAt(from2.Count - 1);
            }

            return false;
        }

        // every reference builds its own pool from the levels it's allowed to reach, so this is already level-aware
        private static bool CanSupply(IPalReference parent, ActiveSkill skill) =>
            parent.InheritedActiveSkills.Contains(skill);
    }
}
