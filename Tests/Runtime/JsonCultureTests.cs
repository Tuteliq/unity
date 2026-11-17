using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Tuteliq.Tests
{
    /// <summary>
    /// JSON must not depend on the player's locale: German, French and Swedish
    /// write 0.5 as "0,5", and Swedish writes -3 with a Unicode minus sign.
    /// </summary>
    public class JsonCultureTests
    {
        private CultureInfo _original;

        [SetUp]
        public void SaveCulture() => _original = Thread.CurrentThread.CurrentCulture;

        [TearDown]
        public void RestoreCulture() => Thread.CurrentThread.CurrentCulture = _original;

        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        [TestCase("sv-SE")]
        public void Serialize_Numbers_UseInvariantCulture(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            var json = MiniJson.Serialize(new Dictionary<string, object>
            {
                { "half", 0.5 },
                { "quarter", 0.25f },
                { "negative", -3 },
                { "big", -1234567L }
            });

            Assert.AreEqual("{\"half\":0.5,\"quarter\":0.25,\"negative\":-3,\"big\":-1234567}", json);
        }

        [TestCase("de-DE")]
        [TestCase("sv-SE")]
        public void Deserialize_Numbers_UseInvariantCulture(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            var data = (Dictionary<string, object>)MiniJson.Deserialize("{\"a\":-3,\"b\":0.5,\"c\":1e-7}");

            Assert.AreEqual(-3L, data["a"]);
            Assert.AreEqual(0.5, data["b"]);
            Assert.AreEqual(1e-7, (double)data["c"], 1e-12);
        }
    }
}
