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
            this.dataBaseSaveDialog = new System.Windows.Forms.SaveFileDialog();
            this.tab1.SuspendLayout();
            this.grpInitialize.SuspendLayout();
            this.grpProjectInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.grpInitialize);
            this.tab1.Groups.Add(this.grpProjectInfo);
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
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpInitialize;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnAddDatabase;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteDatabase;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnDeleteProjectInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnModifyProjectInfo;
        private System.Windows.Forms.SaveFileDialog dataBaseSaveDialog;
    }

    partial class ThisRibbonCollection
    {
        internal Ribbon Ribbon
        {
            get { return this.GetRibbon<Ribbon>(); }
        }
    }
}
