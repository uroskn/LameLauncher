using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace LameLauncher
{
    public partial class UpdateWindow : Form
    {
        public delegate void UpdateMaxTicks(int tick);
        public delegate void UpdateTick();
        public delegate void CloseWindow();
        public delegate void UpdateStatus(string status);
        public delegate void UpdateProgressText(string text);
        public delegate void UpdateAddMaxTicks(int ticks);

        public UpdateMaxTicks maxticks;
        public UpdateTick newtick;
        public UpdateStatus setstatus;
        public CloseWindow weredone;
        public UpdateProgressText newprogress;
        public UpdateAddMaxTicks addticks;

        public Updater upd;
        public bool runthread;

        public bool update_success;
        public string update_error;

        private Thread thrd;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D && e.Modifiers == Keys.Control)
            {
                ConsoleLogger.debugscreen.Show();
            }
            base.OnKeyDown(e);
        }

        public void setspeed(string speed)
        {
            label3.Text = speed;
        }

        public void ProgressTick()
        {
            this.progressBar1.Value = this.progressBar1.Value + 1;
            this.progressBar1.Refresh();
        }

        public void AddMaxTicks(int ticks)
        {
            this.progressBar1.Maximum = this.progressBar1.Maximum + ticks + 1;
            this.progressBar1.Refresh();
        }

        public void SetMaxTicks(int tick)
        {
            this.progressBar1.Value = 0;
            this.progressBar1.Maximum = tick;
            this.progressBar1.Refresh();
        }

        public void NewStatus(string status)
        {
            this.label2.Text = status;
        }

        public void Done()
        {
            this.Close();
        }

        public UpdateWindow()
        {
            InitializeComponent();
            maxticks = new UpdateMaxTicks(this.SetMaxTicks);
            newtick = new UpdateTick(this.ProgressTick);
            setstatus = new UpdateStatus(this.NewStatus);
            weredone = new CloseWindow(this.Done);
            newprogress = new UpdateProgressText(this.setspeed);
            addticks = new UpdateAddMaxTicks(this.AddMaxTicks);
            runthread = false;
            this.KeyPreview = true;
        }

        public void RunUpdate()
        {
            this.label2.Text = "";
            SetMaxTicks(0);
            update_success = false;
            update_error = "";
            thrd = new Thread(upd.UpdateMinecraft);
            runthread = true;
            this.ShowDialog();
        }

        private void UpdateWindow_Shown(object sender, EventArgs e)
        {
            if (runthread) thrd.Start();
        }
    }
}
