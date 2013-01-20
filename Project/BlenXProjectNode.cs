using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dema.BlenX.VisualStudio;
using Microsoft.VisualStudio.Project;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.VisualStudio.Shell.Interop;
using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using Microsoft.VisualStudio;


namespace Dema.BlenX.VisualStudio.Project
{
   public class BlenXProjectNode : ProjectNode
   {
      private static ImageList imageList;

      internal enum SimFiles
      {
          Prog,
          Types,
          Func
      }

      static BlenXProjectNode()
      {
         imageList = Utilities.GetImageList(typeof(BlenXProjectNode).Assembly.GetManifestResourceStream("Dema.BlenX.VisualStudio.Project.Resources.BlenXProjectNode.bmp"));

         codeTypes.Add(".prog", SimFiles.Prog);
         codeTypes.Add(".types", SimFiles.Types);
         codeTypes.Add(".func", SimFiles.Func);
         codeTypes.Add(".type", SimFiles.Types);
         codeTypes.Add(".decl", SimFiles.Func);
      }

      internal static int imageIndex;
      public override int ImageIndex
      {
         get { return imageIndex; }
      }


      private BlenXProjectPackage package;

      public BlenXProjectNode(BlenXProjectPackage package)
      {
         this.package = package;
         imageIndex = this.ImageHandler.ImageList.Images.Count;

         foreach (Image img in imageList.Images)
         {
            this.ImageHandler.AddImage(img);
         }
      }

      public BlenXProjectPackage BlenXPackage
      {
          get { return package; }
      }

      public override Guid ProjectGuid
      {
         get { return GuidList.guidBaseProjectFactory; }
      }
      public override string ProjectType
      {
          //get { return "BlenXProjectType"; }
          get { return "BlenX"; }
      }

      //protected internal VSLangProj.VSProject VSProject
      //{
      //    get
      //    {
      //        if (vsProject == null)
      //            vsProject = new OAVSProject(this);
      //        return vsProject;
      //    }
      //}
      private IVsHierarchy InteropSafeHierarchy
      {
         get
         {
            IntPtr unknownPtr = Utilities.QueryInterfaceIUnknown(this);
            if (IntPtr.Zero == unknownPtr)
            {
               return null;
            }
            IVsHierarchy hier = Marshal.GetObjectForIUnknown(unknownPtr) as IVsHierarchy;
            return hier;
         }
      }

      public override void AddFileFromTemplate(
          string source, string target)
      {
         string nameSpace = this.FileTemplateProcessor.GetFileNamespace(target, this);
         string className = Path.GetFileNameWithoutExtension(target);

         this.FileTemplateProcessor.AddReplace("$nameSpace$", nameSpace);
         this.FileTemplateProcessor.AddReplace("$className$", className);

         this.FileTemplateProcessor.UntokenFile(source, target);
         this.FileTemplateProcessor.Reset();
      }

      protected override Guid[] GetConfigurationIndependentPropertyPages()
      {
         Guid[] result = new Guid[1];
         result[0] = typeof(GeneralPropertyPage).GUID;
         return result;
      }
      protected override Guid[] GetPriorityProjectDesignerPages()
      {
         Guid[] result = new Guid[1];
         result[0] = typeof(GeneralPropertyPage).GUID;
         return result;
      }

      public override int Close()
      {
         if (Site != null)
         {
            IBlenXLibraryManager libraryManager = Site.GetService(typeof(IBlenXLibraryManager)) as IBlenXLibraryManager;
            if (null != libraryManager)
            {
               libraryManager.UnregisterHierarchy(this.InteropSafeHierarchy);
            }
         }

         return base.Close();
      }


