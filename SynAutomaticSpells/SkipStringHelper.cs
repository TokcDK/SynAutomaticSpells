using Mutagen.Bethesda.Synthesis.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StringCompareSettings
{
    public class StringCompareSetting
    {
        [SynthesisTooltip("String keyword name which search for")]
        public string? Name;
        [SynthesisSettingName("Compare type")]
        [SynthesisTooltip("Compare type, how to compare")]
        public CompareType Compare = CompareType.Contains;
        [SynthesisTooltip("Case insensetive compare, comparing ignore case")]
        public bool IgnoreCase = true;
        [SynthesisTooltip("Commentary for the strings. Just to understand")]
        public string? Comment;
    }

    public class StringCompareSettingContainer
    {
        [SynthesisSettingName("String")]
        [SynthesisTooltip("Click to open string parameters")]
        public StringCompareSetting? StringSetting;
    }

    public enum CompareType
    {
        Equals,
        StartsWith,
        Contains,
        EndsWith,
        Regex,
    }

    public static class StringCompareHelpers
    {
        //public static bool IsUsingList = false;

        public static bool HasAnyFromList(this string? inputString, IEnumerable<StringCompareSettingContainer> list, IEnumerable<StringCompareSettingContainer> blackList)
        {
            //if (IsUsingList) return false;
            if (string.IsNullOrWhiteSpace(inputString)) return false;

            foreach (var setting in blackList)
            {
                if (setting.StringSetting == null) continue;

                if (IsFound(inputString, setting.StringSetting)) return false;
            }

            foreach (var setting in list)
            {
                if (setting.StringSetting == null) continue;

                if (IsFound(inputString, setting.StringSetting)) return true;
            }

            return false;
        }
        public static bool HasAnyFromList(this string? inputString, IEnumerable<StringCompareSetting> list, IEnumerable<StringCompareSetting> blackList)
        {
            //if (IsUsingList) return false;
            if (string.IsNullOrWhiteSpace(inputString)) return false;

            foreach (var setting in blackList)
            {
                if (setting == null) continue;

                if (IsFound(inputString, setting)) return false;
            }

            foreach (var setting in list)
            {
                if (setting==null) continue;

                if(IsFound(inputString, setting)) return true;
            }

            return false;
        }
        public static bool HasAllFromList(this string? inputString, IEnumerable<StringCompareSetting> list, IEnumerable<StringCompareSetting> blackList)
        {
            //if (IsUsingList) return false;
            if (string.IsNullOrWhiteSpace(inputString)) return false;

            foreach (var setting in blackList)
            {
                if (setting == null) continue;

                if (IsFound(inputString, setting)) return false;
            }

            int count = list.Count();
            foreach (var setting in list)
            {
                if (setting != null && !IsFound(inputString, setting)) return false;

                count--;
            }

            if (count == 0) return true;

            return false;
        }

        private static bool IsFound(string inputString, StringCompareSetting stringData)
        {
            if (string.IsNullOrWhiteSpace(stringData.Name)) return false;

            if (stringData.Compare == CompareType.Contains)
            {
                if (stringData.IgnoreCase)
                {
                    if (inputString.Contains(stringData.Name, StringComparison.InvariantCultureIgnoreCase)) return true;
                }
                else if (inputString.Contains(stringData.Name, StringComparison.InvariantCulture)) return true;
            }
            else if (stringData.Compare == CompareType.Equals)
            {
                if (stringData.IgnoreCase)
                {
                    if (string.Equals(inputString, stringData.Name, StringComparison.InvariantCultureIgnoreCase)) return true;
                }
                else if (string.Equals(inputString, stringData.Name, StringComparison.InvariantCulture)) return true;
            }
            else if (stringData.Compare == CompareType.StartsWith)
            {
                if (stringData.IgnoreCase)
                {
                    if (inputString.StartsWith(stringData.Name, StringComparison.InvariantCultureIgnoreCase)) return true;
                }
                else if (inputString.StartsWith(stringData.Name, StringComparison.InvariantCulture)) return true;
            }
            else if (stringData.Compare == CompareType.EndsWith)
            {
                if (stringData.IgnoreCase)
                {
                    if (inputString.EndsWith(stringData.Name, StringComparison.InvariantCultureIgnoreCase)) return true;
                }
                else if (inputString.EndsWith(stringData.Name, StringComparison.InvariantCulture)) return true;
            }
            else if (stringData.Compare == CompareType.Regex)
            {
                try
                {
                    if (stringData.IgnoreCase)
                    {
                        if (Regex.IsMatch(inputString, stringData.Name, RegexOptions.IgnoreCase)) return true;
                    }
                    else if(Regex.IsMatch(inputString, stringData.Name, RegexOptions.None)) return true;
                }
                catch (RegexParseException) { } // catch invalid regex error
            }

            return false;
        }
    }
}
