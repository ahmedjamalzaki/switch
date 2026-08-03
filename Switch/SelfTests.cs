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
                AssertEqual(test.Value, KeyboardLayoutConverter.Convert(test.Key), test.Key);
            }

            AssertEqual(string.Empty, KeyboardLayoutConverter.Convert(string.Empty), "empty text");
            AssertEqual("123!", KeyboardLayoutConverter.Convert("123!"), "numbers and punctuation");
            AssertEqual("ahmed", KeyboardLayoutConverter.Convert(KeyboardLayoutConverter.Convert("ahmed")), "English round trip");
            AssertEqual("Hello", KeyboardLayoutConverter.Convert(KeyboardLayoutConverter.Convert("Hello")), "mixed-case round trip");
        }

        private static void AssertEqual(string expected, string actual, string input)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("Failed conversion: " + input + " => " + actual);
        }
    }
}
