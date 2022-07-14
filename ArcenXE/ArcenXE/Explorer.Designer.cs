namespace ArcenXE
{
    partial class Explorer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && (components != null) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ToolBar = new System.Windows.Forms.ToolStrip();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.SearchBar = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ToolBar
            // 
            this.ToolBar.Location = new System.Drawing.Point(0, 0);
            this.ToolBar.Name = "ToolBar";
            this.ToolBar.Size = new System.Drawing.Size(283, 25);
            this.ToolBar.TabIndex = 0;
            this.ToolBar.Text = "Toolbar";
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.treeView1.Location = new System.Drawing.Point(0, 61);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(283, 651);
            this.treeView1.TabIndex = 1;
            // 
            // SearchBar
            // 
            this.SearchBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchBar.Location = new System.Drawing.Point(0, 28);
            this.SearchBar.MinimumSize = new System.Drawing.Size(60, 20);
            this.SearchBar.Name = "SearchBar";
            this.SearchBar.PlaceholderText = "Type to search...";
            this.SearchBar.Size = new System.Drawing.Size(283, 23);
            this.SearchBar.TabIndex = 2;
            // 
            // Explorer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(283, 712);
            this.Controls.Add(this.SearchBar);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.ToolBar);
            this.Name = "Explorer";
            this.Text = "Explorer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ToolStrip ToolBar;
        private TreeView treeView1;
        private TextBox SearchBar;
    }
}