using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using AdamMil.Utilities;

namespace BirdhouseManor
{

#region ActionCardBase
public abstract class ActionCardBase : CardBase
{
  protected ActionCardBase() { }
  
  protected ActionCardBase(XmlNode node) : base(node)
  {
    IsAttack = node.GetBoolAttribute("isAttack");
    IsMove   = node.GetBoolAttribute("isMove");
  }

  public bool IsAttack { get; protected set; }
  public bool IsMove { get; protected set; }
}
#endregion

#region Attack
sealed class Attack : NamedGameObject
{
  internal Attack(XmlNode node) : base(node)
  {
    AttackBonusText = node.GetAttributeValue("bonus");
    DamageText      = node.GetAttributeValue("damage");
    MissDamageText  = node.GetAttributeValue("missDamage");
  }

  public string AttackBonusText { get; private set; }
  public string DamageText { get; private set; }
  public string MissDamageText { get; private set; }
}
#endregion

#region CardBase
public abstract class CardBase : NamedGameObjectWithCount
{
  protected CardBase() { }

  protected CardBase(XmlNode node) : base(node)
  {
    AttackBonusText   = node.GetAttributeValue("attackBonus");
    DamageText        = node.GetAttributeValue("damage");
    DescriptionMarkup = node.GetAttributeValue("description");
    FlavorText        = node.GetAttributeValue("flavor");

    XmlNode descriptionNode = node.SelectSingleNode("description");
    if(descriptionNode != null) DescriptionMarkup = descriptionNode.InnerXml.Trim();

    FlavorText = node.SelectValue("flavor", FlavorText);
  }

  public string AttackBonusText { get; protected set; }
  public string DamageText { get; protected set; }
  public string DescriptionMarkup { get; protected set; }
  public string FlavorText { get; protected set; }

  public abstract bool CanTakeEffect();
  public abstract void Use();
}
#endregion

#region DungeonCard
sealed class DungeonCard : NamedGameObjectWithCount
{
  internal DungeonCard(XmlNode node, List<DungeonCardSquare>[] squares, Size tileSize) : base(node)
  {
    Difficulty = (DungeonCardDifficulty)Enum.Parse(typeof(DungeonCardDifficulty), node.GetAttributeValue("difficulty"), true);
    DrawType   = (DungeonCardDrawType)Enum.Parse(typeof(DungeonCardDrawType), node.GetAttributeValue("type"), true);
    Image      = node.GetAttributeValue("src");
    Name       = node.GetAttributeValue("name");

    this.squares = new DungeonCardSquare[tileSize.Height, tileSize.Width][];
    for(int i=0,y=0; y<tileSize.Height; y++)
    {
      for(int x=0; x<tileSize.Width; i++,x++) this.squares[y, x] = squares[i].ToArray();
    }
  }

  public DungeonCardDifficulty Difficulty { get; private set; }
  public DungeonCardDrawType DrawType { get; private set; }
  public string Image { get; private set; }

  public DungeonCardSquare[] GetSquareVersions(int x, int y)
  {
    return squares[y, x];
  }

  readonly DungeonCardSquare[,][] squares;
}
#endregion

#region DungeonCardDifficulty
enum DungeonCardDifficulty : byte
{
  /// <summary>Drawing the card will not force the player to drawn an encounter card.</summary>
  Easy,
  /// <summary>Drawing the card will force the player to drawn an encounter card.</summary>
  Hard
}
#endregion

#region DungeonCardSquare
struct DungeonCardSquare
{
  internal DungeonCardSquare(ReadOnlyCollection<SquareVersion> versions, Rotation rotation)
  {
    Versions = versions;
    Rotation = rotation;
  }

  public readonly ReadOnlyCollection<SquareVersion> Versions;
  public readonly Rotation Rotation;
}
#endregion

#region DungeonCardDrawType
enum DungeonCardDrawType : byte
{
  /// <summary>The card will be added to the deck of dungeon tiles by default.</summary>
  Random=0,
  /// <summary>The card will not be added to the deck of dungeon tiles by default.</summary>
  Special,
  /// <summary>The card will not be added to the deck of dungeon tiles, but will be automatically placed at the beginning of
  /// the game, by default.
  /// </summary>
  Start
}
#endregion

#region EncounterCard
public abstract class EncounterCard : CardBase
{
  protected EncounterCard() { }

  protected EncounterCard(XmlNode node) : base(node)
  {
    Type = (EncounterCardType)Enum.Parse(typeof(EncounterCardType), node.LocalName, true);
  }

