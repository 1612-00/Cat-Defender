using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CatDefender.Tests
{
    public class LaunchTests
    {
        [Test]
        public void Select_IncludesInsideAndExcludesOutside()
        {
            var pos = new List<(float x, float y)> { (0f, 0f), (1.7f, 0f), (1.9f, 0f), (1f, 1f), (5f, 5f) };
            var result = new List<int>();

            LaunchBlast.Select(pos, 0f, 0f, 1.8f, result);

            CollectionAssert.AreEqual(new[] { 0, 1, 3 }, result);
        }

        [Test]
        public void MergeBoard_SlotEmptyAfterRemoveAndCanBeFilledAgain()
        {
            var board = new MergeBoard(2, 15);
            board.Place(0, 1);
            board.Place(1, 1);

            board.Remove(0);

            Assert.AreEqual(0, board.LevelAt(0));
            Assert.AreEqual(0, board.FirstEmpty());
            Assert.IsTrue(board.Place(0, 1));
        }

        [Test]
        public void GameplayScene_BoardViewHasLaunchPreview()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Gameplay.unity", OpenSceneMode.Additive);
            try
            {
                BoardView board = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<BoardView>(true))
                    .SingleOrDefault();
                Assert.IsNotNull(board);
                Assert.IsNotNull(new SerializedObject(board).FindProperty("launchPreview").objectReferenceValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
