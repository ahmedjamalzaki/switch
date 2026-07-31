using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Switch
{
    internal static class KeyboardLayoutConverter
    {
        private static readonly Dictionary<char, string> EnglishToArabic = new Dictionary<char, string>
        {
            { 'q', "ض" }, { 'w', "ص" }, { 'e', "ث" }, { 'r', "ق" }, { 't', "ف" }, { 'y', "غ" }, { 'u', "ع" }, { 'i', "ه" }, { 'o', "خ" }, { 'p', "ح" },
            { '[', "ج" }, { ']', "د" }, { '\\', "\\" }, { 'a', "ش" }, { 's', "س" }, { 'd', "ي" }, { 'f', "ب" }, { 'g', "ل" }, { 'h', "ا" }, { 'j', "ت" }, { 'k', "ن" }, { 'l', "م" }, { ';', "ك" }, { '\'', "ط" },
            { 'z', "ئ" }, { 'x', "ء" }, { 'c', "ؤ" }, { 'v', "ر" }, { 'b', "لا" }, { 'n', "ى" }, { 'm', "ة" }, { ',', "و" }, { '.', "ز" }, { '/', "ظ" }, { '`', "ذ" },
            { 'Q', "َ" }, { 'W', "ً" }, { 'E', "ُ" }, { 'R', "ٌ" }, { 'T', "ﻹ" }, { 'Y', "إ" }, { 'U', "`" }, { 'I', "÷" }, { 'O', "×" }, { 'P', "؛" },
            { '{', "<" }, { '}', ">" }, { '|', "|" }, { 'A', "ِ" }, { 'S', "ٍ" }, { 'D', "]" }, { 'F', "[" }, { 'G', "ﻷ" }, { 'H', "أ" }, { 'J', "ـ" }, { 'K', "،" }, { 'L', "/" }, { ':', ":" }, { '\"', "\"" },
            { 'Z', "~" }, { 'X', "ْ" }, { 'C', "}" }, { 'V', "{" }, { 'B', "ﻵ" }, { 'N', "آ" }, { 'M', "'" }, { '<', "," }, { '>', "." }, { '?', "؟" }, { '~', "ّ" }, { '(', ")" }, { ')', "(" }
        };

        private static readonly Dictionary<char, char> ArabicToEnglish = EnglishToArabic
            .Where(pair => pair.Value.Length == 1)
            .ToDictionary(pair => pair.Value[0], pair => pair.Key);

        private static readonly Dictionary<string, string> ArabicLigatures = new Dictionary<string, string>
        {
            { "لا", "b" }, { "لإ", "T" }, { "لأ", "G" }, { "لآ", "B" },
            { "ﻻ", "b" }, { "ﻼ", "b" }, { "ﻹ", "T" }, { "ﻺ", "T" },
            { "ﻷ", "G" }, { "ﻸ", "G" }, { "ﻵ", "B" }, { "ﻶ", "B" }
        };

        public static string Convert(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = NormalizePunctuation(text);
            var arabic = text.Count(IsArabicCharacter);
            var english = text.Count(character => !IsArabicCharacter(character) && char.IsLetter(character));

            if (arabic >= english)
            {
                foreach (var ligature in ArabicLigatures)
                    text = text.Replace(ligature.Key, ligature.Value);

                var result = new StringBuilder(text.Length);
                foreach (var character in text)
                {
                    char key;
                    result.Append(ArabicToEnglish.TryGetValue(character, out key) ? key : character);
                }
                return result.ToString();
            }

            var arabicResult = new StringBuilder(text.Length);
            foreach (var character in text)
            {
                string value;
                arabicResult.Append(EnglishToArabic.TryGetValue(character, out value) ? value : character.ToString());
            }
            return arabicResult.ToString();
        }

        private static string NormalizePunctuation(string text)
        {
            return text.Replace('’', '\'').Replace('‘', '\'').Replace('”', '\"').Replace('“', '\"')
                .Replace('–', '-').Replace('—', '-');
        }

        private static bool IsArabicCharacter(char character)
        {
            return (character >= 0x0600 && character <= 0x06FF) ||
                (character >= 0x0750 && character <= 0x077F) ||
                (character >= 0x08A0 && character <= 0x08FF) ||
                (character >= 0xFB50 && character <= 0xFDFF) ||
                (character >= 0xFE70 && character <= 0xFEFF);
        }
    }
}
