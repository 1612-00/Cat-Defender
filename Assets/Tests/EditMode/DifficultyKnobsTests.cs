using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CatDefender.EditorTools;

namespace CatDefender.Tests
{
    /// <summary>D.2/D.3 trong gameplay-depth-plan: hàng chính, hệ số level, dòng lý do thua.</summary>
    public class DifficultyKnobsTests
    {
        static readonly int[] Lanes = { 1, 3, 4 };

        [Test]
        public void WeightedLane_ShareOne_AlwaysMain()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 200; i++) Assert.AreEqual(3, Picker.WeightedLane(Lanes, 3, 1f, rng));
        }

        [Test]
        public void WeightedLane_ShareZero_NeverMain()
        {
            var rng = new System.Random(2);
            for (int i = 0; i < 200; i++) Assert.AreNotEqual(3, Picker.WeightedLane(Lanes, 3, 0f, rng));
        }

        [Test]
        public void WeightedLane_SingleLane_ReturnsIt()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 50; i++) Assert.AreEqual(2, Picker.WeightedLane(new[] { 2 }, 2, 0.6f, rng));
        }

        [Test]
        public void WeightedLane_Share06_MainFrequencyNear06AndStaysInActive()
        {
            var rng = new System.Random(4);
            int main = 0;
            for (int i = 0; i < 2000; i++)
            {
                int lane = Picker.WeightedLane(Lanes, 4, 0.6f, rng);
                Assert.Contains(lane, Lanes);
                if (lane == 4) main++;
            }
            Assert.That(main / 2000f, Is.InRange(0.55f, 0.65f));
        }

        [Test]
        public void LevelFactor_TableThenLateGrowth()
        {
            float[] table = { 1.0f, 1.4f, 1.6f, 1.9f, 2.2f, 2.5f };
            for (int n = 1; n <= 6; n++) Assert.AreEqual(table[n - 1], GameBuilder.LevelFactor(n), 1e-5f);
            Assert.AreEqual(2.5f * 1.04f, GameBuilder.LevelFactor(7), 1e-4f);
        }

        [Test]
        public void GameplayScene_HudHasLoseReasonText()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Gameplay.unity", OpenSceneMode.Additive);
            try
            {
                Hud hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Hud>(true)).SingleOrDefault();
                Assert.IsNotNull(hud);
                Assert.IsNotNull(new SerializedObject(hud).FindProperty("loseReasonText").objectReferenceValue);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
