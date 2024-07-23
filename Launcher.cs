using System;
using System.Diagnostics;

namespace ADD_Launcher
{
    public class Launcher
    {
        private ADDLauncher addLauncher;

        private string baseDirectory = "";
        private string filePath = "";
        public string exeFilePath = "";
        public Launcher(ADDLauncher addLauncher)
        {
            this.addLauncher = addLauncher;
        }
        public async Task LaunchExecutableFromResponseAsync(string responseText, string traingPerson)
        {
            string _heliName = "";
            string _traingType = "";
            string _playerName = "";
            string _rank = "";

            string[] parts = responseText.Split(new string[] { ": " }, StringSplitOptions.None);

            if (parts.Length > 1)
            {
                string[] keyValuePairs = parts[1].Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var pair in keyValuePairs)
                {
                    string[] keyValue = pair.Split(new char[] { '=' });

                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0].Trim() == "_heliName")
                        {
                            _heliName = keyValue[1].Trim();
                        }
                        else if (keyValue[0].Trim() == "_traingType")
                        {
                            _traingType = keyValue[1].Trim();
                        }
                        else if (keyValue[0].Trim() == "_playerName")
                        {
                            //string[] _playerNames = keyValue[1].Trim().Split("@");
                            //_playerName = (traingPerson == "Pilot") ? _playerNames[0] : _playerNames[1];
                            _playerName = keyValue[1].Trim();
                        }
                        else if (keyValue[0].Trim() == "_rank")
                        {
                            //string[] _ranks = keyValue[1].Trim().Split("@");
                            //_rank = (traingPerson == "Pilot") ? _ranks[0] : _ranks[1];
                            _rank = keyValue[1].Trim();
                        }
                    }
                }

                if ((_heliName == "" || _heliName == "None") || _traingType == "") return;

                baseDirectory = "C:\\ADD";
                filePath = Path.Combine(baseDirectory, $"{_heliName}_{_traingType}_{traingPerson}");
                exeFilePath = Path.Combine(filePath, "ADD_HelicopterVRTraining.exe");


                if (!Directory.Exists(baseDirectory))
                {
                    Directory.CreateDirectory(baseDirectory);
                }

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                if (File.Exists(exeFilePath))
                {
                    if (_heliName != "" && _traingType != "")
                    {
                        ExecuteExe(exeFilePath, _playerName, _rank);
                        Console.WriteLine("Training started.");
                        //addLauncher.DetectStatusFile(filePath);
                    }
                }
                else
                {
                    Console.WriteLine($" Warning! : '{exeFilePath}' file does not exist");
                }
            }

            void ExecuteExe(string filePath, string playerName, string rank)
            {
                try
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    Process[] runningProcess = Process.GetProcessesByName(fileNameWithoutExtension);

                    if (runningProcess.Any())
                    {
                        Console.WriteLine("The program is already running");
                        return;
                    }

                    string arguments = $"--playername \"{playerName}\" --rank \"{rank}\"";

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                    };
                    //addLauncher.audioPlayer.PlaySound("RequestPath");
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing file: {ex.Message}");
                }

            }
        }
    }
}


