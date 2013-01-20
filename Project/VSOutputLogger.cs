using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using Microsoft.VisualStudio;
using Dema.BlenX.Tasks;

namespace Dema.VisualStudio
{

    public class VSOutputLogger : ILogger, IOutputWriter
    {
        private IEventSource source;
        private IVsOutputWindowPane outputPanel;

        public VSOutputLogger()
        {
            outputPanel = null;
        }

        public VSOutputLogger(IServiceProvider serviceProvider)
        {
            IVsOutputWindow outputWindow = serviceProvider.GetService(
                        typeof(SVsOutputWindow)) as IVsOutputWindow;

            Guid guidGeneralPane = VSConstants.GUID_OutWindowGeneralPane;
            outputWindow.GetPane(ref guidGeneralPane, out outputPanel);
        }

        public void Initialize(IEventSource eventSource)
        {
            this.source = eventSource;


            eventSource.ProjectStarted += new ProjectStartedEventHandler(eventSource_ProjectStarted);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(eventSource_ProjectFinished);
            //eventSource.WarningRaised += new BuildWarningEventHandler(Events_WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
            //eventSource.BuildFinished += new BuildFinishedEventHandler(Events_BuildFinished);
            //eventSource.BuildStarted += new BuildStartedEventHandler(Events_BuildStarted);
            eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
            //eventSource.CustomEventRaised += new CustomBuildEventHandler(Events_CustomEventRaised);
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
            Log(e.Message + "\n");
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            var s = String.Format("========== Simulation {0}: Project: {1}, Time: {2} ==========\n",
                (e.Succeeded ? "finished" : "FAILED"), e.ProjectFile, e.Timestamp);
            Log(s);
        }

        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            var s = String.Format("------ Simulation started: Project: {0}, Time: {1} ------\n",
                e.ProjectFile, e.Timestamp);
            Log(s);
        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            //TODO: task list?
            var s = String.Format("{0}({1},{2}): error {3}: {4}\n",
                e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);

            Log(s);
        }

        public void Log(string s)
        {
            if (outputPanel != null)
            {
                outputPanel.OutputString(s);
            }
            else
                Console.WriteLine(s);
        }

        public string Parameters
        {
            get; set;
         }

        public void Shutdown()
        {
            
        }

        public LoggerVerbosity Verbosity
        {
            get; set;
        }
    }
}
