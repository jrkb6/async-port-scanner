using System;
using NUnit.Framework;
using PortScanner;
using PortScanner.Model;

namespace PortScannerTests
{
    class InputValidatorTests
    {
        [Test]
        public void isIPRangeValidWithInvalidStrings()
        {
            Assert.False(InputValidator.IsIPRangeValid(null));
            Assert.False(InputValidator.IsIPRangeValid(" "));
            Assert.False(InputValidator.IsIPRangeValid(""));
        }

        [Test]
        public void isIPRangeValidWithCIDRNotation()
        {
            //wrong cidr 
            Assert.False(InputValidator.IsIPRangeValid("1.1.1.1/23"));
            //correct cidr 
            Assert.True(InputValidator.IsIPRangeValid("1.1.1.1/24"));
            Assert.True(InputValidator.IsIPRangeValid("255.255.254.1/24"));
        }
        [Test]
        public void isIPRangeValidWithRangeNotation()
        {


            Assert.True(InputValidator.IsIPRangeValid("1.1.1.1-223"));
            Assert.False(InputValidator.IsIPRangeValid("1.1.1.1- 243"));
        }

        [Test]
        public void testIPRangeInvalidIP()
        {
            Assert.Throws<ArgumentException>(() => new IPRange("invalidIp"));
        }
    }
}
