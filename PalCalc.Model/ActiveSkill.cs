using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Model
{
    public class ActiveSkill
    {
        public ActiveSkill(string name, string internalName, PalElement element)
        {
            Name = name;
            InternalName = internalName;
            Element = element;
            ElementInternalName = element?.InternalName;
        }

        public string Name { get; private set; }

        [JsonConverter(typeof(CaseInsensitiveStringDictionaryConverter<string>))]
        public Dictionary<string, string> LocalizedNames { get; set; }

        // (avoid serializing a whole copy of each element for a skill, the proper element objects will be resolved
        // by `PalDBSerializer`)

        [JsonProperty]
        internal string ElementInternalName { get; private set; }
        [JsonIgnore]
        public PalElement Element { get; internal set; }

        public string InternalName { get; private set; }

        public bool CanInherit { get; set; }

        public bool HasSkillFruit { get; set; }

        public int Power { get; set; }
        public float CooldownSeconds { get; set; }

        /// <summary>
        /// Dense index assigned to skills which can be inherited through breeding, used for bit-set
        /// representations of skill collections. -1 for skills which can't be inherited.
        /// </summary>
        [JsonIgnore]
        public int InheritanceIndex { get; internal set; } = -1;

        public override string ToString() => Name;

        // (these are used as set/dictionary keys throughout the solver, so the string hash is cached)
        private int hashCode;

        public override bool Equals(object obj) =>
            ReferenceEquals(this, obj) || (obj as ActiveSkill)?.InternalName == InternalName;

        public override int GetHashCode() => hashCode != 0 ? hashCode : (hashCode = InternalName.GetHashCode());
    }

    public class UnrecognizedActiveSkill : ActiveSkill
    {
        public UnrecognizedActiveSkill(string internalName) : base($"'{internalName}' (unrecognized)", internalName, null) { }
    }
}
