namespace VisAssistDatabaseBackEnd.Forms
{
    partial class ReColororRenumberWiresForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnGo = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cboRange = new System.Windows.Forms.ComboBox();
            this.cboDirection = new System.Windows.Forms.ComboBox();
            this.lblOrder = new System.Windows.Forms.Label();
            this.lblRange = new System.Windows.Forms.Label();
            this.lblLabel = new System.Windows.Forms.Label();
            this.cboColor = new System.Windows.Forms.ComboBox();
            this.lblRenumber = new System.Windows.Forms.Label();
            this.txtPrefix = new System.Windows.Forms.TextBox();
            this.txtNumber = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnGo
            // 
            this.btnGo.Location = new System.Drawing.Point(270, 207);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(75, 23);
            this.btnGo.TabIndex = 0;
            this.btnGo.Text = "Go";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(355, 207);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // cboRange
            // 
            this.cboRange.FormattingEnabled = true;
            this.cboRange.Items.AddRange(new object[] {
            "Current Page",
            "Current Selection",
            "All Pages"});
            this.cboRange.Location = new System.Drawing.Point(27, 44);
            this.cboRange.Margin = new System.Windows.Forms.Padding(1);
            this.cboRange.Name = "cboRange";
            this.cboRange.Size = new System.Drawing.Size(144, 21);
            this.cboRange.TabIndex = 14;
            // 
            // cboDirection
            // 
            this.cboDirection.FormattingEnabled = true;
            this.cboDirection.Items.AddRange(new object[] {
            "Top-Bottom",
            "Bottom-Top",
            "Left-Right",
            "Right-Left"});
            this.cboDirection.Location = new System.Drawing.Point(27, 100);
            this.cboDirection.Margin = new System.Windows.Forms.Padding(1);
            this.cboDirection.Name = "cboDirection";
            this.cboDirection.Size = new System.Drawing.Size(144, 21);
            this.cboDirection.TabIndex = 20;
            // 
            // lblOrder
            // 
            this.lblOrder.AutoSize = true;
            this.lblOrder.Location = new System.Drawing.Point(27, 77);
            this.lblOrder.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.lblOrder.Name = "lblOrder";
            this.lblOrder.Size = new System.Drawing.Size(33, 13);
            this.lblOrder.TabIndex = 21;
            this.lblOrder.Text = "Order";
            // 
            // lblRange
            // 
            this.lblRange.AutoSize = true;
            this.lblRange.Location = new System.Drawing.Point(27, 20);
            this.lblRange.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.lblRange.Name = "lblRange";
            this.lblRange.Size = new System.Drawing.Size(39, 13);
            this.lblRange.TabIndex = 22;
            this.lblRange.Text = "Range";
            // 
            // lblLabel
            // 
            this.lblLabel.AutoSize = true;
            this.lblLabel.Location = new System.Drawing.Point(27, 133);
            this.lblLabel.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.lblLabel.Name = "lblLabel";
            this.lblLabel.Size = new System.Drawing.Size(70, 13);
            this.lblLabel.TabIndex = 28;
            this.lblLabel.Text = "Starting Color";
            // 
            // cboColor
            // 
            this.cboColor.FormattingEnabled = true;
            this.cboColor.Items.AddRange(new object[] {
            "Yellow",
            "Red",
            "Green",
            "Blue",
            "Pink",
            "Orange",
            "Purple",
            "Teal"});
            this.cboColor.Location = new System.Drawing.Point(30, 160);
            this.cboColor.Margin = new System.Windows.Forms.Padding(1);
            this.cboColor.Name = "cboColor";
            this.cboColor.Size = new System.Drawing.Size(144, 21);
            this.cboColor.TabIndex = 29;
            // 
            // lblRenumber
            // 
            this.lblRenumber.AutoSize = true;
            this.lblRenumber.Location = new System.Drawing.Point(27, 193);
            this.lblRenumber.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.lblRenumber.Name = "lblRenumber";
            this.lblRenumber.Size = new System.Drawing.Size(83, 13);
            this.lblRenumber.TabIndex = 32;
            this.lblRenumber.Text = "Starting Number";
            // 
            // txtPrefix
            // 
            this.txtPrefix.Location = new System.Drawing.Point(30, 160);
            this.txtPrefix.Name = "txtPrefix";
            this.txtPrefix.Size = new System.Drawing.Size(144, 20);
            this.txtPrefix.TabIndex = 33;
            // 
            // txtNumber
            // 
            this.txtNumber.Location = new System.Drawing.Point(30, 210);
            this.txtNumber.Name = "txtNumber";
            this.txtNumber.Size = new System.Drawing.Size(144, 20);
            this.txtNumber.TabIndex = 34;
            // 
            // ReColororRenumberWiresForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(442, 272);
            this.Controls.Add(this.txtNumber);
            this.Controls.Add(this.txtPrefix);
            this.Controls.Add(this.lblRenumber);
            this.Controls.Add(this.cboColor);
            this.Controls.Add(this.lblLabel);
            this.Controls.Add(this.lblRange);
            this.Controls.Add(this.lblOrder);
            this.Controls.Add(this.cboDirection);
            this.Controls.Add(this.cboRange);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnGo);
            this.Name = "ReColororRenumberWiresForm";
            this.Text = "ReColororRenumberWiresForm";
            this.Load += new System.EventHandler(this.ReColororRenumberWiresForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btnGo;
        public System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.ComboBox cboRange;
        public System.Windows.Forms.ComboBox cboDirection;
        public System.Windows.Forms.Label lblOrder;
        public System.Windows.Forms.Label lblRange;
        public System.Windows.Forms.Label lblLabel;
        public System.Windows.Forms.ComboBox cboColor;
        public System.Windows.Forms.Label lblRenumber;
        public System.Windows.Forms.TextBox txtPrefix;
        public System.Windows.Forms.TextBox txtNumber;
    }
}