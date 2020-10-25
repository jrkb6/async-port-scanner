using System;

namespace PortScanner.Model
{
    public class OpenPort
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string ToString()
        {
            return $"{Host}:{Port} is open.";
        }
    }
}