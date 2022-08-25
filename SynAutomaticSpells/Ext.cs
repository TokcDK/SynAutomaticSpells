using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SkyrimNPCHelpers;
using StringCompareSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SynAutomaticSpells.Program;

namespace SynAutomaticSpells
{
    internal static class Ext
    {
        internal static Dictionary<INpcGetter, NPCInfo> GetNPCInfoList(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            bool useNpcModExclude = Settings.Value.NativeSettings.NpcModExclude.Count > 0;
            bool useNpcModExcludeByName = Settings.Value.NativeSettings.NpcModExclude.Count > 0;
            bool useNpcExclude = Settings.Value.ASIS.NPCExclusions.Count > 0;
            bool useNpcInclude = Settings.Value.ASIS.NPCInclusions.Count > 0;
            bool useNpcKeywordExclude = Settings.Value.ASIS.NPCKeywordExclusions.Count > 0;
            var npcInfoList = new Dictionary<INpcGetter, NPCInfo>();
            foreach (var npcGetterContext in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
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
                var sourceModKey = state.LinkCache.ResolveAllContexts<INpc, INpcGetter>(npcGetter.FormKey).Last().ModKey;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc source mod is in excluded list");
                if (useNpcModExclude && Settings.Value.NativeSettings.NpcModExclude.Contains(sourceModKey)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc source mod is in included list");
                if (useNpcModExcludeByName && sourceModKey.FileName.String.HasAnyFromList(Settings.Value.ASIS.NPCModExclusions)) continue;

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc has spells");
                if (npcGetter.ActorEffect == null) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc edid is not empty");
                if (string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc in ignore list");
                if (useNpcExclude && npcGetter.EditorID.HasAnyFromList(Settings.Value.ASIS.NPCExclusions)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc in included list");
                if (useNpcInclude && !npcGetter.EditorID.HasAnyFromList(Settings.Value.ASIS.NPCInclusions)) continue;
                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc has keywords from ignore list");
                if (useNpcKeywordExclude && npcGetter.Keywords != null)
                {
                    bool skip = false;
                    foreach (var keywordGetterFormLink in npcGetter.Keywords)
                    {
                        if (!keywordGetterFormLink.TryResolve(state.LinkCache, out var keywordGeter)) continue;
                        if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                        if (!keywordGeter.EditorID.HasAnyFromList(Settings.Value.ASIS.NPCKeywordExclusions)) continue;

                        skip = true;
                        break;
                    }

                    if (IsDebugNPC) Console.WriteLine($"{npcDebugID} skip npc if has excluded keyword:{skip}");
                    if (skip) continue;
                }

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} get npc info");
                NPCInfo? npcInfo = GetNPCInfo(npcGetter, state);
                if (npcInfo == null) continue;

                if (IsDebugNPC) Console.WriteLine($"{npcDebugID} add npc info");
                npcInfoList.Add(npcGetter, npcInfo);
            }

            return npcInfoList;
        }

        internal static NPCInfo? GetNPCInfo(INpcGetter npcGetter, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} try get npc with untemplated spells list");
            if (!npcGetter.TryUnTemplate(state.LinkCache, NpcConfiguration.TemplateFlag.SpellList, out var unTemplatedNpcSpells)) return null;

            if (IsDebugNPC) Console.WriteLine($"{nameof(GetNPCInfo)} try get npc with untemplated stats");
            if (!npcGetter.TryUnTemplate(state.LinkCache, NpcConfiguration.TemplateFlag.Stats, out var unTemplatedNpcStats)) return null;

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
                //if (!spellRecordGetterFormLink.TryResolve(state.LinkCache, out var spellRecordGetter)) continue;
                var spellGetterFormlink = new FormLink<ISpellGetter>(spellRecordGetterFormLink.FormKey);
                if (spellGetterFormlink == null) continue;
                if (!spellGetterFormlink.TryResolve(state.LinkCache, out var spellGetter)) continue;

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

                    if (!mEffect.BaseEffect.TryResolve(state.LinkCache, out var effect)) continue;

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
                    if (!keywordGetterFormLink.TryResolve(state.LinkCache, out var keywordGeter)) continue;
                    if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                    if (!keywordGeter.EditorID.HasAnyFromList(Settings.Value.ASIS.EffectKeywordInclusions)) continue;
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

        internal static Dictionary<ISpellGetter, SpellInfo> GetSpellInfoList(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            bool useModInclude = Settings.Value.NativeSettings.SpellModInclude.Count > 0 || Settings.Value.ASIS.SpellModNInclusions.Count > 0;
            bool useSpellExclude = Settings.Value.ASIS.SpellExclusons.Count > 0;
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = new();
            foreach (var spellGetterContext in state.EnumerateSpellGetterContexts())
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
                        spellDebugID = $"{nameof(GetSpellInfoList)}/{nameof(spellGetterContext.Record.EditorID)}";
                        Console.WriteLine($"{spellDebugID} debug begin!");
                        IsDebugSpell = true;
                    }
                }

                var spellGetter = spellGetterContext.Record;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spel is from included mods");
                var sourceModKey = state.LinkCache.ResolveAllContexts<ISpell, ISpellGetter>(spellGetter.FormKey).Last().ModKey;
                if (useModInclude && !Settings.Value.NativeSettings.SpellModInclude.Contains(sourceModKey)
                    && !sourceModKey.FileName.String.HasAnyFromList(Settings.Value.ASIS.SpellModNInclusions)) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spell cast type is valid");
                if (!IsValidSpellType(spellGetter)) continue;
                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if already added");
                if (spellInfoList.ContainsKey(spellGetter)) continue;
                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if has empty edid");
                if (string.IsNullOrWhiteSpace(spellGetter.EditorID)) continue;
                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if the spell is in excluded list");
                if (useSpellExclude && spellGetter.EditorID.HasAnyFromList(Settings.Value.ASIS.SpellExclusons)) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} try to get spell info");
                var spellInfo = GetSpellInfo(spellGetter, state);
                if (spellInfo == null) continue;

