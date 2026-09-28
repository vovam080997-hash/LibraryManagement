using System;
using System.Security.Cryptography;
using System.Text;

namespace LibraryManagement.Utils;




public static class PasswordHasher
{
    public static string Hash(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }


    public static bool IsHash(string text)
    {
        if (text.Length != 64)
            return false;

        foreach (char c in text)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }
        return true;
    }
}
