using UnityEngine;

namespace DrawRush.Dev
{
    /// <summary>
    /// Dev tool: saves the next N frames as PNGs at a fixed frame rate (Time.captureFramerate), so a
    /// motion can be reviewed frame by frame or turned into a GIF. Not used by the game.
    /// </summary>
    public sealed class FrameGrabber : MonoBehaviour
    {
        private string _folder;
        private int _left, _index;

        public static void Begin(string folder, int frames, int frameRate)
        {
            System.IO.Directory.CreateDirectory(folder);
            var grabber = new GameObject("FrameGrabber").AddComponent<FrameGrabber>();
            grabber._folder = folder;
            grabber._left = frames;
            Time.captureFramerate = frameRate;
        }

        private void LateUpdate()
        {
            if (_left <= 0)
            {
                Time.captureFramerate = 0;
                Destroy(gameObject);
                return;
            }
            ScreenCapture.CaptureScreenshot($"{_folder}/f{_index:000}.png");
            _index++;
            _left--;
        }
    }
}
