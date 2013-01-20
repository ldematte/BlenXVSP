using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace Dema.BlenX.Tasks
{

   public class SimTask : ITask
   {
      private IBuildEngine engine;
      public IBuildEngine BuildEngine
      {
         get { return engine; }
         set { engine = value; }
      }

      private ITaskHost host;
      public ITaskHost HostObject
      {
         get { return host; }
         set { host = value; }
      }

      public string OutputBaseName
      {
         get;
         set;
      }

      [Output]
      public string OutputSpecName
      {
         get;
         set;
      }

      [Required]
      public string TypesName
      {
         get;
         set;
      }

      [Required]
      public string SourceDir
      {
         get;
         set;
      }

      [Required]
      public string ProgName
      {
         get;
         set;
      }

      public string FuncName
      {
         get;
         set;
      }

      public string[] Args { get; set; }

      public string SimPath { get; set; }

      public bool Execute()
      {
         try
         {
            StringBuilder paramString = new StringBuilder();

            if (Args != null)
            {
               foreach (string arg in Args)
               {
                  paramString.Append(" ");
                  paramString.Append(arg);
               }
            }

            paramString.Append(" \"");
            paramString.Append(Path.Combine(SourceDir, ProgName));
            paramString.Append("\"");

            paramString.Append(" \"");
            paramString.Append(Path.Combine(SourceDir, TypesName));
            paramString.Append("\"");


            if (FuncName != null && FuncName.Length > 0)
            {
               paramString.Append(" \"");
               paramString.Append(Path.Combine(SourceDir, FuncName));
               paramString.Append("\"");
            }

            if (!String.IsNullOrEmpty(OutputBaseName))
            {
               paramString.Append(" --output=");
               paramString.Append(Path.GetFileName(OutputBaseName));
               OutputSpecName = Path.GetFileName(OutputBaseName) + ".spec";
            }


            ProcessStartInfo psi = new ProcessStartInfo();

            if (!String.IsNullOrEmpty(OutputBaseName))
               psi.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(OutputBaseName));

            psi.CreateNoWindow = true;
            psi.Arguments = paramString.ToString();
            psi.FileName = SimPath;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.ErrorDialog = false;

            psi.RedirectStandardError = true;
            //psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;



            Process simProcess = new Process();


            simProcess.StartInfo = psi;
            simProcess.Start();

            //StreamWriter inputWriter = simProcess.StandardInput;
            StreamReader outputReader = simProcess.StandardOutput;
            StreamReader errorReader = simProcess.StandardError;

            Thread t = new Thread(() =>
                {
                   string line;
                   while ((line = outputReader.ReadLine()) != null)
                      LogError(line);

                   while ((line = errorReader.ReadLine()) != null)
                      LogError(line);
                });
            t.Start();
            simProcess.WaitForExit();

            t.Join();

            if (simProcess.ExitCode != 0)
            {

               LogError("SIM failed. Exit code: " + simProcess.ExitCode);
               return false;
            }
         }
         catch (Exception ex)
         {
            LogError("Error executing simulation task:" + ex.Message);
            LogError(ex.StackTrace);
            return false;

         }
         return true;
      }

      public static Regex regex = new Regex(
            "([a-zA-Z\\.\\\\:]*)\\(([0-9]+), ([0-9]+)\\) : (error|warning)" +
            "[ ]?([A-Z][0-9]+)?:(.*)",
          RegexOptions.CultureInvariant
          | RegexOptions.Compiled);

      private void LogError(string s)
      {
         //Regex regex = new Regex("\\(\\([a-zA-Z]:\\)?[^:(\t\n]+\\)(\\([0-9]+\\)): \\(error\\|warning\\) [A-Z][0-9]+:(.)*");
         if (regex.IsMatch(s))
         {
            var tokens = regex.Split(s);
            var line = Int32.Parse(tokens[2]);
            var col = Int32.Parse(tokens[3]);
            engine.LogErrorEvent(new BuildErrorEventArgs("",
                tokens[5], //code
                tokens[1], //file
                line, //line 
                col, //column
                line, //endline 
                col, //endcolumn
                tokens[6], //message
                "", "Sim"));
         }
         else
            //engine.LogErrorEvent(new BuildErrorEventArgs("", "", "", 0, 0, 0, 0, s, "", "Sim"));
            engine.LogMessageEvent(new BuildMessageEventArgs(s, "", "Sim", MessageImportance.High));
      }
   }
}