      public override void Load(string filename, string location, string name, uint flags, ref Guid iidProject, out int canceled)
      {
         base.Load(filename, location, name, flags, ref iidProject, out canceled);
         // WAP ask the designer service for the CodeDomProvider corresponding to the project node.
         //this.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);
         //this.OleServiceProvider.AddService(typeof(System.CodeDom.Compiler.CodeDomProvider), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);

         IBlenXLibraryManager libraryManager = Site.GetService(typeof(IBlenXLibraryManager)) as IBlenXLibraryManager;
         if (null != libraryManager)
         {
            libraryManager.RegisterHierarchy(this.InteropSafeHierarchy);
         }

         //If this is a WPFFlavor-ed project, then add a project-level DesignerContext service to provide
         //event handler generation (EventBindingProvider) for the XAML designer.
         //this.OleServiceProvider.AddService(typeof(DesignerContext), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);

      }

      protected override ReferenceContainerNode CreateReferenceContainerNode()
      {
         return null;
      }

      protected internal override void ProcessReferences()
      {
          //base.ProcessReferences();
      }

       // Load -> ProcessFiles -> AddFileNodeToNode -> AddChild & CreateFileNode
      public override void AddChild(HierarchyNode node)
      {
          base.AddChild(node);
      }

      // Create a node that corresponds to a file
      public override FileNode CreateFileNode(ProjectElement item)
      {
          if (item.Item.Name.Equals("SimResults"))
              return new SimResultsNode(this, item);
          else if (IsItemTypeFileType(item.Item.Name))
              return new BlenXFileNode(this, item);
          else 
            return new FileNode(this, item);
      }

       // (after dialog) Add existing item -> AdItemWithSpecific
      public override int AddItem(uint itemIdLoc, VSADDITEMOPERATION op, string itemName, uint filesToOpen, string[] files, IntPtr dlgOwner, VSADDRESULT[] result)
      {
          // Special MSBuild case: we invoked it directly instead of using the "add item" dialog.
          if (itemName != null && itemName.Equals("SimResult"))
          {
              SimResultsNode resultsNode = null;
              if (this.IsSimResultFilePresent())
                  resultsNode = this.FindChild("Results.sim") as SimResultsNode;

              if (resultsNode == null)
              {
                  resultsNode = CreateAndAddSimResultsFile(); //TODO: what about save on project unload/exit?
              }


              var simulationTimestamp = DateTime.Now;

              bool filePresent = false;
              bool renameOutput = true;
              Boolean.TryParse(GetProjectProperty("RenameOutput"), out renameOutput);

              string baseOutputName = GetProjectProperty("BaseOutputName");
              if (String.IsNullOrEmpty(baseOutputName))
              {
                  var msBuildProject = BlenXProjectPackage.MSBuildProjectFromIVsProject(this);
                  foreach (Microsoft.Build.BuildEngine.BuildItem item in msBuildProject.EvaluatedItems)
                  {
                      if (item.Name.Equals(SimFiles.Prog.ToString()))
                      {
                          baseOutputName = item.Include;
                          break;
                      }
                  }

                  if (String.IsNullOrEmpty(baseOutputName))
                      baseOutputName = Path.GetFileNameWithoutExtension(this.ProjectFile) + ".prog";
              }

              string newOutputName;
              if (renameOutput)
              {
                  newOutputName = baseOutputName + simulationTimestamp.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss");
                  // TODO: try to move!

                  filePresent = TryCopy(baseOutputName, newOutputName, "spec");
                  TryCopy(baseOutputName, newOutputName, "E.out");
                  TryCopy(baseOutputName, newOutputName, "C.out");
                  TryCopy(baseOutputName, newOutputName, "V.out");
              }
              else
              {
                  newOutputName = baseOutputName;
                  filePresent = File.Exists(this.ProjectFolder + Path.DirectorySeparatorChar + baseOutputName + ".spec");
              }

              if (filePresent)
              {
                  resultsNode.AddEntry(newOutputName, this.ProjectFolder, simulationTimestamp);
              }

              return VSConstants.S_OK;
          }
          // Add existing item: itemIdLoc = 4294967294, VSADDITEMOP_OPENFILE, itemName = null, files (filename), null, VSADDRESULT 1 (init with ADDRESULT_Failure)          
          return base.AddItem(itemIdLoc, op, itemName, filesToOpen, files, dlgOwner, result);
      }