  public EncounterCardType Type { get; protected set; }

  public override string ToString()
  {
    return Type.ToString() + ": " + base.ToString();
  }
}
#endregion

#region EncounterCardType
public enum EncounterCardType
{
  Environment, Event, Trap
}
#endregion

#region EntityClass
abstract class EntityClass : NamedGameObjectWithCount
{
  internal EntityClass(XmlNode node) : base(node)
  {
    FlavorText = node.GetAttributeValue("flavor");
    Race       = node.GetAttributeValue("race");
    TokenImage = node.GetAttributeValue("tokenImg");

    FlavorText = node.SelectValue("flavor", FlavorText);
  }

  public string FlavorText { get; private set; } 
  public string Race { get; private set; }
  public string TokenImage { get; private set; }
}
#endregion

#region Game
sealed class Game
{
  public Game(string xmlFile)
  {
    // load the game.xml file and validate it against the schema
    XmlDocument doc = new XmlDocument();
    using(StreamReader schema = new StreamReader(Path.Combine(Program.DataPath, "game.xsd")))
    {
      doc.Schemas.Add(XmlSchema.Read(schema, null));
    }

    GameDirectory = Path.GetDirectoryName(Path.GetFullPath(xmlFile));

    XmlReaderSettings settings = new XmlReaderSettings();
    settings.CloseInput       = true;
    settings.IgnoreComments   = true;
    settings.IgnoreWhitespace = true;
    using(XmlReader xmlReader = XmlReader.Create(File.OpenText(xmlFile), settings)) doc.Load(xmlReader);
    doc.Validate(null);

    // check that the schema version is supported
    if(doc.DocumentElement.GetInt32Attribute("schemaVersion") != 1)
    {
      throw new NotSupportedException("Only version 1 game files are supported by this version of Birdhouse Manor.");
    }

    LoadAssembly(doc.DocumentElement.GetAttributeValue("assembly"));
    LoadSquares(doc.DocumentElement.SelectSingleNode("squareTypes"),
                doc.DocumentElement.SelectSingleNode("squares"));
    LoadTokenTypes(doc.DocumentElement.SelectSingleNode("tokens"));
    LoadDungeonCards(doc.DocumentElement.SelectSingleNode("dungeonCards"));
    LoadEncounterCards(doc.DocumentElement.SelectSingleNode("encounterCards"));
    LoadTreasureCards(doc.DocumentElement.SelectSingleNode("treasureCards"));
    LoadPowerCards(doc.DocumentElement.SelectSingleNode("powers"));
    LoadSkills();
    LoadHeroes(doc.DocumentElement.SelectSingleNode("heroes"));
    LoadMonsters(doc.DocumentElement.SelectSingleNode("monsters"));
    LoadMonsters(doc.DocumentElement.SelectSingleNode("villains"));
  }

  /// <summary>Gets the directory containing the game.xml file from which the <see cref="Game"/> object was initialized.</summary>
  public string GameDirectory { get; private set; }

  /// <summary>Gets the size of a normal square, in pixels. This does not represent the number of pixels displayed on the screen,
  /// but the number of pixels in the source image files. This property exists because not all square images are the same size --
  /// some may be larger or smaller than the normal square size.
  /// </summary>
  public Size SquareSize { get; private set; }

  /// <summary>Gets the size of a dungeon card in squares.</summary>
  public Size TileSize { get; private set; }

  void LoadAssembly(string path)
  {
    if(!string.IsNullOrEmpty(path))
    {
      if(Path.IsPathRooted(path)) throw new XmlSchemaValidationException("The assembly path must be relative.");
      assembly = System.Reflection.Assembly.LoadFile(Path.Combine(GameDirectory, path));
    }
  }

