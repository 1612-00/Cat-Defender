namespace CatDefender
{
    public enum MoveResult { None, Moved, Merged, Swapped }

    /// <summary>Lưới ô chứa cấp mèo. 0 = ô trống. Logic thuần, không phụ thuộc Unity.</summary>
    public sealed class MergeBoard
    {
        readonly int[] levels;
        public int MaxLevel { get; }
        public int SlotCount => levels.Length;

        public MergeBoard(int slotCount, int maxLevel)
        {
            levels = new int[slotCount];
            MaxLevel = maxLevel;
        }

        public int LevelAt(int slot) => levels[slot];

        public int FirstEmpty()
        {
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] == 0) return i;
            return -1;
        }

        public bool Place(int slot, int level)
        {
            if (levels[slot] != 0 || level < 1 || level > MaxLevel) return false;
            levels[slot] = level;
            return true;
        }

        public void Remove(int slot) => levels[slot] = 0;

        /// <summary>Kéo mèo từ ô này sang ô khác: di chuyển, ghép (cùng cấp, chưa tối đa) hoặc đổi chỗ.</summary>
        public MoveResult Move(int from, int to)
        {
            if (from == to || levels[from] == 0) return MoveResult.None;
            if (levels[to] == 0)
            {
                levels[to] = levels[from];
                levels[from] = 0;
                return MoveResult.Moved;
            }
            if (levels[to] == levels[from] && levels[to] < MaxLevel)
            {
                levels[to]++;
                levels[from] = 0;
                return MoveResult.Merged;
            }
            (levels[from], levels[to]) = (levels[to], levels[from]);
            return MoveResult.Swapped;
        }
    }
}
