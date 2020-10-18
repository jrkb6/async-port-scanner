using System;
using System.Collections.Generic;
using System.Net;
using NLog;

namespace PortScanner.Model
{
    public class IPRange
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        // 4 bytes array for ip regions
        private byte[] _beginIp;
        private byte[] _endIp;
        public IPRange(string ipRange)
        {
            if (ipRange == null)
                throw new ArgumentNullException();

            if (!TryParseCIDR(ipRange) && !TryParseRange(ipRange))
                throw new ArgumentException();
        }

        public IEnumerable<IPAddress> GetAllIP()
        {
            var capacity = 1;
            for (var i = 0; i < 4; i++)
            {
                capacity *= _endIp[i] - _beginIp[i] + 1;

            }

            List<IPAddress> ips = new List<IPAddress>(capacity);
            //create all enumeration
            for (int i0 = _beginIp[0]; i0 <= _endIp[0]; i0++)
            {
                for (int i1 = _beginIp[1]; i1 <= _endIp[1]; i1++)
                {
                    for (int i2 = _beginIp[2]; i2 <= _endIp[2]; i2++)
                    {
                        for (int i3 = _beginIp[3]; i3 <= _endIp[3]; i3++)
                        {
                            ips.Add(new IPAddress(new byte[] {(byte) i0, (byte) i1, (byte) i2, (byte) i3}));
                        }
                    }
                }
            }
            logger.Debug("Number of ips to be scanned: {}",capacity);
            return ips;
        }


        /// Parse IP-range string in CIDR notation. ex : "10.10.10.10/16".
        public bool TryParseCIDR(string ipRange)
        {
            if (InputValidator.IsIPRangeValid(ipRange))
            {
                
                string[] split = ipRange.Split('/');

                if (split.Length != 2)
                    return false;
                //get class
                byte cidrBit = byte.Parse(split[1]);
                uint ip = 0;
                String[] ipPart = split[0].Split('.');
                for (int i = 0; i < 4; i++)
                {
                    ip <<= 8;
                    ip += uint.Parse(ipPart[i]);
                }

                //calculate base of cidr
                byte shiftBits = (byte) (32 - cidrBit);
                uint ip1 = (ip >> shiftBits) << shiftBits;

                if (ip1 != ip) // Check correct subnet address
                    return false;

                uint ip2 = ip1 >> shiftBits;
                for (int k = 0; k < shiftBits; k++)
                {
                    ip2 = (ip2 << 1) + 1;
                }

                _beginIp = new byte[4];
                _endIp = new byte[4];

                for (int i = 0; i < 4; i++)
                {
                    _beginIp[i] = (byte) ((ip1 >> (3 - i) * 8) & 255);
                    _endIp[i] = (byte) ((ip2 >> (3 - i) * 8) & 255);
                }

                return true;
            }
            logger.Error("invalid ip cidr notation{}", ipRange);
            return false;
        }

       
        /// Parse IP-range example: "10.10.10.10-254-255"
        
        private bool TryParseRange(string ipRange)
        {
            if (InputValidator.IsIPRangeValid(ipRange))
            {
                String[] ipParts = ipRange.Split('.');

                _beginIp = new byte[4];
                _endIp = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    string[] rangeParts = ipParts[i].Split('-');

                    if (rangeParts.Length < 1 || rangeParts.Length > 2)
                        return false;

                    _beginIp[i] = byte.Parse(rangeParts[0]);
                    _endIp[i] = (rangeParts.Length == 1) ? _beginIp[i] : byte.Parse(rangeParts[1]);
                }

                return true;
            }
            logger.Error("invalid ip range notation{}", ipRange);
            return false;
        }

       
    }
}