using System;
using System.Linq;
using NUnit.Framework;

namespace Tuteliq.Tests
{
    public class LanguageTests
    {
        private static readonly string[] SupportedCodes =
        {
            "en", "es", "pt", "uk", "sv", "no", "da", "fi", "de", "fr", "nl", "pl", "it", "tr", "ro", "el",
            "cs", "hu", "bg", "hr", "sk", "lt", "lv", "et", "sl", "mt", "ga", "zh", "ja", "ko", "ar", "ru"
        };

        [Test]
        public void Language_HasExactlyTheSupportedCodes()
        {
            var codes = Enum.GetNames(typeof(Language)).Select(name => name.ToLowerInvariant());
            CollectionAssert.AreEquivalent(SupportedCodes, codes);
        }

        [Test]
        public void ParseLanguage_EverySupportedCode_RoundTrips()
        {
            foreach (var code in SupportedCodes)
            {
                object parsed = EnumExtensions.ParseLanguage(code);
                Assert.AreEqual(code, parsed?.ToString().ToLowerInvariant(), code);
            }
        }

        [Test]
        public void ParseLanguage_UnknownCode_IsNotEnglish()
        {
            object parsed = EnumExtensions.ParseLanguage("xx");
            Assert.IsNull(parsed);
        }

        [Test]
        public void ParseLanguage_Catalan_IsNotSupported()
        {
            object parsed = EnumExtensions.ParseLanguage("ca");
            Assert.IsNull(parsed);
        }
    }
}
