using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using System.ComponentModel;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;

namespace Dema.BlenX.VisualStudio.Project
{
   [ComVisible(true), Guid("6BC7046B-B110-40d8-9F23-34263D8D2936")]
    public class GeneralPropertyPage : SettingsPage, EnvDTE80.IInternalExtenderProvider
    {

      public GeneralPropertyPage()
          : base()
      {
         this.Name = "General";
      }

      private string baseOutputName;
      [Category("Output options")]
      [DisplayName("Base output name")]
      [Description("The prefix and path used for output files")]
      public string BaseOutputName
      {
          get { return baseOutputName; }
          set { baseOutputName = value; IsDirty = true; }
      }

      private bool renameOutput;
      [Category("Output options")]
      [DisplayName("Rename output")]
      [Description("Indicates if output files should be renamed or overwritten when they already exists")]
      public bool RenameOutput
      {
          get { return renameOutput; }
          set { renameOutput = value; IsDirty = true; }
      }

      protected override void BindProperties()
      {
          this.baseOutputName = this.ProjectMgr.GetProjectProperty("BaseOutputName", true);
          //TODO: initialize with project name?
          this.renameOutput = true;
          Boolean.TryParse(this.ProjectMgr.GetProjectProperty("RenameOutput", true), out this.renameOutput);
      }

      protected override int ApplyChanges()
      {
          this.ProjectMgr.SetProjectProperty("BaseOutputName", this.baseOutputName);
          this.ProjectMgr.SetProjectProperty("RenameOutput", this.renameOutput.ToString());
         this.IsDirty = false;

         return VSConstants.S_OK;
      }

      #region IInternalExtenderProvider Members

      bool EnvDTE80.IInternalExtenderProvider.CanExtend(string extenderCATID, string extenderName, object extendeeObject)
      {
          IVsHierarchy outerHierarchy = HierarchyNode.GetOuterHierarchy(this.ProjectMgr);
          if (outerHierarchy is EnvDTE80.IInternalExtenderProvider)
              return ((EnvDTE80.IInternalExtenderProvider)outerHierarchy).CanExtend(extenderCATID, extenderName, extendeeObject);
          return false;
      }

      object EnvDTE80.IInternalExtenderProvider.GetExtender(string extenderCATID, string extenderName, object extendeeObject, EnvDTE.IExtenderSite extenderSite, int cookie)
      {
          IVsHierarchy outerHierarchy = HierarchyNode.GetOuterHierarchy(this.ProjectMgr);
          if (outerHierarchy is EnvDTE80.IInternalExtenderProvider)
              return ((EnvDTE80.IInternalExtenderProvider)outerHierarchy).GetExtender(extenderCATID, extenderName, extendeeObject, extenderSite, cookie);
          return null;
      }

      object EnvDTE80.IInternalExtenderProvider.GetExtenderNames(string extenderCATID, object extendeeObject)
      {
          IVsHierarchy outerHierarchy = HierarchyNode.GetOuterHierarchy(this.ProjectMgr);
          if (outerHierarchy is EnvDTE80.IInternalExtenderProvider)
              return ((EnvDTE80.IInternalExtenderProvider)outerHierarchy).GetExtenderNames(extenderCATID, extendeeObject);
          return null;
      }

      #endregion
   }
}
