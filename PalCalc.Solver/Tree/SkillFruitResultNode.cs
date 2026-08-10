using PalCalc.Model;
using PalCalc.Solver.PalReference;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PalCalc.Solver.Tree
{
    public class SkillFruitResultNode : IBreedingTreeNode
    {
        public SkillFruitResultNode(SkillFruitPalReference pref, IBreedingTreeNode inputNode)
        {
            PalRef = pref;
            this.inputNode = inputNode;
            this.operationNode = new SkillFruitOperationNode(pref);
        }

        private IBreedingTreeNode inputNode, operationNode;

        public IPalReference PalRef { get; }

        public IEnumerable<IBreedingTreeNode> Children => [inputNode, operationNode];

        public IEnumerable<string> DescriptionLines
        {
            get
            {
                var asFruit = PalRef as SkillFruitPalReference;
                yield return $"Result of {asFruit.Pal.Name} Skill Fruits";
                yield return $"{asFruit.Gender} gender w/ {asFruit.EffectivePassives.PassiveSkillListToString()}";
            }
        }

        public string Description => string.Join('\n', DescriptionLines);

        public IEnumerable<(IBreedingTreeNode, int)> TraversedTopDown(int currentDepth)
        {
            foreach (var n in operationNode.TraversedTopDown(currentDepth + 1))
                yield return n;

            yield return (this, currentDepth);

            foreach (var n in inputNode.TraversedTopDown(currentDepth + 1))
                yield return n;
        }
    }
}
