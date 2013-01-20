using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio;

namespace Dema.BlenX.VisualStudio.Project
{
    public class BlenXFileNode : FileNode
    {
        public BlenXFileNode(BlenXProjectNode root, ProjectElement element)
            : base(root, element)
        {
        }    

        /// <summary>
        /// Gets a handle to the icon that should be set for this node
        /// </summary>
        /// <param name="open">Whether the folder is open, ignored here.</param>
        /// <returns>Handle to icon for the node</returns>
        public override object GetIconHandle(bool open)
        {
            //TODO: replace?
            return this.ProjectMgr.ImageHandler.GetIconHandle(this.ImageIndex);
        }

        public override int ImageIndex
        {
            get
            {
                // Check if the file is there.
                if (!this.CanShowDefaultIcon())
                {
                    return (int)ProjectNode.ImageName.MissingFile;
                }

                //Check for known extensions                
                return (int)ProjectNode.ImageName.ScriptFile;
            }
        }
    }
}
