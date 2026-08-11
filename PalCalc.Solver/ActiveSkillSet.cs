using PalCalc.Model;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PalCalc.Solver
{
    /// <summary>
    /// A set of active skills stored as a bit mask over <see cref="ActiveSkill.InheritanceIndex"/>.
    /// Skills which can't be inherited through breeding have no index and are never present.
    /// </summary>
    /// <remarks>
    /// Set operations on inheritable skills happen for nearly every candidate the solver produces,
    /// which made list/hash-set based handling a measurable share of total runtime.
    /// </remarks>
    public readonly struct ActiveSkillSet : IEquatable<ActiveSkillSet>
    {
        public const int Capacity = 128;

        private readonly ulong low;
        private readonly ulong high;

        private ActiveSkillSet(ulong low, ulong high)
        {
            this.low = low;
            this.high = high;
        }

        public static ActiveSkillSet Of(IEnumerable<ActiveSkill> skills)
        {
            ulong low = 0, high = 0;
            foreach (var skill in skills)
            {
                var index = skill.InheritanceIndex;
                if (index < 0) continue;
                if (index >= Capacity)
                    throw new ArgumentOutOfRangeException(nameof(skills), $"Active skill '{skill.InternalName}' exceeds the {Capacity} inheritable skill limit.");

                if (index < 64) low |= 1UL << index;
                else high |= 1UL << (index - 64);
            }

            return new(low, high);
        }

        public bool IsEmpty => (low | high) == 0;

        public int Count => BitOperations.PopCount(low) + BitOperations.PopCount(high);

        public bool Contains(ActiveSkill skill) => ContainsIndex(skill.InheritanceIndex);

        public bool ContainsIndex(int index)
        {
            if (index < 0) return false;
            return index < 64
                ? (low & (1UL << index)) != 0
                : (high & (1UL << (index - 64))) != 0;
        }

        public bool ContainsAll(ActiveSkillSet other) =>
            (low & other.low) == other.low && (high & other.high) == other.high;

        public ActiveSkillSet WithIndex(int index) =>
            index < 64
                ? new(low | (1UL << index), high)
                : new(low, high | (1UL << (index - 64)));

        public ActiveSkillSet Except(ActiveSkillSet other) => new(low & ~other.low, high & ~other.high);

        public static ActiveSkillSet operator |(ActiveSkillSet a, ActiveSkillSet b) => new(a.low | b.low, a.high | b.high);
        public static ActiveSkillSet operator &(ActiveSkillSet a, ActiveSkillSet b) => new(a.low & b.low, a.high & b.high);

        /// <summary>
        /// Writes the index of each contained skill into <paramref name="destination"/> and returns how many were written.
        /// </summary>
        public int CopyIndicesTo(Span<int> destination)
        {
            var count = 0;

            var bits = low;
            while (bits != 0)
            {
                destination[count++] = BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;
            }

            bits = high;
            while (bits != 0)
            {
                destination[count++] = 64 + BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;
            }

            return count;
        }

        public List<ActiveSkill> ToList()
        {
            var result = new List<ActiveSkill>(Count);
            Span<int> indices = stackalloc int[Capacity];
            var count = CopyIndicesTo(indices);
            for (int i = 0; i < count; i++)
                result.Add(ActiveSkillIndex.SkillAt(indices[i]));

            return result;
        }

        public bool Equals(ActiveSkillSet other) => low == other.low && high == other.high;
        public override bool Equals(object obj) => obj is ActiveSkillSet other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(low, high);

        public static bool operator ==(ActiveSkillSet a, ActiveSkillSet b) => a.Equals(b);
        public static bool operator !=(ActiveSkillSet a, ActiveSkillSet b) => !a.Equals(b);
    }

    internal static class ActiveSkillIndex
    {
        private static ActiveSkill[] byIndex;

        public static ActiveSkill SkillAt(int index)
        {
            if (byIndex == null)
            {
                var db = PalDB.LoadEmbedded();
                var built = new ActiveSkill[ActiveSkillSet.Capacity];
                foreach (var skill in db.ActiveSkills)
                    if (skill.InheritanceIndex >= 0)
                        built[skill.InheritanceIndex] = skill;

                byIndex = built;
            }

            return byIndex[index];
        }
    }
}
