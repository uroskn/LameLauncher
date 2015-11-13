using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Web;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Globalization;
using System.Text.RegularExpressions;

// Never bothered to rename it, do you mind?

namespace LameLauncher
{
    public partial class Form1 : Form
    {
        public const string version = "151113";
        public const string launchargs = "-Xms512m -Xmx1024m -cp \"$CLASSPATH\" -Djava.library.path=\"$LLDIR\" net.minecraft.client.main.Main --username \"$USER\" --gameDir \"$LLDIR\" --assetsDir \"$LLDIR/assets\" --assetIndex LCWTF --version LCWTF $SERVERSTR --userProperties '{}' --accessToken 123";

        private int ticks;
        private int dec;
        private bool loonix;
        private bool hasjava;
        private bool offline;
        private int gameticks;
        private System.Diagnostics.Process game;
        private string javaname;
        public static ConfigFile config;
        public static VarStorage variables;

        private static string GetHash(string s)
        {
            MD5 sec = new MD5CryptoServiceProvider();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bt = enc.GetBytes(s);
            return Updater.bytesToHash(sec.ComputeHash(bt));
        }

        public static string getFingerprint()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            string interfaces = "";
            if ((nics != null) && (nics.Length >= 1))
            {
                foreach (NetworkInterface adapter in nics)
                {
                    PhysicalAddress address = adapter.GetPhysicalAddress();
                    byte[] bytes = address.GetAddressBytes();
                    for (int i = 0; i < bytes.Length; i++) interfaces = interfaces + bytes[i].ToString("X2");
                }
            }
            return GetHash(interfaces);
        }


        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D && e.Modifiers == Keys.Control)
            {
                ConsoleLogger.debugscreen.Show();
            }
            base.OnKeyDown(e);
        }

        public Form1()
        {
            InitializeComponent();
            variables = new VarStorage();
            variables.SetValue("version", version);
            ConsoleLogger.debugscreen = new DebugScr();
            ConsoleLogger.debugscreen.Show();
            bool hide = true;
            string chdir = "";
            if ((File.Exists("ll-debugme")) || (File.Exists("ll-debugme.txt"))) hide = false;
            if (Environment.GetCommandLineArgs().Length >= 2)
            {
                string[] cmdline = Environment.GetCommandLineArgs();
                int i = 1;
                while (i < cmdline.Length)
                {
                  	if (cmdline[i] == "--debug") hide = false;
                  	else if (cmdline[i] == "--dir") chdir = cmdline[(i+1)];
        	          i++;
                }
            }
            if (hide) ConsoleLogger.debugscreen.Hide();
            this.KeyPreview = true;
            ConsoleLogger.LogData("LL " + Form1.version + " @ " + Environment.GetCommandLineArgs()[0], "Main");
            ConsoleLogger.LogData("Environment fingerprint: " + getFingerprint());
            dec = -50;
            offline = false;
            hasjava = false;
            Updater upd = new Updater(".");
            loonix = Updater.IsLoonix();
            string os = "Windoze";
            javaname = "java";
            string fd = "\\";
            if (loonix)
            {
                fd = "/";
                os = "Linux";
            }
            string installpath = "";
            string tempdir = "";
            if (!loonix)
            {
                installpath = Environment.GetEnvironmentVariable("APPDATA") + fd + "LameCraft";
                tempdir = Environment.GetEnvironmentVariable("TEMP");
            }
            else
            {
                installpath = Environment.GetEnvironmentVariable("HOME") + fd + ".LameCraft";
                tempdir = "/tmp";
            }
            if (chdir != "") installpath = chdir;
            variables.SetValue("lldir", installpath);
            ConsoleLogger.LogData("OS: " + os, "Main");
            ConsoleLogger.LogData("MCIP: " + installpath, "Main");
            ConsoleLogger.LogData("TMP: " + tempdir, "Main");
            if (!Directory.Exists(installpath)) Directory.CreateDirectory(installpath);
            Directory.SetCurrentDirectory(installpath);
            config = new ConfigFile("launcher.cfg");
            label4.Text = "LameLauncher version: " + version + ", Minecraft: " + upd.GetCurrentVersion() + ", " + os;
            button3.Location = new Point(button3.Location.X + 400, button3.Location.Y);
            button2.Location = new Point(button2.Location.X + 400, button2.Location.Y);
            label5.Location = new Point(label5.Location.X + 400, label5.Location.Y);
            textBox3.Location = new Point(textBox3.Location.X + 400, textBox3.Location.Y);
            DetectJava();
            if (hasjava) label6.Hide();
            if (config.GetValue("offlinemode", "0") == "0")
            {
                try
                {
                    ConsoleLogger.LogData("Cekiram dosegljivost auth serverja...", "Main");
                    upd.GetHTTPFile("https://minecraft.knuples.net/auth.php?ping=1", tempdir + fd + "deleteme.mclauncher.auth.chk", 1500);
                    File.Delete(tempdir + fd + "deleteme.mclauncher.update.chk");
                }
                catch (Exception e)
                {
                    ConsoleLogger.LogData("*** AUTH SERVER NEDOSEGLJIV ***", "Main");
                    ConsoleLogger.LogData(e.Message, "Main");
                    ConsoleLogger.LogData("Delujem v offline nacinu!", "Main");
                    offline = true;
                }
                if ((config.GetValue("chkupdate", "1") == "1") && (!offline))
                {
                    bool isupdate = false;
                    try
                    {
                        // This is so important, we can't mess this up!
                        ConsoleLogger.LogData("Cekiram za update launcherja...", "Main");
                        upd.GetHTTPFile("https://minecraft.knuples.net/updates/launcher/" + version + "." + os, tempdir + fd + "deleteme.mclauncher.update.chk" + version, 300, 1);
                        File.Delete(tempdir + fd + "deleteme.mclauncher.update.chk");
                        UpdateWindow uw = new UpdateWindow();
                        uw.NewStatus("Launcher ima nov update, downloadam...");
                        ConsoleLogger.LogData("Launcher ima nov update!", "Main");
                        isupdate = true;
                        uw.runthread = false;
                        uw.Show();
                        Application.DoEvents();
                        string ofile = tempdir + fd + "deleteme." + version + "mclauncher.update.exe";
                        Application.DoEvents();
                        upd.GetHTTPFile("https://minecraft.knuples.net/updates/launcher/" + version + "." + os + ".bin", ofile, 1000);
                        uw.Hide();
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                        startInfo.FileName = ofile;
                        startInfo.Arguments = Environment.GetCommandLineArgs()[0];
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.StartInfo = startInfo;
                        proc.Start();
                        Application.Exit();
                        Environment.Exit(0);
                    }
                    catch { 
                        /** WTF JE BLO TO, SHIT, SHIT, SHIT, DO TEGA NEBI SMELO PRIT!
                            Konec je z nami, fertik smo, adijo, zbrisi system32 in zvali krivdo
                            na Windowse. Po možnosti poišči še vse .doc in .jpg fajle in jih zbriši,
                            da se bo mel user za kaj sekirat, razen tega kak ne more minecrafta igrat **/
                        if (isupdate)
                        {
                            MessageBox.Show("Poskusal downloadat update, ampak mi nekako ni uspelo.\nProsim, prenesi update s strani!");
                            Environment.Exit(1);
                        }
                        /* Ali pa crashni aplikacijo. Jup, to bi tudi šlo. */
                        Application.Exit();
                    }
                    while (testForMinecraftUpdate(upd)) 
                      ConsoleLogger.LogData("Retesting for update...");
                }
            }
            else
            {
                ConsoleLogger.LogData("Hoces offline nacin? Tudi prav!", "Main");
                offline = true;
            }
            if (offline)
            {
                checkBox2.Visible = false;
                linkLabel1.Visible = false;
                label7.Visible = true;
                checkBox1.Location = new Point(171, checkBox1.Location.Y);
            }
            if (config.GetValue("remember", "1") == "1")
            {
                textBox1.Text = config.GetValue("user", "");
                textBox2.Text = config.GetValue("pass", "");
                checkBox1.Checked = true;
            }
            if (config.GetValue("autologin", "1") == "1") checkBox2.Checked = true;
            ConsoleLogger.LogData("Init koncan, fire away!", "Main");
        }

        private bool testForMinecraftUpdate(Updater upd)
        {
            try
            {
                upd.updwindow = null;
                upd.DownloadFile("update", null, false);
                // There apprently is a new update!
                // Check if minecraft.jar was tampered with
                if (config.GetValue("minecrafthash", "") != "")
                {
                    if (upd.GetMD5HashFromFile("meinkraft.jar") != config.GetValue("minecrafthash", ""))
                    {
                        MessageBox.Show("Poskusal zagnati update, ampak zgleda, da je nekdo spreminjal meinkraft.jar!\n\n" +
                                        "Prekinjam update, updejtaj na roke, kakor ves in znas!");
                        throw new Exception("");
                    }
                }
                UpdateWindow updater = new UpdateWindow();
                updater.upd = upd;
                upd.updwindow = updater;
                updater.RunUpdate();
                return true;
            }
            catch { };
            return false;
        }

        private void DetectJava()
        {
            string command = "java";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = command;
            startInfo.UseShellExecute = true;
            process.StartInfo = startInfo;
            try
            {
                process.Start();
                process.WaitForExit();
                hasjava = true;
                ConsoleLogger.LogData("Java in PATH, good.", 1);
            }
            catch
            {
                // No java? That's bad. Windows mogoc? PATH fucked up?
                // Ker to je Windows, kjer je cudez da kaj dejansko dela po planu!
                if (!loonix)
                {
                    ConsoleLogger.LogData("Jave ne najdem v PATH. Tole je ziher Windows.");
                    string progfiles = Environment.GetEnvironmentVariable("ProgramFiles");
                    string jpath = null;
                    if (Directory.Exists(progfiles + "\\Java")) jpath = progfiles + "\\Java";
                    else if (Directory.Exists(progfiles + " (x86)\\Java")) jpath = progfiles + " (x86)\\Java";
                    if (jpath != null)
                    {
                        foreach (string path in Directory.GetDirectories(jpath))
                        {
                            if (File.Exists(path + "\\bin\\java.exe"))
                            {
                                javaname = path + "\\bin\\java.exe";
                                hasjava = true;
                                break;
                            }
                        }
                    }
                    if (hasjava) ConsoleLogger.LogData("Najdu javo v: " + javaname);
                    else ConsoleLogger.LogData("Ne najdem jave, fuck!");
                }
            };
        }
            
        // Shamelesly stolen from MSDN
        private bool invalid_email;
        public bool IsValidEmail(string strIn)
        {
            invalid_email = false;
            if (String.IsNullOrEmpty(strIn)) return false;
            try { strIn = Regex.Replace(strIn, @"(@)(.+)$", this.DomainMapper, RegexOptions.None); }
            catch { return false; }
            if (invalid_email) return false;
            try {
                return Regex.IsMatch(strIn,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase);
            }
            catch { return false; }
        }

        private string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();
            string domainName = match.Groups[2].Value;
            try { domainName = idn.GetAscii(domainName); }
            catch (ArgumentException) { invalid_email = true; }
            return match.Groups[1].Value + domainName;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if ((textBox1.Text != "") && (hasjava))
            {
                if (!offline)
                {
                    if (textBox2.Text != "") button1.Enabled = true;
                    else button1.Enabled = false;
                }
                else button1.Enabled = true;
            }
            else button1.Enabled = false;
            if (!IsValidEmail(textBox3.Text)) button2.Enabled = false;
            else button2.Enabled = button1.Enabled;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ticks++;
            linkLabel1.Location = new Point(linkLabel1.Location.X - dec, linkLabel1.Location.Y);
            button1.Location = new Point(button1.Location.X - dec, button1.Location.Y);
            checkBox1.Location = new Point(checkBox1.Location.X - dec, checkBox1.Location.Y);
            checkBox2.Location = new Point(checkBox2.Location.X - dec, checkBox2.Location.Y);
            button3.Location = new Point(button3.Location.X - dec, button3.Location.Y);
            button2.Location = new Point(button2.Location.X - dec, button2.Location.Y);
            label5.Location = new Point(label5.Location.X - dec, label5.Location.Y);
            textBox3.Location = new Point(textBox3.Location.X - dec, textBox3.Location.Y);
            if (ticks > 7)
            {
                timer1.Enabled = false;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ticks = 0;
            timer1.Enabled = true;
            dec = dec * -1;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ticks = 0;
            timer1.Enabled = true;
            dec = dec * -1;
        }

        private void ShowError(string error)
        {
           label6.Text = error;
           label6.Show();
           timer2.Enabled = true;
           return;
        }

        private bool IsInputOK(string user, string password)
        {
    	    byte[] bpasswd = Encoding.UTF8.GetBytes (password);
    	    if (bpasswd.Length > 60) 
            {
                ShowError("Predolgo geslo (ne sme biti daljse od 60 znakov");
                return false;
    	    }
    	    if (!System.Text.RegularExpressions.Regex.IsMatch(user, @"^[a-zA-Z0-9_]+$")) 
            {
                ShowError ("Uporabnisko ime vsebuje neveljavne znake!");
                return false;
    	    }
    	    return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!offline)
            {
                if (!IsInputOK(textBox1.Text, textBox2.Text)) return;
                WebClient wc = new WebClient();
                wc.Proxy = null;
                ConsoleLogger.LogData("Poskusam auth...", "AuthBTN");
                string result = wc.DownloadString("https://minecraft.knuples.net/auth.php?user=" + textBox1.Text + "&pass=" + textBox2.Text + "&hwid=" + getFingerprint());
                if (result != "OK")
                {
                    ConsoleLogger.LogData("Nope, ne bo slo", "AuthBTN");
                    label6.Text = "Napacen username in/ali password";
                    label6.Show();
                    textBox2.Text = "";
                    timer2.Enabled = true;
                    return;
                }
            }
            else ConsoleLogger.LogData("Smo offline nacinu, delam fake auth!", "AuthBTN");
            ConsoleLogger.LogData("OK, je kulj, zaganjam!", "AuthBTN");
            button1.Text = "Zaganjam...";
            button1.Refresh();
            this.Refresh();
            SaveConfig();
            gameticks = 0;
            System.Threading.Thread.Sleep(1000);
            game = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = javaname;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            string ds = ";";
            if (loonix) ds = ":";
            variables.SetValue("serverstr", "");
            if ((checkBox2.Checked) && (!offline)) variables.SetValue("serverstr", " --server server.minecraft.knuples.net");
            string classpath = "";
            string[] files = Directory.GetFiles(config.GetValue("classpath", @"$LLDIR", variables));
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Substring(files[i].Length - 4) == ".jar") classpath = classpath + files[i] + ds;
            }
            variables.SetValue("classpath", classpath);
            string lnchargs = config.GetValue("launchargs", launchargs);
            ConsoleLogger.LogData("CONFIGARGS: " + lnchargs, "AuthBTN");
            variables.SetValue("user", textBox1.Text);
            startInfo.Arguments = variables.ProcessString(lnchargs);
            ConsoleLogger.LogData("Zaganjam javo z komando: ", "AuthBTN");
            ConsoleLogger.LogData(startInfo.FileName + " " + startInfo.Arguments, "AuthBTN");
            game.StartInfo = startInfo;
            game.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(Form1.GameOutput);
            game.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(Form1.GameOutput);
            game.Start();
            game.BeginOutputReadLine();
            game.BeginErrorReadLine();
            this.Hide();
            timer4.Enabled = true;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Enabled = false;
            label6.Hide();
            label6.ForeColor = Color.Red;
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (gameticks == 900)
            {
                gameticks = 0;
                WebClient wc = new WebClient();
                wc.Proxy = null;
                ConsoleLogger.LogData("900 tickov preteklo, reauth!");
                try
                {
                    wc.DownloadString("https://minecraft.knuples.net/auth.php?user=" + textBox1.Text + "&pass=" + textBox2.Text + "&hwid=" + getFingerprint());
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogData("Ne gre reauth, Error? He?", "OnTick");
                    ConsoleLogger.LogData("** " + ex.Message, "OnTick");
                }
            }
            if (game.HasExited)
            {
                ConsoleLogger.LogData("Java je sla k rakom zvizgat, zakljucujem!");
                if (!ConsoleLogger.debugscreen.Visible)
                {
                    Application.Exit();
                    Environment.Exit(0);
                }
                else
                {
                    timer3.Enabled = false;
                    ConsoleLogger.debugscreen.closeonexit = true;
                }
            }
            gameticks++;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!IsInputOK(textBox1.Text, textBox2.Text)) return;
            WebClient wc = new WebClient();
            wc.Proxy = null;
            ConsoleLogger.LogData("Poskusam ustvarit novega userja...");
            string result = wc.DownloadString("https://minecraft.knuples.net/new.php?user=" + textBox1.Text + "&pass=" + textBox2.Text + "&mail=" + textBox3.Text + "&hwid=" + getFingerprint());
            ConsoleLogger.LogData("Dobil nazaj: " + result);
            if (result != "OK")
            {
                label6.Text = "Username ze obstaja";
                label6.Show();
                textBox1.Text = "";
                timer2.Enabled = true;
                ConsoleLogger.LogData("Ze obstaja!");
                return;
            }
            label6.Text = "Uspesno registriran";
            label6.ForeColor = Color.Lime;
            label6.Show();
            timer4.Enabled = true;
            button3_Click(sender, e);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked) config.SetValue("autologin", "1");
            else config.SetValue("autologin", "0");
        }

        private void SaveConfig()
        {
            ConsoleLogger.LogData("Shranjujem config...");
            if (checkBox1.Checked)
            {
                config.SetValue("user", textBox1.Text);
                config.SetValue("pass", textBox2.Text);
                config.SetValue("remember", "1");
            }
            else
            {
                config.SetValue("user", "");
                config.SetValue("pass", "");
                config.SetValue("remember", "0");
            }
            if (checkBox2.Checked) config.SetValue("autologin", "1");
            else config.SetValue("autologin", "0");
            config.FlushConfig();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) config.SetValue("remember", "1");
            else config.SetValue("remember", "0");
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == (char)13) && (button2.Enabled))
            {
                if (dec == -50) button1_Click(sender, null);
                else button2_Click(sender, null);
            }
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            timer3.Enabled = true;
            timer4.Enabled = false;
        }

        public static void GameOutput(object sender, System.Diagnostics.DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                ConsoleLogger.LogData(args.Data, "Game");
            }
        }
    }
}
