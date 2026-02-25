using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using VisAssistDatabaseBackEnd.ShapeUtilities.Wire;

namespace VisAssistDatabaseBackEnd.Forms
{
    public partial class ReColororRenumberWiresForm : Form
    {
        public ReColororRenumberWiresForm()
        {
            InitializeComponent();
        }

        private void ReColororRenumberWiresForm_Load(object sender, EventArgs e)
        {

        }

        public string m_sAction = "";
        public void Display(string sAction)
        {
            m_sAction = sAction;
            switch (m_sAction)
            {
                case "Recolor":
                    {
                        btnGo.Text = "Recolor";
                        txtPrefix.Visible = false;
                        txtNumber.Visible = false;
                        lblRenumber.Visible = false;
                        cboColor.Visible = true;
                        lblLabel.Text = "Starting Color";
                        break;
                    }
                case "Renumber":
                    {
                        btnGo.Text = "Renumber";
                        cboColor.Visible = false;
                        txtPrefix.Visible = true;
                        txtNumber.Visible = true;
                        lblRenumber.Visible = true;
                        lblLabel.Text = "Starting Prefix";
                        break;
                    }
            }
        }



        private void btnGo_Click(object sender, EventArgs e)
        {


            switch(m_sAction)
            {
                case "Recolor":
                    {
                        RecolorWire.GatherReColorInfo(this);
                        break;
                    }
                case "Renumber":
                    {
                        RenumberWire.GatherRenumberInfo(this);
                        break;
                    }
            }

            this.Close();
            
        }
    }
}
