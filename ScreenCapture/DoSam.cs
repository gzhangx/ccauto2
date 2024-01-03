using ccAuto2;
using System;
using System.Diagnostics;
using System.IO;


namespace ccauto
{
    internal class DoSam
    {
        public static void ExecuteSamProcess(EventRequester.RequestAndResult samResult, Action endAction)
        {
            samResult.doRequest(tbf =>
            {
                var tmStr = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                var fileName = "d:\\segan\\out\\coc\\test" + tmStr + ".png";
                File.WriteAllBytes(fileName, tbf);
                var command = "d:\\segan\\testwithfile.bat " + fileName;
                ExecuteCmd(command);
                Console.WriteLine("cmd.exe /c " + command);
                endAction();
            });
        }


        static void ExecuteCmd(string command)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            ProcessInfo.CreateNoWindow = false;
            ProcessInfo.UseShellExecute = true;

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            ExitCode = Process.ExitCode;
            //Process.Close();
        }
    }
}
