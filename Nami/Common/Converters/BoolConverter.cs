﻿using System.Text.RegularExpressions;

namespace Nami.Common.Converters
{
    public sealed class BoolConverter : BaseArgumentConverter<bool>
    {
        private static readonly Regex _tRegex;
        private static readonly Regex _fRegex;


        static BoolConverter()
        {
            _tRegex = new Regex(
                @"^(y+e*(s*|(a*|u*)(h*|p*))|d+a+(v+a+j+)?|1+|e+n+a+b+l+e|o+n+|t+(r+u+e+)?)$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            _fRegex = new Regex(
                @"^(n+(o*(p+e+)?|a*h*|e*i*|y*e*t*)|0+|d+i+s+a+b+l+e|o+f+|f+(a+l+s+e+)?)$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
        }


        public override bool TryConvert(string value, out bool result)
        {
            bool parses = true;

            if (_tRegex.IsMatch(value))
                result = true;
            else if (_fRegex.IsMatch(value))
                result = false;
            else
                return bool.TryParse(value, out result);

            return parses;
        }
    }
}