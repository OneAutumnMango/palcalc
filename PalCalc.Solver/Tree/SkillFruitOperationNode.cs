using PalCalc.Solver.PalReference;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.Solver.Tree
{
    public class SkillFruitOperationNode(SkillFruitPalReference pref) : IBreedingTreeNode
    {
        public IPalReference PalRef => pref;

        public IEnumerable<IBreedingTreeNode> Children => [];

        public IEnumerable<string> DescriptionLines => [
            $"Skill fruits for {pref.Pal.Name}",
            .. pref.TaughtSkills.Select(s => s.Name)
        ];

        public string Description => string.Join('\n', DescriptionLines);

        public IEnumerable<(IBreedingTreeNode, int)> TraversedTopDown(int currentDepth)
        {
            yield return (this, currentDepth);
        }
    }
}
