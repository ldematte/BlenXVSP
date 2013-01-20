namespace Dema.BlenX.VisualStudio.Project
{
    partial class BetaSimControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }


        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.resultsListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openWithGraphToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openWithPlotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // resultsListView
            // 
            this.resultsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.resultsListView.ContextMenuStrip = this.contextMenu;
            this.resultsListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsListView.FullRowSelect = true;
            this.resultsListView.Location = new System.Drawing.Point(0, 0);
            this.resultsListView.MultiSelect = false;
            this.resultsListView.Name = "resultsListView";
            this.resultsListView.ShowItemToolTips = true;
            this.resultsListView.Size = new System.Drawing.Size(150, 150);
            this.resultsListView.TabIndex = 0;
            this.resultsListView.UseCompatibleStateImageBehavior = false;
            this.resultsListView.View = System.Windows.Forms.View.Details;
            this.resultsListView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.resultsListView_MouseUp);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "File Name";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Time";
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openWithGraphToolStripMenuItem,
            this.openWithPlotToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(165, 48);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            // 
            // openWithGraphToolStripMenuItem
            // 
            this.openWithGraphToolStripMenuItem.Name = "openWithGraphToolStripMenuItem";
            this.openWithGraphToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.openWithGraphToolStripMenuItem.Text = "Open with Graph";
            this.openWithGraphToolStripMenuItem.Click += new System.EventHandler(this.openWithGraphToolStripMenuItem_Click);
            // 
            // openWithPlotToolStripMenuItem
            // 
            this.openWithPlotToolStripMenuItem.Name = "openWithPlotToolStripMenuItem";
            this.openWithPlotToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.openWithPlotToolStripMenuItem.Text = "Open with Plot";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Path";
            // 
            // BetaSimControl
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.resultsListView);
            this.Name = "BetaSimControl";
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.ListView resultsListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem openWithGraphToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openWithPlotToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader3;

    }
}
