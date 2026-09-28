using System;

namespace LibraryManagement.Models;

public class LibraryException : Exception
{
    public LibraryException(string message) : base(message)
    {
    }
}
