namespace VisAssistDatabaseBackEnd
{
    partial class Ribbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public Ribbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tab1 = this.Factory.CreateRibbonTab();
            this.grpInitialize = this.Factory.CreateRibbonGroup();
            this.btnAddDatabase = this.Factory.CreateRibbonButton();
            this.btnDeleteDatabase = this.Factory.CreateRibbonButton();
            this.grpProjectInfo = this.Factory.CreateRibbonGroup();
            this.btnAddProjectWithVisio = this.Factory.CreateRibbonButton();
            this.btnGetProjectInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteProjectInfo = this.Factory.CreateRibbonButton();
            this.grpFile = this.Factory.CreateRibbonGroup();
            this.btnAddFile = this.Factory.CreateRibbonButton();
            this.btnGetFileData = this.Factory.CreateRibbonButton();
            this.btnDeleteFileInfo = this.Factory.CreateRibbonButton();
            this.btnAssociateFile = this.Factory.CreateRibbonButton();
            this.btnDisAssociateFile = this.Factory.CreateRibbonButton();
            this.btnDeleteFile = this.Factory.CreateRibbonButton();
            this.btnAssociateAnotherFile = this.Factory.CreateRibbonButton();
            this.btnAssociateOrphanedFile = this.Factory.CreateRibbonButton();
            this.grpPages = this.Factory.CreateRibbonGroup();
            this.btnAddPageInfo = this.Factory.CreateRibbonButton();
            this.btnDeletePageInfo = this.Factory.CreateRibbonButton();
            this.btnModifyPage = this.Factory.CreateRibbonButton();
            this.btnGetPageName = this.Factory.CreateRibbonButton();
            this.grpShapes = this.Factory.CreateRibbonGroup();
            this.btnAddShapeInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteShapeInfo = this.Factory.CreateRibbonButton();
            this.grpWireInfo = this.Factory.CreateRibbonGroup();
            this.btnAddWireInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteWireInfo = this.Factory.CreateRibbonButton();
            this.btnChangeFileName = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.grpInitialize.SuspendLayout();
            this.grpProjectInfo.SuspendLayout();
            this.grpFile.SuspendLayout();
            this.grpPages.SuspendLayout();
            this.grpShapes.SuspendLayout();
            this.grpWireInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.grpInitialize);
            this.tab1.Groups.Add(this.grpProjectInfo);
            this.tab1.Groups.Add(this.grpFile);
            this.tab1.Groups.Add(this.grpPages);
            this.tab1.Groups.Add(this.grpShapes);
            this.tab1.Groups.Add(this.grpWireInfo);
            this.tab1.Label = "TabAddIns";
            this.tab1.Name = "tab1";
            // 
            // grpInitialize
            // 
            this.grpInitialize.Items.Add(this.btnAddDatabase);
            this.grpInitialize.Items.Add(this.btnDeleteDatabase);
            this.grpInitialize.Label = "Initialize";
            this.grpInitialize.Name = "grpInitialize";
            // 
            // btnAddDatabase
            // 
            this.btnAddDatabase.Label = "Add Database";
            this.btnAddDatabase.Name = "btnAddDatabase";
            this.btnAddDatabase.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddDatabase_Click);
            // 
            // btnDeleteDatabase
            // 
            this.btnDeleteDatabase.Label = "Delete Database";
            this.btnDeleteDatabase.Name = "btnDeleteDatabase";
            this.btnDeleteDatabase.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeleteDatabase_Click);
            // 
            // grpProjectInfo
            // 
            this.grpProjectInfo.Items.Add(this.btnAddProjectWithVisio);
            this.grpProjectInfo.Items.Add(this.btnGetProjectInfo);
            this.grpProjectInfo.Items.Add(this.btnDeleteProjectInfo);
            this.grpProjectInfo.Label = "Project Info";
            this.grpProjectInfo.Name = "grpProjectInfo";
            // 
            // btnAddProjectWithVisio
            // 
            this.btnAddProjectWithVisio.Label = "Add New Project";
            this.btnAddProjectWithVisio.Name = "btnAddProjectWithVisio";
            this.btnAddProjectWithVisio.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddProjectWithVisio_Click);
            // 
            // btnGetProjectInfo
            // 
            this.btnGetProjectInfo.Label = "Get Project Info";
            this.btnGetProjectInfo.Name = "btnGetProjectInfo";
            this.btnGetProjectInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnGetProjectInfo_Click);
            // 
            // btnDeleteProjectInfo
            // 
            this.btnDeleteProjectInfo.Label = "Delete Project";
            this.btnDeleteProjectInfo.Name = "btnDeleteProjectInfo";
            this.btnDeleteProjectInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeleteProjectInfo_Click);
            // 
            // grpFile
            // 
            this.grpFile.Items.Add(this.btnAddFile);
            this.grpFile.Items.Add(this.btnGetFileData);
            this.grpFile.Items.Add(this.btnDeleteFileInfo);
            this.grpFile.Items.Add(this.btnAssociateFile);
            this.grpFile.Items.Add(this.btnDisAssociateFile);
            this.grpFile.Items.Add(this.btnDeleteFile);
            this.grpFile.Items.Add(this.btnAssociateAnotherFile);
            this.grpFile.Items.Add(this.btnAssociateOrphanedFile);
            this.grpFile.Items.Add(this.btnChangeFileName);
            this.grpFile.Label = "File Info";
            this.grpFile.Name = "grpFile";
            // 
            // btnAddFile
            // 
            this.btnAddFile.Label = "Add Another File";
            this.btnAddFile.Name = "btnAddFile";
            this.btnAddFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddFile_Click);
            // 
            // btnGetFileData
            // 
            this.btnGetFileData.Label = "Get File Info";
            this.btnGetFileData.Name = "btnGetFileData";
            this.btnGetFileData.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnGetFileData_Click);
            // 
            // btnDeleteFileInfo
            // 
            this.btnDeleteFileInfo.Label = "Delete File Info";
            this.btnDeleteFileInfo.Name = "btnDeleteFileInfo";
            this.btnDeleteFileInfo.Visible = false;
            this.btnDeleteFileInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeleteFileInfo_Click);
            // 
            // btnAssociateFile
            // 
            this.btnAssociateFile.Label = "Associate Another File";
            this.btnAssociateFile.Name = "btnAssociateFile";
            this.btnAssociateFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAssociateFile_Click);
            // 
            // btnDisAssociateFile
            // 
            this.btnDisAssociateFile.Label = "Disassociate File";
            this.btnDisAssociateFile.Name = "btnDisAssociateFile";
            this.btnDisAssociateFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDisAssociateFile_Click);
            // 
            // btnDeleteFile
            // 
            this.btnDeleteFile.Label = "Delete File";
            this.btnDeleteFile.Name = "btnDeleteFile";
            this.btnDeleteFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeleteFile_Click);
            // 
            // btnAssociateAnotherFile
            // 
            this.btnAssociateAnotherFile.Label = "";
            this.btnAssociateAnotherFile.Name = "btnAssociateAnotherFile";
            // 
            // btnAssociateOrphanedFile
            // 
            this.btnAssociateOrphanedFile.Label = "Associate Orphaned File";
            this.btnAssociateOrphanedFile.Name = "btnAssociateOrphanedFile";
            this.btnAssociateOrphanedFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAssociateOrphanedFile_Click);
            // 
            // grpPages
            // 
            this.grpPages.Items.Add(this.btnAddPageInfo);
            this.grpPages.Items.Add(this.btnDeletePageInfo);
            this.grpPages.Items.Add(this.btnModifyPage);
            this.grpPages.Items.Add(this.btnGetPageName);
            this.grpPages.Label = "Page Info";
            this.grpPages.Name = "grpPages";
            // 
            // btnAddPageInfo
            // 
            this.btnAddPageInfo.Label = "Add 50 Pages";
            this.btnAddPageInfo.Name = "btnAddPageInfo";
            this.btnAddPageInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddPageInfo_Click);
            // 
            // btnDeletePageInfo
            // 
            this.btnDeletePageInfo.Label = "Delete";
            this.btnDeletePageInfo.Name = "btnDeletePageInfo";
            this.btnDeletePageInfo.Visible = false;
            this.btnDeletePageInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeletePageInfo_Click);
            // 
            // btnModifyPage
            // 
            this.btnModifyPage.Label = "Modify";
            this.btnModifyPage.Name = "btnModifyPage";
            this.btnModifyPage.Visible = false;
            this.btnModifyPage.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnModifyPage_Click);
            // 
            // btnGetPageName
            // 
            this.btnGetPageName.Label = "Get";
            this.btnGetPageName.Name = "btnGetPageName";
            this.btnGetPageName.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnGetPageName_Click);
            // 
            // grpShapes
            // 
            this.grpShapes.Items.Add(this.btnAddShapeInfo);
            this.grpShapes.Items.Add(this.btnDeleteShapeInfo);
            this.grpShapes.Label = "Shape Info";
            this.grpShapes.Name = "grpShapes";
            // 
            // btnAddShapeInfo
            // 
            this.btnAddShapeInfo.Label = "Add Shape Info";
            this.btnAddShapeInfo.Name = "btnAddShapeInfo";
            this.btnAddShapeInfo.Visible = false;
            // 
            // btnDeleteShapeInfo
            // 
            this.btnDeleteShapeInfo.Label = "Delete Shape Info";
            this.btnDeleteShapeInfo.Name = "btnDeleteShapeInfo";
            this.btnDeleteShapeInfo.Visible = false;
            // 
            // grpWireInfo
            // 
            this.grpWireInfo.Items.Add(this.btnAddWireInfo);
            this.grpWireInfo.Items.Add(this.btnDeleteWireInfo);
            this.grpWireInfo.Label = "Wire Info";
            this.grpWireInfo.Name = "grpWireInfo";
            // 
            // btnAddWireInfo
            // 
            this.btnAddWireInfo.Label = "Add Wire Info";
            this.btnAddWireInfo.Name = "btnAddWireInfo";
            this.btnAddWireInfo.Visible = false;
            this.btnAddWireInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddWireInfo_Click);
            // 
            // btnDeleteWireInfo
            // 
            this.btnDeleteWireInfo.Label = "Delete Wire Info";
            this.btnDeleteWireInfo.Name = "btnDeleteWireInfo";
            this.btnDeleteWireInfo.Visible = false;
            // 
            // btnChangeFileName
            // 
            this.btnChangeFileName.Label = "Change File Name";
            this.btnChangeFileName.Name = "btnChangeFileName";
            this.btnChangeFileName.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnChangeFileName_Click);
            // 
            // Ribbon
            // 
            this.Name = "Ribbon";
            this.RibbonType = "Microsoft.Visio.Drawing";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.Ribbon_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.grpInitialize.ResumeLayout(false);
            this.grpInitialize.PerformLayout();
            this.grpProjectInfo.ResumeLayout(false);
            this.grpProjectInfo.PerformLayout();
            this.grpFile.ResumeLayout(false);
            this.grpFile.PerformLayout();
            this.grpPages.ResumeLayout(false);
            this.grpPages.PerformLayout();
            this.grpShapes.ResumeLayout(false);
            this.grpShapes.PerformLayout();
            this.grpWireInfo.ResumeLayout(false);
            this.grpWireInfo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpInitialize;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddDatabase;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpPages;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpShapes;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteDatabase;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteFileInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddPageInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeletePageInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddShapeInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteShapeInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnModifyPage;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpWireInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddWireInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteWireInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnGetPageName;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnGetProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnGetFileData;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddProjectWithVisio;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAssociateAnotherFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDisAssociateFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAssociateFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAssociateOrphanedFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnChangeFileName;
    }

    partial class ThisRibbonCollection
    {
        internal Ribbon Ribbon
        {
            get { return this.GetRibbon<Ribbon>(); }
        }
    }
}
