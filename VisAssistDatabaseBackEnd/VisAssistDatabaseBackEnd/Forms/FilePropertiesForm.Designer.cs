namespace VisAssistDatabaseBackEnd.Forms
{
    partial class FilePropertiesForm
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
            this.dgvFileData = new System.Windows.Forms.DataGridView();
            this.btnAddFile = new System.Windows.Forms.Button();
            this.btnUpdateFile = new System.Windows.Forms.Button();
            this.FileID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RevisionID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FilePath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CreatedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastModifiedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Version = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Class = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DrawingType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.WirePrefix = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IgnoreWireColor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AllowDuplicateTags = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ShowPointData = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnDeleteFile = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFileData)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvFileData
            // 
            this.dgvFileData.AllowUserToAddRows = false;
            this.dgvFileData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFileData.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FileID,
            this.ProjectID,
            this.RevisionID,
            this.FileName,
            this.FilePath,
            this.CreatedDate,
            this.LastModifiedDate,
            this.Version,
            this.Class,
            this.DrawingType,
            this.WirePrefix,
            this.IgnoreWireColor,
            this.AllowDuplicateTags,
            this.ShowPointData});
            this.dgvFileData.Location = new System.Drawing.Point(36, 47);
            this.dgvFileData.Name = "dgvFileData";
            this.dgvFileData.RowHeadersVisible = false;
            this.dgvFileData.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvFileData.Size = new System.Drawing.Size(1403, 300);
            this.dgvFileData.TabIndex = 0;
            // 
            // btnAddFile
            // 
            this.btnAddFile.Location = new System.Drawing.Point(1461, 61);
            this.btnAddFile.Name = "btnAddFile";
            this.btnAddFile.Size = new System.Drawing.Size(75, 23);
            this.btnAddFile.TabIndex = 1;
            this.btnAddFile.Text = "Add File";
            this.btnAddFile.UseVisualStyleBackColor = true;
            this.btnAddFile.Click += new System.EventHandler(this.btnAddFile_Click);
            // 
            // btnUpdateFile
            // 
            this.btnUpdateFile.Location = new System.Drawing.Point(1461, 115);
            this.btnUpdateFile.Name = "btnUpdateFile";
            this.btnUpdateFile.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateFile.TabIndex = 2;
            this.btnUpdateFile.Text = "Update File";
            this.btnUpdateFile.UseVisualStyleBackColor = true;
            this.btnUpdateFile.Click += new System.EventHandler(this.btnUpdateFile_Click);
            // 
            // FileID
            // 
            this.FileID.HeaderText = "File ID";
            this.FileID.Name = "FileID";
            this.FileID.ReadOnly = true;
            // 
            // ProjectID
            // 
            this.ProjectID.HeaderText = "Project ID";
            this.ProjectID.Name = "ProjectID";
            this.ProjectID.ReadOnly = true;
            // 
            // RevisionID
            // 
            this.RevisionID.HeaderText = "Revision ID";
            this.RevisionID.Name = "RevisionID";
            // 
            // FileName
            // 
            this.FileName.HeaderText = "File Name";
            this.FileName.Name = "FileName";
            // 
            // FilePath
            // 
            this.FilePath.HeaderText = "File Path";
            this.FilePath.Name = "FilePath";
            // 
            // CreatedDate
            // 
            this.CreatedDate.HeaderText = "Created Date";
            this.CreatedDate.Name = "CreatedDate";
            // 
            // LastModifiedDate
            // 
            this.LastModifiedDate.HeaderText = "Modified Date";
            this.LastModifiedDate.Name = "LastModifiedDate";
            // 
            // Version
            // 
            this.Version.HeaderText = "Version";
            this.Version.Name = "Version";
            this.Version.ReadOnly = true;
            // 
            // Class
            // 
            this.Class.HeaderText = "Class";
            this.Class.Name = "Class";
            this.Class.ReadOnly = true;
            // 
            // DrawingType
            // 
            this.DrawingType.HeaderText = "Drawing Type";
            this.DrawingType.Name = "DrawingType";
            // 
            // WirePrefix
            // 
            this.WirePrefix.HeaderText = "Wire Prefix";
            this.WirePrefix.Name = "WirePrefix";
            // 
            // IgnoreWireColor
            // 
            this.IgnoreWireColor.HeaderText = "Ignore Wire Color";
            this.IgnoreWireColor.Name = "IgnoreWireColor";
            // 
            // AllowDuplicateTags
            // 
            this.AllowDuplicateTags.HeaderText = "Allow Duplicate Tags";
            this.AllowDuplicateTags.Name = "AllowDuplicateTags";
            // 
            // ShowPointData
            // 
            this.ShowPointData.HeaderText = "Show Point Data";
            this.ShowPointData.Name = "ShowPointData";
            // 
            // btnDeleteFile
            // 
            this.btnDeleteFile.Location = new System.Drawing.Point(1461, 155);
            this.btnDeleteFile.Name = "btnDeleteFile";
            this.btnDeleteFile.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteFile.TabIndex = 3;
            this.btnDeleteFile.Text = "Delete File";
            this.btnDeleteFile.UseVisualStyleBackColor = true;
            this.btnDeleteFile.Click += new System.EventHandler(this.btnDeleteFile_Click);
            // 
            // FilePropertiesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1669, 390);
            this.Controls.Add(this.btnDeleteFile);
            this.Controls.Add(this.btnUpdateFile);
            this.Controls.Add(this.btnAddFile);
            this.Controls.Add(this.dgvFileData);
            this.Name = "FilePropertiesForm";
            this.Text = "FilePropertiesForm";
            this.Load += new System.EventHandler(this.FilePropertiesForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFileData)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.DataGridView dgvFileData;
        public System.Windows.Forms.Button btnAddFile;
        public System.Windows.Forms.Button btnUpdateFile;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProjectID;
        private System.Windows.Forms.DataGridViewTextBoxColumn RevisionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn FileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilePath;
        private System.Windows.Forms.DataGridViewTextBoxColumn CreatedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn LastModifiedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn Version;
        private System.Windows.Forms.DataGridViewTextBoxColumn Class;
        private System.Windows.Forms.DataGridViewTextBoxColumn DrawingType;
        private System.Windows.Forms.DataGridViewTextBoxColumn WirePrefix;
        private System.Windows.Forms.DataGridViewTextBoxColumn IgnoreWireColor;
        private System.Windows.Forms.DataGridViewTextBoxColumn AllowDuplicateTags;
        private System.Windows.Forms.DataGridViewTextBoxColumn ShowPointData;
        public System.Windows.Forms.Button btnDeleteFile;
    }
}