                if (IsDebugSpell) Console.WriteLine($"{spellDebugID} add spell info");
                spellInfoList.TryAdd(spellGetter, spellInfo);
            }

            return spellInfoList;
        }

        internal static bool IsValidSpellType(ISpellGetter spellGetter)
        {
            return spellGetter.Type == SpellType.Spell
                    && spellGetter.CastType != CastType.ConstantEffect
                    && spellGetter.CastType != CastType.Scroll;
        }

        internal static IEnumerable<Mutagen.Bethesda.Plugins.Cache.IModContext<ISkyrimMod, ISkyrimModGetter, ISpell, ISpellGetter>?> EnumerateSpellGetterContexts(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (Settings.Value.NativeSettings.GetSpellsFromSpelltomes)
            {
                foreach (var bookContext in state.LoadOrder.PriorityOrder.Book().WinningContextOverrides())
                {
                    if (bookContext.Record.Teaches is not BookSpell bookSpell) continue;

                    if (!bookSpell.Spell.TryResolveContext<ISkyrimMod, ISkyrimModGetter, ISpell, ISpellGetter>(state.LinkCache, out var spellContext)) continue;

                    yield return spellContext;
                }
            }
            else foreach (var spellContext in state.LoadOrder.PriorityOrder.Spell().WinningContextOverrides()) yield return spellContext;
        }

        internal static SpellInfo? GetSpellInfo(ISpellGetter spellGetter, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
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

                if (!mEffect.BaseEffect.TryResolve(state.LinkCache, out var effect)) continue;

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

        internal static uint CalcCost(float effectBaseCost, float mag, int dur)
        {
            return (uint)Math.Floor(effectBaseCost * Math.Pow((mag * dur / 10), 1.1));
        }

        internal static void AddUpdateSkill(Dictionary<Skill, uint> requiredSkills, Skill skill, uint minimumSkillLevel)
        {
            if (requiredSkills.ContainsKey(skill))
            {
                if (requiredSkills[skill] < minimumSkillLevel) requiredSkills[skill] = minimumSkillLevel;
            }
            else requiredSkills.Add(skill, minimumSkillLevel);
        }

        internal static Skill GetSkillByActorValue(ActorValue effectMagicSkillActorValue)
        {
            return (Skill)Enum.Parse(typeof(Skill), effectMagicSkillActorValue.ToString());
        }

        internal static bool IsMagicSkill(ActorValue effectMagicSkill)
        {
            return effectMagicSkill == ActorValue.Alteration
                || effectMagicSkill == ActorValue.Conjuration
                || effectMagicSkill == ActorValue.Destruction
                || effectMagicSkill == ActorValue.Illusion
                || effectMagicSkill == ActorValue.Restoration;
        }
    }
}
