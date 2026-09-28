using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LibraryManagement.Utils;


public static class InputHelper
{
    public static string ReadLine(string prompt)
    {
        Console.Write(prompt);
        string? line = Console.ReadLine();
        if (line == null)
            throw new EndOfStreamException("Input ended.");
        return line.Trim();
    }


    public static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            string value = ReadLine(prompt);
            if (value.Length == 0)
            {
                ShowError("This field cannot be empty.");
                continue;
            }
            if (value.Contains('|'))
            {
                ShowError("The '|' character is not allowed.");
                continue;
            }
            return value;
        }
    }


    public static int ReadPositiveInt(string prompt)
    {
        while (true)
        {
            string text = ReadLine(prompt);
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) && number > 0)
                return number;

            ShowError("Please enter a positive whole number (for example 3).");
        }
    }


    public static decimal ReadPositiveDecimal(string prompt)
    {
        while (true)
        {
            string text = ReadLine(prompt).Replace(',', '.');
            if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount))
            {
                amount = Math.Round(amount, 2);
                if (amount > 0)
                    return amount;
            }
            ShowError("Please enter a positive amount (for example 2.50).");
        }
    }

    public static bool Confirm(string question)
    {
        while (true)
        {
            string answer = ReadLine(question + " (y/n): ").ToLowerInvariant();
            if (answer == "y" || answer == "yes")
                return true;
            if (answer == "n" || answer == "no")
                return false;
            ShowError("Please answer y or n.");
        }
    }


    public static string ReadPassword(string prompt)
    {
        if (Console.IsInputRedirected)
            return ReadLine(prompt);

        try
        {
            Console.Write(prompt);
            var sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return sb.ToString();
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
            }
        }
        catch (InvalidOperationException)
        {

            Console.WriteLine();
            return ReadLine(prompt);
        }
    }

    public static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[!] " + message);
        Console.ResetColor();
    }

    public static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[OK] " + message);
        Console.ResetColor();
    }

    public static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
