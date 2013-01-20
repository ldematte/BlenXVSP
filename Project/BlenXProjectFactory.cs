using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Project;
using System.Runtime.InteropServices;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Dema.BlenX.VisualStudio.Project
{
   [Guid(GuidList.guidBaseProjectFactoryString)]
   class BlenXProjectFactory : ProjectFactory
   {
      private BlenXProjectPackage package;

      public BlenXProjectFactory(BlenXProjectPackage package)
         : base(package)
      {
         this.package = package;
      }
      protected override ProjectNode CreateProject()
      {
         BlenXProjectNode project = new BlenXProjectNode(this.package);

         project.SetSite((IOleServiceProvider)((IServiceProvider)this.package).GetService(typeof(IOleServiceProvider)));
         return project;
      }
   }
}
