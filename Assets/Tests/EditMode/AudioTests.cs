using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CatDefender.Tests
{
    public class AudioTests
    {
        const string ConfigPath = "Assets/Game/Data/AudioConfig.asset";

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
        public void SceneAudioManager_ReferencesSharedAudioConfig(string scenePath)
        {
            // Arrange
            string yaml = File.ReadAllText(scenePath);
            string manager = ComponentBlock(yaml, "CatDefender.AudioManager");

            // Act
            Match match = Regex.Match(manager, @"config:\s*\{fileID:\s*11400000,\s*guid:\s*([a-f0-9]+)");

            // Assert
            Assert.IsTrue(match.Success, $"AudioManager has no AudioConfig in {scenePath}");
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(ConfigPath), match.Groups[1].Value);
        }

        [Test]
        public void GameplayBuyButton_HasNoAutomaticCueToAvoidErrorAndPurchaseDoubleSound()
        {
            // Arrange
            string yaml = File.ReadAllText("Assets/Game/Scenes/Gameplay.unity");
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
            string yaml = File.ReadAllText("Assets/Game/Scenes/Gameplay.unity");

            // Act
            int cue = ButtonCue(yaml, "NextLevelButton");

            // Assert
            Assert.AreEqual((int)AudioCue.Confirm, cue);
        }

        [Test]
        public void AudioConfig_HasOneExplicitClipForEveryCue()
        {
            // Arrange
            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            Assert.IsNotNull(config);

            // Act
            AudioCueClip[] entries = config.cues;
            AudioCue[] expected = Enum.GetValues(typeof(AudioCue)).Cast<AudioCue>()
                .Where(cue => cue != AudioCue.None).ToArray();

            // Assert
            Assert.AreEqual(expected.Length, entries.Length);
            CollectionAssert.AreEquivalent(expected, entries.Select(entry => entry.cue));
            Assert.IsTrue(entries.All(entry => entry.clip != null));
            Assert.IsTrue(entries.GroupBy(entry => entry.cue).All(group => group.Count() == 1));
        }

        [Test]
        public void AudioConfig_CatPurchaseUsesEconomyConfirmationClip()
        {
            // Arrange
            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);

            // Act
            AudioCueClip entry = config.cues.Single(cue => cue.cue == AudioCue.CatPurchase);

            // Assert
            Assert.AreEqual("Assets/Game/Audio/Economy_Upgrade/confirmation_001.ogg",
                AssetDatabase.GetAssetPath(entry.clip));
        }

        [Test]
        public void AudioConfig_NewCuesUseTheClipsSpecifiedInAudioNotes()
        {
            // Arrange
            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            var expectedClips = new Dictionary<AudioCue, string>
            {
                { AudioCue.Merge, "Assets/Game/Audio/UI_Interaction/merge-cat.wav" },
                { AudioCue.LevelComplete, "Assets/Game/Audio/UI_Interaction/game-level-completed.wav" },
                { AudioCue.GameOver, "Assets/Game/Audio/UI_Interaction/musical-game-over.wav" },
                { AudioCue.WaveStart, "Assets/Game/Audio/UI_Interaction/classic-alarm.wav" },
                { AudioCue.BossAppear, "Assets/Game/Audio/UI_Interaction/giant-monster-footstep.wav" },
                { AudioCue.PerkOffer, "Assets/Game/Audio/UI_Interaction/happy-bell-alert.wav" },
                { AudioCue.CatTrash, "Assets/Game/Audio/UI_Interaction/money-bag-drop.wav" },
                { AudioCue.WallHit, "Assets/Game/Audio/Combat/impactWood_heavy_004.ogg" },
                { AudioCue.WallCritical, "Assets/Game/Audio/Combat/facility-alarm.wav" },
                { AudioCue.BossStun, "Assets/Game/Audio/Combat/impactPlate_heavy_004.ogg" },
                { AudioCue.EnemySplit, "Assets/Game/Audio/Combat/long-pop.wav" },
                { AudioCue.BoxingActivate, "Assets/Game/Audio/Economy_Upgrade/confirmation_001.ogg" }
            };

            // Act / Assert
            foreach (KeyValuePair<AudioCue, string> expected in expectedClips)
            {
                AudioCueClip entry = config.cues.Single(cue => cue.cue == expected.Key);
                Assert.AreEqual(expected.Value, AssetDatabase.GetAssetPath(entry.clip), expected.Key.ToString());
            }
        }

        [Test]
        public void AudioConfig_UsesVariationsAndPrioritiesForBusyCombat()
        {
            // Arrange
            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);

            // Act
            AudioCueClip shot = Cue(config, AudioCue.CatShoot);
            AudioCueClip hit = Cue(config, AudioCue.BulletHit);
            AudioCueClip coin = Cue(config, AudioCue.Coin);
            AudioCueClip wall = Cue(config, AudioCue.WallHit);
            AudioCueClip boss = Cue(config, AudioCue.BossExplosion);
            AudioCueClip result = Cue(config, AudioCue.LevelComplete);

            // Assert
            Assert.That(shot.volume, Is.InRange(0.30f, 0.40f));
            Assert.That(hit.volume, Is.InRange(0.30f, 0.40f));
            Assert.That(coin.volume, Is.InRange(0.15f, 0.30f));
            Assert.AreEqual(0.06f, shot.pitchVariation);
            Assert.AreEqual(0.10f, shot.volumeVariation);
            Assert.AreEqual(0.06f, hit.pitchVariation);
            Assert.AreEqual(0.10f, coin.volumeVariation);
            Assert.Less(shot.priority, wall.priority);
            Assert.AreEqual(100, wall.priority);
            Assert.AreEqual(100, boss.priority);
            Assert.AreEqual(100, result.priority);
            Assert.AreEqual(AudioCueBus.Gameplay, shot.bus);
            Assert.AreEqual(AudioCueBus.UI, Cue(config, AudioCue.Switch).bus);
        }

        [Test]
        public void AudioConfig_UsesMenuAndGameplayMusicWithSeparateMixControls()
        {
            // Arrange
            AudioConfig config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);

            // Assert
            Assert.AreEqual("Assets/Game/Audio/UI_Interaction/main_menu_music_mastered.mp3",
                AssetDatabase.GetAssetPath(config.menuMusic));
            Assert.AreEqual("Assets/Game/Audio/UI_Interaction/DST-TowerDefenseTheme.mp3",
                AssetDatabase.GetAssetPath(config.gameplayMusic));
            Assert.That(config.menuMusicVolume, Is.InRange(0.10f, 0.20f));
            Assert.That(config.gameplayMusicVolume, Is.InRange(0.10f, 0.20f));
            Assert.That(config.bossMusicMultiplier, Is.LessThan(1f));
            Assert.That(config.resultMusicMultiplier, Is.LessThan(config.bossMusicMultiplier));
            Assert.That(config.musicFadeSeconds, Is.GreaterThan(0f));
            Assert.That(config.sfxVoiceCount, Is.InRange(6, 8));
        }

        [Test]
        public void AudioImports_UseMonoCompressedEffectsAndStreamingStereoMusic()
        {
            // Arrange
            AudioImporter menu = (AudioImporter)AssetImporter.GetAtPath(
                "Assets/Game/Audio/UI_Interaction/main_menu_music_mastered.mp3");
            AudioImporter gameplay = (AudioImporter)AssetImporter.GetAtPath(
                "Assets/Game/Audio/UI_Interaction/DST-TowerDefenseTheme.mp3");
            AudioImporter effect = (AudioImporter)AssetImporter.GetAtPath(
                "Assets/Game/Audio/Combat/laserSmall_000.ogg");

            // Assert
            Assert.AreEqual(AudioClipLoadType.Streaming, menu.defaultSampleSettings.loadType);
            Assert.AreEqual(AudioClipLoadType.Streaming, gameplay.defaultSampleSettings.loadType);
            Assert.IsFalse(menu.forceToMono);
            Assert.IsFalse(gameplay.forceToMono);
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, effect.defaultSampleSettings.loadType);
            Assert.IsTrue(effect.forceToMono);
            Assert.IsTrue(effect.defaultSampleSettings.preloadAudioData);
        }

        static AudioCueClip Cue(AudioConfig config, AudioCue cue) =>
            config.cues.Single(entry => entry.cue == cue);

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
