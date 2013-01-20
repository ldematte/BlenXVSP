using System;
using System.Collections.Generic;
using Babel;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Dema.BlenX.VisualStudio
{
   [ComVisible(true)]
   [Guid(BlenXPackageConstants.guidBProgLangService)]
   public partial class BlenXLanguageService : BabelLanguageService
   {
      public BlenXLanguageService(Package package) : base(package)
      {
         
      }

      public override string GetFormatFilterList()
      {
         return Configuration.FormatList;
      }

      public override ViewFilter CreateViewFilter(CodeWindowManager mgr, IVsTextView newView)
      {
         return new BlenXViewFilter(mgr, newView);
      }
   }
}