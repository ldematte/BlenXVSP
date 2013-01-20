using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
using UICommands = Microsoft.VisualStudio.VSConstants.VsUIHierarchyWindowCmdIds;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using System.Xml.Serialization;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;


namespace Dema.BlenX.VisualStudio.Project
{
    public class SimResultsNode : FileNode
    {
        private BetaSimResults simResults;
        private BlenXProjectPackage package;
        private string simResultsFileName;

        public SimResultsNode(BlenXProjectNode root, ProjectElement element)
            : base(root, element)
        {
            simResultsFileName = root.ProjectFolder + Path.DirectorySeparatorChar + "Results.sim";
            package = root.BlenXPackage;
            bool simFilePresent = false;
            if (File.Exists(simResultsFileName))
            {
                try
                {
                    simResults = BetaSimResults.LoadFromFile(simResultsFileName);
                    if (simResults != null)
                        simFilePresent = true;
                }
                catch
                {
                    simFilePresent = false;
                }
            }
            if (!simFilePresent)
            {
                // create a new, empty one
                simResults = new BetaSimResults();
                BetaSimResults.SaveToFile(simResultsFileName, simResults);
            }

            Debug.Assert(simResults != null);

            // Refresh window
            var window = package.FindToolWindow(typeof(BetaSimToolWindow), 0, true) as BetaSimToolWindow;
            if (window != null)
            {
                // Add
                window.Update(simResults.ResultList);
            }
        }

        protected override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
        {
            if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet97)
            {
                if ((VsCommands)cmd == VsCommands.Open)
                {
                    result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                }
            }
            result |= QueryStatusResult.NOTSUPPORTED | QueryStatusResult.INVISIBLE;
            return VSConstants.S_OK;
        }

        //protected override int ExecCommandOnNode(Guid guidCmdGroup, uint cmd, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        //{
        //    if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet97 && ((VsCommands)cmd == VsCommands.Open) ||
        //        guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidVsUIHierarchyWindowCmds && ((UICommands)cmd == UICommands.UIHWCMDID_DoubleClick))
        //    {
        //        return VSConstants.S_OK;                
        //    }
        //    return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
        //}

        internal void AddEntry(string newOutputName, string path, DateTime simulationTimestamp)
        {
            // Check if we overwrote something
            simResults.ResultList.RemoveAll((result) => (result.BaseOutputName == newOutputName && result.OutputPath == path));            
            simResults.ResultList.Add(new BetaSimResult { BaseOutputName = newOutputName, OutputPath = path, SimulationTime = simulationTimestamp });

            BetaSimResults.SaveToFile(simResultsFileName, simResults);

            var window = package.FindToolWindow(typeof(BetaSimToolWindow), 0, true) as BetaSimToolWindow;
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            // Add
            window.Update(simResults.ResultList);

            // Show
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
