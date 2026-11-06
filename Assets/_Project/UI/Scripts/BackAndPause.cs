using DrawRush.Collection;
using DrawRush.Core;
using DrawRush.Shop;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DrawRush.UI
{
    /// <summary>
    /// Android platform manners. The back button (Escape in the Input System) closes the top
    /// menu (shop, collection, settings); during live play it pauses by opening settings. Leaving
    /// the app mid-chase (home button, a call, a notification) also opens settings, so the player
    /// comes back to a paused game instead of a hit they never saw coming.
    /// Spawns itself after the scene loads, so no scene wiring is needed.
    /// </summary>
    public sealed class BackAndPause : MonoBehaviour
    {
        private SettingsMenu _settings;
        private ShopController _shop;
        private CollectionController _collection;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn()
        {
            if (FindFirstObjectByType<BackAndPause>() != null) return;
            var go = new GameObject(nameof(BackAndPause));
            DontDestroyOnLoad(go);
            go.AddComponent<BackAndPause>();
        }

        // Live play: the game clock runs and no menu covers it (countdown, win and lose all freeze it).
        private static bool PlayIsLive => Time.timeScale > 0f && !MenuPause.IsOpen;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            Resolve();
            if (_shop != null && _shop.IsOpen) _shop.Close();
            else if (_collection != null && _collection.IsOpen) _collection.Close();
            else if (_settings != null && _settings.IsOpen) _settings.Close();
            else if (PlayIsLive && _settings != null) _settings.Open();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseIfLive();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) PauseIfLive();
        }

        private void PauseIfLive()
        {
            if (!PlayIsLive) return;
            Resolve();
            if (_settings != null) _settings.Open();
        }

        private void Resolve()
        {
            if (_settings == null) _settings = FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
            if (_shop == null) _shop = FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
            if (_collection == null) _collection = FindFirstObjectByType<CollectionController>(FindObjectsInactive.Include);
        }
    }
}
