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
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.NodeNameLabel = new System.Windows.Forms.Label();
            this.NodeNameTextBox = new System.Windows.Forms.TextBox();
            this.CancelButton = new System.Windows.Forms.Button();
            this.CreateButton = new System.Windows.Forms.Button();
            this.NodeNameErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.IsCommentCheckBox = new System.Windows.Forms.CheckBox();
            this.IsCommentLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.NodeNameErrorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // NodeNameLabel
            // 
            this.NodeNameLabel.AutoSize = true;
            this.NodeNameLabel.Location = new System.Drawing.Point(12, 21);
            this.NodeNameLabel.Name = "NodeNameLabel";
            this.NodeNameLabel.Size = new System.Drawing.Size(69, 15);
            this.NodeNameLabel.TabIndex = 0;
            this.NodeNameLabel.Text = "Node name";
            // 
            // NodeNameTextBox
            // 
            this.NodeNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NodeNameErrorProvider.SetIconAlignment(this.NodeNameTextBox, System.Windows.Forms.ErrorIconAlignment.TopLeft);
            this.NodeNameTextBox.Location = new System.Drawing.Point(87, 17);
            this.NodeNameTextBox.MaxLength = 80;
            this.NodeNameTextBox.Name = "NodeNameTextBox";
            this.NodeNameTextBox.PlaceholderText = "Node name";
            this.NodeNameTextBox.Size = new System.Drawing.Size(307, 23);
            this.NodeNameTextBox.TabIndex = 1;
            this.NodeNameTextBox.Leave += new System.EventHandler(this.NodeNameTextBox_Leave);
            // 
            // CancelButton
            // 
            this.CancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButton.Location = new System.Drawing.Point(319, 77);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 4;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // CreateButton
            // 
            this.CreateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateButton.Location = new System.Drawing.Point(238, 77);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(75, 23);
            this.CreateButton.TabIndex = 5;
            this.CreateButton.Text = "Create";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // NodeNameErrorProvider
            // 
            this.NodeNameErrorProvider.BlinkRate = 0;
            this.NodeNameErrorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.NodeNameErrorProvider.ContainerControl = this;
            // 
            // IsCommentCheckBox
            // 
            this.IsCommentCheckBox.AutoSize = true;
            this.IsCommentCheckBox.Location = new System.Drawing.Point(87, 56);
            this.IsCommentCheckBox.Name = "IsCommentCheckBox";
            this.IsCommentCheckBox.Size = new System.Drawing.Size(15, 14);
            this.IsCommentCheckBox.TabIndex = 6;
            this.IsCommentCheckBox.UseVisualStyleBackColor = true;
            // 
            // IsCommentLabel
            // 
            this.IsCommentLabel.AutoSize = true;
            this.IsCommentLabel.Location = new System.Drawing.Point(12, 55);
            this.IsCommentLabel.Name = "IsCommentLabel";
            this.IsCommentLabel.Size = new System.Drawing.Size(61, 15);
            this.IsCommentLabel.TabIndex = 7;
            this.IsCommentLabel.Text = "Comment";
            // 
            // CreateTopNodeDialog
            // 
            this.AcceptButton = this.CreateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 112);
            this.Controls.Add(this.IsCommentLabel);
            this.Controls.Add(this.IsCommentCheckBox);
            this.Controls.Add(this.CreateButton);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.NodeNameTextBox);
            this.Controls.Add(this.NodeNameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreateTopNodeDialog";
            this.ShowIcon = false;
            this.Text = "Create new Top Node";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.CreateTopNodeDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.NodeNameErrorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label NodeNameLabel;
        private TextBox NodeNameTextBox;
        private ErrorProvider NodeNameErrorProvider;
        private new Button CancelButton;
        private Button CreateButton;
        private Label IsCommentLabel;
        private CheckBox IsCommentCheckBox;
    }
}