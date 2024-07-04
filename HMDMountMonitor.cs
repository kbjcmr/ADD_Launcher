using System.Management;

public class HMDMountMonitor
{
    private string hmdDeviceID;
    private int monitoringInterval;
    public static event EventHandler<HMDMountEventArgs> HMDMountStateChanged;

    private static System.Threading.Timer _timer;
    private static bool _isHMDMounted;
    private static readonly object _Mlock = new object();

    public HMDMountMonitor(string hmdDeviceID, int monitoringInterval)
    {
        this.hmdDeviceID = hmdDeviceID;
        this.monitoringInterval = monitoringInterval;
    }

    public static bool isHMDMounted
    {
        get { lock (_Mlock) return _isHMDMounted; }
        private set
        {
            lock (_Mlock)
            {
                if (_isHMDMounted != value)
                {
                    _isHMDMounted = value;
                    OnHMDMountStateChanged(new HMDMountEventArgs { IsMounted = _isHMDMounted });
                }
            }
        }
    }

    public void StartMountMonitoring()
    {
        _timer = new System.Threading.Timer(CheckMount, hmdDeviceID, 0, monitoringInterval);
    }

    public void StopMountMonitoring()
    {
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
    }

    private void CheckMount(object state)
    {
        string oculusVendorId = (string)state;
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity");

        bool hmdFound = false;

        lock (_Mlock)
        {
            foreach (ManagementObject device in searcher.Get())
            {
                string deviceId = device["DeviceID"]?.ToString() ?? string.Empty;

                if (deviceId.Contains(oculusVendorId))
                {
                    hmdFound = true;
                    break;
                }
            }
            isHMDMounted = hmdFound;
        }
    }

    public static bool IsHMDMounted()
    {
        lock (_Mlock)
        {
            return isHMDMounted;
        }
    }

    private static void OnHMDMountStateChanged(HMDMountEventArgs e)
    {
        HMDMountStateChanged?.Invoke(null, e);
    }

    public class HMDMountEventArgs : EventArgs
    {
        public bool IsMounted { get; set; }
    }
}
