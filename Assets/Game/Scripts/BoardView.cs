using UnityEngine;
using UnityEngine.InputSystem;

namespace CatDefender
{
    /// <summary>Lưới ô đặt mèo: hiển thị, mua, kéo thả, ghép, bỏ vào thùng rác. Mỗi ô thuộc một hàng.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        const int IdleOrder = 10, DragOrder = 900;

        [SerializeField] GameManager game;
        [SerializeField] Cat catPrefab;
        [SerializeField] CatData[] catsByLevel;
        [SerializeField] Vector2[] slots;
        /// <summary>Hàng của từng ô, cùng thứ tự với <see cref="slots"/>.</summary>
        [SerializeField] int[] slotLanes;
        /// <summary>Icon thùng rác trên HUD; thả mèo vào đây để bỏ.</summary>
        [SerializeField] RectTransform trashIcon;
        [SerializeField] float slotRadius = 0.6f;

        MergeBoard board;
        Cat[] cats;
        Camera cam;
        int dragFrom = -1;
        bool wasDown;

        public bool HasEmptySlot => board.FirstEmpty() >= 0;

        /// <summary>Vị trí thùng rác trong thế giới. Canvas dạng overlay nên toạ độ của icon chính là pixel màn hình.</summary>
        public Vector2 TrashWorld => cam.ScreenToWorldPoint(trashIcon.position);

        void Awake()
        {
            board = new MergeBoard(slots.Length, catsByLevel.Length);
            cats = new Cat[slots.Length];
            cam = Camera.main;
        }

        public bool AddCat(int level)
        {
            int slot = board.FirstEmpty();
            if (slot < 0) return false;
            board.Place(slot, Mathf.Clamp(level, 1, catsByLevel.Length));
            Spawn(slot);
            return true;
        }

        public void StunLane(int lane, float seconds)
        {
            for (int i = 0; i < cats.Length; i++)
                if (cats[i] != null && slotLanes[i] == lane) cats[i].Stun(seconds);
        }

        void Spawn(int slot)
        {
            if (cats[slot] != null) Destroy(cats[slot].gameObject);
            Cat cat = Instantiate(catPrefab, slots[slot], Quaternion.identity, transform);
            cat.Init(catsByLevel[board.LevelAt(slot) - 1], game, slotLanes[slot]);
            cat.GetComponent<SpriteRenderer>().sortingOrder = IdleOrder;
            cats[slot] = cat;
        }

        void Place(int slot)
        {
            cats[slot].transform.position = slots[slot];
            cats[slot].Lane = slotLanes[slot];
        }

        void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || game.State != GameState.Playing || game.Paused)
            {
                CancelDrag();
                wasDown = pointer != null && pointer.press.isPressed;
                return;
            }

            // Tự dò cạnh nhấn thay vì wasPressedThisFrame: không lỡ lần nhấn khi Input System
            // cập nhật nhiều lần trong một khung hình.
            Vector2 world = cam.ScreenToWorldPoint(pointer.position.ReadValue());
            bool down = pointer.press.isPressed;
            if (down && !wasDown) Press(world);
            else if (down) Drag(world);
            else if (wasDown) Release(world);
            wasDown = down;
        }

        /// <summary>Bắt đầu kéo con mèo ở vị trí này, nếu có.</summary>
        public void Press(Vector2 world)
        {
            int slot = SlotAt(world);
            if (slot < 0 || cats[slot] == null) return;
            dragFrom = slot;
            cats[slot].GetComponent<SpriteRenderer>().sortingOrder = DragOrder;
        }

        public void Drag(Vector2 world)
        {
            if (dragFrom >= 0) cats[dragFrom].transform.position = world;
        }

        /// <summary>Thả mèo đang kéo: vào thùng rác, vào ô khác (di chuyển, ghép, đổi chỗ) hoặc trả về ô cũ.</summary>
        public void Release(Vector2 world)
        {
            if (dragFrom < 0) return;
            int from = dragFrom;
            dragFrom = -1;
            cats[from].GetComponent<SpriteRenderer>().sortingOrder = IdleOrder;

            if (Vector2.Distance(world, TrashWorld) <= slotRadius)
            {
                board.Remove(from);
                Destroy(cats[from].gameObject);
                cats[from] = null;
                return;
            }

            int to = SlotAt(world);
            MoveResult result = to < 0 ? MoveResult.None : board.Move(from, to);
            switch (result)
            {
                case MoveResult.Moved:
                    cats[to] = cats[from];
                    cats[from] = null;
                    Place(to);
                    break;
                case MoveResult.Merged:
                    Destroy(cats[from].gameObject);
                    cats[from] = null;
                    Spawn(to);
                    break;
                case MoveResult.Swapped:
                    (cats[from], cats[to]) = (cats[to], cats[from]);
                    Place(from);
                    Place(to);
                    break;
                default:
                    Place(from);
                    break;
            }
        }

        void CancelDrag()
        {
            if (dragFrom < 0) return;
            Place(dragFrom);
            cats[dragFrom].GetComponent<SpriteRenderer>().sortingOrder = IdleOrder;
            dragFrom = -1;
        }

        int SlotAt(Vector2 world)
        {
            for (int i = 0; i < slots.Length; i++)
                if (Vector2.Distance(world, slots[i]) <= slotRadius) return i;
            return -1;
        }
    }
}
