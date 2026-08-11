using PalCalc.Model;
using PalCalc.Solver;
using PalCalc.UI.Localization;
using PalCalc.UI.Model;
using PalCalc.UI.ViewModel.Mapped;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI
{
    internal static class ModelExtensions
    {
        public static ILocalizedText ShortLabel(this LocationType locType) =>
            locType switch
            {
                LocationType.Palbox => LocalizationCodes.LC_PAL_LOC_PALBOX.Bind(),
                LocationType.Base => LocalizationCodes.LC_PAL_LOC_BASE.Bind(),
                LocationType.PlayerParty => LocalizationCodes.LC_PAL_LOC_PARTY.Bind(),
                LocationType.ViewingCage => LocalizationCodes.LC_PAL_LOC_VIEWING_CAGE.Bind(),
                LocationType.Custom => LocalizationCodes.LC_PAL_LOC_CUSTOM.Bind(),
                LocationType.DimensionalPalStorage => LocalizationCodes.LC_PAL_LOC_DPS_SHORT.Bind(),
                LocationType.GlobalPalStorage => LocalizationCodes.LC_PAL_LOC_GPS_SHORT.Bind(),
                _ => throw new NotImplementedException()
            };

        public static ILocalizedText FullLabel(this LocationType locType) =>
            locType switch
            {
                LocationType.Palbox => LocalizationCodes.LC_PAL_LOC_PALBOX.Bind(),
                LocationType.Base => LocalizationCodes.LC_PAL_LOC_BASE.Bind(),
                LocationType.PlayerParty => LocalizationCodes.LC_PAL_LOC_PARTY.Bind(),
                LocationType.ViewingCage => LocalizationCodes.LC_PAL_LOC_VIEWING_CAGE.Bind(),
                LocationType.Custom => LocalizationCodes.LC_PAL_LOC_CUSTOM.Bind(),
                LocationType.DimensionalPalStorage => LocalizationCodes.LC_PAL_LOC_DPS_FULL.Bind(),
                LocationType.GlobalPalStorage => LocalizationCodes.LC_PAL_LOC_GPS_FULL.Bind(),
                _ => throw new NotImplementedException()
            };

        public static ILocalizedText Label(this PalGender gender) =>
            gender switch
            {
                PalGender.FEMALE => LocalizationCodes.LC_COMMON_GENDER_FEMALE.Bind(),
                PalGender.MALE => LocalizationCodes.LC_COMMON_GENDER_MALE.Bind(),
                PalGender.WILDCARD => LocalizationCodes.LC_COMMON_GENDER_WILDCARD.Bind(),
                PalGender.OPPOSITE_WILDCARD => LocalizationCodes.LC_COMMON_GENDER_OPPOSITE_WILDCARD.Bind(),
                PalGender.NONE => LocalizationCodes.LC_COMMON_GENDER_NONE.Bind(),
                _ => throw new NotImplementedException()
            };

        public static PassiveSkillsPreset ToPreset(this PalSpecifierViewModel spec) =>
            new()
            {
                Passive1InternalName = spec.RequiredPassives.Passive1?.ModelObject?.InternalName,
                Passive2InternalName = spec.RequiredPassives.Passive2?.ModelObject?.InternalName,
                Passive3InternalName = spec.RequiredPassives.Passive3?.ModelObject?.InternalName,
                Passive4InternalName = spec.RequiredPassives.Passive4?.ModelObject?.InternalName,

                OptionalPassive1InternalName = spec.OptionalPassives.Passive1?.ModelObject?.InternalName,
                OptionalPassive2InternalName = spec.OptionalPassives.Passive2?.ModelObject?.InternalName,
                OptionalPassive3InternalName = spec.OptionalPassives.Passive3?.ModelObject?.InternalName,
                OptionalPassive4InternalName = spec.OptionalPassives.Passive4?.ModelObject?.InternalName,
            };

        public static ActiveSkillsPreset ToActiveSkillsPreset(this PalSpecifierViewModel spec) =>
            new()
            {
                ActiveSkill1InternalName = spec.TargetActiveSkills.ActiveSkill1?.ModelObject?.InternalName,
                ActiveSkill2InternalName = spec.TargetActiveSkills.ActiveSkill2?.ModelObject?.InternalName,
                ActiveSkill3InternalName = spec.TargetActiveSkills.ActiveSkill3?.ModelObject?.InternalName,
                ActiveSkill4InternalName = spec.TargetActiveSkills.ActiveSkill4?.ModelObject?.InternalName,
                ActiveSkill5InternalName = spec.TargetActiveSkills.ActiveSkill5?.ModelObject?.InternalName,
                ActiveSkill6InternalName = spec.TargetActiveSkills.ActiveSkill6?.ModelObject?.InternalName,
            };

        public static PalTargetConfig ToTargetConfig(this PalSpecifierViewModel spec) =>
            new()
            {
                Pal = spec.TargetPal?.ModelObject?.InternalName,
                RequiredPassives = spec.RequiredPassives.AsModelEnumerable().Select(p => p.InternalName).ToList(),
                OptionalPassives = spec.OptionalPassives.AsModelEnumerable().Select(p => p.InternalName).ToList(),
                ActiveSkills = spec.TargetActiveSkills.AsModelEnumerable().Select(s => s.InternalName).ToList(),
                RequiredGender = spec.RequiredGender?.Value ?? PalGender.WILDCARD,
                MinIV_HP = spec.MinIv_HP,
                MinIV_Attack = spec.MinIv_Attack,
                MinIV_Defense = spec.MinIv_Defense,
            };

        public static void ApplyTo(this PalTargetConfig config, PalSpecifierViewModel spec)
        {
            var db = PalDB.LoadEmbedded();

            spec.TargetPal = PalViewModel.Make(config.Pal.InternalToPal(db));

            spec.RequiredPassives.CopyFrom(new(config.RequiredPassives.Select(n => n.InternalToStandardPassive(db))));
            spec.OptionalPassives.CopyFrom(new(config.OptionalPassives.Select(n => n.InternalToStandardPassive(db))));
            spec.TargetActiveSkills.CopyFrom(new(config.ActiveSkills.Select(n => n.ToActive(db))));

            spec.RequiredGender = PalGenderViewModel.Make(config.RequiredGender);
            spec.MinIv_HP = config.MinIV_HP;
            spec.MinIv_Attack = config.MinIV_Attack;
            spec.MinIv_Defense = config.MinIV_Defense;
        }
    }
}
