using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace LameLauncher
{
    static class Program
    {
        public static bool isloonix;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128)) isloonix = true;
            else isloonix = false;
            ConsoleLogger.debugscreen = null;

            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                delegate(object appsend, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
                {
                    var caroot = new X509Certificate(LameLauncher.Properties.Resources.caroot);
                    if (caroot.Issuer == certificate.Issuer) return true;
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        return true;
                    if ((Form1.config != null) && (Form1.config.GetValue("ignoressl", "0") == "1")) return true;
                    ConsoleLogger.LogData("Certificate error: " + sslPolicyErrors.ToString(), "Program");
                    ConsoleLogger.LogData("Fault certificate: " + certificate.ToString(), "Program");
                    return false;
                };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
