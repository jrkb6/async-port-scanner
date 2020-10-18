using System;
using System.Text.RegularExpressions;

namespace PortScanner
{
    public class InputValidator
    {
        private const string CidrPattern = @"^([0-9]{1,3}\.){3}[0-9]{1,3}($|/(16|24))$";
        private const string rangePattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}-\d{1,3}";
        
        public static bool IsValid(String inp, Regex rgx)
        {
            if (String.IsNullOrWhiteSpace(inp))
            {
                return false;
            }
            return rgx.IsMatch(inp);
        }

        public static bool IsIPRangeValid(String ipText)
        {
            Regex cidrRegex = new Regex(CidrPattern);
            Regex rangeRegex = new Regex(rangePattern);
            return IsValid(ipText, cidrRegex) || IsValid(ipText, rangeRegex);
        }
    }
}