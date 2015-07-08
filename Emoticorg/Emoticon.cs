using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emoticorg
{
    public class Emoticon
    {
        public const int TYPE_ASCII = 0;
        public const int TYPE_IMAGE = 1;

        public string guid { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public byte[] data { get; set; }
        public long lastUsed { get; set; }
        public int type { get; set; }
        public string parentGuid { get; set; }
        public string keyboardRegex { get; set; }
        public string[] keyboardEquivalent { get; set; }
        public int flags { get; set; }
    }
}
