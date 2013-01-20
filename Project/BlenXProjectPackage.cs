// VsPkg.cs : Implementation of Project
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Project;
using Dema.BlenX.VisualStudio;
using Microsoft.VisualStudio;
using System.IO;
using Dema.VisualStudio;

namespace Dema.BlenX.VisualStudio.Project
{
   /// <summary>
   /// This is the class that implements the package exposed by this assembly.
   ///
   /// The minimum requirement for a class to be considered a valid package for Visual Studio
   /// is to implement the IVsPackage interface and register itself with the shell.
   /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
   /// to do it: it derives from the Package class that provides the implementation of the 
   /// IVsPackage interface and uses the registration attributes defined in the framework to 
   /// register itself and its components with the shell.
   /// </summary>
   // This attribute tells the registration utility (regpkg.exe) that this class needs
   // to be registered as package.
   [PackageRegistration(UseManagedResourcesOnly = true)]
   // A Visual Studio component can be registered under different regitry roots; for instance
   // when you debug your package you want to register it in the experimental hive. This
   // attribute specifies the registry root to use if no one is provided to regpkg.exe with
   // the /root switch.
   [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]
   // This attribute is used to register the informations needed to show the this package
   // in the Help/About dialog of Visual Studio.
   //[InstalledProductRegistration(true, null, null, null)]
   [InstalledProductRegistration(false, "#110", "#112", "1.0", IconResourceID = 400)]
   // In order be loaded inside Visual Studio in a machine that has not the VS SDK installed, 
   // package needs to have a valid load key (it can be requested at 
   // http://msdn.microsoft.com/vstudio/extend/). This attributes tells the shell that this 
   // package has a load key embedded in its resources.
   [ProvideLoadKey("Standard", "1.0", "BlenXProject", "Dema", 1)]
   [ProvideProjectFactory(
      typeof(BlenXProjectFactory),
      "BlenXProject",
      "BlenX Project Files (*.bxproj);*.bxproj",
      "bxproj", "bxproj",
      ".\\NullPath",
      LanguageVsTemplate = "BlenX")]
   //[ProvideProjectItem(
   //    typeof(BlenXProjectFactory),
   //    "BlenX file",
   //    @"..\..\Templates\ItemTemplates\BlenX",        
   //    100)]
   [ProvideObject(typeof(GeneralPropertyPage))]
   // This attribute is needed to let the shell know that this package exposes some menus.
   [ProvideMenuResource(1000, 1)]
   // This attribute registers a tool window exposed by this package.
   [ProvideToolWindow(typeof(BetaSimToolWindow),
   Style = Microsoft.VisualStudio.Shell.VsDockStyle.Tabbed,
   Window = "EEFA5220-E298-11D0-8F78-00A0C9110057")] //Properties
   [ProvideOptionPage(typeof(GeneralOptionsPage), "BlenX Project", "General", 110, 113, true)]
   [Guid(GuidList.guidProjectPkgString)]
   public sealed class BlenXProjectPackage : ProjectPackage, IVsInstalledProduct
   {
      /// <summary>
      /// Default constructor of the package.
      /// Inside this method you can place any initialization code that does not require 
      /// any Visual Studio service because at this point the package object is created but 
      /// not sited yet inside Visual Studio environment. The place to do all the other 
      /// initialization is the Initialize method.
      /// </summary>
      public BlenXProjectPackage()
      {
         Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
      }

      /// <summary>
      /// This function is called when the user clicks the menu item that shows the 
      /// tool window. See the Initialize method to see how the menu item is associated to 
      /// this function using the OleMenuCommandService service and the MenuCommand class.
      /// </summary>
      internal void ShowToolWindow(object sender, EventArgs e)
      {
         // Get the instance number 0 of this tool window. This window is single instance so this instance
         // is actually the only one.
         // The last flag is set to true so that if the tool window does not exists it will be created.
         ToolWindowPane window = this.FindToolWindow(typeof(BetaSimToolWindow), 0, true);
         if ((null == window) || (null == window.Frame))
         {
            throw new NotSupportedException(Resources.CanNotCreateWindow);
         }
         IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
         Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
      }