  void LoadDungeonCards(XmlNode dungeonCardsNode)
  {
    TileSize = GameXmlUtilities.ParseSize(dungeonCardsNode.GetAttributeValue("tileSize"));

    // build a map of square versions by character
    Dictionary<char, ReadOnlyCollection<SquareVersion>> squaresByChar = new Dictionary<char, ReadOnlyCollection<SquareVersion>>();
    Dictionary<char, List<SquareVersion>> tempSquaresByChar = new Dictionary<char, List<SquareVersion>>();
    char continuationChar = '\0';
    foreach(SquareType type in squareTypes.Values)
    {
      foreach(SquareVersion version in type.Versions)
      {
        List<SquareVersion> versions;
        if(!tempSquaresByChar.TryGetValue(version.Character, out versions)) // if we haven't seen this character before...
        {
          tempSquaresByChar[version.Character] = versions = new List<SquareVersion>(); // create a version new list for it
        }
        else if(versions[0].Square != version.Square) // otherwise, if we've seen it before with a different square type...
        {
          throw new XmlSchemaValidationException("Square character " + version.Character.ToString() +
                                                 " was defined for multiple square types.");
        }
        else if(versions[0].Size != version.Size) // or if we've seen it with a different size...
        {
          throw new XmlSchemaValidationException("Square character " + version.Character.ToString() +
                                                 " was defined with different sizes.");
        }

        // if the character maps to a continuation square, keep track of the character
        if(StringUtility.ExactlyEquals(version.Square.Name, SquareType.ContinuationName))
        {
          if(continuationChar != '\0')
          {
            throw new XmlSchemaValidationException("The continuation square character was defined multiple times.");
          }
          continuationChar = version.Character;
        }

        versions.Add(version);
      }
    }

    // convert the lists to read-only collections
    foreach(KeyValuePair<char, List<SquareVersion>> pair in tempSquaresByChar)
    {
      squaresByChar[pair.Key] = new ReadOnlyCollection<SquareVersion>(pair.Value.ToArray());
    }

    // load the default floor
    DungeonCardSquare defaultFloor;
    {
      ReadOnlyCollection<SquareVersion> defaultFloorVersions;
      if(!squaresByChar.TryGetValue(dungeonCardsNode.GetAttributeValue("defaultFloor")[0], out defaultFloorVersions))
      {
        throw new XmlSchemaValidationException("Undefined default floor character " +
                                               dungeonCardsNode.GetAttributeValue("defaultFloor"));
      }
      defaultFloor = new DungeonCardSquare(defaultFloorVersions, Rotation.None);
    }

    // go through all the dungeon cards
    List<DungeonCardSquare>[] squares = new List<DungeonCardSquare>[TileSize.Width * TileSize.Height];
    for(int i=0; i<squares.Length; i++) squares[i] = new List<DungeonCardSquare>();

    foreach(XmlNode cardNode in dungeonCardsNode.ChildNodes)
    {
      string errorName = cardNode.GetAttributeValue("type") + ":" + cardNode.GetAttributeValue("name");

      // process each layer node
      foreach(XmlNode layerNode in cardNode.SelectNodes("layer"))
      {
        char[] layer = ParseLayer(layerNode.InnerText);
        if(layer.Length != squares.Length) throw new XmlSchemaValidationException("Invalid layer size for card: " + errorName);

        for(int i=0; i<squares.Length; i++)
        {
          ReadOnlyCollection<SquareVersion> versions;
          if(!squaresByChar.TryGetValue(layer[i], out versions))
          {
            throw new XmlSchemaValidationException("Undefined square character " + layer[i].ToString() + " in card " +
                                                   errorName);
          }

          string squareType = versions[0].Square.Name;

          // if it's the first square in this position and it's not a floor or wall, add the default floor first
          if(squares[i].Count == 0 && !StringUtility.ExactlyEquals(squareType, SquareType.FloorName) &&
             !StringUtility.ExactlyEquals(squareType, SquareType.WallName))
          {
            squares[i].Add(defaultFloor);
          }

          // if it's a blank square or a continuation, don't add anything else
          if(StringUtility.ExactlyEquals(squareType, SquareType.BlankName) || layer[i] == continuationChar) continue;

          Size size = versions[0].Size;
          Rotation rotation = Rotation.None;
          // if it's an object covering multiple squares, then we need to examine nearby continuations to determine its rotation
          if(size.Width > 1 || size.Height > 1)
          {
            // the character is considered to be at the top-left corner of the object, and continuation characters must fill the
            // other characters in the object's size. so a 2x2 object can be orientated in these ways:
            //   O* *O ** ** and a 2x1 object in these ways: O* O *O *
            //   ** ** *O O*                                    *    O
            // so we have to check the four orientations
            int foundRotation = -1;
            for(int rot=0; rot<4; rot++)
            {
              int xi, yi, width, height;
              // calculate the direction from the start point to the other corner
              if(rot == 0) { xi = yi = 1; }
              else if(rot == 1) { xi = -1; yi = 1; }
              else if(rot == 2) { xi = yi = -1; }
              else { xi = 1; yi = -1; }

              // calculate the rotated width and height
              if((rot & 1) == 0) { width = size.Width; height = size.Height; }
              else { width = size.Height; height = size.Width; }

              // now check each square in the rotated bounds (except the start square) to see that it's a continuation
              bool found = false;
              for(int ox=0,tx=i%TileSize.Width; ox<width; tx+=xi, ox++)
              {
                for(int oy=0,ty=i/TileSize.Width; oy<height; ty+=yi, oy++)
                {
                  if(tx < 0 || tx >= TileSize.Width || ty < 0 || ty >= TileSize.Width) goto done;
                  int squareIndex = ty*TileSize.Width + tx;
                  if(squareIndex != i && layer[squareIndex] != continuationChar) goto done;
                }
              }
              found = true;

              done:
              if(found)
              {
                if(foundRotation != -1) // if we already found a rotation, a second one makes it ambiguous
                {
                  throw new XmlSchemaValidationException("Layout of character " + layer[i].ToString() + " in tile " + errorName +
                                                         " was ambiguous because it matched multiple rotations.");
                }
                foundRotation = rot;
              }
            }

            if(foundRotation == -1)
            {
              throw new XmlSchemaValidationException("No valid rotation was found for character " + layer[i].ToString() +
                                                     " in tile " + errorName);
            }

            rotation = (Rotation)foundRotation; // convert the rotation index into a SquareRotation value
          }

          squares[i].Add(new DungeonCardSquare(versions, rotation));
        }
      }

      // process each square node
      foreach(XmlNode squareNode in cardNode.SelectNodes("square"))
      {
        char c = squareNode.GetAttributeValue("character")[0];
        ReadOnlyCollection<SquareVersion> versions;
        if(!squaresByChar.TryGetValue(c, out versions))
        {
          throw new XmlSchemaValidationException("Undefined square character " + c.ToString() + " in card " + errorName);
        }

        Point location = ParseLocation(squareNode.GetAttributeValue("location"));
        Rotation rotation =
          (Rotation)Enum.Parse(typeof(Rotation), squareNode.GetAttributeValue("rotation"), true);

        // ensure it's a valid rotation of the item by checking the opposite corner
        Size size = versions[0].Size;

        // calculate the offset from the tile location to its other corner
        size.Width--; // reduce the dimensions by one so that adding the dimension will reference the other edge, inclusively
        size.Height--;
        int x, y;
        if(rotation == Rotation.None) { x = size.Width; y = size.Height; }
        else if(rotation == Rotation.Right) { x = -size.Height; y = size.Width; }
        else if(rotation == Rotation.UpsideDown) { x = -size.Width; y = -size.Height; }
        else { x = size.Height; y = -size.Width; }

        // add the location to the offset to get the position of the other corner, and validate that it's in bounds
        x += location.X;
        y += location.Y;
        if(x < 0 || x >= TileSize.Width || x < 0 || x >= TileSize.Height)
        {
          throw new XmlSchemaValidationException("Square location " + squareNode.GetAttributeValue("location") +
                                                 " out of bounds for card " + errorName +
                                                 " (considering rotation of the object, if applicable)");
        }

        squares[location.Y * TileSize.Width + location.X].Add(new DungeonCardSquare(versions, rotation));
      }

      // create the card
      dungeonCards.Add(new DungeonCard(cardNode, squares, TileSize));

      // clear the lists in preparation for the next card
      foreach(var list in squares) list.Clear();
    }
  }

