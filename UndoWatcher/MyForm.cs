using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using acApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices; 
using Autodesk.AutoCAD.EditorInput;  

namespace UndoWatcher
{
    public partial class MyForm : Form
    {
        public MyForm()
        {
            InitializeComponent();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            Document doc = acApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                MessageBox.Show("There is no active document.", this.Text);
                return;
            }

            // only run it if no command is running
            string s = (string)acApp.GetSystemVariable("CMDNAMES");
            if (s != "")
            {
                MessageBox.Show("Cannot run it now. Wait until command \"" + s + "\" finishes.", this.Text);
                return;
            }

            //acApp.SetSystemVariable("CMDECHO", 0); // _UNDOWATCHER_RESETUNDO will set it back
            doc.SendStringToExecute("_CMDECHO 0 ", false, false, false);
            doc.SendStringToExecute("_.UNDO _Control _None ", false, false, false);
            doc.SendStringToExecute("_.UNDO _All ", false, false, false);
            doc.SendStringToExecute("_UNDOWATCHER_RESETUNDO ", false, false, false);
            doc.SendStringToExecute("_CMDECHO 1 ", false, false, false);
        }
    }
}
