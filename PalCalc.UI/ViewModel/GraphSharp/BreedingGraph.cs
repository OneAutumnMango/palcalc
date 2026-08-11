using GraphSharp;
using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.Tree;
using PalCalc.UI.Model;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.ViewModel.GraphSharp
{
    public class BreedingEdge : TypedEdge<IBreedingTreeNodeViewModel>
    {
        public BreedingEdge(IBreedingTreeNodeViewModel parent, IBreedingTreeNodeViewModel child) : base(parent, child, EdgeTypes.Hierarchical) { }

        public override string ToString() => "";
    }

    public class BreedingGraph : HierarchicalGraph<IBreedingTreeNodeViewModel, BreedingEdge>
    {
        private BreedingGraph(CachedSaveGame source, GameSettings settings, BreedingTree tree, IEnumerable<ActiveSkill> targetActiveSkills)
        {
            Tree = tree;

            var requiredSkills = ResolveRequiredSkills(tree, targetActiveSkills);
            Nodes = tree.AllNodes
                .Select(p => IBreedingTreeNodeViewModel.FromModel(source, settings, p.Item1, requiredSkills.GetValueOrDefault(p.Item1) ?? []))
                .ToList();
        }

        // A skill only needs to be carried by one lineage; once a pal learns it naturally its parents don't need it.
        public static Dictionary<IBreedingTreeNode, List<ActiveSkill>> ResolveRequiredSkills(BreedingTree tree, IEnumerable<ActiveSkill> targetActiveSkills)
        {
            var db = PalDB.LoadEmbedded();
            var result = new Dictionary<IBreedingTreeNode, List<ActiveSkill>>();

            bool LearnsNaturally(IBreedingTreeNode node, ActiveSkill skill) =>
                db.BreedingSkills.GetValueOrDefault(skill.Name)?.Any(ls => ls.PalName == node.PalRef.Pal.Name) ?? false;

            void Visit(IBreedingTreeNode node, List<ActiveSkill> needed)
            {
                result[node] = needed;

                var children = node.Children.ToList();
                if (children.Count == 0) return;

                var childNeeds = children.ToDictionary(c => c, _ => new List<ActiveSkill>());

                // each parent of a bred pal can only pass down a limited number of skills
                var capped = node.PalRef is BredPalReference;
                bool HasRoom(IBreedingTreeNode c) => !capped || childNeeds[c].Count < GameConstants.MaxInheritedActiveSkills;

                foreach (var skill in needed.Where(s => !LearnsNaturally(node, s)))
                {
                    var candidates = children
                        .Where(c => (LearnsNaturally(c, skill) || c.PalRef.InheritedActiveSkills.Contains(skill)) && HasRoom(c))
                        .ToList();

                    var provider =
                        candidates.FirstOrDefault(c => LearnsNaturally(c, skill)) ??
                        candidates.FirstOrDefault();

                    if (provider != null) childNeeds[provider].Add(skill);
                }

                foreach (var child in children) Visit(child, childNeeds[child]);
            }

            Visit(tree.Root, (targetActiveSkills ?? []).Intersect(tree.Root.PalRef.InheritedActiveSkills).ToList());

            return result;
        }

        public BreedingTree Tree { get; private set; }
        public List<IBreedingTreeNodeViewModel> Nodes { get; }

        public bool NeedsRefresh => Nodes.OfType<IRefreshableNode>().Any(n => n.NeedsRefresh);

        public IBreedingTreeNodeViewModel NodeFor(IBreedingTreeNode pref) => Nodes.Single(n => n.Value == pref);

        public static BreedingGraph FromPalReference(CachedSaveGame source, GameSettings settings, IPalReference palRef, IEnumerable<ActiveSkill> targetActiveSkills)
        {
            var tree = new BreedingTree(palRef);
            var result = new BreedingGraph(source, settings, tree, targetActiveSkills);

            result.AddVertexRange(result.Nodes);

            // breeding tree is upside down relative to breeding direction
            foreach (var (child, _) in tree.AllNodes)
            {
                var childNode = result.NodeFor(child);
                foreach (var parent in child.Children)
                {
                    var parentNode = result.NodeFor(parent);
                    result.AddEdge(new BreedingEdge(parent: parentNode, child: childNode));
                    // parentNode (input) feeds into childNode (output), so parentNode's consumer is childNode
                    parentNode.SetConsumer(childNode);
                }
            }

            return result;
        }
    }
}
