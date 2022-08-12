using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using StringCompareSettings;
using System.Collections.Generic;

namespace SynAutomaticSpells
{
    public class PatcherSettings
    {
        [SynthesisOrder]
        [SynthesisTooltip("Native settings of the patcher containing more convenient way to add items to lists")]
        public NativeSettings NativeSettings = new();

        [SynthesisOrder]
        [SynthesisTooltip("ASIS like options, can be read from ASIS AutomaticSpell.ini if exist or entered manually here")]
        public ASISOptions ASIS = new();

        [SynthesisOrder]
        [SynthesisTooltip("Debug options")]
        public DebugOptions Debug = new();
    }
    public class NativeSettings
    {
        [SynthesisOrder]
        [SynthesisDiskName("GetSpellsFromSpelltomes")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Determine if need to get spells from avalaible spelltomes instead of spells list.\nCan prevent more unvanted spells to be added.")]
        public bool GetSpellsFromSpelltomes = true;
        [SynthesisOrder]
        [SynthesisDiskName("NpcModExclude")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Determine excluded mods for npcs")]
        public HashSet<ModKey> NpcModExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellModInclude")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Determine included mods for spells")]
        public HashSet<ModKey> SpellModInclude = new();
    }

    public class ASISOptions
    {
        [SynthesisOrder]
        [SynthesisDiskName("NPCInclusions")]
        //[SynthesisSettingName("NPC Include")]
        [SynthesisTooltip("Strings determine included npcs by editor id")]
        public HashSet<StringCompareSettingGroup> NPCInclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCExclusions")]
        //[SynthesisSettingName("NPC Exclude")]
        [SynthesisTooltip("Strings determine excluded npcs by editor id")]
        public HashSet<StringCompareSettingGroup> NPCExclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCKeywordExclusions")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded npcs by editor id")]
        public HashSet<StringCompareSettingGroup> NPCKeywordExclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCModExclusions")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded mods for npcs")]
        public HashSet<StringCompareSettingGroup> NPCModExclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellExclusons")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine excluded spells by editor id")]
        public HashSet<StringCompareSettingGroup> SpellExclusons = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellModNInclusions")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Strings determine included mods for spells")]
        public HashSet<StringCompareSettingGroup> SpellModNInclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("EffectKeywordInclusions")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine allowed magic effect keywords by editor id for spell types")]
        public HashSet<StringCompareSettingGroup> EffectKeywordInclusions = new()
        {
            new StringCompareSettingGroup()
            { 
                StringsList=new List<StringCompareSetting>()
                { 
                    new StringCompareSetting() { Name = "MAGIC", IgnoreCase = true, Compare = CompareType.StartsWith } 
                } 
            }
        };
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
        public HashSet<StringCompareSettingGroup> NpcEDIDListForDebug = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellEDIDListForDebug")]
        [SynthesisTooltip("List of Spell Editor ID wich for which will be displayed debug messages")]
        public HashSet<StringCompareSettingGroup> SpellEDIDListForDebug = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpelEffectlEDIDListForDebug")]
        [SynthesisTooltip("List of Spell effect Editor ID wich for which will be displayed debug messages")]
        public HashSet<StringCompareSettingGroup> SpelEffectlEDIDListForDebug = new();
    }
}
