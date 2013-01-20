using System.Security.Permissions;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;

namespace Dema.BlenX.VisualStudio.Project
{
    /// <summary>
    /// Summary description for MyControl.
    /// </summary>
    public partial class BetaSimControl : UserControl
    {
       private BlenXProjectPackage package;
        public BetaSimControl(BlenXProjectPackage package)
        {
           this.package = package;
            InitializeComponent();
        }

        /// <summary> 
        /// Let this control process the mnemonics.
        /// </summary>
        [UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows)]
        protected override bool ProcessDialogChar(char charCode)
        {
              // If we're the top-level form or control, we need to do the mnemonic handling
              if (charCode != ' ' && ProcessMnemonic(charCode))
              {
                    return true;
              }
              return base.ProcessDialogChar(charCode);
        }

        /// <summary>
        /// Enable the IME status handling for this control.
        /// </summary>
        protected override bool CanEnableIme
        {
            get
            {
                return true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        private void button1_Click(object sender, System.EventArgs e)
        {
            

            MessageBox.Show(this,
                            string.Format(System.Globalization.CultureInfo.CurrentUICulture, "We are inside {0}.button1_Click()", this.ToString()),
                            "Simulation Output");
        }

        internal void UpdateList(List<BetaSimResult> list)
        {
            //Always replace
            resultsListView.Items.Clear();
            foreach (var result in list)
            {
                var item = new ListViewItem(result.BaseOutputName);
                item.SubItems.Add(result.SimulationTime.ToShortDateString() + " " + result.SimulationTime.ToShortTimeString());
                item.SubItems.Add(result.OutputPath);

                resultsListView.Items.Add(item);                
            }

            //foreach (var result in list)
            //{
            //    bool alreadyPresent = false;
            //    foreach (ListViewItem item in this.resultsListView.Items)
            //    {
            //        if (item.Text == result.BaseOutputName && item.SubItems[1].Text == result.OutputPath)
            //        {
            //            alreadyPresent = true;
            //            break;
            //        }
            //    }

            //    if (!alreadyPresent)
            //    {
            //        var item = new ListViewItem(result.BaseOutputName);
            //        item.SubItems.Add(result.SimulationTime.ToShortDateString() + " " + result.SimulationTime.ToShortTimeString());
            //        item.SubItems.Add(result.OutputPath);

            //        resultsListView.Items.Add(item);
            //    }
            //}
        }

        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var lti = resultsListView.HitTest(lp);
            if (lti.Item == null)  
                e.Cancel = true;            
        }

        private Point lp;

        private void resultsListView_MouseUp(object sender, MouseEventArgs e)
        {
            lp = e.Location;
        }

        private void openWithGraphToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (resultsListView.SelectedItems.Count > 0)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.Arguments = System.IO.Path.Combine(resultsListView.SelectedItems[0].SubItems[2].Text, resultsListView.SelectedItems[0].Text + ".spec"); //single item select

                string graphPath = package.GeneralOptions.GraphDirectory;
                if (!File.Exists(graphPath))
                {
                   // try adding Graph.exe"
                   graphPath = graphPath + Path.DirectorySeparatorChar + "Graph.exe";
                }
                psi.FileName = graphPath;

                Process.Start(psi);
            }               
        }
    }
}
