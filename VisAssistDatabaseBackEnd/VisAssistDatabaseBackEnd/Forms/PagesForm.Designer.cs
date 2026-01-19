namespace VisAssistDatabaseBackEnd.Forms
{
    partial class PagesForm
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
            this.dgvPages = new System.Windows.Forms.DataGridView();
            this.PageID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PageName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProjectID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PageIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CreatedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastModifiedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Version = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Class = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Orientation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Scale = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtFileID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnGetPages = new System.Windows.Forms.Button();
            this.btnUpdatePages = new System.Windows.Forms.Button();
            this.btnGetAllPages = new System.Windows.Forms.Button();
            this.btnDeletePage = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPages)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvPages
            // 
            this.dgvPages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPages.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PageID,
            this.PageName,
            this.ProjectID,
            this.FileID,
            this.PageIndex,
            this.CreatedDate,
            this.LastModifiedDate,
            this.Version,
            this.Class,
            this.Orientation,
            this.Scale});
            this.dgvPages.Location = new System.Drawing.Point(49, 108);
            this.dgvPages.Name = "dgvPages";
            this.dgvPages.RowHeadersVisible = false;
            this.dgvPages.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPages.Size = new System.Drawing.Size(1145, 276);
            this.dgvPages.TabIndex = 0;
            // 
            // PageID
            // 
            this.PageID.HeaderText = "Page ID";
            this.PageID.Name = "PageID";
            // 
            // PageName
            // 
            this.PageName.HeaderText = "Page Name";
            this.PageName.Name = "PageName";
            // 
            // ProjectID
            // 
            this.ProjectID.HeaderText = "Project ID";
            this.ProjectID.Name = "ProjectID";
            // 
            // FileID
            // 
            this.FileID.HeaderText = "FileID";
            this.FileID.Name = "FileID";
            // 
            // PageIndex
            // 
            this.PageIndex.HeaderText = "Page Index";
            this.PageIndex.Name = "PageIndex";
            // 
            // CreatedDate
            // 
            this.CreatedDate.HeaderText = "Created Date";
            this.CreatedDate.Name = "CreatedDate";
            // 
            // LastModifiedDate
            // 
            this.LastModifiedDate.HeaderText = "Last Modified Date";
            this.LastModifiedDate.Name = "LastModifiedDate";
            // 
            // Version
            // 
            this.Version.HeaderText = "Version";
            this.Version.Name = "Version";
            // 
            // Class
            // 
            this.Class.HeaderText = "Class";
            this.Class.Name = "Class";
            // 
            // Orientation
            // 
            this.Orientation.HeaderText = "Orientation";
            this.Orientation.Name = "Orientation";
            // 
            // Scale
            // 
            this.Scale.HeaderText = "Scale";
            this.Scale.Name = "Scale";
            // 
            // txtFileID
            // 
            this.txtFileID.Location = new System.Drawing.Point(49, 61);
            this.txtFileID.Name = "txtFileID";
            this.txtFileID.Size = new System.Drawing.Size(100, 20);
            this.txtFileID.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(49, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "FileID";
            // 
            // btnGetPages
            // 
            this.btnGetPages.Location = new System.Drawing.Point(201, 61);
            this.btnGetPages.Name = "btnGetPages";
            this.btnGetPages.Size = new System.Drawing.Size(75, 23);
            this.btnGetPages.TabIndex = 3;
            this.btnGetPages.Text = "Get Pages";
            this.btnGetPages.UseVisualStyleBackColor = true;
            this.btnGetPages.Click += new System.EventHandler(this.btnGetPages_Click);
            // 
            // btnUpdatePages
            // 
            this.btnUpdatePages.Location = new System.Drawing.Point(320, 61);
            this.btnUpdatePages.Name = "btnUpdatePages";
            this.btnUpdatePages.Size = new System.Drawing.Size(100, 23);
            this.btnUpdatePages.TabIndex = 4;
            this.btnUpdatePages.Text = "Update Pages";
            this.btnUpdatePages.UseVisualStyleBackColor = true;
            this.btnUpdatePages.Click += new System.EventHandler(this.btnUpdatePages_Click);
            // 
            // btnGetAllPages
            // 
            this.btnGetAllPages.Location = new System.Drawing.Point(201, 32);
            this.btnGetAllPages.Name = "btnGetAllPages";
            this.btnGetAllPages.Size = new System.Drawing.Size(100, 23);
            this.btnGetAllPages.TabIndex = 5;
            this.btnGetAllPages.Text = "Get All Pages";
            this.btnGetAllPages.UseVisualStyleBackColor = true;
            this.btnGetAllPages.Click += new System.EventHandler(this.btnGetAllPages_Click);
            // 
            // btnDeletePage
            // 
            this.btnDeletePage.Location = new System.Drawing.Point(444, 61);
            this.btnDeletePage.Name = "btnDeletePage";
            this.btnDeletePage.Size = new System.Drawing.Size(100, 23);
            this.btnDeletePage.TabIndex = 6;
            this.btnDeletePage.Text = "Delete Page";
            this.btnDeletePage.UseVisualStyleBackColor = true;
            this.btnDeletePage.Click += new System.EventHandler(this.btnDeletePage_Click);
            // 
            // PagesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1474, 448);
            this.Controls.Add(this.btnDeletePage);
            this.Controls.Add(this.btnGetAllPages);
            this.Controls.Add(this.btnUpdatePages);
            this.Controls.Add(this.btnGetPages);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtFileID);
            this.Controls.Add(this.dgvPages);
            this.Name = "PagesForm";
            this.Text = "Delete Page";
            this.Load += new System.EventHandler(this.PagesForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPages)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.DataGridView dgvPages;
        public System.Windows.Forms.DataGridViewTextBoxColumn PageID;
        public System.Windows.Forms.DataGridViewTextBoxColumn PageName;
        public System.Windows.Forms.DataGridViewTextBoxColumn ProjectID;
        public System.Windows.Forms.DataGridViewTextBoxColumn FileID;
        public System.Windows.Forms.DataGridViewTextBoxColumn PageIndex;
        public System.Windows.Forms.DataGridViewTextBoxColumn CreatedDate;
        public System.Windows.Forms.DataGridViewTextBoxColumn LastModifiedDate;
        public System.Windows.Forms.DataGridViewTextBoxColumn Version;
        public System.Windows.Forms.DataGridViewTextBoxColumn Class;
        public System.Windows.Forms.DataGridViewTextBoxColumn Orientation;
        public System.Windows.Forms.DataGridViewTextBoxColumn Scale;
        public System.Windows.Forms.TextBox txtFileID;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Button btnGetPages;
        public System.Windows.Forms.Button btnUpdatePages;
        public System.Windows.Forms.Button btnGetAllPages;
        public System.Windows.Forms.Button btnDeletePage;
    }
}