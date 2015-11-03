using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LameLauncher
{
    public partial class DebugScr : Form
    {
        public delegate void Update(string status);

        public Update logdata;
        public bool closeonexit;

        public void updatetxt(string txt)
        {
            textBox1.AppendText(txt + "\r\n");
        }

        public DebugScr()
        {
            InitializeComponent();
            closeonexit = false;
            logdata = new Update(this.updatetxt);
        }

        private void DebugScr_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void DebugScr_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closeonexit)
            {
                Application.Exit();
                Environment.Exit(0);
            }
            e.Cancel = true;
            this.Hide();
        }
    }
}
