using PalCalc.Model;
using PalCalc.Solver.PalReference;
using System.Collections.Concurrent;
using System.Collections.Frozen;
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
        private static readonly ConcurrentDictionary<(Pal, int), List<ActiveSkill>> naturalSkillsByPal = new();
        private static readonly ConcurrentDictionary<(Pal, int), ActiveSkillSet> naturalSkillMaskByPal = new();
        private static readonly ConcurrentDictionary<(PalDB, int), FrozenDictionary<Pal, ActiveSkillSet>> naturalSkillMasksByLevel = new();

        /// <summary>
        /// The skills this pal species learns on its own by leveling up to at most <paramref name="maxLevel"/>.
        /// </summary>
        public static List<ActiveSkill> NaturalSkillsOf(Pal pal, int maxLevel) =>
            naturalSkillsByPal.GetOrAdd((pal, maxLevel), static key =>
                PalDB.LoadEmbedded().BreedingSkills.Values
                    .SelectMany(ls => ls)
                    .Where(ls => ls.PalName == key.Item1.Name && ls.Level <= key.Item2)
                    .Select(ls => ls.Skill)
                    .Distinct()
                    .ToList()
            );

        public static ActiveSkillSet NaturalSkillMaskOf(Pal pal, int maxLevel) =>
            naturalSkillMaskByPal.GetOrAdd((pal, maxLevel), static key => ActiveSkillSet.Of(NaturalSkillsOf(key.Item1, key.Item2)));

        /// <summary>
        /// A prebuilt lookup of every known pal's natural skill mask, for use in hot loops where the
        /// max level is fixed.
        /// </summary>
        public static FrozenDictionary<Pal, ActiveSkillSet> NaturalSkillMasks(PalDB db, int maxLevel) =>
            naturalSkillMasksByLevel.GetOrAdd(
                (db, maxLevel),
                static key => key.Item1.Pals.ToFrozenDictionary(p => p, p => NaturalSkillMaskOf(p, key.Item2))
            );

        public static bool LearnsNaturally(Pal pal, ActiveSkill skill, int maxLevel) =>
            NaturalSkillMaskOf(pal, maxLevel).Contains(skill);

        /// <summary>
        /// Whether <paramref name="reference"/> can actually end up with all of <paramref name="required"/>
        /// without any single parent needing to pass down more than the game allows.
        /// </summary>
        public static bool CanProvide(IPalReference reference, IReadOnlyList<ActiveSkill> required) =>
            required.Count == 0 || CanProvide(reference, ActiveSkillSet.Of(required));

        public static bool CanProvide(IPalReference reference, ActiveSkillSet required)
        {
            if (required.IsEmpty) return true;

            // skill fruits add skills their input can't inherit, so they're handled before the shortcut below
            if (reference is SkillFruitPalReference || reference is SurgeryTablePalReference || reference is BredPalReference)
                return Evaluate(reference, required, null);

            return reference.InheritableActiveSkills.ContainsAll(required);
        }

        private static bool Evaluate(
            IPalReference reference,
            ActiveSkillSet required,
            Dictionary<(IPalReference, ActiveSkillSet), bool> memo
        )
        {
            if (required.IsEmpty) return true;

            switch (reference)
            {
                case SkillFruitPalReference fruit:
                    return Evaluate(fruit.Input, required.Except(ActiveSkillSet.Of(fruit.TaughtSkills)), memo);

                case SurgeryTablePalReference surgery:
                    return Evaluate(surgery.Input, required, memo);

                case BredPalReference bred:
                    if (!bred.InheritableActiveSkills.ContainsAll(required)) return false;

                    var inherited = required.Except(bred.NaturalActiveSkillSet);
                    if (inherited.IsEmpty) return true;
                    if (inherited.Count > 2 * GameConstants.MaxInheritedActiveSkills) return false;

                    memo ??= [];
                    var memoKey = (reference, inherited);
                    if (memo.TryGetValue(memoKey, out var cached)) return cached;

                    memo[memoKey] = false; // guard against re-entry

                    Span<int> indices = stackalloc int[2 * GameConstants.MaxInheritedActiveSkills];
                    var count = inherited.CopyIndicesTo(indices);
                    var result = TrySplit(
                        bred.Parent1,
                        bred.Parent2,
                        indices[..count],
                        0,
                        default, 0,
                        default, 0,
                        memo
                    );

                    memo[memoKey] = result;
                    return result;

                default:
                    return reference.InheritableActiveSkills.ContainsAll(required);
            }
        }

        private static bool TrySplit(
            IPalReference parent1,
            IPalReference parent2,
            ReadOnlySpan<int> skillIndices,
            int index,
            ActiveSkillSet from1,
            int count1,
            ActiveSkillSet from2,
            int count2,
            Dictionary<(IPalReference, ActiveSkillSet), bool> memo
        )
        {
            if (index == skillIndices.Length)
                return Evaluate(parent1, from1, memo) && Evaluate(parent2, from2, memo);

            var skillIndex = skillIndices[index];

            if (
                count1 < GameConstants.MaxInheritedActiveSkills &&
                parent1.InheritableActiveSkills.ContainsIndex(skillIndex) &&
                TrySplit(parent1, parent2, skillIndices, index + 1, from1.WithIndex(skillIndex), count1 + 1, from2, count2, memo)
            ) return true;

            if (
                count2 < GameConstants.MaxInheritedActiveSkills &&
                parent2.InheritableActiveSkills.ContainsIndex(skillIndex) &&
                TrySplit(parent1, parent2, skillIndices, index + 1, from1, count1, from2.WithIndex(skillIndex), count2 + 1, memo)
            ) return true;

            return false;
        }
    }
}
