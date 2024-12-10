using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace N2NGO.Utils;

internal class BinExecutor
{
    public Task? BinProcessTask { get; private set; }

    internal Process? _currentProcess = null;

    internal StringBuilder _output = new();
    internal StringBuilder _error = new();

    public string GetOutput => _output.ToString();
    public string GetError => _error.ToString();
    public string WriteInput { set { if (_currentProcess is null) throw new NullReferenceException("BinExecutor: Cannot write input: process is null, did you forget to call ExecuteAsync?"); _currentProcess.StandardInput.WriteLine(value); } }

    public bool IsCompleted => BinProcessTask is null || BinProcessTask.IsCompleted;

    public Action<string?>? ActionOnOutput { get; set; } = null;
    public Action<string?>? ActionOnError { get; set; } = null;

    /// <summary>
    /// Begin binary execution asynchronously
    /// </summary>
    /// <param name="exec">Command</param>
    /// <returns>If there is an existing <see cref="BinProcessTask"/> that has not been completed, null is returned.</returns>
    public async Task<int?> ExecuteAsync(string exec, string args, bool clear = true)
    {
        if (!IsCompleted)
            return null;

        if (clear)
        {
            _output.Clear();
            _error.Clear();
        }

        _currentProcess = new()
        {
            StartInfo = new()
            {
                FileName = exec,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };

        _currentProcess.OutputDataReceived += OutputDataReceived;
        _currentProcess.ErrorDataReceived += ErrorDataReceived;

        _currentProcess.Start();

        _currentProcess.BeginOutputReadLine();
        _currentProcess.BeginErrorReadLine();

        BinProcessTask = _currentProcess.WaitForExitAsync();
        await BinProcessTask;

        return _currentProcess.ExitCode;
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is not null)
        {
            _output.Append(e.Data);
            if (ActionOnOutput is not null)
                ActionOnOutput(e.Data);
        }
    }

    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is not null)
        {
            _error.Append(e.Data);
            if (ActionOnError is not null)
                ActionOnError(e.Data);
        }
    }
}
