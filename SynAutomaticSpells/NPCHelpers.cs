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

namespace SkyrimNPCHelpers
{
    public static class SkyrimNPCExtensions
    {
        public static bool TryUnTemplate(this INpcGetter npcGetter, Mutagen.Bethesda.Plugins.Cache.ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, NpcConfiguration.TemplateFlag templateFlag, out INpcGetter npc)
        {
            INpcGetter? untemplatedNpc = npcGetter.UnTemplate(linkCache, templateFlag);
            if (untemplatedNpc == null)
            {
                npc = default!;
                return false;
            };

            npc = untemplatedNpc;
            return true;
        }

        /// <summary>
        /// Untemplated npc has not input flag or null template
        /// </summary>
        /// <param name="npcGetter"></param>
        /// <param name="linkCache"></param>
        /// <param name="templateFlag"></param>
        /// <returns></returns>
        public static INpcGetter? UnTemplate(this INpcGetter npcGetter, Mutagen.Bethesda.Plugins.Cache.ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, NpcConfiguration.TemplateFlag templateFlag)
        {
            if (npcGetter.Template == null || !npcGetter.Configuration.TemplateFlags.HasFlag(templateFlag)) return npcGetter;

            if (npcGetter.Template.IsNull
                //|| npcGetter.Template.FormKey.IsNull
                || !npcGetter!.Template.TryResolve(linkCache, out var templateNpcSpawnGetter)
                || templateNpcSpawnGetter is not INpcGetter templateNpcGetter
                )
            {
                return null;
            }

            return UnTemplate(templateNpcGetter, linkCache, templateFlag);
        }
    }
}
