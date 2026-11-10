using System;
using UnityEngine;

namespace CatDefender
{
    public sealed class Wall : MonoBehaviour
    {
        [SerializeField] Transform barFill;
        [SerializeField] float barBottomY = -1.42f;
        [SerializeField] float barHeight = 4.05f;

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public event Action Changed;

        public void Init(int maxHp)
        {
            MaxHp = Hp = maxHp;
            Refresh();
        }

        public void Damage(int amount)
        {
            Hp = Mathf.Max(0, Hp - amount);
            Refresh();
        }

        public void Repair(int amount)
        {
            Hp = Mathf.Min(MaxHp, Hp + amount);
            Refresh();
        }

        /// <summary>Tăng máu tối đa và hồi luôn phần vừa tăng.</summary>
        public void AddMax(int amount)
        {
            MaxHp += amount;
            Hp += amount;
            Refresh();
        }

        void Refresh()
        {
            float f = MaxHp > 0 ? (float)Hp / MaxHp : 0;
            Vector3 s = barFill.localScale;
            barFill.localScale = new Vector3(s.x, barHeight * f, 1);
            Vector3 p = barFill.position;
            barFill.position = new Vector3(p.x, barBottomY + barHeight * f * 0.5f, p.z);
            Changed?.Invoke();
        }
    }
}