  void LoadEncounterCards(XmlNode cardsNode)
  {
    foreach(XmlNode node in cardsNode.ChildNodes) encounterCards.Add(new XmlEncounterCard(node));
    LoadFromAssembly(encounterCards);
  }

  void LoadFromAssembly<T>(List<T> list)
  {
    if(assembly != null)
    {
      foreach(Type type in assembly.GetTypes())
      {
        if(type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
        {
          System.Reflection.ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
          if(constructor != null) list.Add((T)constructor.Invoke(null));
        }
      }
    }
  }

  void LoadHeroes(XmlNode heroesNode)
  {
    foreach(XmlNode node in heroesNode.ChildNodes) heroes.Add(new HeroClass(node, powerCards, skills));
  }

  void LoadMonsters(XmlNode monstersNode)
  {
    if(monstersNode != null)
    {
      foreach(XmlNode node in monstersNode.ChildNodes) monsters.Add(new MonsterClass(node));
    }
  }

  void LoadPowerCards(XmlNode powersNode)
  {
    foreach(XmlNode node in powersNode.ChildNodes) powerCards.Add(new XmlPowerCard(node));
    LoadFromAssembly(powerCards);
  }

  void LoadSkills()
  {
    LoadFromAssembly(skills);
  }

  void LoadSquares(XmlNode typesNode, XmlNode squaresNode)
  {
    Dictionary<string, List<SquareVersion>> squareVersions = new Dictionary<string, List<SquareVersion>>();

    SquareSize = GameXmlUtilities.ParseSize(squaresNode.GetAttributeValue("squareSize"));

    // load the square versions
    foreach(XmlNode squareNode in squaresNode.ChildNodes)
    {
      string type = squareNode.GetAttributeValue("type");
      List<SquareVersion> versions;
      if(!squareVersions.TryGetValue(type, out versions)) squareVersions[type] = versions = new List<SquareVersion>();
      versions.Add(new SquareVersion(squareNode));
    }

    // load square type names defined in the XML file
    Dictionary<string, bool> types = new Dictionary<string, bool>();
    if(typesNode != null)
    {
      foreach(XmlNode typeNode in typesNode.ChildNodes)
      {
        string type = typeNode.GetAttributeValue("name");
        if(types.ContainsKey(type)) throw new XmlSchemaValidationException("Square type was defined multiple times: " + type);
        types.Add(type, typeNode.GetBoolAttribute("isObject"));
      }
    }

    // add built-in type names that weren't defined in the XML file
    foreach(string typeName in new string[] { SquareType.BlankName, SquareType.ContinuationName, SquareType.FloorName,
                                              SquareType.SpawnName, SquareType.WallName })
    {
      if(!types.ContainsKey(typeName)) types[typeName] = false;
    }

    // check that there are no versions for square types that don't exist
    foreach(string typeName in squareVersions.Keys)
    {
      if(!types.ContainsKey(typeName)) throw new XmlSchemaValidationException("Undefined square type: " + typeName);
    }

    // finally, construct the SquareType objects from the type and version information
    foreach(KeyValuePair<string, bool> pair in types)
    {
      List<SquareVersion> versions;
      if(!squareVersions.TryGetValue(pair.Key, out versions))
      {
        throw new XmlSchemaValidationException("Square type has no squares defined: " + pair.Key);
      }
      squareTypes.Add(pair.Key, new SquareType(pair.Key, pair.Value, versions));
    }
  }

  void LoadTokenTypes(XmlNode tokensNode)
  {
    if(tokensNode != null)
    {
      foreach(XmlNode tokenNode in tokensNode.ChildNodes)
      {
        TokenType tokenType = new TokenType(tokenNode);
        if(tokenTypes.ContainsKey(tokenType.Name))
        {
          throw new XmlSchemaValidationException("Token type was defined multiple types: " + tokenType.Name);
        }
        tokenTypes.Add(tokenType.Name, tokenType);
      }
    }
  }

  void LoadTreasureCards(XmlNode cardsNode)
  {
    foreach(XmlNode node in cardsNode.ChildNodes) treasureCards.Add(new XmlTreasureCard(node));
    LoadFromAssembly(treasureCards);
  }

  readonly Dictionary<string, SquareType> squareTypes = new Dictionary<string, SquareType>();
  readonly Dictionary<string, TokenType> tokenTypes = new Dictionary<string, TokenType>();
  readonly List<DungeonCard> dungeonCards = new List<DungeonCard>();
  readonly List<EncounterCard> encounterCards = new List<EncounterCard>();
  readonly List<HeroClass> heroes = new List<HeroClass>();
  readonly List<MonsterClass> monsters = new List<MonsterClass>();
  readonly List<PowerCard> powerCards = new List<PowerCard>();
  readonly List<Skill> skills = new List<Skill>();
  readonly List<TreasureCard> treasureCards = new List<TreasureCard>();
  System.Reflection.Assembly assembly;

  static char[] ParseLayer(string layerText)
  {
    List<char> chars = new List<char>();
    foreach(char c in layerText)
    {
      if(!char.IsWhiteSpace(c)) chars.Add(c);
    }
    return chars.ToArray();
  }

  static Point ParseLocation(string location)
  {
    int comma = location.IndexOf(','), x = int.Parse(location.Substring(0, comma), CultureInfo.InvariantCulture);
    int y = int.Parse(location.Substring(comma+1), CultureInfo.InvariantCulture);
    if(x <= 0 || y <= 0) throw new XmlSchemaValidationException("Invalid size: " + location);
    return new Point(x, y);
  }
}
#endregion

#region GameXmlUtilities
static class GameXmlUtilities
{
  /// <summary>Parses a size of the format WIDTHxHEIGHT (e.g. 2x3) and returns it as a <see cref="Size"/> object. Sizes with
  /// zero or negative dimensions are not allowed and will cause an exception to be thrown.
  /// </summary>
  public static Size ParseSize(string size)
  {
    int x = size.IndexOf('x'), width = int.Parse(size.Substring(0, x), CultureInfo.InvariantCulture);
    int height = int.Parse(size.Substring(x+1), CultureInfo.InvariantCulture);
    if(width <= 0 || height <= 0) throw new XmlSchemaValidationException("Invalid size: " + size);
    return new Size(width, height);
  }
}
#endregion

#region HeroClass
sealed class HeroClass : EntityClass
{
  internal HeroClass(XmlNode node, List<PowerCard> availablePowers, List<Skill> availableSkills) : base(node)
  {
    List<LevelDescription> levels = new List<LevelDescription>();
    foreach(XmlNode levelNode in node.SelectNodes("level"))
    {
      levels.Add(new LevelDescription(levelNode, availablePowers, availableSkills));
    }
    Levels = new ReadOnlyCollection<LevelDescription>(levels.ToArray());
  }

