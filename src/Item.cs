using System;
using System.Collections.Generic;

namespace UnturnedDataSerializer {
    public class Item {
        public uint version { get; set; }
        public SortedSet<string> dependencies { get; set; }
    }
}