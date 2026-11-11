using System.Collections.Generic;

namespace CatDefender
{
    /// <summary>Phóng mèo: chọn các vị trí nằm trong bán kính nổ. Logic thuần, không phụ thuộc Unity.</summary>
    public static class LaunchBlast
    {
        /// <summary>Ghi vào <paramref name="result"/> chỉ số của mọi vị trí cách tâm không quá bán kính (tính cả biên).</summary>
        public static void Select(IReadOnlyList<(float x, float y)> positions, float cx, float cy, float radius, List<int> result)
        {
            result.Clear();
            float r2 = radius * radius;
            for (int i = 0; i < positions.Count; i++)
            {
                float dx = positions[i].x - cx, dy = positions[i].y - cy;
                if (dx * dx + dy * dy <= r2) result.Add(i);
            }
        }
    }
}
