using System;
using System.Linq;

namespace DapperCodeGenerator.Core.Extensions
{
    public static class StringExtensions
    {
        public static string Repeat(this string value, int quantity)
        {
            return new System.Text.StringBuilder().Insert(0, value, quantity).ToString();
        }

        public static string FirstCharToLower(this string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToLower() + input.Substring(1);
        }
    }
}
