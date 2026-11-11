namespace CatDefender
{
    public enum MoveResult { None, Moved, Merged, Swapped }
    public enum MergeDirection { None, Horizontal, Vertical }

    /// <summary>Lưới ô chứa cấp mèo. 0 = ô trống. Logic thuần, không phụ thuộc Unity.</summary>
    public sealed class MergeBoard
    {
        readonly int[] levels;
        readonly int[] lanes;
        readonly int[] columns;
        public int MaxLevel { get; }
        public int SlotCount => levels.Length;

        public MergeBoard(int slotCount, int maxLevel)
        {
            levels = new int[slotCount];
            lanes = new int[slotCount];
            columns = new int[slotCount];
            for (int i = 0; i < slotCount; i++) columns[i] = i;
            MaxLevel = maxLevel;
        }

        public MergeBoard(int maxLevel, int[] slotLanes, int[] slotColumns)
        {
            if (slotLanes == null) throw new System.ArgumentNullException(nameof(slotLanes));
            if (slotColumns == null) throw new System.ArgumentNullException(nameof(slotColumns));
            if (slotLanes.Length != slotColumns.Length)
                throw new System.ArgumentException("Slot lane and column counts must match.");

            levels = new int[slotLanes.Length];
            lanes = (int[])slotLanes.Clone();
            columns = (int[])slotColumns.Clone();
            MaxLevel = maxLevel;
        }

        public int LevelAt(int slot) => levels[slot];

        public MergeDirection DirectionBetween(int first, int second)
        {
            if (first == second) return MergeDirection.None;
            if (lanes[first] == lanes[second] && System.Math.Abs(columns[first] - columns[second]) == 1)
                return MergeDirection.Horizontal;
            if (columns[first] == columns[second] && System.Math.Abs(lanes[first] - lanes[second]) == 1)
                return MergeDirection.Vertical;
            return MergeDirection.None;
        }

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
            if (levels[to] == levels[from] && levels[to] < MaxLevel &&
                DirectionBetween(from, to) != MergeDirection.None)
            {
                levels[to]++;
                levels[from] = 0;
                return MoveResult.Merged;
            }
            (levels[from], levels[to]) = (levels[to], levels[from]);
            return MoveResult.Swapped;
        }
    }

    public static class MergeStrikeRules
    {
        public static bool AffectsLane(MergeDirection direction, int firstLane, int secondLane, int lane)
        {
            if (direction == MergeDirection.Horizontal) return lane == firstLane;
            if (direction == MergeDirection.Vertical) return lane == firstLane || lane == secondLane;
            return false;
        }
    }
}
