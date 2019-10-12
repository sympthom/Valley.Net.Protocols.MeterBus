using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public static class UnitPrefix
    {
        public static string GetUnitPrefix(int magnitude)
        {
            switch (magnitude)
            {
                case 0: return string.Empty;
                case -3: return "m";
                case -6: return "my";
                case 1: return "10 ";
                case 2: return "100 ";
                case 3: return "k";
                case 4: return "10 k";
                case 5: return "100 k";
                case 6: return "M";
                case 9: return "T";
                default: return $"1e{magnitude}";
            }
        }
    }
}
