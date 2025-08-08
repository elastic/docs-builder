using System.Diagnostics;

Process.Start(new ProcessStartInfo
{
    FileName = "bash",
    Arguments = "doit.sh",
    UseShellExecute = false
})?.WaitForExit();
