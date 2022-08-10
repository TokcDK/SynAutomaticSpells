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

        static bool IsDebugNPC = false;
        static bool IsDebugSpell = false;
        static bool IsDebugSpellEffect = false;
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

            Console.WriteLine("Distribute spells to npcs..\n-----------");
            int patchedNpcCount = 0;
            foreach (var npcInfo in npcsInfoList)
            {
                // init debug if enabled
                string npcDebugID = "";
                if (Settings.Value.Debug.IsDebugNpc)
                {
                    IsDebugNPC = false;
                    if (npcInfo.Key.EditorID.HasAnyFromList(Settings.Value.Debug.NpcEDIDListForDebug))
                    {
                        npcDebugID = $"Method:{nameof(RunPatch)}/NPC:{npcInfo.Key.EditorID}";
                        Console.WriteLine($"{npcDebugID} debug begin!");
                        IsDebugNPC = true;
                    }
                }

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} get spell effects info");
                var spellsToAdd = new List<ISpellGetter>();
                foreach (var spellInfo in spellInfoList)
                {
                    // init debug if enabled
                    string spellDebugID = "";
                    if (Settings.Value.Debug.IsDebugSpell)
                    {
                        IsDebugSpell = false;
                        if (spellInfo.Key.EditorID.HasAnyFromList(Settings.Value.Debug.SpellEDIDListForDebug))
                        {
                            spellDebugID = $"Method:{nameof(GetSpellInfoList)}/Spell:{nameof(spellInfo.Key.EditorID)}";
                            Console.WriteLine($"{spellDebugID} debug begin!");
                            IsDebugSpell = true;
                        }
                    }

                    if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if {npcInfo.Key.EditorID} can add the spell");
                    if (!CanGetTheSpell(npcInfo.Value, spellInfo)) continue;
                    if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if {npcInfo.Key.EditorID} spells already contains the spell");
                    if (npcInfo.Key.ActorEffect!.Contains(spellInfo.Key)) continue;

                    if (IsDebugSpell) Console.WriteLine($"{spellDebugID} add spells in spells list for {npcInfo.Key.EditorID}");
                    spellsToAdd.Add(spellInfo.Key);
                }

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if any spells must added");
                var addedCount = spellsToAdd.Count;
                if (addedCount == 0) continue;

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} add spells for npc");
                if(!Settings.Value.Debug.IsDebugNpc && !Settings.Value.Debug.IsDebugSpell && !Settings.Value.Debug.IsDebugSpellEffect) Console.WriteLine($"Add {addedCount} spells for '{npcInfo.Key.EditorID}'");
                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcInfo.Key);
                foreach (var spellToAdd in spellsToAdd) npc.ActorEffect!.Add(spellToAdd);
                patchedNpcCount++;
            }

            Console.WriteLine($"\n\nPatched {patchedNpcCount} npcs.\n-----------");
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
                if (npcGetterContext == null) continue;

                // init debug if enabled
                string npcDebugID = "";
                if (Settings.Value.Debug.IsDebugNpc)
                {
                    IsDebugNPC = false;
                    if (npcGetterContext.Record.EditorID.HasAnyFromList(Settings.Value.Debug.NpcEDIDListForDebug))
                    {
                        npcDebugID = $"Method:{nameof(RunPatch)}/NPC:{nameof(npcGetterContext.Record.EditorID)}";
                        Console.WriteLine($"{npcDebugID} debug begin!");
                        IsDebugNPC = true;
                    }
                }

                // skip invalid
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check npc getter");
                var npcGetter = npcGetterContext.Record;
                if (npcGetter == null) continue;
                var sourceModKey = State!.LinkCache.ResolveAllContexts<INpc, INpcGetter>(npcGetter.FormKey).Last().ModKey;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc source mod is in excluded list");
                if (useNpcModExclude && Settings.Value.NpcModExclude.Contains(sourceModKey)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc source mod is in included list");
                if (useNpcModExcludeByName && sourceModKey.FileName.String.HasAnyFromList(Settings.Value.NpcModNameExclude)) continue;

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc has spells");
                if (npcGetter.ActorEffect == null) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc edid is not empty");
                if (string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc in ignore list");
                if (useNpcExclude && npcGetter.EditorID.HasAnyFromList(Settings.Value.NpcExclude)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc in included list");
                if (useNpcInclude && !npcGetter.EditorID.HasAnyFromList(Settings.Value.NpcInclude)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc has keywords from ignore list");
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

                    if (IsDebugNPC) Console.WriteLine($"{npcDebugID} skip npc if has excluded keyword:{skip}");
                    if (skip) continue;
                }

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} get npc info");
                NPCInfo? npcInfo = GetNPCInfo(npcGetter);
                if (npcInfo == null) continue;

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} add npc info");
                npcInfoList.Add(npcGetter, npcInfo);
            }

            return npcInfoList;
        }

        private static NPCInfo? GetNPCInfo(INpcGetter npcGetter)
        {
            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} get npc with untemplated spells list");
            INpcGetter? unTemplatedNpcSpells = UnTemplate(npcGetter, NpcConfiguration.TemplateFlag.SpellList);
            if (unTemplatedNpcSpells == null) return null;

            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} get npc with untemplated stats");
            INpcGetter? unTemplatedNpcStats = UnTemplate(npcGetter, NpcConfiguration.TemplateFlag.Stats);
            if (unTemplatedNpcStats == null) return null;

            var npcInfo = new NPCInfo();
            // FireDamageConcAimed
            // get effects per equipSlot
            var spells = unTemplatedNpcSpells.ActorEffect;
            var npcSpellEffectsInfo = new Dictionary<ISpellGetter, IMagicEffectGetter>();
            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} get npc spells info");
            foreach (var spellRecordGetterFormLink in spells!)
            {
                if (spellRecordGetterFormLink == null) continue;

                // reconvert from ispellrecordgetter to ispellrecord
                //if (!spellRecordGetterFormLink.TryResolve(State!.LinkCache, out var spellRecordGetter)) continue;
                var spellGetterFormlink = new FormLink<ISpellGetter>(spellRecordGetterFormLink.FormKey);
                if (spellGetterFormlink == null) continue;
                if (!spellGetterFormlink.TryResolve(State!.LinkCache, out var spellGetter)) continue;

                // init debug if enabled
                string spellDebugID = "";
                if (Settings.Value.Debug.IsDebugSpell)
                {
                    IsDebugSpell = false;
                    if (spellGetter.EditorID.HasAnyFromList(Settings.Value.Debug.SpellEDIDListForDebug))
                    {
                        spellDebugID = $"Method:{nameof(GetSpellInfoList)}/Spell:{nameof(spellGetter.EditorID)}";
                        Console.WriteLine($"{spellDebugID} debug begin!");
                        IsDebugSpell = true;
                    }
                }

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spell cast type is valid");
                if (!IsValidSpellType(spellGetter)) continue;

                uint curCost = default;
                bool firstMainIsSet = false; // control to set first main effect because curcost is 0
                IMagicEffectGetter? mainEffect = null;
                foreach (var mEffect in spellGetter.Effects)
                {
                    if (mEffect == null
                        || mEffect.Data == null
                        || mEffect.BaseEffect.IsNull
                        || mEffect.BaseEffect.FormKey.IsNull) continue;

                    if (!mEffect.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;
                    
                    // init debug if enabled
                    string spellEffectDebugID = "";
                    if (Settings.Value.Debug.IsDebugSpellEffect)
                    {
                        IsDebugSpellEffect = false;
                        if (spellGetter.EditorID.HasAnyFromList(Settings.Value.Debug.SpelEffectlEDIDListForDebug))
                        {
                            spellEffectDebugID = $"Method:{nameof(GetSpellInfoList)}/Effect:{nameof(effect.EditorID)}";
                            Console.WriteLine($"{spellEffectDebugID} debug begin!");
                            IsDebugSpellEffect = true;
                        }
                    }

                    float mag = mEffect.Data!.Magnitude;
                    if (mag < 1) mag = 1;

                    int dur = mEffect.Data.Duration;
                    if (dur == 0) dur = 10;

                    var cost = CalcCost(effect.BaseCost, mag, dur);
                    if (IsDebugSpellEffect) Console.WriteLine($"{spellEffectDebugID}/{effect.EditorID}:{nameof(effect.BaseCost)}:{spellGetter.BaseCost},{nameof(mag)}:{mag},{nameof(dur)}:{dur},{nameof(cost)}:{cost},{nameof(curCost)}:{curCost},{nameof(firstMainIsSet)}:{firstMainIsSet}");
                    if (!firstMainIsSet || cost > curCost)
                    {
                        if (IsDebugSpellEffect) Console.WriteLine($"{spellEffectDebugID} effect is set as main");
                        firstMainIsSet = true;
                        curCost = cost;
                        mainEffect = effect;
                    }
                }

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if main effect is set:{mainEffect}");
                if (mainEffect == null) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} add npc spell effects info");
                npcSpellEffectsInfo.Add(spellGetter, mainEffect);
            }

            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} check if any npc spell effects info added");
            if (npcSpellEffectsInfo.Count == 0) return null;

            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} check and add equip type and keywords list for it");
            foreach (var entry in npcSpellEffectsInfo)
            {
                if (entry.Value == null) continue;
                if (entry.Value.Keywords == null) continue;

                var equipType = entry.Key.EquipmentType;
                foreach (var keywordGetterFormLink in entry.Value!.Keywords!)
                {
                    if (!keywordGetterFormLink.TryResolve(State!.LinkCache, out var keywordGeter)) continue;
                    if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                    if (!keywordGeter.EditorID.HasAnyFromList(Settings.Value.EffectKeywordInclude)) continue;
                    npcInfo.AddEquipTypeKeywords(equipType, keywordGeter);
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

            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} 6");
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
            bool useModInclude = Settings.Value.SpellModInclude.Count > 0 || Settings.Value.SpellModNameInclude.Count > 0;
            bool useSpellExclude = Settings.Value.SpellExclude.Count > 0;
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = new();
            foreach (var spellGetterContext in EnumerateSpellGetterContexts())
            {
                // skip invalid
                if (spellGetterContext == null) continue;

                // init debug if enabled
                string spellDebugID = "";
                if (Settings.Value.Debug.IsDebugSpell)
                {
                    IsDebugSpell = false;
                    if (spellGetterContext.Record.EditorID.HasAnyFromList(Settings.Value.Debug.SpellEDIDListForDebug))
                    {
                        spellDebugID = $"{nameof(GetSpellInfoList)}/{ nameof(spellGetterContext.Record.EditorID)}";
                        Console.WriteLine($"{spellDebugID} debug begin!");
                        IsDebugSpell = true;
                    }
                }                

                var spellGetter = spellGetterContext.Record;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spel is from included mods");
                var sourceModKey = State!.LinkCache.ResolveAllContexts<ISpell, ISpellGetter>(spellGetter.FormKey).Last().ModKey;
                if (useModInclude && !Settings.Value.SpellModInclude.Contains(sourceModKey)
                    && !sourceModKey.FileName.String.HasAnyFromList(Settings.Value.SpellModNameInclude)) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spell cast type is valid");
                if (!IsValidSpellType(spellGetter)) continue;
                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if already added");
                if (spellInfoList.ContainsKey(spellGetter)) continue;
                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if has empty edid");
                if (string.IsNullOrWhiteSpace(spellGetter.EditorID)) continue;
                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if the spell is in excluded list");
                if (useSpellExclude && spellGetter.EditorID.HasAnyFromList(Settings.Value.SpellExclude)) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} try to get spell info");
                var spellInfo = GetSpellInfo(spellGetter);
                if (spellInfo == null) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} add spell info");
                spellInfoList.TryAdd(spellGetter, spellInfo);
            }

            return spellInfoList;
        }

        private static bool IsValidSpellType(ISpellGetter spellGetter)
        {
            return spellGetter.Type == SpellType.Spell
                    && spellGetter.CastType != CastType.ConstantEffect
                    && spellGetter.CastType != CastType.Scroll;
        }

        private static IEnumerable<Mutagen.Bethesda.Plugins.Cache.IModContext<ISkyrimMod, ISkyrimModGetter, ISpell, ISpellGetter>?> EnumerateSpellGetterContexts()
        {
            if (Settings.Value.IsSpellsFromSpelltomes)
            {
                foreach (var bookContext in State!.LoadOrder.PriorityOrder.Book().WinningContextOverrides())
                {
                    if (bookContext.Record.Teaches is not BookSpell bookSpell) continue;

                    if (!bookSpell.Spell.TryResolveContext<ISkyrimMod, ISkyrimModGetter, ISpell, ISpellGetter>(State!.LinkCache, out var spellContext)) continue;

                    yield return spellContext;
                }
            }
            else foreach (var spellContext in State!.LoadOrder.PriorityOrder.Spell().WinningContextOverrides()) yield return spellContext;
        }

        private static SpellInfo? GetSpellInfo(ISpellGetter spellGetter)
        {
            SpellInfo spellInfo = new();
            uint curCost = default;
            bool firstMainIsSet = false; // control to set first main effect because curcost is 0
            foreach (var mEffect in spellGetter.Effects)
            {
                if (mEffect == null
                    || mEffect.Data == null
                    || mEffect.BaseEffect.IsNull
                    || mEffect.BaseEffect.FormKey.IsNull) continue;

                if (!mEffect.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;

                var effectMagicSkillActorValue = effect.MagicSkill;

                // init debug if enabled
                string spellEffectDebugID = "";
                if (Settings.Value.Debug.IsDebugSpellEffect)
                {
                    IsDebugSpellEffect = false;
                    if (spellGetter.EditorID.HasAnyFromList(Settings.Value.Debug.SpelEffectlEDIDListForDebug))
                    {
                        spellEffectDebugID = $"Method:{nameof(GetSpellInfoList)}/Effect:{nameof(effect.EditorID)}";
                        Console.WriteLine($"{spellEffectDebugID} debug begin!");
                        IsDebugSpellEffect = true;
                    }
                }

                // add required skills and thir max required levels
                if (IsDebugSpellEffect) Console.WriteLine($"{spellEffectDebugID} check if spell effect is one of main magic schools");
                if (!IsMagicSkill(effectMagicSkillActorValue)) continue;

                var skill = GetSkillByActorValue(effectMagicSkillActorValue);

                AddUpdateSkill(spellInfo.RequiredSkills, skill, effect.MinimumSkillLevel);

                // set spell info
                float mag = mEffect.Data.Magnitude;
                if (mag < 1) mag = 1F;

                int dur = mEffect.Data.Duration;
                if (dur == 0) dur = 10;

                // calculate main skill and effect
                var cost = CalcCost(effect.BaseCost, mag, dur);
                if (IsDebugSpellEffect) Console.WriteLine($"{spellEffectDebugID}/{effect.EditorID}:{nameof(effect.BaseCost)}:{spellGetter.BaseCost},{nameof(mag)}:{mag},{nameof(dur)}:{dur},{nameof(cost)}:{cost},{nameof(curCost)}:{curCost},{nameof(firstMainIsSet)}:{firstMainIsSet}");
                if (!firstMainIsSet || cost > curCost)
                {
                    if (IsDebugSpellEffect) Console.WriteLine($"{spellEffectDebugID} effect and skill set as main");
                    firstMainIsSet = true;
                    spellInfo.MainSkill = skill;
                    curCost = cost;
                    spellInfo.MainEffect = effect;
                }
            }

            return !firstMainIsSet ? null : spellInfo;
        }

        private static uint CalcCost(float effectBaseCost, float mag, int dur)
        {
            return (uint)Math.Floor(effectBaseCost * Math.Pow((mag * dur / 10), 1.1));
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
            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check main effect");
            //if (npcGetter.PlayerSkills == null) return false;
            if (spellInfo.Value.MainEffect == null) return false;
            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check if spell info have any keywords");
            if (spellInfo.Value.MainEffect.Keywords == null) return false;

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check if npc actor values is more or equal of reuired by spell");
            foreach (var requiredSkillInfo in spellInfo.Value.RequiredSkills)
            {
                if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check actor value");
                if (requiredSkillInfo.Value > npcInfo.SkillLevels[requiredSkillInfo.Key]) return false;
                if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} checked actor value");
                //if (npcGetter.PlayerSkills.SkillValues.First(s => s.Key == requiredSkillInfo.Key).Value < requiredSkillInfo.Value) return false;
            }

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} add valid spell keywords for compare");
            var spellValidKeywords = new List<IKeywordGetter>();
            foreach (var keywordFormLinkGetter in spellInfo.Value.MainEffect!.Keywords)
            {
                if (keywordFormLinkGetter.IsNull) continue;

                if (!keywordFormLinkGetter.TryResolve(State!.LinkCache, out var keyword)) continue;

                var edid = keyword.EditorID;
                if (string.IsNullOrWhiteSpace(edid)) continue;
                if (!keyword.EditorID.HasAnyFromList(Settings.Value.EffectKeywordInclude)) continue;
                spellValidKeywords.Add(keyword);
            }

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check if any valid spell keywords added");
            if (spellValidKeywords.Count == 0) return false;

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check if npc hace the equip type ");
            if (!npcInfo.HandEffects.ContainsKey(spellInfo.Key.EquipmentType.FormKey)) return false;

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} set keywords by equip type");
            var npcSpellsEffectsValidKeywords = npcInfo.HandEffects[spellInfo.Key.EquipmentType.FormKey];
            if (npcSpellsEffectsValidKeywords == null) return false;

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} check if spell have any same keyword as npc spells had");
            foreach (var keywordGetter in spellValidKeywords) if (npcSpellsEffectsValidKeywords.Contains(keywordGetter)) return true;

            if (IsDebugSpell) Console.WriteLine($"{nameof(CanGetTheSpell)} no keywords was equal, return false");
            if (IsDebugSpell) Console.WriteLine($"{nameof(spellValidKeywords)}:\n{string.Join("\n", spellValidKeywords.Select(k=>k.EditorID))}\n\n{nameof(npcSpellsEffectsValidKeywords)}:\n{string.Join("\n", npcSpellsEffectsValidKeywords.Select(k=>k.EditorID))}");
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
