using System;
using System.Collections.Generic;
using System.Drawing;
using AdamMil.Mathematics.Combinatorics;
using AdamMil.Mathematics.Random;

namespace BirdhouseManor
{

#region Board
sealed class Board
{
  Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();
}
#endregion

#region Deck
/// <summary>Represents a deck of cards (actually of any <see cref="NamedGameObjectWithCount"/>).</summary>
sealed class Deck<C> where C : NamedGameObjectWithCount
{
  /// <summary>Initializes a new, empty deck.</summary>
  public Deck() { }

  /// <summary>Initializes the deck of cards from the given card types (respecting <see cref="CardBase.Count"/>), but does not
  /// shuffle the deck.
  /// </summary>
  public Deck(IEnumerable<C> cardTypes)
  {
    if(cardTypes == null) throw new ArgumentNullException();

    foreach(C card in cardTypes)
    {
      for(int i=0; i<card.Count; i++) cards.Add(card);
    }
  }

  /// <summary>Gets the number of cards remaining in the deck.</summary>
  public int Count
  {
    get { return cards.Count; }
  }

  /// <summary>Adds a card to the top of the deck.</summary>
  public void Add(C card)
  {
    Add(card, DeckLocation.Top);
  }

  /// <summary>Adds a card to the given location within the deck.</summary>
  public void Add(C card, DeckLocation location)
  {
    if(card == null) throw new ArgumentNullException();
    if(location == DeckLocation.Top) cards.Add(card);
    else cards.Insert(0, card);
  }

  /// <summary>Removes all cards from the other deck and adds them to this deck.</summary>
  public void AddCards(Deck<C> otherDeck)
  {
    if(otherDeck == null) throw new ArgumentNullException();
    cards.AddRange(otherDeck.cards);
    otherDeck.cards.Clear();
  }

  /// <summary>Takes and returns a card from the top of the deck.</summary>
  public C Draw()
  {
    return Draw(DeckLocation.Top);
  }

  /// <summary>Takes and returns a card from the given location within the deck.</summary>
  public C Draw(DeckLocation location)
  {
    if(Count == 0) throw new InvalidOperationException("The deck is empty.");
    int index = location == DeckLocation.Top ? cards.Count-1 : 0;
    C card = cards[index];
    cards.RemoveAt(card.Count-1);
    return card;
  }

  /// <summary>Shuffles the deck using the given random number generator.</summary>
  public void Shuffle(RandomNumberGenerator random)
  {
    cards.RandomlyPermute(random);
  }

  readonly List<C> cards = new List<C>();
}
#endregion

#region DeckLocation
enum DeckLocation
{
  Top=0, Bottom, Random
}
#endregion

#region GameState
sealed class GameState
{
  public GameState(Game game)
  {
    if(game == null) throw new ArgumentNullException();
    this.game = game;
  }

  readonly Game game;
  readonly Board board = new Board();
  readonly Deck<DungeonCard> dungeonCards = new Deck<DungeonCard>();
  readonly Deck<EncounterCard> encounterCards = new Deck<EncounterCard>(), encounterDiscard = new Deck<EncounterCard>();
  readonly Deck<MonsterClass> monsterCards = new Deck<MonsterClass>(), monsterDiscard = new Deck<MonsterClass>();
  readonly Deck<MonsterClass> xpPile = new Deck<MonsterClass>();
  readonly Deck<TreasureCard> treasureCards = new Deck<TreasureCard>(), treasureDiscard = new Deck<TreasureCard>();
}
#endregion

#region Tile
sealed class Tile
{
  public Tile(DungeonCard card, SquareVersion[,] squares, Rotation rotation)
  {
    if(card == null || squares == null) throw new ArgumentNullException();
    this.squares   = squares;
    this.card      = card;
    this.rotation  = rotation;
    connectedTiles = new Tile[4];
  }

  public string Name
  {
    get { return card.Name; }
  }

  readonly SquareVersion[,] squares;
  readonly Tile[] connectedTiles;
  readonly DungeonCard card;
  readonly Rotation rotation;
}
#endregion

} // namespace BirdhouseManor
