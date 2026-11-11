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
        /// <summary>Cột của từng ô; dùng cùng hàng để xác định ghép ngang/dọc.</summary>
        [SerializeField] int[] slotColumns;
        [SerializeField] SpriteRenderer[] mergePreviewSlots;
        [SerializeField] SpriteRenderer[] mergePreviewLanes;
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
            board = new MergeBoard(catsByLevel.Length, slotLanes, slotColumns);
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

        public bool StunLane(int lane, float seconds)
        {
            bool stunned = false;
            for (int i = 0; i < cats.Length; i++)
            {
                if (cats[i] == null || slotLanes[i] != lane) continue;
                cats[i].Stun(seconds);
                stunned = true;
            }
            return stunned;
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
            AudioManager.Play(AudioCue.SelectCat);
        }

        public void Drag(Vector2 world)
        {
            if (dragFrom < 0) return;
            cats[dragFrom].transform.position = world;
            UpdateMergePreview(dragFrom, SlotAt(world));
        }

        /// <summary>Thả mèo đang kéo: vào thùng rác, vào ô khác (di chuyển, ghép, đổi chỗ) hoặc trả về ô cũ.</summary>
        public void Release(Vector2 world)
        {
            if (dragFrom < 0) return;
            int from = dragFrom;
            dragFrom = -1;
            HideMergePreview();
            cats[from].GetComponent<SpriteRenderer>().sortingOrder = IdleOrder;

            if (Vector2.Distance(world, TrashWorld) <= slotRadius)
            {
                board.Remove(from);
                Destroy(cats[from].gameObject);
                cats[from] = null;
                AudioManager.Play(AudioCue.CatTrash);
                return;
            }

            int to = SlotAt(world);
            MergeDirection direction = to < 0 ? MergeDirection.None : board.DirectionBetween(from, to);
            MoveResult result = to < 0 ? MoveResult.None : board.Move(from, to);
            if (result == MoveResult.Merged)
            {
                AudioManager.Play(AudioCue.Merge);
                AudioManager.Vibrate();
            }
            else if (result != MoveResult.None) AudioManager.Play(AudioCue.DropCat);
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
                    game.MergeStrike(direction, slotLanes[from], slotLanes[to], cats[to].Data, cats[to].transform.position);
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
            HideMergePreview();
            Place(dragFrom);
            cats[dragFrom].GetComponent<SpriteRenderer>().sortingOrder = IdleOrder;
            dragFrom = -1;
        }

        void UpdateMergePreview(int from, int to)
        {
            MergeDirection direction = MergeDirection.None;
            if (to >= 0 && to != from && board.LevelAt(from) > 0 &&
                board.LevelAt(from) == board.LevelAt(to) && board.LevelAt(from) < board.MaxLevel)
                direction = board.DirectionBetween(from, to);

            for (int lane = 0; lane < mergePreviewLanes.Length; lane++)
            {
                mergePreviewLanes[lane].enabled = direction != MergeDirection.None &&
                    MergeStrikeRules.AffectsLane(direction, slotLanes[from], to >= 0 ? slotLanes[to] : -1, lane);
            }
            for (int slot = 0; slot < mergePreviewSlots.Length; slot++)
                mergePreviewSlots[slot].enabled = direction != MergeDirection.None && slot == to;
        }

        void HideMergePreview()
        {
            for (int i = 0; i < mergePreviewLanes.Length; i++)
                mergePreviewLanes[i].enabled = false;
            for (int i = 0; i < mergePreviewSlots.Length; i++)
                mergePreviewSlots[i].enabled = false;
        }

        int SlotAt(Vector2 world)
        {
            for (int i = 0; i < slots.Length; i++)
                if (Vector2.Distance(world, slots[i]) <= slotRadius) return i;
            return -1;
        }
    }
}
