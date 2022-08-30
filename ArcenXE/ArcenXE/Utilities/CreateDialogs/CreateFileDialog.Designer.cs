namespace ArcenXE.Utilities.CreateDialogs
{
    partial class CreateFileDialog
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
            this.FileNameLabel = new System.Windows.Forms.Label();
            this.FileNameTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // CreateFileDialog
            // 
            this.Name = "CreateFileDialog";
            this.Text = "Create new file";
            // 
            // FileNameLabel
            // 
            this.FileNameLabel.AutoSize = true;
            this.FileNameLabel.Location = new System.Drawing.Point( 12, 20 );
            this.FileNameLabel.Name = "FileNameLabel";
            this.FileNameLabel.Size = new System.Drawing.Size( 58, 15 );
            this.FileNameLabel.TabIndex = 0;
            this.FileNameLabel.Text = "File name";
            // 
            // FileNameTextBox
            // 
            this.FileNameTextBox.Location = new System.Drawing.Point( 76, 17 );
            this.FileNameTextBox.MaxLength = 80;
            this.FileNameTextBox.Name = "FileNameTextBox";
            this.FileNameTextBox.PlaceholderText = "File name without extension";
            this.FileNameTextBox.Size = new System.Drawing.Size( 331, 23 );
            this.FileNameTextBox.TabIndex = 1;
            this.FileNameTextBox.Leave += new System.EventHandler( this.FileNameTextBox_Leave );
            this.ClientSize = new System.Drawing.Size( 419, 108 );
            this.Controls.Add( this.FileNameLabel );
            this.Controls.Add( this.FileNameTextBox );
            this.ResumeLayout( false );
            this.PerformLayout();
        }

        #endregion

        private Label FileNameLabel;
        private TextBox FileNameTextBox;
    }
}