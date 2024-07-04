using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

public class ProcessMonitor
{
    private string processNameContains;
    private int monitoringInterval;

    private System.Threading.Timer _timer;
    private bool _isProcessRunning;
    private readonly object _lock = new object();

    public event EventHandler<ProcessMonitorEventArgs> ProcessStateChanged;

    public ProcessMonitor(string processNameContains, int monitoringInterval)
    {
        this.processNameContains = processNameContains;
        this.monitoringInterval = monitoringInterval;
    }

    public void StartProcessMonitoring()
    {
        _timer = new System.Threading.Timer(CheckProcess, processNameContains, 0, monitoringInterval);
    }

    public void StopProcessMonitoring()
    {
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
    }

    private void CheckProcess(object state)
    {
        string processNameContains = (string)state;
        Process[] processes = Process.GetProcesses();
        bool isRunning;

        lock (_lock)
        {
            isRunning = processes.Any(p => p.ProcessName.Contains(processNameContains, StringComparison.OrdinalIgnoreCase));
            if (isRunning != _isProcessRunning)
            {
                _isProcessRunning = isRunning;
                OnProcessStateChanged(new ProcessMonitorEventArgs { IsRunning = _isProcessRunning });
            }
        }
    }

    protected virtual void OnProcessStateChanged(ProcessMonitorEventArgs e)
    {
        ProcessStateChanged?.Invoke(this, e);
    }

    public bool IsProcessRunning()
    {
        lock (_lock)
        {
            return _isProcessRunning;
        }
    }
}

public class ProcessMonitorEventArgs : EventArgs
{
    public bool IsRunning { get; set; }
}
