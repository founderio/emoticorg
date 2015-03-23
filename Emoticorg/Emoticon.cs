using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emoticorg
{
    public class Emoticon
    {
        public string guid { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public byte[] data { get; set; }
        public long lastUsed { get; set; }
    }
}
