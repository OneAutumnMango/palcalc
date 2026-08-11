using Newtonsoft.Json;
using PalCalc.Model;
using PalCalc.Solver.PalReference;
using PalCalc.Solver.PalReference.Properties;
using PalCalc.Solver.Tree;
using PalCalc.UI.ViewModel.GraphSharp;
using PalCalc.UI.ViewModel.Solver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PalCalc.UI.Model
{
    /// <summary>
    /// Serializes a breeding result into a shareable JSON or plain-text tree.
    /// </summary>
    public static class BreedingTreeExporter
    {
        public static string ToJson(BreedingResultViewModel result)
        {
            if (result?.Graph == null) return null;

            var root = result.Graph.Tree.Root;
            var required = BreedingGraph.ResolveRequiredSkills(result.Graph.Tree, result.TargetActiveSkills);
            var final = result.DisplayedResult;

            var export = new Dictionary<string, object>
            {
                ["target"] = new Dictionary<string, object>
                {
                    ["pal"] = final.Pal.Name,
                    ["passives"] = final.EffectivePassives.Select(p => p.Name).ToList(),
                    ["activeSkills"] = result.TargetActiveSkills.Select(s => s.Name).ToList(),
                },
                ["summary"] = new Dictionary<string, object>
                {
                    ["timeEstimate"] = final.BreedingEffort.TimeSpanSecondsStr(),
                    ["breedingSteps"] = final.NumTotalBreedingSteps,
                    ["eggs"] = final.NumTotalEggs,
                    ["wildPals"] = final.NumTotalWildPals,
                    ["goldCost"] = final.TotalCost,
                },
                ["tree"] = BuildNode(root, required),
            };

            return JsonConvert.SerializeObject(export, Formatting.Indented);
        }

        public static string ToText(BreedingResultViewModel result)
        {
            if (result?.Graph == null) return null;

            var required = BreedingGraph.ResolveRequiredSkills(result.Graph.Tree, result.TargetActiveSkills);
            var final = result.DisplayedResult;

            var sb = new StringBuilder();
            sb.AppendLine($"{final.Pal.Name} - {Join(final.EffectivePassives.Select(p => p.Name))}");
            if (result.TargetActiveSkills.Count > 0)
                sb.AppendLine($"Active skills: {Join(result.TargetActiveSkills.Select(s => s.Name))}");
            sb.AppendLine($"~{final.BreedingEffort.TimeSpanSecondsStr()}, {final.NumTotalBreedingSteps} breeding steps, ~{final.NumTotalEggs} eggs, {final.NumTotalWildPals} wild pals");
            sb.AppendLine();

            AppendText(sb, result.Graph.Tree.Root, required, 0);
            return sb.ToString();
        }

        private static void AppendText(StringBuilder sb, IBreedingTreeNode node, IReadOnlyDictionary<IBreedingTreeNode, List<ActiveSkill>> required, int depth)
        {
            var pref = node.PalRef;
            var parts = new List<string>
            {
                $"[{SourceOf(pref)}] {pref.Pal.Name} ({GenderOf(pref)})",
                Join(pref.ActualPassives.Select(p => p.Name)),
            };

            var req = required.GetValueOrDefault(node);
            if (req != null && req.Count > 0) parts.Add($"skills: {Join(req.Select(s => s.Name))}");

            if (pref is SurgeryTablePalReference stpr) parts.Add($"surgery: {Join(stpr.Operations.Select(DescribeOperation))}");
            if (pref is SkillFruitPalReference sfpr) parts.Add($"skill fruits: {Join(sfpr.TaughtSkills.Select(s => s.Name))}");
            if (pref is BredPalReference bpr) parts.Add($"~{bpr.AvgRequiredBreedings} eggs");
            if (pref is OwnedPalReference or CompositeOwnedPalReference) parts.Add(pref.Location.ToString());

            sb.Append(new string(' ', depth * 2)).AppendLine(string.Join(" | ", parts.Where(p => !string.IsNullOrEmpty(p))));

            foreach (var input in InputsOf(node))
                AppendText(sb, input, required, depth + 1);
        }

        private static Dictionary<string, object> BuildNode(IBreedingTreeNode node, IReadOnlyDictionary<IBreedingTreeNode, List<ActiveSkill>> required)
        {
            var pref = node.PalRef;
            var obj = new Dictionary<string, object>
            {
                ["source"] = SourceOf(pref),
                ["pal"] = pref.Pal.Name,
                ["gender"] = GenderOf(pref),
                ["passives"] = pref.ActualPassives.Select(p => p.Name).ToList(),
            };

            var req = required.GetValueOrDefault(node);
            if (req != null && req.Count > 0) obj["requiredActiveSkills"] = req.Select(s => s.Name).ToList();

            if (pref.IVs.HP != IV_Value.Random || pref.IVs.Attack != IV_Value.Random || pref.IVs.Defense != IV_Value.Random)
            {
                obj["ivs"] = new Dictionary<string, string>
                {
                    ["hp"] = pref.IVs.HP.ToString(),
                    ["attack"] = pref.IVs.Attack.ToString(),
                    ["defense"] = pref.IVs.Defense.ToString(),
                };
            }

            switch (pref)
            {
                case OwnedPalReference or CompositeOwnedPalReference:
                    obj["location"] = pref.Location.ToString();
                    break;

                case BredPalReference bpr:
                    obj["avgEggs"] = bpr.AvgRequiredBreedings;
                    break;

                case SurgeryTablePalReference stpr:
                    obj["surgery"] = stpr.Operations.Select(DescribeOperation).ToList();
                    break;

                case SkillFruitPalReference sfpr:
                    obj["skillFruits"] = sfpr.TaughtSkills.Select(s => s.Name).ToList();
                    break;
            }

            var inputs = InputsOf(node).Select(n => BuildNode(n, required)).ToList();
            if (inputs.Count > 0) obj[pref is BredPalReference ? "parents" : "input"] = inputs;

            return obj;
        }

        // operation nodes are display-only, their contents are already merged into the owning node
        private static IEnumerable<IBreedingTreeNode> InputsOf(IBreedingTreeNode node) =>
            node.Children.Where(c => c is not SurgeryOperationNode && c is not SkillFruitOperationNode);

        private static string SourceOf(IPalReference pref) =>
            pref switch
            {
                BredPalReference => "Bred",
                WildPalReference => "Wild",
                OwnedPalReference or CompositeOwnedPalReference => "Owned",
                SurgeryTablePalReference => "Surgery",
                SkillFruitPalReference => "SkillFruit",
                _ => pref.GetType().Name,
            };

        private static string GenderOf(IPalReference pref) =>
            pref.Gender switch
            {
                PalGender.MALE => "Male",
                PalGender.FEMALE => "Female",
                PalGender.OPPOSITE_WILDCARD => "Opposite of sibling",
                _ => "Either",
            };

        private static string DescribeOperation(ISurgeryOperation op) =>
            op switch
            {
                AddPassiveSurgeryOperation add => $"add {add.AddedPassive.Name}",
                ReplacePassiveSurgeryOperation rep => $"replace {rep.RemovedPassive.Name} with {rep.AddedPassive.Name}",
                _ => op.ToString(),
            };

        private static string Join(IEnumerable<string> values) => string.Join(", ", values);
    }
}