      private bool TryCopy(string baseOutputName, string newOutputName, string ext)
      {
          try
          {
              var sourceFile = this.ProjectFolder + Path.DirectorySeparatorChar + baseOutputName + "." + ext;
              if (!File.Exists(sourceFile))
                  return false;

              var destFile = this.ProjectFolder + Path.DirectorySeparatorChar + newOutputName + "." + ext;
              File.Move(sourceFile, destFile);
              return true;
          }
          catch (IOException)
          {
              return false;
          }
      }

      private SimResultsNode CreateAndAddSimResultsFile()
      {	
			string baseDir = this.GetBaseDirectoryForAddingFiles(this);
			// If we did not get a directory for node that is the parent of the item then fail.
			if(String.IsNullOrEmpty(baseDir))
			{
				return null;
			}

			// Pre-calculates some paths that we can use when calling CanAddItems
			string newFileName = Path.Combine(baseDir, "Results.sim");

            string[] filesToAdd = new string[] { newFileName };

            // Ask tracker objects if we can add files
          	VSQUERYADDFILEFLAGS[] flags = this.GetQueryAddFileFlags(filesToAdd);
			if(!Tracker.CanAddItems(filesToAdd, flags))
			{
				// We were not allowed to add the files
				return null;
			}

			if(!this.ProjectMgr.QueryEditProjectFile(false))
			{
				throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
			}

			// Add the files to the hierarchy
            var resultNode = this.FindChild(newFileName) as SimResultsNode;
			
            if(resultNode != null)
            {
                // If the file was already added, continue
                return resultNode;
            }

			//this.AddNewFileNodeToHierarchy(n, newFileName);
			resultNode = new SimResultsNode(this, this.AddFileToMsBuild(newFileName));
			this.AddChild(resultNode);

			// Notify listeners that items were appended
            this.OnItemsAppended(this);


            return resultNode;
      }

      private bool IsSimResultFilePresent()
      {
          //TODO!
          var msBuildProject = BlenXProjectPackage.MSBuildProjectFromIVsProject(this);
          foreach (Microsoft.Build.BuildEngine.BuildItem item in msBuildProject.EvaluatedItems)
          {
              if (item.Name.Equals("SimResults"))
              {
                  return true;
              }
          }
          return false;
      }

       // Add existing Item: addType == AddExistingItem
       // This runs before the dialog
      protected override int AddItemToHierarchy(HierarchyAddType addType)
      {
          return base.AddItemToHierarchy(addType);
      }

      public override int AddItemWithSpecific(uint itemIdLoc, VSADDITEMOPERATION op, string itemName, uint filesToOpen, string[] files, IntPtr dlgOwner, uint editorFlags, ref Guid editorType, string physicalView, ref Guid logicalView, VSADDRESULT[] result)
      {
          //editor type?
          return base.AddItemWithSpecific(itemIdLoc, op, itemName, filesToOpen, files, dlgOwner, editorFlags, ref editorType, physicalView, ref logicalView, result);
      }

      public override int AddProjectReference()
      {
          return -1;
      }

      protected internal override int AddWebReference()
      {
          return -1;
      }

       //ExcludeFromProject -> Remove (HierarchyNode) -> RemoveChild(this)
      public override void Remove(bool removeFromStorage)
      {
          base.Remove(removeFromStorage);
      }

      public override void RemoveChild(HierarchyNode node)
      {
          base.RemoveChild(node);
      }

      public override int RemoveItem(uint reserved, uint itemId, out int result)
      {
          return base.RemoveItem(reserved, itemId, out result);
      }

      private static Dictionary<string, SimFiles> codeTypes = new Dictionary<string, SimFiles>();

      public override bool IsCodeFile(string fileName)
      {
          string ext = Path.GetExtension(fileName).ToLower();
          if (!codeTypes.ContainsKey(ext))
              return false;
          else
              return true;
      }

