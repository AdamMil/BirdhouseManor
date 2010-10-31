using System;
using System.IO;

namespace BirdhouseManor
{

static class Program
{
  /// <summary>Gets the path to the program's data directory.</summary>
  public static string DataPath
  {
    get
    {
      if(_dataPath == null)
      {
        string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        _dataPath = Path.Combine(exePath, "data");
        // TODO: the next line is for development. remove it?
        if(!Directory.Exists(_dataPath)) _dataPath = Path.Combine(exePath, "../../data");
        if(!Directory.Exists(_dataPath)) throw new InvalidOperationException("The data directory cannot be found.");
      }
      return _dataPath;
    }
  }

  static void Main()
  {
    Game game = new Game(Path.Combine(DataPath, "games/CastleRavenloft/game.xml"));
  }

  static string _dataPath;
}

} // namespace BirdhouseManor