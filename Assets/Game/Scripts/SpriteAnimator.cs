using System;
using UnityEngine;

namespace CatDefender
{
    // ponytail: lật khung hình trực tiếp thay cho Animator + AnimationClip.
    // Đủ cho nhân vật chỉ có 2–4 trạng thái; chuyển sang Animator nếu cần blend hoặc animation event.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteAnimator : MonoBehaviour
    {
        public const float Fps = 30f;

        SpriteRenderer sr;
        Sprite[] frames;
        float time;
        bool loop;
        Action onDone;

        public SpriteRenderer Renderer => sr ? sr : sr = GetComponent<SpriteRenderer>();

        public void Play(Sprite[] newFrames, bool looping, Action done = null)
        {
            frames = newFrames;
            loop = looping;
            onDone = done;
            time = 0;
            if (frames != null && frames.Length > 0) Renderer.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            time += Time.deltaTime;
            int i = (int)(time * Fps);
            if (i >= frames.Length)
            {
                if (!loop)
                {
                    Renderer.sprite = frames[frames.Length - 1];
                    frames = null;
                    var cb = onDone;
                    onDone = null;
                    cb?.Invoke();
                    return;
                }
                i %= frames.Length;
            }
            Renderer.sprite = frames[i];
        }
    }
}
