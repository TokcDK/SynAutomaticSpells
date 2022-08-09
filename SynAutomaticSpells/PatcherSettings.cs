using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
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
        [SynthesisDiskName("NpcModExclude")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded mods for npcs")]
        public HashSet<string> NpcModExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellExclude")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine excluded spells by editor id")]
        public HashSet<StringCompareSetting> SpellExclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("SpellModInclude")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Strings determine included mods for spells")]
        public HashSet<string> SpellModInclude = new();
        [SynthesisOrder]
        [SynthesisDiskName("EffectKeywordInclude")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine allowed magic effect keywords by editor id for spell types")]
        public HashSet<StringCompareSetting> EffectKeywordInclude = new();
    }
}
