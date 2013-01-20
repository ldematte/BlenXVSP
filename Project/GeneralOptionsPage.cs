using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace Dema.BlenX.VisualStudio.Project
{
   [Guid(GuidList.guidGeneralOptionsString)]
   public class GeneralOptionsPage : DialogPage
   {
      private string betaSimDirectory;

      [Category("Directories"), 
       Description("Path to the SIM executable")]
      public string BetaSimDirectory
      {
         get { return betaSimDirectory; }
         set { betaSimDirectory = value; }
      }

      private string graphDirectory;

      [Category("Directories"),
       Description("Path to the Graph executable")]
      public string GraphDirectory
      {
         get { return graphDirectory; }
         set { graphDirectory = value; }
      }



   }
}
