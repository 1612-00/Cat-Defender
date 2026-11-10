using UnityEngine;
using UnityEngine.InputSystem;

namespace CatDefender
{
    /// <summary>Lưới ô đặt mèo: hiển thị, mua, kéo thả, ghép, bỏ vào thùng rác.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        const int IdleOrder = 10, DragOrder = 900;

        [SerializeField] GameManager game;
        [SerializeField] Cat catPrefab;
        [SerializeField] CatData[] catsByLevel;
        [SerializeField] Vector2[] slots;
        [SerializeField] Vector2 trash;
        [SerializeField] float slotRadius = 0.6f;

        MergeBoard board;
        Cat[] cats;
        Camera cam;
        int dragFrom = -1;
        bool wasDown;

        public bool HasEmptySlot => board.FirstEmpty() >= 0;

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
            board.Place(slot, level);
            Spawn(slot);
            return true;
        }

        void Spawn(int slot)
        {
            if (cats[slot] != null) Destroy(cats[slot].gameObject);
            Cat cat = Instantiate(catPrefab, slots[slot], Quaternion.identity, transform);
            cat.Init(catsByLevel[board.LevelAt(slot) - 1], game);
            cat.GetComponent<SpriteRenderer>().sortingOrder = IdleOrder;
            cats[slot] = cat;
        }

        void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || game.State != GameState.Playing)
            {
                CancelDrag();
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

            if (Vector2.Distance(world, trash) <= slotRadius)
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
                    cats[to].transform.position = slots[to];
                    break;
                case MoveResult.Merged:
                    Destroy(cats[from].gameObject);
                    cats[from] = null;
                    Spawn(to);
                    break;
                case MoveResult.Swapped:
                    (cats[from], cats[to]) = (cats[to], cats[from]);
                    cats[from].transform.position = slots[from];
                    cats[to].transform.position = slots[to];
                    break;
                default:
                    cats[from].transform.position = slots[from];
                    break;
            }
        }

        void CancelDrag()
        {
            if (dragFrom < 0) return;
            cats[dragFrom].transform.position = slots[dragFrom];
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
