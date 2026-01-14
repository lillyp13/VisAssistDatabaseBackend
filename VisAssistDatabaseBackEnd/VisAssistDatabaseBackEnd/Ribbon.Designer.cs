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
            this.btnProjectInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteProjectInfo = this.Factory.CreateRibbonButton();
            this.btnModifyProjectInfo = this.Factory.CreateRibbonButton();
            this.grpFile = this.Factory.CreateRibbonGroup();
            this.btnAddFileInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteFileInfo = this.Factory.CreateRibbonButton();
            this.btnModifyFile = this.Factory.CreateRibbonButton();
            this.grpPages = this.Factory.CreateRibbonGroup();
            this.btnAddPageInfo = this.Factory.CreateRibbonButton();
            this.btnDeletePageInfo = this.Factory.CreateRibbonButton();
            this.btnModifyPage = this.Factory.CreateRibbonButton();
            this.grpShapes = this.Factory.CreateRibbonGroup();
            this.btnAddShapeInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteShapeInfo = this.Factory.CreateRibbonButton();
            this.grpWireInfo = this.Factory.CreateRibbonGroup();
            this.btnAddWireInfo = this.Factory.CreateRibbonButton();
            this.btnDeleteWireInfo = this.Factory.CreateRibbonButton();
            this.btnGetPageName = this.Factory.CreateRibbonButton();
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
            // 
            // grpProjectInfo
            // 
            this.grpProjectInfo.Items.Add(this.btnProjectInfo);
            this.grpProjectInfo.Items.Add(this.btnDeleteProjectInfo);
            this.grpProjectInfo.Items.Add(this.btnModifyProjectInfo);
            this.grpProjectInfo.Label = "Project Info";
            this.grpProjectInfo.Name = "grpProjectInfo";
            // 
            // btnProjectInfo
            // 
            this.btnProjectInfo.Label = "Add Project Info";
            this.btnProjectInfo.Name = "btnProjectInfo";
            this.btnProjectInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnProjectInfo_Click);
            // 
            // btnDeleteProjectInfo
            // 
            this.btnDeleteProjectInfo.Label = "Delete Project Info";
            this.btnDeleteProjectInfo.Name = "btnDeleteProjectInfo";
            this.btnDeleteProjectInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeleteProjectInfo_Click);
            // 
            // btnModifyProjectInfo
            // 
            this.btnModifyProjectInfo.Label = "Modify Project Info";
            this.btnModifyProjectInfo.Name = "btnModifyProjectInfo";
            this.btnModifyProjectInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnModifyProjectInfo_Click);
            // 
            // grpFile
            // 
            this.grpFile.Items.Add(this.btnAddFileInfo);
            this.grpFile.Items.Add(this.btnDeleteFileInfo);
            this.grpFile.Items.Add(this.btnModifyFile);
            this.grpFile.Label = "File Info";
            this.grpFile.Name = "grpFile";
            // 
            // btnAddFileInfo
            // 
            this.btnAddFileInfo.Label = "Add File Info";
            this.btnAddFileInfo.Name = "btnAddFileInfo";
            this.btnAddFileInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddFileInfo_Click);
            // 
            // btnDeleteFileInfo
            // 
            this.btnDeleteFileInfo.Label = "Delete File Info";
            this.btnDeleteFileInfo.Name = "btnDeleteFileInfo";
            this.btnDeleteFileInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeleteFileInfo_Click);
            // 
            // btnModifyFile
            // 
            this.btnModifyFile.Label = "Modify File";
            this.btnModifyFile.Name = "btnModifyFile";
            this.btnModifyFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnModifyFile_Click);
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
            this.btnAddPageInfo.Label = "Add";
            this.btnAddPageInfo.Name = "btnAddPageInfo";
            this.btnAddPageInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddPageInfo_Click);
            // 
            // btnDeletePageInfo
            // 
            this.btnDeletePageInfo.Label = "Delete";
            this.btnDeletePageInfo.Name = "btnDeletePageInfo";
            this.btnDeletePageInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnDeletePageInfo_Click);
            // 
            // btnModifyPage
            // 
            this.btnModifyPage.Label = "Modify";
            this.btnModifyPage.Name = "btnModifyPage";
            this.btnModifyPage.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnModifyPage_Click);
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
            // 
            // btnDeleteShapeInfo
            // 
            this.btnDeleteShapeInfo.Label = "Delete Shape Info";
            this.btnDeleteShapeInfo.Name = "btnDeleteShapeInfo";
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
            this.btnAddWireInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnAddWireInfo_Click);
            // 
            // btnDeleteWireInfo
            // 
            this.btnDeleteWireInfo.Label = "Delete Wire Info";
            this.btnDeleteWireInfo.Name = "btnDeleteWireInfo";
            // 
            // btnGetPageName
            // 
            this.btnGetPageName.Label = "Get";
            this.btnGetPageName.Name = "btnGetPageName";
            this.btnGetPageName.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnGetPageName_Click);
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
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpPages;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpShapes;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteDatabase;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddFileInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteFileInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddPageInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeletePageInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddShapeInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteShapeInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnModifyProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnModifyFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnModifyPage;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpWireInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddWireInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteWireInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnGetPageName;
    }

    partial class ThisRibbonCollection
    {
        internal Ribbon Ribbon
        {
            get { return this.GetRibbon<Ribbon>(); }
        }
    }
}
