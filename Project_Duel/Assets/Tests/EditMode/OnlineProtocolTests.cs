using NUnit.Framework;
using JunzhenDuijue;

namespace JunzhenDuijue.Tests.EditMode
{
    public class OnlineProtocolTests
    {
        [Test]
        public void OnlineDeckSelectionDto_FromDeckData_CopiesFields()
        {
            var deck = new DeckData
            {
                Id = "deck_a",
                DisplayName = "\u6d4b\u8bd5\u5957\u724c",
                RemovedSuit = "\u9ed1\u6843",
                CardIds = new System.Collections.Generic.List<string> { "NO001", "NO002", "NO003" }
            };
            var dto = OnlineDeckSelectionDto.FromDeckData(deck);
            Assert.AreEqual(deck.Id, dto.DeckId);
            Assert.AreEqual(deck.DisplayName, dto.DisplayName);
            Assert.AreEqual(deck.RemovedSuit, dto.RemovedSuit);
            CollectionAssert.AreEqual(deck.CardIds, dto.CardIds);
        }

        [TestCase("", OnlineClientService.DefaultServerUrl)]
        [TestCase("127.0.0.1:5057", "ws://127.0.0.1:5057/ws")]
        [TestCase("http://127.0.0.1:5057", "ws://127.0.0.1:5057/ws")]
        [TestCase("ws:127.0.0.1:5057/WS", "ws://127.0.0.1:5057/ws")]
        [TestCase("wss://example.com/socket", "wss://example.com/socket")]
        public void NormalizeServerUrl_NormalizesCommonInputs(string input, string expected)
        {
            string normalized = OnlineClientService.NormalizeServerUrl(input);
            Assert.AreEqual(expected, normalized);
        }
    }
}
