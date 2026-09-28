using System;
using System.IO;
using System.Text;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.UI;

namespace LibraryManagement;

public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            var fileManager = new FileManager();
            fileManager.EnsureDataFiles();    

            var library = new LibrarySystem(fileManager);
            var app = new ConsoleApp(library);
            app.Run();
        }
        catch (EndOfStreamException)
        {
          
        }
        catch (LibraryException ex)
        {
            Console.WriteLine("Fatal error: " + ex.Message);
        }
    }
}
