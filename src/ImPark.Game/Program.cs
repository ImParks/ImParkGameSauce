namespace ImPark.Game;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using var game = new ImParkGame();
        game.Run();
    }
}