  public ReadOnlyCollection<LevelDescription> Levels { get; private set; }
}
#endregion

#region LevelDescription
sealed class LevelDescription
{
  internal LevelDescription(XmlNode node, List<PowerCard> availablePowers, List<Skill> availableSkills)
  {
    ACBonus    = node.GetInt32Attribute("ac");
    Class      = node.GetAttributeValue("class");
    HPBonus    = node.GetInt32Attribute("hp");
    Level      = node.GetInt32Attribute("level");
    SpeedBonus = node.GetInt32Attribute("speed");
    SurgeBonus = node.GetInt32Attribute("surge");

    NewSkills = new ReadOnlyCollection<Skill>(StringUtility.Split(node.GetAttributeValue("skills"), ',', sn =>
    {
      string name = sn.Trim();
      Skill skill = availableSkills.FirstOrDefault(s => StringUtility.ExactlyEquals(s.Name, name));
      if(skill == null) throw new XmlSchemaValidationException("A skill named \"" + name + "\" could not be found.");
      return skill;
    }, StringSplitOptions.RemoveEmptyEntries));

    List<PowerSelection> powers = new List<PowerSelection>();
    foreach(XmlNode powerNode in node.SelectNodes("powers/power"))
    {
      PowerSelection powerSelection =  new PowerSelection(powerNode);
      if(!string.IsNullOrEmpty(powerSelection.Name) && !availablePowers.Any(p => powerSelection.Name.ExactlyEquals(p.Name)))
      {
        throw new XmlSchemaValidationException("A power named \"" + powerSelection.Name + "\" could be found.");
      }
      else if(!string.IsNullOrEmpty(powerSelection.Class) &&
              !availablePowers.Any(p => powerSelection.Class.ExactlyEquals(p.Class)))
      {
        throw new XmlSchemaValidationException("No power class named \"" + powerSelection.Class + "\" could be found.");
      }
      powers.Add(powerSelection);
    }
    NewPowers = new ReadOnlyCollection<PowerSelection>(powers.ToArray());
  }

