using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace Dema.BlenX.VisualStudio
{
   class BlenXViewFilter : ViewFilter
   {
      public BlenXViewFilter(CodeWindowManager mgr, IVsTextView view)
         : base(mgr, view)
      {

      }

      protected override int QueryCommandStatus(ref Guid guidCmdGroup, uint nCmdId)
      {
         if (guidCmdGroup == VSConstants.VSStd2K)
         {
            if (nCmdId == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET
                || nCmdId == (uint)VSConstants.VSStd2KCmdID.SURROUNDWITH)
            {
               return (int)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
            }
         }

         return base.QueryCommandStatus(ref guidCmdGroup, nCmdId);
      }

      public override bool HandlePreExec(ref Guid guidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
      {
         if (guidCmdGroup == VSConstants.VSStd2K)
         {
            if (nCmdId == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
            {
               ExpansionProvider ep = this.GetExpansionProvider();
               if (this.TextView != null && ep != null)
               {
                  ep.DisplayExpansionBrowser(this.TextView, "Insert BlenX snippet...", null, false, null, false);
               }
               return true;
            }
            else if (nCmdId == (uint)VSConstants.VSStd2KCmdID.SURROUNDWITH)
            {
               ExpansionProvider ep = this.GetExpansionProvider();
               if (this.TextView != null && ep != null)
               {
                  ep.DisplayExpansionBrowser(this.TextView, "Surround with BlenX snippet...", null, false, null, false);
               }
               return true;
            }
         }

         return base.HandlePreExec(ref guidCmdGroup, nCmdId, nCmdexecopt, pvaIn, pvaOut);
      }
   }
}