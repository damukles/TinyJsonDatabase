namespace TinyJsonDatabase.Core
{
    public class IndexTree : Tree<byte[], uint>
    {
        public IndexTree(ITreeNodeManager<byte[], uint> nodeManager, bool allowDuplicateKeys = false) : base(nodeManager, allowDuplicateKeys)
        {
        }
    }
}