  public int ACBonus { get; private set; }
  public string Class { get; private set; }
  public int HPBonus { get; private set; }
  public int Level { get; private set; }
  public ReadOnlyCollection<PowerSelection> NewPowers { get; private set; }
  public ReadOnlyCollection<Skill> NewSkills { get; private set; }
  public int SpeedBonus { get; private set; }
  public int SurgeBonus { get; private set; }

  public override string ToString()
  {
    return "Level " + Level.ToString();
  }
}
#endregion

#region MonsterClass
class MonsterClass : EntityClass
{
  internal MonsterClass(XmlNode node) : base(node)
  {
    AC    = node.GetInt32Attribute("ac");
    Class = node.GetAttributeValue("class");
    HP    = node.GetInt32Attribute("hp");
    Size  = node.GetInt32Attribute("size");
    XP    = node.GetInt32Attribute("xp", 0);

    foreach(XmlNode attackNode in node.SelectNodes("attacks/attack")) attacks.Add(new Attack(attackNode));
    foreach(XmlNode powerNode in node.SelectNodes("powers/power")) powers.Add(new XmlMonsterPower(powerNode));
    foreach(XmlNode tacticNode in node.SelectNodes("tactics/tactic")) tactics.Add(new XmlTactic(tacticNode));
  }

  public int AC { get; protected set; }
  public string Class { get; protected set; }
  public int HP { get; protected set; }
  public int Size { get; protected set; }
  public int XP { get; protected set; }

