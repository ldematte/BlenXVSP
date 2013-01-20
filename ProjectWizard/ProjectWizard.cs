using System;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TemplateWizard;
using EnvDTE;
using System.Windows;


namespace Dema.BlenX
{
   public class ProjectWizard : IWizard
   {
      private WizardDialogBox wizard;
       
      #region IWizard Members

      // This method is called before opening any item that 
      // has the OpenInEditor attribute.
      public void BeforeOpeningFile(ProjectItem projectItem)
      {
      }

      public void ProjectFinishedGenerating(Project project)
      {


         if (wizard.WizardData.AddFuncFile)
         {
            string dir = Path.GetDirectoryName(project.FullName);
            string code = "//Add your functions here\n";
            StreamWriter sw = new StreamWriter(dir + "\\" + wizard.WizardData.BaseFileName + ".func");
            sw.Write(code);
            sw.Close();
            project.ProjectItems.AddFromFile(dir + "\\" + wizard.WizardData.BaseFileName + ".func");
         }
      }

      // This method is only called for item templates,
      // not for project templates.
      public void ProjectItemFinishedGenerating(ProjectItem projectItem)
      {
      }

      // This method is called after the project is created.
      public void RunFinished()
      {
      }

      public void RunStarted(object automationObject, System.Collections.Generic.Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
      {
         wizard = new WizardDialogBox();
         bool dialogResult = (bool)wizard.ShowDialog();
         if (!dialogResult)
            throw new WizardCancelledException("The user has cancelled the wizard.");
      }

      // This method is only called for item templates,
      // not for project templates.
      public bool ShouldAddProjectItem(string filePath)
      {
         return true;
      }

      #endregion
   }
}
