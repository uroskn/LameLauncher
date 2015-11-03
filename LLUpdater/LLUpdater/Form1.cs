using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Reflection;

namespace LLUpdater
{
    public partial class Form1 : Form
    {
        private string appath;

        public Form1()
        {
            InitializeComponent();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                delegate(object appsend, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
                {
                    var caroot = new X509Certificate(LLUpdater.Properties.Resources.caroot);
                    return (caroot.Issuer == certificate.Issuer);
                };
            try
            {
                string[] cmdline = Environment.GetCommandLineArgs();
                string fname = System.IO.Path.GetTempPath() + @"\LL-update-" +(new Random()).Next(10000, 99999).ToString() + ".exe";
                if (cmdline.Length == 1)
                {
                    throw new Exception("Invalid command line arguments. Dafuq?");
                }
                else
                {
                    for (int i = 1; i < cmdline.Length; i++) appath = appath + cmdline[i] + " ";
                    appath = appath.Substring(0, appath.Length - 1);
                }
                WebClient webClient = new WebClient();
                webClient.DownloadFile("https://minecraft.knuples.net/LameLauncher.exe", fname);
                int tries = 0;
                System.IO.File.Delete(appath);
                while (File.Exists(appath)) 
                {
                    tries++;
                    if (tries > 50) throw new Exception("File is in use, tired of waiting (5s).");
                    System.Threading.Thread.Sleep(100);
                }
                System.IO.File.Move(fname, appath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Prislo je do napake pri nadgradnji LameLauncherja.\nProsim, da preneses novega s spletne strani.\n\nNapaka:\n" + ex.ToString());
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Process.Start(appath);
            Application.Exit();
        }
    }
}