      private Microsoft.Build.Framework.ILogger msBuildLogger;

      /////////////////////////////////////////////////////////////////////////////
      // Overriden Package Implementation
      /// <summary>
      /// Initialization of the package; this method is called right after the package is sited, so this is the place
      /// where you can put all the initilaization code that rely on services provided by VisualStudio.
      /// </summary>
      protected override void Initialize()
      {
         Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
         base.Initialize();

         // Add our command handlers for menu (commands must exist in the .vsct file)
         OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
         if (null != mcs)
         {
            // Create the command for the menu item.
            CommandID menuCommandID = new CommandID(GuidList.guidProjectCmdSet, (int)PkgCmdIDList.cmdidRunSim);
            MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
            mcs.AddCommand(menuItem);
            // Create the command for the tool window
            CommandID toolwndCommandID = new CommandID(GuidList.guidProjectCmdSet, (int)PkgCmdIDList.cmdidSimOutput);
            MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
            mcs.AddCommand(menuToolWin);
         }

         ProjectUtilities.SetServiceProvider(this);

         msBuildLogger = new VSOutputLogger(this);

         this.RegisterProjectFactory(new BlenXProjectFactory(this));
      }


      internal static Microsoft.Build.BuildEngine.Project MSBuildProjectFromIVsProject(IVsProject project)
      {
         return Microsoft.Build.BuildEngine.Engine.GlobalEngine.GetLoadedProject(ProjectUtilities.GetProjectFilePath(project));
      }

      private static Microsoft.Build.BuildEngine.BuildTask GetNamedTask(Microsoft.Build.BuildEngine.Target target, string taskName)
      {
         if (target != null)
         {
            foreach (object taskObj in target)
            {
               Microsoft.Build.BuildEngine.BuildTask task = (Microsoft.Build.BuildEngine.BuildTask)taskObj;
               if (task.Name == taskName)
               {
                  return task;
               }
            }
         }

         return null;
      }

      static bool ContainsTask(Microsoft.Build.BuildEngine.UsingTaskCollection tasks, string taskName)
      {
         foreach (Microsoft.Build.BuildEngine.UsingTask task in tasks)
            if (task.TaskName == taskName)
               return true;
         return false;
      }

      public GeneralOptionsPage GeneralOptions
      {
         get { return this.GetDialogPage(typeof(GeneralOptionsPage)) as GeneralOptionsPage; }
      }

