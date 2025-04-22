using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSystemCtl.Core.Data;

namespace WinSystemCtl.Data
{
    public static class Enums
    {
        public static IEnumerable<IOType> IOTypes => Enum.GetValues(typeof(IOType)).Cast<IOType>();

        public static int IOTypesMaxIndex => Enum.GetValues(typeof(IOType)).Length - 1;

        public static IEnumerable<string> Encodings => Encoding.GetEncodings().Select(x => x.Name);

        public static int EncodingsMaxIndex => Encoding.GetEncodings().Length - 1;

        public static IEnumerable<LanguageOptions> LanguageOptions => Enum.GetValues(typeof(LanguageOptions)).Cast<LanguageOptions>();

        public static int LanguageOptionsMaxIndex => Enum.GetValues(typeof(LanguageOptions)).Length - 1;
    }
}
