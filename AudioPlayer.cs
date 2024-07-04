using System.Media;

namespace ADD_Launcher
{
    internal class AudioPlayer
    {
        public void PlaySound(string fileName)
        {
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string soundFilePath = Path.Combine(solutionDirectory, "narration", (fileName + ".wav"));

            SoundPlayer player = new SoundPlayer(soundFilePath);
            player.Play();
        }
    }
}