using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ADD_Launcher.ADDLauncher;

namespace ADD_Launcher
{
    internal class Config
    {
        private readonly AudioPlayer _audioPlayer;
        public string domain { get; private set; }
        public string traingPerson { get; private set; }

        public Config(AudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
            LoadConfig();
        }

        public void LoadConfig()
        {
            _audioPlayer.PlaySound("LauncherStart");
            Console.WriteLine("Launcher Program Start");
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string txtFilePath = Path.Combine(solutionDirectory, "launcher_info.txt");

            try
            {
                if (File.Exists(txtFilePath))
                {
                    string jsonContent = File.ReadAllText(txtFilePath);
                    LauncherInfo launcherInfo = JsonConvert.DeserializeObject<LauncherInfo>(jsonContent);
                    domain = launcherInfo.Domain;
                    traingPerson = launcherInfo.TrainingPerson;
                }
                else
                {
                    Console.WriteLine($"Error: File not found - {txtFilePath}");
                    domain = "default_domain";
                    traingPerson = "default_training_person";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
            }
        }
    }
}
