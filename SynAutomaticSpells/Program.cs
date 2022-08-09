using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
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
            public readonly Dictionary<FormKey, List<IKeywordGetter>> HandEffects = new();
            public readonly Dictionary<Skill, uint> SkillLevels = new();

            internal void AddEquipslotEffect(IFormLinkNullableGetter<IEquipTypeGetter> equipSlot, IKeywordGetter keyword)
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

            // get spell infos
            Console.WriteLine("Get spells info..");
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = GetSpellInfoList();
            Console.WriteLine("Get npc info..");
            Dictionary<FormKey, NPCInfo> npcsInfoList = GetNPCInfoList();

            Console.WriteLine("Set spells to npc..");
            foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // skip invalid
                if (npcGetter == null || string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                if (npcGetter.ActorEffect == null) continue;
                if (npcGetter.ActorEffect.Count == 0) continue;
                if (!npcsInfoList.ContainsKey(npcGetter.FormKey)) continue;
                //if (npcGetter.ActorEffect == null || npcGetter.ActorEffect.Count == 0) continue;

                var npcInfo = npcsInfoList[npcGetter.FormKey];

                var spellsToAdd = new List<ISpellGetter>();
                foreach (var spellInfo in spellInfoList)
                {
                    if (!CanGetTheSpell(npcInfo, spellInfo)) continue;
                    if (npcGetter.ActorEffect!.Contains(spellInfo.Key)) continue;

                    Console.WriteLine($"Add '{spellInfo.Key.EditorID}' to '{npcGetter.EditorID}'..");
                    spellsToAdd.Add(spellInfo.Key);
                }

                if (spellsToAdd.Count == 0) continue;

                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);
                foreach (var spellToAdd in spellsToAdd) npc.ActorEffect!.Add(spellToAdd);
            }

            Console.WriteLine("Finished..");
        }

        private static Dictionary<FormKey, NPCInfo> GetNPCInfoList()
        {
            var npcInfoList = new Dictionary<FormKey, NPCInfo>();
            foreach (var npcGetter in State!.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // some npc checks for validness
                if (npcGetter == null) continue;
                if (npcGetter.ActorEffect == null) continue;

                NPCInfo? npcInfo = GetNPCInfo(npcGetter);
                if (npcInfo == null) continue;

                npcInfoList.Add(npcGetter.FormKey, npcInfo);
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
                if (!spellRecordGetterFormLink.TryResolve(State!.LinkCache, out var spellRecordGetter)) continue;
                var spellGetterFormlink = new FormLink<ISpellGetter>(spellRecordGetter.FormKey);
                if (spellGetterFormlink==null) continue;
                if (!spellGetterFormlink.TryResolve(State!.LinkCache, out var spellGetter)) continue;
                if (spellGetter.Keywords==null) continue;

                int curCost = -1;
                if (spellGetter.BaseCost <= 0) continue;
                IMagicEffectGetter? mainEffect = null;
                foreach (var mEffect in spellGetter.Effects)
                {
                    if (!mEffect.BaseEffect.TryResolve(State!.LinkCache, out var effect)) continue;

                    float mag = mEffect.Data!.Magnitude;
                    if (mag < 1)
                    {
                        mag = 1;
                    }

                    int dur = mEffect.Data.Duration;
                    if (dur == 0)
                    {
                        dur = 10;
                    }
                    var cost = CalcCost(spellGetter.BaseCost, mag, dur);
                    if (cost > curCost)
                    {
                        curCost = cost;
                        mainEffect = effect;
                    }
                }

                if (mainEffect != null) npcSpellEffectsInfo.Add(spellGetter, mainEffect);
            }

            if (npcSpellEffectsInfo == null) return null;

            foreach (var entry in npcSpellEffectsInfo)
            {
                var equipSlot = entry.Key.EquipmentType;
                foreach (var keywordGetterFormLink in entry.Value!.Keywords!)
                {
                    if (!keywordGetterFormLink.TryResolve(State!.LinkCache, out var keywordGeter)) continue;
                    if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                    foreach (string prefix in new[] { "MAGIC" })
                    {
                        if (keywordGeter.EditorID.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            npcInfo.AddEquipslotEffect(equipSlot, keywordGeter);
                        }
                    }
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

        private static INpcGetter? UnTemplate(INpcGetter npcGetter, NpcConfiguration.TemplateFlag templateFlag)
        {
            INpcGetter? untemplatedNpc = npcGetter;
            while (untemplatedNpc.Configuration.TemplateFlags.HasFlag(templateFlag))
            {
                if(untemplatedNpc.Template==null 
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
                foreach (var prefix in new[] { "MAGIC" })
                {
                    if (!edid.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) continue;

                    validKeywords.Add(keyword);
                    break;
                }
            }

            if (validKeywords.Count==0) return false;
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
