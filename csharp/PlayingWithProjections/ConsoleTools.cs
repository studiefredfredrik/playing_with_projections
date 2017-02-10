using System;

namespace PlayingWithProjections
{
    public static class ConsoleTools
    {
        public static void WriteColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
