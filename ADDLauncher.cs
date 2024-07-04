using Newtonsoft.Json;

namespace ADD_Launcher
{
    public partial class ADDLauncher : Form
    {
        private HttpClientHandler httpClientHandler;
        private HMDMountMonitor hmdMountMonitor;
        private ProcessMonitor processMonitor_Oculus;
        private ProcessMonitor processMonitor_Training;
        private AudioPlayer audioPlayer;
        private Config config;
        private Launcher launcher;
        private StatusInfo statusInfo;

        private bool isTrainingRunning = false;
        private bool receivedExecuteInfoResponse = false;
        private bool isRestartTraining = false;    

        //private DateTime lastStatusFileChangeTime = DateTime.MinValue;
        //private static readonly object eventLock = new object();
        public ADDLauncher()
        {
            hmdMountMonitor = new HMDMountMonitor("VID_2833", 2000);
            processMonitor_Oculus = new ProcessMonitor("OculusClient", 2000);
            processMonitor_Training = new ProcessMonitor("ADD_HelicopterVRTraining", 2000);

            audioPlayer = new AudioPlayer();
            config = new Config(audioPlayer);
            launcher = new Launcher(this);
            statusInfo = new StatusInfo();

            httpClientHandler = new HttpClientHandler();

            HMDMountMonitor.HMDMountStateChanged += OnHMDMountStateChanged;
            hmdMountMonitor.StartMountMonitoring();

            processMonitor_Oculus.ProcessStateChanged += OnProcessStateChanged1;
            processMonitor_Oculus.StartProcessMonitoring();

            processMonitor_Training.ProcessStateChanged += OnProcessStateChanged2;
            processMonitor_Training.StartProcessMonitoring();

            ExecuteOnProcessRun();
        }

        struct RequestUrl
        {
            public const string targetExecutable = "/targetExecutable";
            public const string statusInfo = "/statusInfo";
        }

        public class LauncherInfo
        {
            [JsonProperty("Domain")]
            public required string Domain { get; set; }

            [JsonProperty("TrainingPerson")]
            public required string TrainingPerson { get; set; }
        }
        public class TrainingStatusInfo
        {
            [JsonProperty("isConnectedTraining")]
            public required bool isConnectedTraining { get; set; }

            [JsonProperty("isReloadTraining")]
            public required bool isReloadTraining { get; set; }

            [JsonProperty("exitMessage")]
            public required string exitMessage { get; set; }

            [JsonProperty("initTrackingRotation")]
            public required string initTrackingRotation { get; set; }

            [JsonProperty("lastTrackingPosition")]
            public required string lastTrackingPosition { get; set; }

            [JsonProperty("timestamp")]
            public required string timestamp { get; set; }
        }
        public class StatusInfo
        {
            public int statusBoolArrayToInt { get; set; }
            public int statusBoolArrayLength { get; set; }
        }
        private async void ExecuteOnProcessRun()
        {
            while (true)
            {
                bool isTargetProcessRunning = processMonitor_Oculus.IsProcessRunning();
                bool isOculusConnected = HMDMountMonitor.IsHMDMounted();

                if (isTargetProcessRunning && isOculusConnected && !isTrainingRunning)
                {
                    await Task.Run(async () =>
                    {
                        audioPlayer.PlaySound("RequestPath");
                        isTrainingRunning = true;
                        await Task.Delay(10000);
                        await RequestAndExecuteAsync(config.domain + RequestUrl.targetExecutable);
                    });
                }
                await Task.Delay(2000);
            }
        }
        private async Task RequestAndExecuteAsync(string queryString)
        {
            while (!receivedExecuteInfoResponse)
            {
                string responseText = await httpClientHandler.GetResponseAsync(queryString);
                if (responseText != null)
                {
                    receivedExecuteInfoResponse = true;
                    launcher.LaunchExecutableFromResponseAsync(responseText, config.traingPerson);
                }
                else
                {
                    await Task.Delay(3000);
                }
            }
        }
        private async Task RequestStatusAsync()
        {
            string serverUrl = config.domain + RequestUrl.statusInfo;
            string queryString = $"{serverUrl}?statusBoolArrayToInt={statusInfo.statusBoolArrayToInt}&statusBoolArrayLength={statusInfo.statusBoolArrayLength}&trainingPerson={config.traingPerson}";

            Console.WriteLine(queryString);

            string responseText = await httpClientHandler.GetResponseAsync(queryString);

            if (responseText != null)
            {
                // Console.WriteLine($"Status Response : " + responseText);
            }
            else
            {
                await Task.Delay(3000);
            }
        }

        public async Task DetectStatusFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && receivedExecuteInfoResponse)
            {
                string statusFilePath = Path.Combine(filePath, "ADD_HelicopterVRTraining_Data", "StreamingAssets", "TrainingStatus", "trainingstatus.txt");

                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(statusFilePath),
                    Filter = Path.GetFileName(statusFilePath),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    //InternalBufferSize = 64 * 1024
                };

