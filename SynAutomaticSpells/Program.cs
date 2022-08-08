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
            private readonly Dictionary<FormKey, List<IKeywordGetter>> HandEffects = new();
            private readonly Dictionary<Skill, uint> SkillLevels = new();

            public virtual void AddEquipslotEffect(FormKey f, IKeywordGetter k)
            {
                List<IKeywordGetter> keySet = HandEffects[f];
                if (keySet == null)
                {
                    keySet = new() { k };
                    HandEffects.Add(f, keySet);
                }
                else keySet.Add(k);
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
            Dictionary<ISpellGetter, SpellInfo> spellInfoList = new();
            foreach (var spellGetter in state.LoadOrder.PriorityOrder.Spell().WinningOverrides())
            {
                if (spellGetter.Type != SpellType.Spell || spellInfoList.ContainsKey(spellGetter)) continue;

                var spellBaseCost = spellGetter.BaseCost;

                // get spell infos
                SpellInfo spellInfo = new();
                int curCost = -1;
                foreach (var mEffect in spellGetter.Effects)
                {
                    if (mEffect == null
                        || mEffect.Data == null
                        || mEffect.BaseEffect.IsNull
                        || mEffect.BaseEffect.FormKey.IsNull) continue;

                    if (!mEffect.BaseEffect.TryResolve(state.LinkCache, out var effect)) continue;

                    var effectMagicSkillActorValue = effect.MagicSkill;

                    // add required skills and thir max required levels
                    if (!IsMagicSkill(effectMagicSkillActorValue)) continue;

                    var skill = (Skill)Enum.Parse(typeof(Skill), effectMagicSkillActorValue.ToString()); // convert ActorValue to Skill by value name string

                    if (spellInfo.RequiredSkills.ContainsKey(skill))
                    {
                        if (spellInfo.RequiredSkills[skill] < effect.MinimumSkillLevel) spellInfo.RequiredSkills[skill] = effect.MinimumSkillLevel;
                    }
                    else spellInfo.RequiredSkills.Add(skill, effect.MinimumSkillLevel);

                    float mag = mEffect.Data.Magnitude;
                    if (mag < 1) mag = 1F;

                    int dur = mEffect.Data.Duration;
                    if (dur == 0) dur = 10;

                    // calculate main skill and effect
                    double cost = spellBaseCost * Math.Pow((mag * dur / 10), 1.1);
                    int iCost = (int)Math.Floor(cost);
                    if (iCost > curCost)
                    {
                        spellInfo.MainSkill = skill;
                        curCost = iCost;
                        spellInfo.MainEffect = effect;
                    }
                }

                if (curCost != -1) spellInfoList.TryAdd(spellGetter, spellInfo);
            }

            foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // skip invalid
                if (npcGetter == null || string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                //if (npcGetter.ActorEffect == null || npcGetter.ActorEffect.Count == 0) continue;


                foreach (var spellInfo in spellInfoList)
                {
                    if (!CanGetTheSpell(npcGetter, spellInfo)) continue;

                }

                //List<IFormLinkGetter<ISpellRecordGetter>> npcSpells = new();
                //foreach (var npcSpellRecordGetter in npcGetter.ActorEffect)
                //{
                //    if (npcSpellRecordGetter.IsNull) continue;

                //    npcSpells.Add(npcSpellRecordGetter);
                //}

                //if (npcSpells.Count == 0) continue;

                //var skills = GetActorSkillLevels(npcGetter);
                //if (skills == null || skills.Count == 0) continue;

                //List<IFormLinkGetter<ISpellRecordGetter>> npcCanGetSpells = new();
                //foreach (var spellRecordGetter in spellInfoList)
                //{
                //    if (!IsNpcCanGetTheSpell(npcGetter, spellRecordGetter)) continue;

                //    npcCanGetSpells.Add(spellRecordGetter);
                //}

                //if (npcCanGetSpells.Count == 0) continue;

                //var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);

                //foreach (var spellRecordGetter in spellInfoList) npc.ActorEffect!.Add(spellRecordGetter);
            }

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

            var effects = validKeywords.All(k=>);
            if (effects != null && magicKeys.Count > 0)
            {
                foreach (KYWD key in magicKeys)
                {
                    if (effects.Contains(key))
                    {
                        return true;
                    }
                }
            }
            foreach (var validKeyword in validKeywords)
            {
                foreach (var key in magicKeys)
                {
                    if (effects.Contains(key))
                    {
                        return true;
                    }
                }
            }


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
