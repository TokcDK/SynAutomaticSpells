using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using StringCompareSettings;
using System.Collections.Generic;

namespace SynAutomaticSpells
{
    public enum SearchMethod
    {
        OR,
        AND
    }

    public class PatcherSettings
    {
        [SynthesisOrder]
        [SynthesisDiskName("NPCInclude")]
        //[SynthesisSettingName("NPC Include")]
        [SynthesisTooltip("Strings determine included npcs by editor id")]
        public HashSet<StringCompareSetting> NpcInclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCExclude")]
        //[SynthesisSettingName("NPC Exclude")]
        [SynthesisTooltip("Strings determine excluded npcs by editor id")]
        public HashSet<StringCompareSetting> NpcExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("NpcKeywordExclude")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded npcs by editor id")]
        public HashSet<StringCompareSetting> NpcKeywordExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("NpcModNameExclude")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded mods for npcs")]
        public HashSet<StringCompareSetting> NpcModNameExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("NpcModExclude")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Determine excluded mods for npcs")]
        public HashSet<ModKey> NpcModExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellExclude")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine excluded spells by editor id")]
        public HashSet<StringCompareSetting> SpellExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellModNameInclude")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Strings determine included mods for spells")]
        public HashSet<StringCompareSetting> SpellModNameInclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellModInclude")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Strings determine included mods for spells")]
        public HashSet<ModKey> SpellModInclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("EffectKeywordInclude")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine allowed magic effect keywords by editor id for spell types")]
        public HashSet<StringCompareSetting> EffectKeywordInclude = new()
        {
            new StringCompareSetting(){Name="MAGIC", IgnoreCase=true, Compare= CompareType.StartsWith},
        };
        [SynthesisOrder]
        [SynthesisDiskName("IsSpellsFromSpelltomes")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Determine if need to get spells from spelltomes")]
        public bool IsSpellsFromSpelltomes = false;
        public DebugOptions Debug = new();
    }

    public class DebugOptions
    {
        [SynthesisOrder]
        [SynthesisDiskName("IsDebugNpc")]
        [SynthesisTooltip("Enable debug messages")]
        public bool IsDebugNpc = false;
        [SynthesisOrder]
        [SynthesisDiskName("IsDebugSpell")]
        [SynthesisTooltip("Enable debug messages")]
        public bool IsDebugSpell = false;
        [SynthesisOrder]
        [SynthesisDiskName("IsDebugSpellEffect")]
        [SynthesisTooltip("Enable debug messages")]
        public bool IsDebugSpellEffect = false;
        [SynthesisOrder]
        [SynthesisDiskName("NpcEDIDListForDebug")]
        [SynthesisTooltip("List of NPC Editor ID wich for which will be displayed debug messages")]
        public HashSet<StringCompareSetting> NpcEDIDListForDebug = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellEDIDListForDebug")]
        [SynthesisTooltip("List of Spell Editor ID wich for which will be displayed debug messages")]
        public HashSet<StringCompareSetting> SpellEDIDListForDebug = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpelEffectlEDIDListForDebug")]
        [SynthesisTooltip("List of Spell effect Editor ID wich for which will be displayed debug messages")]
        public HashSet<StringCompareSetting> SpelEffectlEDIDListForDebug = new();
    }
}