                watcher.Changed += OnStatusFileChanged;
                watcher.EnableRaisingEvents = true;
            }
        }
        public void CalculateStatusBoolArrayToIntandLength()
        {
            bool isOculusRunning = processMonitor_Oculus.IsProcessRunning();
            bool isTrainingRunning = processMonitor_Training.IsProcessRunning();
            bool isHMDMounted = HMDMountMonitor.IsHMDMounted();

            bool[] statusBools = new bool[] { isOculusRunning, isTrainingRunning, isHMDMounted };

            statusInfo.statusBoolArrayToInt = BoolArrayToInt(statusBools);
            statusInfo.statusBoolArrayLength = statusBools.Length;
        }

        static int BoolArrayToInt(bool[] boolArray)
        {
            int result = 0;
            for (int i = 0; i < boolArray.Length; i++)
            {
                if (boolArray[i])
                {
                    result |= (1 << i);
                }
            }
            return result;
        }

        #region Event
        private void OnHMDMountStateChanged(object sender, HMDMountMonitor.HMDMountEventArgs e)
        {
            CalculateStatusBoolArrayToIntandLength();
            if (e.IsMounted)
            {
                audioPlayer.PlaySound("HMDMount");
                Console.WriteLine("HMD is mounted.");
            }
            else
            {
                audioPlayer.PlaySound("LostMount");
                Console.WriteLine("HMD is not mounted.");
                RestartProgram();
            }
            RequestStatusAsync();
        }
        private void OnProcessStateChanged1(object sender, ProcessMonitorEventArgs e)
        {
            CalculateStatusBoolArrayToIntandLength();
            if (e.IsRunning)
            {
                Console.WriteLine("OculusClient process is running.");

            }
            else
            {
                Console.WriteLine("OculusClient process is not running.");
            }
            RequestStatusAsync();
        }

        private void OnProcessStateChanged2(object sender, ProcessMonitorEventArgs e)
        {
            CalculateStatusBoolArrayToIntandLength();
            if (e.IsRunning)
            {
                Console.WriteLine("Unity process is running.");
            }
            else
            {
                Console.WriteLine("Unity process is not running.");
                RestartProgram();
            }
            RequestStatusAsync();
        }

        private void OnStatusFileChanged(object source, FileSystemEventArgs e)
        {
            //lock (eventLock)
            //{
            //    DateTime now = DateTime.Now;
            //    if ((now - lastStatusFileChangeTime).TotalMilliseconds < 1000)
            //    {
            //        return;
            //    }
            //    lastStatusFileChangeTime = now;
            //}
            try
            {
                //UpdateExitinfoFromStatusFile();
                Console.WriteLine($"Status File was changed: {e.Name} - {e.ChangeType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
        #endregion
        //async void UpdateExitinfoFromStatusFile()
        //{
        //    string configFilePath = Path.Combine(filePath, "ADD_HelicopterVRTraining_Data", "StreamingAssets", "Config", "config.txt");

        //    try
        //    {
        //        if (File.Exists(configFilePath))
        //        {
        //            string jsonContent = File.ReadAllText(configFilePath);
        //            TrainingStatusInfo trainingStatusInfo = JsonConvert.DeserializeObject<TrainingStatusInfo>(jsonContent);

        //           // if (configInfo.isConnectedTraining == true) return;
        //           // ExitInfo exitInfo = new ExitInfo();

        //            if (trainingStatusInfo != null)
        //            {
        //                exitInfo.exit_logMessage = trainingStatusInfo.exitMessage;
        //                exitInfo.exit_timestamp = trainingStatusInfo.timestamp;
        //            }
        //            else
        //            {
        //                Console.WriteLine("Error: Deserialized object is null.");
        //                SetDefaultExitInfo();
        //            }
        //            if (exitInfo.exit_logMessage == "EXIT_CODE_ABNORMAL")
        //            {
        //                CheckOculusDeviceAndRunExcutable();
        //            }
        //            else if (exitInfo.exit_logMessage == "EXIT_CODE_NORMAL")
        //            {
        //                Thread.Sleep(5000);
        //            }

        //            await RequestExitAsync(exitInfo);
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Error: File not found - {configFilePath}");
        //            SetDefaultExitInfo();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error reading file: {ex.Message}");
        //    }

        //}
        //void SetDefaultExitInfo()
        //{
        //    exitInfo.exit_logMessage = "None";
        //    exitInfo.exit_timestamp = "None";
        //}

        public async Task RestartProgram()
        {
            bool isOculusRunning = processMonitor_Oculus.IsProcessRunning();
            bool isTrainingRunning = processMonitor_Training.IsProcessRunning();
            bool isHMDMounted = HMDMountMonitor.IsHMDMounted();

            await Task.Delay(5000);

            while (!isRestartTraining)
            {
                if (isHMDMounted && isOculusRunning && !isTrainingRunning)
                {
                    launcher.ExecuteExe(launcher.exeFilePath);
                    isRestartTraining = true;
                }
                else
                {
                    await Task.Delay(3000);
                }
            }
        }

    }
}