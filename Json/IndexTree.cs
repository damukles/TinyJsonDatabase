using System;
using System.Collections.Generic;
using TinyJsonDatabase.Core;

namespace TinyJsonDatabase.Json
{
    public class IndexTree : Tree<byte[], uint>
    {
        public IndexTree(ITreeNodeManager<byte[], uint> nodeManager, bool allowDuplicateKeys = false) : base(nodeManager, allowDuplicateKeys)
        {
        }
    }
}