      //TODO: where do I verify that only one func/prog/types is added? 
      protected override ProjectElement AddFileToMsBuild(string file)
      {
          ProjectElement newItem;

          string itemPath = PackageUtilities.MakeRelativeIfRooted(file, this.BaseURI);
          Debug.Assert(!Path.IsPathRooted(itemPath), "Cannot add item with full path.");

          if (this.IsCodeFile(itemPath))
          {
              string ext = Path.GetExtension(itemPath).ToLower();
              string itemType = codeTypes[ext].ToString();
              newItem = this.CreateMsBuildFileItem(itemPath, itemType);
              //newItem.SetMetadata(ProjectFileConstants.SubType, ProjectFileAttributeValue.Code);
              //newItem.SetMetadata(ProjectFileConstants.SubType, "Model");
          }          
          else if (this.IsSimResultFile(itemPath))
          {
              newItem = this.CreateMsBuildFileItem(itemPath, "SimResults");
          }
          else
          {
              newItem = this.CreateMsBuildFileItem(itemPath, ProjectFileConstants.Content);
              newItem.SetMetadata(ProjectFileConstants.SubType, ProjectFileConstants.Content);
          }

          return newItem;
      }

      private bool IsSimResultFile(string itemPath)
      {
          string fileName = Path.GetFileName(itemPath);
          return (fileName.Equals("Results.sim"));              
      }

      // HierarchyNode -> AddItemToHierarchy

      // AddItem -> AddItemWithSpecific -> AddNewFileNodeToHierarchy (Add exisisting item) -> CreateFileNode
      protected override void AddNewFileNodeToHierarchy(HierarchyNode parentNode, string fileName)
      {
          // Add func at beginning -> calls here
          // Also "Add existing item" calls here
          base.AddNewFileNodeToHierarchy(parentNode, fileName);
      }

      protected override bool IsItemTypeFileType(string type)
      {
          if (String.Compare(type, SimFiles.Prog.ToString(), StringComparison.OrdinalIgnoreCase) == 0
                || String.Compare(type, SimFiles.Types.ToString(), StringComparison.OrdinalIgnoreCase) == 0
                || String.Compare(type, SimFiles.Func.ToString(), StringComparison.OrdinalIgnoreCase) == 0
                || String.Compare(type, "SimResults", StringComparison.OrdinalIgnoreCase) == 0)
              return true;

          // we don't know about this type
          return base.IsItemTypeFileType(type);
      }

      protected internal override void ProcessFiles()
      {
          base.ProcessFiles();
      }

      protected override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
      {
          if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet2K)
          {
              switch ((VsCommands2K)cmd)
              {
                  case VsCommands2K.ADDREFERENCE:
                      result |= QueryStatusResult.NOTSUPPORTED | QueryStatusResult.INVISIBLE;
                      return VSConstants.S_OK;
              }
          }
          else if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet97)
          {
              switch ((VsCommands)cmd)
              {
                  case VsCommands.SetStartupProject:
                  case VsCommands.Start:
                  case VsCommands.StartNoDebug:
                      result |= QueryStatusResult.NOTSUPPORTED | QueryStatusResult.INVISIBLE;
                      return VSConstants.S_OK;
              }
          }

          return base.QueryStatusOnNode(guidCmdGroup, cmd, pCmdText, ref result);
      }

      protected override QueryStatusResult QueryStatusCommandFromOleCommandTarget(Guid guidCmdGroup, uint cmd, out bool handled)
      {
         if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet2K)
         {
             var command = (VsCommands2K)cmd;
             switch (command)
            {
               case VsCommands2K.ADDREFERENCE:
                   handled = false;
                  return QueryStatusResult.NOTSUPPORTED | QueryStatusResult.INVISIBLE;
            }

             return base.QueryStatusCommandFromOleCommandTarget(guidCmdGroup, cmd, out handled);
         }
         else if (guidCmdGroup == Microsoft.VisualStudio.Project.VsMenus.guidStandardCommandSet97)
         {
             switch ((VsCommands)cmd)
             {
                 case VsCommands.SetStartupProject:
                 case VsCommands.Start:
                 case VsCommands.StartNoDebug:
                     handled = false;
                     return QueryStatusResult.NOTSUPPORTED | QueryStatusResult.INVISIBLE;
             }
         }

         handled = false;
         return QueryStatusResult.NOTSUPPORTED | QueryStatusResult.INVISIBLE;
      }

   }
}