      /// <summary>
      /// This function is the callback used to execute a command when the a menu item is clicked.
      /// See the Initialize method to see how the menu item is associated to this function using
      /// the OleMenuCommandService service and the MenuCommand class.
      /// </summary>
      private void MenuItemCallback(object sender, EventArgs e)
      {
         var projects = ProjectUtilities.GetProjectsOfCurrentSelections();
         if (projects.Count > 0)
         {
            IVsProject currentProject = projects[0];
            if (currentProject != null)
            {
               //BlenX projects are msbuild files, get it from the node.
               Microsoft.Build.BuildEngine.Project msbuildProject = MSBuildProjectFromIVsProject(currentProject);
               Microsoft.Build.BuildEngine.Engine.GlobalEngine.UnregisterAllLoggers();
               Microsoft.Build.BuildEngine.Engine.GlobalEngine.RegisterLogger(msBuildLogger);

               //if (!ContainsTask(msbuildProject.UsingTasks, "Sim"))
               //    msbuildProject.AddNewUsingTaskFromAssemblyName("Sim", "Dema.BlenX.Tasks");

               var outputWindow = GetService(
                   typeof(SVsOutputWindow)) as IVsOutputWindow;
               IVsOutputWindowPane pane;
               Guid guidGeneralPane = VSConstants.GUID_OutWindowGeneralPane;
               outputWindow.GetPane(ref guidGeneralPane, out pane);

               // show it
               var uiShell = GetService(
                   typeof(SVsUIShell)) as IVsUIShell;
               IVsWindowFrame frame = null;
               Guid outWinGuid = new Guid(ToolWindowGuids80.Outputwindow);
               uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref outWinGuid, out frame);
               if (frame != null)
               {
                  ErrorHandler.ThrowOnFailure(frame.Show());
               }


               if (pane != null)
               {
                  pane.Activate();
                  //pane.OutputString("Launching simulation...\n");
               }

               // Get the properties from our option page
               var optionsPage = this.GetDialogPage(typeof(GeneralOptionsPage)) as GeneralOptionsPage;

               bool exeFound = false;
               string exePath = null;
               if (!String.IsNullOrEmpty(optionsPage.BetaSimDirectory))
               {
                  if (File.Exists(optionsPage.BetaSimDirectory))
                  {
                     exeFound = true;
                     exePath = optionsPage.BetaSimDirectory;
                  }
                  else if (File.Exists(Path.Combine(optionsPage.BetaSimDirectory, "SIM.exe")))
                  {
                     exeFound = true;
                     exePath = Path.Combine(optionsPage.BetaSimDirectory, "SIM.exe");
                  }
                  else if (File.Exists(Path.Combine(optionsPage.BetaSimDirectory, "SIM64.exe")))
                  {
                     exeFound = true;
                     exePath = Path.Combine(optionsPage.BetaSimDirectory, "SIM64.exe");
                  }
                  else if (File.Exists("SIM.exe"))
                  {
                     exeFound = true;
                     exePath = "SIM.exe";
                  }
                  else if (File.Exists("SIM64.exe"))
                  {
                     exeFound = true;
                     exePath = "SIM64.exe";
                  }
               }

               if (!exeFound)
               {
                  string message = "The BetaSim program was not found. Please specify the correct path to the SIM executable in the project options (under Tools -> Options -> BlenX Project -> General).";
                  int result = 0;
                  Guid guid = Guid.Empty;
                  uiShell.ShowMessageBox(0, ref guid, "Invalid path", message, "", 0,
                     OLEMSGBUTTON.OLEMSGBUTTON_OK,
                     OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                     OLEMSGICON.OLEMSGICON_CRITICAL, 0, out result);
                  return;
               }

               msbuildProject.SetProperty("BetaSimPath", exePath);
               //if (String.IsNullOrEmpty(msbuildProject.GetEvaluatedProperty("BaseOutputName")))
               //   msbuildProject.SetProperty("BaseOutputName", Path.GetFileNameWithoutExtension(msbuildProject.FullFileName));

               var simTarget = msbuildProject.Targets["Simulate"];

               if (msbuildProject.Build("Simulate"))
               {
                  // Use in a non-canonical way to "add" a project item for this simulation result
                  VSADDRESULT[] addResults = new VSADDRESULT[1];
                  string itemName = "SimResult";
                  currentProject.AddItem(VSConstants.VSITEMID_ROOT,
                      VSADDITEMOPERATION.VSADDITEMOP_OPENFILE, itemName,
                      0, //numFilesToOpen
                      null, IntPtr.Zero, addResults);

               }
            }
         }
      }

      #region IVsInstalledProduct Members

      public int IdBmpSplash(out uint pIdBmp)
      {
         pIdBmp = 400;
         return VSConstants.S_OK;
      }

      public int IdIcoLogoForAboutbox(out uint pIdIco)
      {
         pIdIco = 400;
         return VSConstants.S_OK;
      }

      public int OfficialName(out string pbstrName)
      {
         pbstrName = VSPackage._110;
         return VSConstants.S_OK;
      }

      public int ProductDetails(out string pbstrProductDetails)
      {
         pbstrProductDetails = VSPackage._112;
         return VSConstants.S_OK;
      }

      public int ProductID(out string pbstrPID)
      {
         pbstrPID = "1.0";
         return VSConstants.S_OK;
      }

      #endregion
   }
}