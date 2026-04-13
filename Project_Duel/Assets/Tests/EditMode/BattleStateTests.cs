using NUnit.Framework;
using JunzhenDuijue;

namespace JunzhenDuijue.Tests.EditMode
{
    public class BattleStateTests
    {
        [Test]
        public void CreateShuffledDeck_RemovedSuit_RemovesThirteenCards()
        {
            var deck = new DeckData { RemovedSuit = "\u7ea2\u6843" };
            var cards = BattleState.CreateShuffledDeck(deck);
            Assert.AreEqual(39, cards.Count);
            Assert.False(cards.Exists(card => card.Suit == "\u7ea2\u6843"));
        }

        [Test]
        public void Draw_MovesCardsFromDeckToHand()
        {
            var side = new SideState();
            side.Deck.Add(new PokerCard { Suit = "\u9ed1\u6843", Rank = 1 });
            side.Deck.Add(new PokerCard { Suit = "\u65b9\u7247", Rank = 2 });
            int drawn = BattleState.Draw(side, 2);
            Assert.AreEqual(2, drawn);
            Assert.AreEqual(0, side.Deck.Count);
            Assert.AreEqual(2, side.Hand.Count);
        }

        [Test]
        public void Draw_WhenDeckRunsOut_ReshufflesDiscardAndContinues()
        {
            var side = new SideState();
            side.Deck.Add(new PokerCard { Suit = "\u6885\u82b1", Rank = 3 });
            side.DiscardPile.Add(new PokerCard { Suit = "\u9ed1\u6843", Rank = 4 });
            side.DiscardPile.Add(new PokerCard { Suit = "\u65b9\u7247", Rank = 5 });
            int drawn = BattleState.Draw(side, 3);
            Assert.AreEqual(3, drawn);
            Assert.AreEqual(0, side.DiscardPile.Count);
            Assert.AreEqual(3, side.Hand.Count);
        }

        [Test]
        public void FlipGeneral_RecoversAfterTwoOwnTurnEnds()
        {
            var side = new SideState();
            side.GeneralCardIds.Add("NO001");
            side.EnsureGeneralStateCount(1);
            Assert.True(side.IsGeneralFaceUp(0));
            Assert.True(side.FlipGeneralFaceDown(0, true));
            Assert.False(side.IsGeneralFaceUp(0));
            side.RecoverGeneralsAtOwnTurnEnd();
            Assert.False(side.IsGeneralFaceUp(0));
            side.RecoverGeneralsAtOwnTurnEnd();
            Assert.True(side.IsGeneralFaceUp(0));
        }
    }
}
