using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SynAutomaticSpells
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynAutomaticSpells.esp")
                .Run(args);
        }

        public class NPCInfo
        {
            public readonly Dictionary<IFormLinkNullableGetter<IEquipTypeGetter>, List<IKeywordGetter>> HandEffects = new();
            public readonly Dictionary<Skill, uint> SkillLevels = new();

            internal void AddEquipslotEffect(IFormLinkNullableGetter<IEquipTypeGetter> equipSlot, IKeywordGetter keyword)
            {
                if (!HandEffects.ContainsKey(equipSlot))
                {
                    HandEffects.Add(equipSlot, new() { keyword });
                }
                else
                {
                    HandEffects[equipSlot].Add(keyword);
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

            // get spell infos
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = GetSpellInfoList();
            //Dictionary<ISpellGetter, NPCInfo> npcsMap = GetNPCInfoList();

            foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // skip invalid
                if (npcGetter == null || string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                if (npcGetter.ActorEffect == null) continue;
                if (npcGetter.ActorEffect.Count == 0) continue;
                //if (npcGetter.ActorEffect == null || npcGetter.ActorEffect.Count == 0) continue;

                var spellsToAdd = new List<ISpellGetter>();
                foreach (var spellInfo in spellInfoList)
                {
                    if (!CanGetTheSpell(npcGetter, spellInfo)) continue;
                    if (npcGetter.ActorEffect!.Contains(spellInfo.Key)) continue;

                    spellsToAdd.Add(spellInfo.Key);
                }

                if (spellsToAdd.Count == 0) continue;

                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);
                foreach (var spellToAdd in spellsToAdd) npc.ActorEffect!.Add(spellToAdd);
            }
        }

        //private static Dictionary<ISpellGetter, NPCInfo> GetNPCInfoList()
        //{
        //    var npcInfoList = new Dictionary<ISpellGetter, NPCInfo>();
        //    foreach (var npcGetter in State!.LoadOrder.PriorityOrder.Npc().WinningOverrides())
        //    {
        //        // some npc checks for validness
        //        if (npcGetter == null) continue;

        //        NPCInfo? npcInfo = GetNPCInfo(npcGetter);
        //        if (npcInfo == null) continue;

        //    }

        //    return npcInfoList;
        //}

        //private static NPCInfo? GetNPCInfo(INpcGetter npcGetter)
        //{
        //    INpcGetter? unTemplatedSpells = UnTemplate(npcGetter, NpcConfiguration.TemplateFlag.SpellList);
        //    INpcGetter? unTemplatedStats = UnTemplate(npcGetter, NpcConfiguration.TemplateFlag.Stats);
        //    if ((unTemplatedSpells == null) || (unTemplatedStats == null))
        //    {
        //        return null;
        //    }

        //    var npcInfo = new NPCInfo();

        //    // get effects per equipSlot
        //    List<FormLink<ISpellGetter>> spells = GetSpells(unTemplatedSpells);
        //    var npcSpellEffectsInfo = new Dictionary<ISpellGetter, IMagicEffectGetter>();
        //    foreach (FormLink<ISpellGetter> f in spells)
        //    {
        //        if (!f.TryResolve(State!.LinkCache, out var spell)) continue;
        //        if (spell.BaseCost<=0) continue;

        //        int curCost = -1;
        //        IMagicEffectGetter? mainEffect = null;
        //        foreach (var mEffect in spell.Effects)
        //        {
        //            if (!mEffect.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;

        //            float mag = mEffect.Data!.Magnitude;
        //            if (mag < 1)
        //            {
        //                mag = 1;
        //            }

        //            int dur = mEffect.Data.Duration;
        //            if (dur == 0)
        //            {
        //                dur = 10;
        //            }
        //            var cost = CalcCost(spell.BaseCost, mag, dur);
        //            if (cost > curCost)
        //            {
        //                curCost = cost;
        //                mainEffect = effect;
        //            }
        //        }

        //        if (mainEffect != null) npcSpellEffectsInfo.Add(spell, mainEffect);
        //    }

        //    foreach (var entry in npcSpellEffectsInfo)
        //    {
        //        var equipSlot = entry.Key.EquipmentType;
        //        foreach (var f in entry.Value!.Keywords!)
        //        {
        //            if (!f.TryResolve(State!.LinkCache, out var keyword)) continue;
        //            if (string.IsNullOrWhiteSpace(keyword.EditorID)) continue;

        //            string edid = keyword.EditorID;
        //            foreach (string prefix in new[] { "MAGIC" })
        //            {
        //                if (edid.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
        //                {
        //                    npcInfo.AddEquipslotEffect(equipSlot, keyword);
        //                }
        //            }
        //        }
        //    }


        //    // get skills
        //    List<Skill> skills = new();
        //    skills.Add(Skill.Alteration);
        //    skills.Add(Skill.Conjuration);
        //    skills.Add(Skill.Destruction);
        //    skills.Add(Skill.Illusion);
        //    skills.Add(Skill.Restoration);
        //    foreach (Skill s in skills)
        //    {
        //        //npcInfo.SkillLevels.Add(s, unTemplatedStats[s]);
        //    }

        //    return npcInfo;
        //}

        //private static INpcGetter? UnTemplate(INpcGetter npcGetter, NpcConfiguration.TemplateFlag spellList)
        //{
        //    throw new NotImplementedException();
        //}

        private static Dictionary<ISpellGetter, SpellInfo> GetSpellInfoList()
        {
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = new();
            foreach (var spellGetter in State!.LoadOrder.PriorityOrder.Spell().WinningOverrides())
            {
                if (spellGetter.Type != SpellType.Spell || spellInfoList.ContainsKey(spellGetter)) continue;

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

        private static bool CanGetTheSpell(INpcGetter npcGetter, KeyValuePair<ISpellGetter, SpellInfo> spellInfo)
        {
            if (npcGetter.PlayerSkills == null) return false;
            if (spellInfo.Value.MainEffect == null) return false;
            if (spellInfo.Value.MainEffect.Keywords == null) return false;

            foreach (var requiredSkillInfo in spellInfo.Value.RequiredSkills)
            {
                if (npcGetter.PlayerSkills.SkillValues.First(s => s.Key == requiredSkillInfo.Key).Value < requiredSkillInfo.Value) return false;
            }


            var validKeywords = new List<IKeywordGetter>();
            foreach (var keywordFormLinkGetter in spellInfo.Value.MainEffect!.Keywords)
            {
                if (keywordFormLinkGetter.IsNull) continue;

                if (!keywordFormLinkGetter.TryResolve(State!.LinkCache, out var keyword)) continue;

                var edid = keyword.EditorID;
                if (string.IsNullOrWhiteSpace(edid)) continue;
                foreach (var prefix in new[] { "MAGIC" })
                {
                    if (!edid.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) continue;

                    validKeywords.Add(keyword);
                    break;
                }
            }

            //var effects = validKeywords.All(k =>);
            //if (effects != null && magicKeys.Count > 0)
            //{
            //    foreach (KYWD key in magicKeys)
            //    {
            //        if (effects.Contains(key))
            //        {
            //            return true;
            //        }
            //    }
            //}
            foreach (var validKeyword in validKeywords)
            {
                foreach (var keyword in GetActorSpellsMagicEffectKeywords(npcGetter))
                {
                    if (validKeywords.Contains(keyword))
                    {
                        return true;
                    }
                }
            }


            return false;
        }

        private static IEnumerable<IKeywordGetter> GetActorSpellsMagicEffectKeywords(INpcGetter npcGetter)
        {
            foreach (var actorEffect in npcGetter.ActorEffect!)
            {
                if (actorEffect.FormKey.IsNull) continue;
                if (!actorEffect.TryResolve(State!.LinkCache, out var spellRecordGetter)) continue;
                if (spellRecordGetter.FormKey.IsNull) continue;

                var spellGetter = new FormLink<ISpellGetter>(spellRecordGetter.FormKey);
                if (spellGetter == null) continue;
                if (!spellGetter.TryResolve(State!.LinkCache, out var spell)) continue;
                if (spell.Type != SpellType.Spell) continue;

                foreach (var effectGetter in spell.Effects)
                {
                    if (effectGetter == null) continue;
                    if (effectGetter.BaseEffect.FormKey.IsNull) continue;
                    if (!effectGetter.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;
                    if (effect.Keywords == null) continue;

                    foreach (var keywordGetter in effect.Keywords)
                    {
                        if (!keywordGetter.TryResolve(State!.LinkCache, out var keyword)) continue;
                        if (string.IsNullOrWhiteSpace(keyword.EditorID)) continue;

                        foreach (var str in new[] { "MAGIC" })
                        {
                            if (keyword.EditorID.StartsWith(str, StringComparison.InvariantCultureIgnoreCase))
                            {
                                yield return keyword;
                            }
                        }
                    }
                }
            }
        }

        private static bool IsMagicSkill(ActorValue effectMagicSkill)
        {
            return effectMagicSkill == ActorValue.Alteration
                || effectMagicSkill == ActorValue.Conjuration
                || effectMagicSkill == ActorValue.Destruction
                || effectMagicSkill == ActorValue.Illusion
                || effectMagicSkill == ActorValue.Restoration;
        }

        private static bool IsNpcCanGetTheSpell(INpcGetter npcGetter, ISpellGetter spellRecordGetter)
        {
            return false;
        }

        static Dictionary<Skill, int>? GetActorSkillLevels(INpcGetter npcGetter)
        {
            if (npcGetter.PlayerSkills == null) return null;

            Dictionary<Skill, int>? skills = new();
            foreach (var skillValue in npcGetter.PlayerSkills.SkillValues)
            {
                skills.Add(skillValue.Key, skillValue.Value + npcGetter.PlayerSkills.SkillOffsets[skillValue.Key]);
            }

            return skills;
        }
    }
}
