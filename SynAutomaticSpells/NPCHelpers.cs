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
        public static INpcGetter? UnTemplate(this INpcGetter npcGetter, Mutagen.Bethesda.Plugins.Cache.ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, NpcConfiguration.TemplateFlag templateFlag)
        {
            INpcGetter? untemplatedNpc = npcGetter;
            while (untemplatedNpc.Configuration.TemplateFlags.HasFlag(templateFlag))
            {
                if (untemplatedNpc.Template == null
                    || untemplatedNpc.Template.IsNull
                    || untemplatedNpc.Template.FormKey.IsNull
                    || !untemplatedNpc!.Template.TryResolve(linkCache, out var templateNpcSpawnGetter)
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

                if (!templateNpcGetterFormlink.TryResolve(linkCache, out var templateNpcGetter))
                {
                    untemplatedNpc = null;
                    break;
                }

                untemplatedNpc = templateNpcGetter;
            }

            return untemplatedNpc;
        }
    }
}
