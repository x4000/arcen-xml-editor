namespace ArcenXE.Utilities.CreateDialogs
{
    partial class CreateTopNodeDialog
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
        protected override void InitializeComponent()
        {
            base.InitializeComponent();
            this.NodeNameLabel = new System.Windows.Forms.Label();
            this.NodeNameTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // CreateFileDialog
            // 
            this.Name = "CreateTopNodeDialog";
            this.Text = "Create new Top Node";
            // 
            // FileNameLabel
            // 
            this.NodeNameLabel.AutoSize = true;
            this.NodeNameLabel.Location = new System.Drawing.Point( 12, 20 );
            this.NodeNameLabel.Name = "NodeNameLabel";
            this.NodeNameLabel.Size = new System.Drawing.Size( 58, 15 );
            this.NodeNameLabel.TabIndex = 0;
            this.NodeNameLabel.Text = "Node name";
            // 
            // FileNameTextBox
            // 
            this.NodeNameTextBox.Location = new System.Drawing.Point( 76, 17 );
            this.NodeNameTextBox.MaxLength = 80;
            this.NodeNameTextBox.Name = "NodeNameTextBox";
            this.NodeNameTextBox.PlaceholderText = "Node name";
            this.NodeNameTextBox.Size = new System.Drawing.Size( 331, 23 );
            this.NodeNameTextBox.TabIndex = 1;
            this.NodeNameTextBox.Leave += new System.EventHandler( this.NodeNameTextBox_Leave );
            this.ResumeLayout( false );
            this.PerformLayout();
        }

        #endregion

        private Label NodeNameLabel;
        private TextBox NodeNameTextBox;
    }
}