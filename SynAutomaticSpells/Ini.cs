using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynAutomaticSpells
{
    public static class Ini
    {
        public static void ReadIniSectionValuesFrom(this Dictionary<string, HashSet<string>> iniSections, string iniPath)
        {
            //iniSections = new Dictionary<string, HashSet<string>>();
            using StreamReader sr = new(iniPath);
            string sectonName = "";
            var sectionValues = new HashSet<string>();
            while (!sr.EndOfStream)
            {
                string? line = sr.ReadLine();

                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Trim().StartsWith(';')) continue;
                if (line.Trim().StartsWith('[') && line.Trim().EndsWith(']'))
                {
                    if (!string.IsNullOrWhiteSpace(sectonName))
                    {
                        iniSections.AddSectionValues(sectonName, sectionValues);

                        sectionValues = new HashSet<string>();
                    }
                    sectonName = line.Trim().Trim('[', ']').Trim();

                    continue;
                }

                if (string.IsNullOrWhiteSpace(sectonName)) continue;
                var sValue = line.Split(';')[0]; // add value but exclude possible
                if (!sectionValues.Contains(sValue)) sectionValues.Add(sValue);
            }
            iniSections.AddSectionValues(sectonName, sectionValues);
        }

        private static void AddSectionValues(this Dictionary<string, HashSet<string>> iniSections, string sectonName, HashSet<string> sectionValues)
        {
            if (sectionValues.Count > 0)
            {
                if (iniSections.ContainsKey(sectonName)) //when for some reason setion is duplicated
                {
                    var section = iniSections[sectonName];
                    foreach (var v in sectionValues)
                    {
                        if (!section.Contains(v)) section.Add(v);
                    }
                }
                else
                {
                    iniSections.Add(sectonName, sectionValues);
                }
            }
        }
    }
}
