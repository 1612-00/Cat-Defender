using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace CatDefender.Tests
{
    public class AudioTests
    {
        [TestCase("Assets/Game/Scenes/Menu.unity")]
        [TestCase("Assets/Game/Scenes/Gameplay.unity")]
        public void Scene_HasExactlyOneAudioListenerOnMainCamera(string scenePath)
        {
            // Arrange
            string yaml = File.ReadAllText(scenePath);
            string[] blocks = Regex.Split(yaml, @"(?=--- !u!\d+ &\d+)");
            string camera = blocks.Single(block =>
                block.StartsWith("--- !u!1 ") && Regex.IsMatch(block, @"m_Name: Main Camera\r?\n"));

            // Act
            Match cameraId = Regex.Match(camera, @"^--- !u!1 &(\d+)");
            string[] componentIds = Regex.Matches(camera, @"component: \{fileID: (\d+)\}")
                .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();
            string[] listeners = blocks.Where(block => block.StartsWith("--- !u!81 &")).ToArray();

            // Assert
            Assert.AreEqual(1, listeners.Length, scenePath);
            Assert.IsTrue(componentIds.Any(id => Regex.IsMatch(listeners[0], @"^--- !u!81 &" + id + @"\r?\n")),
                $"AudioListener is not attached to Main Camera in {scenePath}; GameObject {cameraId.Groups[1].Value}");
        }

        [TestCase("Assets/Game/Scenes/Menu.unity")]
        [TestCase("Assets/Game/Scenes/Gameplay.unity")]
        public void SceneAudioManager_HasExplicitClipForEveryCue(string scenePath)
        {
            // Arrange
            string yaml = File.ReadAllText(scenePath);
            string audioManager = ComponentBlock(yaml, "CatDefender.AudioManager");

            // Act
            MatchCollection entries = Regex.Matches(audioManager,
                @"-\s+cue:\s*(-?\d+)\s*\r?\n\s+clip:\s*\{fileID:\s*([1-9]\d*),\s*guid:\s*([a-f0-9]+)[^\r\n]*\r?\n\s+volume:\s*([0-9.]+)\r?\n\s+minInterval:\s*([0-9.]+)");

            // Assert
            var cues = new HashSet<int>();
            foreach (Match entry in entries)
            {
                Assert.IsTrue(cues.Add(int.Parse(entry.Groups[1].Value)), $"Duplicate cue in {scenePath}");
                float volume = float.Parse(entry.Groups[4].Value, CultureInfo.InvariantCulture);
                float minInterval = float.Parse(entry.Groups[5].Value, CultureInfo.InvariantCulture);
                Assert.That(volume, Is.InRange(0.01f, 1f), $"Invalid volume in {scenePath}");
                Assert.GreaterOrEqual(minInterval, 0, $"Insufficient cue throttle in {scenePath}");
            }
            int[] expected = Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>()
                .Where(cue => cue != AudioCue.None).Select(cue => (int)cue).ToArray();
            CollectionAssert.AreEquivalent(expected, cues, scenePath);
        }

        [Test]
        public void GameplayBuyButton_HasNoAutomaticCueToAvoidErrorAndPurchaseDoubleSound()
        {
            // Arrange
            const string scenePath = "Assets/Game/Scenes/Gameplay.unity";
            string yaml = File.ReadAllText(scenePath);
            string gameObject = Regex.Split(yaml, @"(?=--- !u!\d+ &\d+)")
                .Single(block => block.StartsWith("--- !u!1 ") && Regex.IsMatch(block, @"m_Name: BuyButton\r?\n"));
            string gameObjectId = Regex.Match(gameObject, @"^--- !u!1 &(\d+)").Groups[1].Value;

            // Act
            string component = ComponentBlockForGameObject(yaml, "CatDefender.AudioButton", gameObjectId);
            int cue = int.Parse(Regex.Match(component, @"\r?\n  cue:\s*(-?\d+)\r?\n").Groups[1].Value);

            // Assert
            Assert.AreEqual((int)AudioCue.None, cue);
        }

        [Test]
        public void GameplayNextLevelButton_HasConfirmCue()
        {
            // Arrange
            const string scenePath = "Assets/Game/Scenes/Gameplay.unity";
            string yaml = File.ReadAllText(scenePath);

            // Act
            int cue = ButtonCue(yaml, "NextLevelButton");

            // Assert
            Assert.AreEqual((int)AudioCue.Confirm, cue);
        }

        [Test]
        public void CatPurchaseCue_UsesEconomyConfirmationClip()
        {
            // Arrange
            string yaml = File.ReadAllText("Assets/Game/Scenes/Gameplay.unity");
            string audioManager = ComponentBlock(yaml, "CatDefender.AudioManager");

            // Act
            Match entry = Regex.Match(audioManager,
                $@"-\s+cue:\s*{(int)AudioCue.CatPurchase}\s*\r?\n\s+clip:\s*\{{fileID:\s*[1-9]\d*,\s*guid:\s*([a-f0-9]+)");

            // Assert
            Assert.IsTrue(entry.Success, "CatPurchase cue must have a clip");
            Assert.AreEqual(AssetDatabase.AssetPathToGUID("Assets/Game/Audio/Economy_Upgrade/confirmation_001.ogg"),
                entry.Groups[1].Value);
        }

        static string ComponentBlock(string yaml, string className) =>
            Regex.Split(yaml, @"(?=--- !u!\d+ &\d+)")
                .Single(block => block.Contains("m_EditorClassIdentifier: CatDefender::" + className));

        static string ComponentBlockForGameObject(string yaml, string className, string gameObjectId) =>
            Regex.Split(yaml, @"(?=--- !u!\d+ &\d+)")
                .Single(block =>
                    block.Contains("m_EditorClassIdentifier: CatDefender::" + className) &&
                    block.Contains("m_GameObject: {fileID: " + gameObjectId + "}"));

        static int ButtonCue(string yaml, string objectName)
        {
            string gameObject = Regex.Split(yaml, @"(?=--- !u!\d+ &\d+)")
                .Single(block => block.StartsWith("--- !u!1 ") && Regex.IsMatch(block, @"m_Name: " + objectName + @"\r?\n"));
            string gameObjectId = Regex.Match(gameObject, @"^--- !u!1 &(\d+)").Groups[1].Value;
            string component = ComponentBlockForGameObject(yaml, "CatDefender.AudioButton", gameObjectId);
            return int.Parse(Regex.Match(component, @"\r?\n  cue:\s*(-?\d+)\r?\n").Groups[1].Value);
        }
    }
}
