using System;


/// <summary>
/// Laittaa pelin k?yntiin.
/// </summary>
static class Ohjelma
{
#if WINDOWS || XBOX
    static void Main(string[] args)
    {
        using (Konetus game = new Konetus())
        {
#if !DEBUG
            game.IsFullScreen = true;
#endif
            game.Run();
        }
    }
#endif
}