  readonly List<Attack> attacks = new List<Attack>();
  readonly List<MonsterPower> powers = new List<MonsterPower>();
  readonly List<Tactic> tactics = new List<Tactic>();
}
#endregion

#region MonsterPower
public abstract class MonsterPower : ActionCardBase
{
  protected MonsterPower() { }
  protected MonsterPower(XmlNode node) : base(node) { }
}
#endregion

#region NamedGameObject
public abstract class NamedGameObject
{
  protected NamedGameObject() { }

  protected NamedGameObject(XmlNode node)
  {
    Name = node.GetAttributeValue("name");
  }

  public string Name { get; protected set; }

  public override string ToString()
  {
    return Name ?? base.ToString();
  }
}
#endregion

#region NamedGameObjectWithCount
public abstract class NamedGameObjectWithCount : NamedGameObject
{
  protected NamedGameObjectWithCount() { }

  protected NamedGameObjectWithCount(XmlNode node) : base(node)
  {
    Count = node.GetInt32Attribute("count", 0);
  }

  public int Count { get; protected set; }
}
#endregion

#region PowerCard
public abstract class PowerCard : ActionCardBase
{
  protected PowerCard() { }

  protected PowerCard(XmlNode node) : base(node)
  {
    Type  = (PowerCardType)Enum.Parse(typeof(PowerCardType), node.LocalName, true);
    Class = node.GetAttributeValue("class");
  }

  public string Class { get; protected set; }
  public PowerCardType Type { get; protected set; }

  public override string ToString()
  {
    return Type.ToString() + ": " + base.ToString();
  }
}
#endregion

#region PowerCardType
public enum PowerCardType
{
  AtWill, Daily, Utility
}
#endregion

#region PowerSelection
sealed class PowerSelection
{
  internal PowerSelection(XmlNode node)
  {
    Name = node.GetAttributeValue("name");
    if(string.IsNullOrEmpty(Name))
    {
      Class = node.GetAttributeValue("class");
      if(string.IsNullOrEmpty(Class) || string.IsNullOrEmpty(node.GetAttributeValue("type")))
      {
        throw new XmlSchemaValidationException("Class and type are required if a name is not given for a power selection.");
      }
      Count = node.GetInt32Attribute("count");
      Type  = (PowerCardType)Enum.Parse(typeof(PowerCardType), node.GetAttributeValue("type"), true);
    }
    else
    {
      if(!string.IsNullOrEmpty(node.GetAttributeValue("class")) || !string.IsNullOrEmpty(node.GetAttributeValue("type")))
      {
        throw new XmlSchemaValidationException("Class and type are prohibited if a name (\"" + Name +
                                               "\") is given for a power selection.");
      }
    }
  }

  public string Class { get; private set; }
  public int Count { get; private set; }
  public string Name { get; private set; }
  public PowerCardType Type { get; private set; }

  public override string ToString()
  {
    return !string.IsNullOrEmpty(Name) ? Name : Count.ToString() + " " + Class + " " + Type.ToString();
  }
}
#endregion

#region Rotation
enum Rotation : byte
{
  /// <summary>The square is not rotated.</summary>
  None=0,
  /// <summary>The square has been rotated 90 degrees clockwise (to the right).</summary>
  Right,
  /// <summary>The square has been rotated 90 degrees counter-clockwise (to the left).</summary>
  Left,
  /// <summary>The square has been rotated 180 degrees, causing it to be upside down.</summary>
  UpsideDown
}
#endregion

#region SquarePlacement
enum SquarePlacement : byte
{
  Center=0, Fill, Random
}
#endregion

#region SquareType
sealed class SquareType
{
  public const string BlankName = "blank", FloorName = "floor", SpawnName = "spawn", WallName = "wall";
  internal const string ContinuationName = "continuation";

  internal SquareType(string name, bool isObject, ICollection<SquareVersion> versions)
  {
    Name     = name;
    IsObject = isObject;
    Versions = new ReadOnlyCollection<SquareVersion>(versions.ToArray());
    foreach(SquareVersion version in Versions) version.Square = this;
  }

