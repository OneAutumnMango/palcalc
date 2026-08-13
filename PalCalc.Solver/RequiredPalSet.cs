using PalCalc.Solver.PalReference;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.Solver
{
    /// <summary>
    /// Owned pal instances which must appear somewhere in a result's breeding tree.
    /// </summary>
    /// <remarks>
    /// Membership is tracked as a bit mask so that partially-satisfied subtrees stay
    /// distinguishable from each other while the solver prunes candidates. Each reference
    /// carries its own mask (<see cref="IPalReference.RequiredPalMask"/>), seeded on the
    /// owned pals built by <c>InitialPalBuilder</c>.
    /// </remarks>
    public sealed class RequiredPalSet
    {
        public const int MaxRequiredPals = 16;

        public static RequiredPalSet Empty { get; } = new RequiredPalSet(null);

        private readonly Dictionary<string, ulong> masksByInstanceId;

        public RequiredPalSet(IEnumerable<string> instanceIds)
        {
            var ids = (instanceIds ?? [])
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .Take(MaxRequiredPals)
                .ToList();

            masksByInstanceId = ids
                .Select((id, index) => (id, mask: 1ul << index))
                .ToDictionary(e => e.id, e => e.mask);

            InstanceIds = ids;
            FullMask = ids.Count == 0 ? 0ul : (1ul << ids.Count) - 1;
        }

        public IReadOnlyList<string> InstanceIds { get; }
        public ulong FullMask { get; }
        public bool IsEmpty => FullMask == 0;

        public bool Contains(string instanceId) =>
            instanceId != null && masksByInstanceId.ContainsKey(instanceId);

        public ulong MaskFor(string instanceId) =>
            instanceId != null && masksByInstanceId.TryGetValue(instanceId, out var mask) ? mask : 0;

        public bool IsSatisfiedBy(IPalReference reference) =>
            IsEmpty || reference.RequiredPalMask == FullMask;

        public ulong MaskOf(IPalReference reference) =>
            IsEmpty ? 0 : reference.RequiredPalMask;
    }
}
