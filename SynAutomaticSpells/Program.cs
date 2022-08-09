using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using StringCompareSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SynAutomaticSpells
{
    public class Program
    {
        static Lazy<PatcherSettings> Settings = null!;
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("PatcherSettings", "settings.json", out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynAutomaticSpells.esp")
                .Run(args);
        }

        public class NPCInfo
        {
            public readonly Dictionary<FormKey, List<IKeywordGetter>> HandEffects = new();
            public readonly Dictionary<Skill, uint> SkillLevels = new();

            internal void AddEquipTypeKeywords(IFormLinkNullableGetter<IEquipTypeGetter> equipSlot, IKeywordGetter keyword)
            {
                if (!HandEffects.ContainsKey(equipSlot.FormKey))
                {
                    HandEffects.Add(equipSlot.FormKey, new() { keyword });
                }
                else
                {
                    HandEffects[equipSlot.FormKey].Add(keyword);
                }
            }
        }

        public class SpellInfo
        {
            public ISpellGetter? Spell;
            public Skill? MainSkill;
            public IMagicEffectGetter? MainEffect;
            public Dictionary<Skill, uint> RequiredSkills = new();
        }

        static IPatcherState<ISkyrimMod, ISkyrimModGetter>? State;
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            State = state;

            SearchAndTryReadASISIni();

            SetAsisAutoSpellsIniValuesToSettings();

            // get spell infos
            Console.WriteLine("Get spells info..");
            var spellInfoList = GetSpellInfoList();
            Console.WriteLine("Get npc info..");
            var npcsInfoList = GetNPCInfoList();

            Console.WriteLine("Add spells to npc if any valid..");
            foreach (var npcInfo in npcsInfoList)
            {
                // skip invalid
                //if (npcGetter == null || string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                //if (npcGetter.ActorEffect == null) continue;
                //if (npcGetter.ActorEffect.Count == 0) continue;
                //if (!npcsInfoList.ContainsKey(npcGetter.FormKey)) continue;
                //if (npcGetter.ActorEffect == null || npcGetter.ActorEffect.Count == 0) continue;

                //var npcInfo = npcsInfoList[npcGetter.FormKey];

                var spellsToAdd = new List<ISpellGetter>();
                foreach (var spellInfo in spellInfoList)
                {
                    if (!CanGetTheSpell(npcInfo.Value, spellInfo)) continue;
                    if (npcInfo.Key.ActorEffect!.Contains(spellInfo.Key)) continue;

                    spellsToAdd.Add(spellInfo.Key);
                }

                var addedCount = spellsToAdd.Count;
                if (addedCount == 0) continue;

                Console.WriteLine($"Add {addedCount} spells for '{npcInfo.Key.EditorID}'");
                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcInfo.Key);
                foreach (var spellToAdd in spellsToAdd) npc.ActorEffect!.Add(spellToAdd);
            }
        }

        private static void SetAsisAutoSpellsIniValuesToSettings()
        {
            Console.WriteLine("Set Asis autospells ini values into settings..");

            foreach (var v in AutomaticSpellsIniParams!["NPCInclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.NpcInclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["NPCExclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Contains
                };

                var list = Settings.Value.NpcExclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["SPELLEXCLUSIONSCONTAINS"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Contains
                };

                var list = Settings.Value.SpellExclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["SPELLEXCLUSIONSSTARTSWITH"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.SpellExclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["EffectKeywordPrefixes"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.EffectKeywordInclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["NPCKeywordExclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.NpcKeywordExclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["NPCModExclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.NpcModNameExclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["spellModInclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.SpellModNameInclude;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }

            AutomaticSpellsIniParams = null;
        }

        public static Dictionary<string, HashSet<string>>? AutomaticSpellsIniParams = new()
        {
            { "NPCInclusions", new HashSet<string>() },
            { "NPCExclusions", new HashSet<string>() },
            { "SPELLEXCLUSIONSCONTAINS", new HashSet<string>() },
            { "SPELLEXCLUSIONSSTARTSWITH", new HashSet<string>() },
            { "EffectKeywordPrefixes", new HashSet<string>() },
            { "NPCKeywordExclusions", new HashSet<string>() },
            { "NPCModExclusions", new HashSet<string>() },
            { "spellModInclusions", new HashSet<string>() },
        };

        private static void SearchAndTryReadASISIni()
        {
            var iniPath = Path.Combine(State!.DataFolderPath, "SkyProc Patchers", "ASIS", "AutomaticSpells.ini");
            if (!File.Exists(iniPath)) return;

            // read AutomaticSpells ini parameters into settings
            Console.WriteLine("Found ASIS 'AutomaticSpells.ini'. Trying to read..");

            Dictionary<string, HashSet<string>> iniSections = new();
            iniSections.ReadIniSectionValuesFrom(iniPath);

            var keys = new HashSet<string>(AutomaticSpellsIniParams!.Keys);
            int iniValuesCount = 0;
            int iniSectionsCount = 0;
            foreach (var key in keys)
            {
                if (!iniSections.ContainsKey(key)) continue;

                var v = iniSections[key];
                AutomaticSpellsIniParams[key] = v;
                iniValuesCount += v.Count;
                iniSectionsCount++;
            }

            Console.WriteLine($"Added {iniSectionsCount} sections and {iniValuesCount} values from 'AutomaticSpells.ini'");
        }

        private static Dictionary<INpcGetter, NPCInfo> GetNPCInfoList()
        {
            bool useNpcModExclude = Settings.Value.NpcModExclude.Count > 0;
            bool useNpcModExcludeByName = Settings.Value.NpcModExclude.Count > 0;
            bool useNpcExclude = Settings.Value.NpcExclude.Count > 0;
            bool useNpcInclude = Settings.Value.NpcInclude.Count > 0;
            bool useNpcKeywordExclude = Settings.Value.NpcKeywordExclude.Count > 0;
            var npcInfoList = new Dictionary<INpcGetter, NPCInfo>();
            foreach (var npcGetterContext in State!.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                // skip invalid
                if (useNpcModExclude && Settings.Value.NpcModExclude.Contains(npcGetterContext.ModKey)) continue;
                if (useNpcModExcludeByName && npcGetterContext.ModKey.FileName.String.HasAnyFromList(Settings.Value.NpcModNameExclude)) continue;
                var npcGetter = npcGetterContext.Record;
                if (npcGetter == null) continue;
                if (npcGetter.ActorEffect == null) continue;
                if (string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                if (useNpcExclude && npcGetter.EditorID.HasAnyFromList(Settings.Value.NpcExclude)) continue;
                if (useNpcInclude && !npcGetter.EditorID.HasAnyFromList(Settings.Value.NpcInclude)) continue;
                if (useNpcKeywordExclude && npcGetter.Keywords != null)
                {
                    bool skip = false;
                    foreach (var keywordGetterFormLink in npcGetter.Keywords)
                    {
                        if (!keywordGetterFormLink.TryResolve(State!.LinkCache, out var keywordGeter)) continue;
                        if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                        if (!keywordGeter.EditorID.HasAnyFromList(Settings.Value.NpcKeywordExclude)) continue;

                        skip = true;
                        break;
                    }

                    if (skip) continue;
                }

                NPCInfo? npcInfo = GetNPCInfo(npcGetter);
                if (npcInfo == null) continue;

                npcInfoList.Add(npcGetter, npcInfo);
            }

            return npcInfoList;
        }

        private static NPCInfo? GetNPCInfo(INpcGetter npcGetter)
        {
            INpcGetter? unTemplatedNpcSpells = UnTemplate(npcGetter, NpcConfiguration.TemplateFlag.SpellList);
            if (unTemplatedNpcSpells == null) return null;

            INpcGetter? unTemplatedNpcStats = UnTemplate(npcGetter, NpcConfiguration.TemplateFlag.Stats);
            if (unTemplatedNpcStats == null) return null;

            var npcInfo = new NPCInfo();

            // get effects per equipSlot
            var spells = unTemplatedNpcSpells.ActorEffect;
            var npcSpellEffectsInfo = new Dictionary<ISpellGetter, IMagicEffectGetter>();
            foreach (var spellRecordGetterFormLink in spells!)
            {
                // reconvert from ispellrecordgetter to ispellrecord
                //if (!spellRecordGetterFormLink.TryResolve(State!.LinkCache, out var spellRecordGetter)) continue;
                var spellGetterFormlink = new FormLink<ISpellGetter>(spellRecordGetterFormLink.FormKey);
                if (spellGetterFormlink == null) continue;
                if (!spellGetterFormlink.TryResolve(State!.LinkCache, out var spellGetter)) continue;

                if (spellGetter.BaseCost <= 0) continue;

                int curCost = -1;
                IMagicEffectGetter? mainEffect = null;
                foreach (var mEffect in spellGetter.Effects)
                {
                    if (!mEffect.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;

                    float mag = mEffect.Data!.Magnitude;
                    if (mag < 1) mag = 1;

                    int dur = mEffect.Data.Duration;
                    if (dur == 0) dur = 10;

                    var cost = CalcCost(spellGetter.BaseCost, mag, dur);
                    if (cost > curCost)
                    {
                        curCost = cost;
                        mainEffect = effect;
                    }
                }

                if (mainEffect != null) npcSpellEffectsInfo.Add(spellGetter, mainEffect);
            }

            if (npcSpellEffectsInfo.Count == 0) return null;

            foreach (var entry in npcSpellEffectsInfo)
            {
                if (entry.Value == null) continue;
                if (entry.Value.Keywords == null) continue;

                var equipType = entry.Key.EquipmentType;
                foreach (var keywordGetterFormLink in entry.Value!.Keywords!)
                {
                    if (!keywordGetterFormLink.TryResolve(State!.LinkCache, out var keywordGeter)) continue;
                    if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                    if (keywordGeter.EditorID.HasAnyFromList(Settings.Value.EffectKeywordInclude)) npcInfo.AddEquipTypeKeywords(equipType, keywordGeter);
                }
            }

            // add skill level values of the npc
            List<Skill> skills = new()
            {
                Skill.Alteration,
                Skill.Conjuration,
                Skill.Destruction,
                Skill.Illusion,
                Skill.Restoration
            };
            foreach (Skill skill in skills)
            {
                npcInfo.SkillLevels.Add(skill, (uint)(unTemplatedNpcStats.PlayerSkills!.SkillValues[skill] + unTemplatedNpcStats.PlayerSkills.SkillOffsets[skill]));
            }

            return npcInfo;
        }

        private static INpcGetter? UnTemplate(INpcGetter npcGetter, NpcConfiguration.TemplateFlag templateFlag)
        {
            INpcGetter? untemplatedNpc = npcGetter;
            while (untemplatedNpc.Configuration.TemplateFlags.HasFlag(templateFlag))
            {
                if (untemplatedNpc.Template == null
                    || untemplatedNpc.Template.IsNull
                    || untemplatedNpc.Template.FormKey.IsNull
                    || !untemplatedNpc!.Template.TryResolve(State!.LinkCache, out var templateNpcSpawnGetter)
                    )
                {
                    untemplatedNpc = null;
                    break;
                }

                var templateNpcGetterFormlink = new FormLink<INpcGetter>(templateNpcSpawnGetter.FormKey);
                if (templateNpcGetterFormlink == null)
                {
                    untemplatedNpc = null;
                    break;
                }

                if (!templateNpcGetterFormlink.TryResolve(State!.LinkCache, out var templateNpcGetter))
                {
                    untemplatedNpc = null;
                    break;
                }

                untemplatedNpc = templateNpcGetter;
            }

            return untemplatedNpc;
        }

        private static Dictionary<ISpellGetter, SpellInfo> GetSpellInfoList()
        {
            bool useModIncludebyModkey = Settings.Value.SpellModInclude.Count > 0;
            bool useModIncludeByName = Settings.Value.SpellModNameInclude.Count > 0;
            bool useSpellExclude = Settings.Value.SpellExclude.Count > 0;
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = new();
            foreach (var spellGetterContext in State!.LoadOrder.PriorityOrder.Spell().WinningContextOverrides())
            {
                // skip invalid
                if ((useModIncludebyModkey && !Settings.Value.SpellModInclude.Contains(spellGetterContext.ModKey))
                    && (useModIncludeByName && !spellGetterContext.ModKey.FileName.String.HasAnyFromList(Settings.Value.SpellModNameInclude))
                    ) continue;
                var spellGetter = spellGetterContext.Record;
                if (spellGetter.Type != SpellType.Spell) continue;
                if (spellInfoList.ContainsKey(spellGetter)) continue;
                if (string.IsNullOrWhiteSpace(spellGetter.EditorID)) continue;
                if (useSpellExclude && spellGetter.EditorID.HasAnyFromList(Settings.Value.SpellExclude)) continue;

                var spellInfo = GetSpellInfo(spellGetter);

                if (spellInfo != null) spellInfoList.TryAdd(spellGetter, spellInfo);
            }

            return spellInfoList;
        }

        private static SpellInfo? GetSpellInfo(ISpellGetter spellGetter)
        {
            SpellInfo spellInfo = new();
            var spellBaseCost = spellGetter.BaseCost;
            int curCost = -1;
            foreach (var mEffect in spellGetter.Effects)
            {
                if (mEffect == null
                    || mEffect.Data == null
                    || mEffect.BaseEffect.IsNull
                    || mEffect.BaseEffect.FormKey.IsNull) continue;

                if (!mEffect.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;

                var effectMagicSkillActorValue = effect.MagicSkill;

                // add required skills and thir max required levels
                if (!IsMagicSkill(effectMagicSkillActorValue)) continue;

                var skill = GetSkillByActorValue(effectMagicSkillActorValue);

                AddUpdateSkill(spellInfo.RequiredSkills, skill, effect.MinimumSkillLevel);

                // set spell info
                float mag = mEffect.Data.Magnitude;
                if (mag < 1) mag = 1F;

                int dur = mEffect.Data.Duration;
                if (dur == 0) dur = 10;

                // calculate main skill and effect
                int cost = CalcCost(spellBaseCost, mag, dur);
                if (cost > curCost)
                {
                    spellInfo.MainSkill = skill;
                    curCost = cost;
                    spellInfo.MainEffect = effect;
                }
            }

            return curCost == -1 ? null : spellInfo;
        }

        private static int CalcCost(uint spellBaseCost, float mag, int dur)
        {
            return (int)Math.Floor(spellBaseCost * Math.Pow((mag * dur / 10), 1.1));
        }

        private static void AddUpdateSkill(Dictionary<Skill, uint> requiredSkills, Skill skill, uint minimumSkillLevel)
        {
            if (requiredSkills.ContainsKey(skill))
            {
                if (requiredSkills[skill] < minimumSkillLevel) requiredSkills[skill] = minimumSkillLevel;
            }
            else requiredSkills.Add(skill, minimumSkillLevel);
        }

        private static Skill GetSkillByActorValue(ActorValue effectMagicSkillActorValue)
        {
            return (Skill)Enum.Parse(typeof(Skill), effectMagicSkillActorValue.ToString());
        }

        private static bool CanGetTheSpell(NPCInfo npcInfo, KeyValuePair<ISpellGetter, SpellInfo> spellInfo)
        {
            //if (npcGetter.PlayerSkills == null) return false;
            if (spellInfo.Value.MainEffect == null) return false;
            if (spellInfo.Value.MainEffect.Keywords == null) return false;

            foreach (var requiredSkillInfo in spellInfo.Value.RequiredSkills)
            {
                if (requiredSkillInfo.Value > npcInfo.SkillLevels[requiredSkillInfo.Key]) return false;
                //if (npcGetter.PlayerSkills.SkillValues.First(s => s.Key == requiredSkillInfo.Key).Value < requiredSkillInfo.Value) return false;
            }

            var validKeywords = new List<IKeywordGetter>();
            foreach (var keywordFormLinkGetter in spellInfo.Value.MainEffect!.Keywords)
            {
                if (keywordFormLinkGetter.IsNull) continue;

                if (!keywordFormLinkGetter.TryResolve(State!.LinkCache, out var keyword)) continue;

                var edid = keyword.EditorID;
                if (string.IsNullOrWhiteSpace(edid)) continue;
                if (keyword.EditorID.HasAnyFromList(Settings.Value.EffectKeywordInclude)) validKeywords.Add(keyword);
            }


            if (validKeywords.Count == 0) return false;

            if (!npcInfo.HandEffects.ContainsKey(spellInfo.Key.EquipmentType.FormKey)) return false;

            var effects = npcInfo.HandEffects[spellInfo.Key.EquipmentType.FormKey];
            if (effects == null) return false;

            foreach (var keywordGetter in validKeywords) if (effects.Contains(keywordGetter)) return true;

            return false;
        }

        private static bool IsMagicSkill(ActorValue effectMagicSkill)
        {
            return effectMagicSkill == ActorValue.Alteration
                || effectMagicSkill == ActorValue.Conjuration
                || effectMagicSkill == ActorValue.Destruction
                || effectMagicSkill == ActorValue.Illusion
                || effectMagicSkill == ActorValue.Restoration;
        }
    }
}