  public string Name { get; private set; }
  public bool IsObject { get; private set; }
  public ReadOnlyCollection<SquareVersion> Versions { get; private set; }

  public override string ToString()
  {
    return Name;
  }
}
#endregion

#region SquareVersion
sealed class SquareVersion
{
  internal SquareVersion(XmlNode node)
  {
    Character = node.GetAttributeValue("character")[0];
    Image     = node.GetAttributeValue("src");
    Placement = (SquarePlacement)Enum.Parse(typeof(SquarePlacement), node.GetAttributeValue("placement"), true);
    Size      = GameXmlUtilities.ParseSize(node.GetAttributeValue("size"));
  }

  public char Character { get; private set; }
  public string Image { get; private set; }
  public SquarePlacement Placement { get; private set; }
  public Size Size { get; private set; }
  public SquareType Square { get; internal set; }
}
#endregion

#region Skill
public abstract class Skill : NamedGameObject
{
  public string DescriptionText { get; protected set; }
}
#endregion

#region Tactic
abstract class Tactic
{
  protected internal Tactic(XmlNode node)
  {
    DescriptionMarkup = node.GetAttributeValue("description");

    XmlNode descriptionNode = node.SelectSingleNode("description");
    if(descriptionNode != null) DescriptionMarkup = descriptionNode.InnerXml.Trim();
  }

  public string DescriptionMarkup { get; protected set; }

  public abstract bool ShouldUse();
  public abstract void Use();
}
#endregion

#region TokenType
sealed class TokenType
{
  internal TokenType(XmlNode tokenNode)
  {
    DescriptionText = tokenNode.GetAttributeValue("description");
    IsCondition     = tokenNode.GetBoolAttribute("isCondition");
    Name            = tokenNode.GetAttributeValue("name");
    IconImage       = tokenNode.GetAttributeValue("iconImg");
    TokenImage      = tokenNode.GetAttributeValue("tokenImg");
  }

  public string DescriptionText { get; private set; }
  public string IconImage { get; private set; }

  /// <summary>Gets whether the token represents a tangible condition affecting an entity, such as being immobilized.</summary>
  public bool IsCondition { get; private set; }

  public string Name { get; private set; }
  public string TokenImage { get; private set; }
}
#endregion

#region TreasureCard
public abstract class TreasureCard : ActionCardBase
{
  protected TreasureCard() { }

  protected TreasureCard(XmlNode node) : base(node)
  {
    Type = (TreasureCardType)Enum.Parse(typeof(TreasureCardType), node.LocalName, true);
  }

  public TreasureCardType Type { get; protected set; }

  public override string ToString()
  {
    return Type.ToString() + ": " + base.ToString();
  }
}
#endregion

#region TreasureCardType
public enum TreasureCardType
{
  Blessing, Fortune, Item
}
#endregion

#region XmlEncounterCard
sealed class XmlEncounterCard : EncounterCard
{
  internal XmlEncounterCard(XmlNode node) : base(node) { }

  public override bool CanTakeEffect()
  {
    throw new NotImplementedException();
  }

  public override void Use()
  {
    throw new NotImplementedException();
  }
}
#endregion

#region XmlMonsterPower
sealed class XmlMonsterPower : MonsterPower
{
  internal XmlMonsterPower(XmlNode node) : base(node) { }

  public override bool CanTakeEffect()
  {
    throw new NotImplementedException();
  }

  public override void Use()
  {
    throw new NotImplementedException();
  }
}
#endregion

#region XmlPowerCard
sealed class XmlPowerCard : PowerCard
{
  internal XmlPowerCard(XmlNode node) : base(node) { }

  public override bool CanTakeEffect()
  {
    throw new NotImplementedException();
  }

  public override void Use()
  {
    throw new NotImplementedException();
  }
}
#endregion

#region XmlTactic
sealed class XmlTactic : Tactic
{
  internal XmlTactic(XmlNode node) : base(node) { }

  public override bool ShouldUse()
  {
    throw new NotImplementedException();
  }

  public override void Use()
  {
    throw new NotImplementedException();
  }
}
#endregion

#region XmlTreasureCard
sealed class XmlTreasureCard : TreasureCard
{
  internal XmlTreasureCard(XmlNode node) : base(node) { }

  public override bool CanTakeEffect()
  {
    throw new NotImplementedException();
  }

  public override void Use()
  {
    throw new NotImplementedException();
  }
}
#endregion

} // namespace BirdhouseManor