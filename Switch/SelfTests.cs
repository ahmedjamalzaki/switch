using System;
using System.Collections.Generic;

namespace Switch
{
    internal static class SelfTests
    {
        internal static void Run()
        {
            var cases = new Dictionary<string, string>
            {
                { "ahmed", "شاةثي" }, { "hello", "اثممخ" }, { "qwe", "ضصث" },
                { "ctrl + shift + space", "ؤفقم + ساهبف + سحشؤث" }, { "Ctrl + Shift + Space", "}فقم + ٍاهبف + ٍحشؤث" },
                { "شاةثي", "ahmed" }, { "أثممخ", "Hello" }, { "اثممخ", "hello" }, { "ضصث", "qwe" },
                { "لا", "b" }, { "ﻹ", "T" }, { "ﻷ", "G" }, { "ﻵ", "B" },
                { "ِأ’ُ] ـِ’ِ/ ~ِ،÷", "AHMED JAMAL ZAKI" }, { "ahmed 123!", "شاةثي 123!" }
            };

            foreach (var test in cases)
            {
                var actual = KeyboardLayoutConverter.Convert(test.Key);
                if (!string.Equals(actual, test.Value, StringComparison.Ordinal))
                    throw new InvalidOperationException("Failed conversion: " + test.Key + " => " + actual);
            }
        }
    }
}
