﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;

namespace LameLauncher
{

    public class Updater
    {
        private Dictionary<string, string> mirrors;
        private Dictionary<string, string> overrides;
        private List<string> mirrorlist;
        public UpdateWindow updwindow;
        private Dictionary<string, string> vars;
        public bool progress;

        public static string bytesToHash(byte[] retVal)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public string GetSHAHashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] retVal = sha1.ComputeHash(file);
            file.Close();
            return bytesToHash(retVal);
        }

        public Updater(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Directory.SetCurrentDirectory(dir);
            vars = new Dictionary<string, string>();
            overrides = new Dictionary<string, string>();
            mirrorlist = new List<string>();
            this.ResetMirrors();
            this.updwindow = null;
        }

        public string GetCurrentMirror()
        {
            return this.mirrors[this.GetVar("MIRROR")];
        }

        public void DownloadCallback(object o, DownloadProgressChangedEventArgs e)
        {
            if (this.updwindow != null)
            {
                this.updwindow.Invoke(updwindow.newprogress, e.BytesReceived.ToString() + "B Downloaded");
            }
        }

        public void GetHTTPFile(string url, string file, int tiemout)
        {
            this.GetHTTPFile(url, file, tiemout, 3);
        }

        public void GetHTTPFile(string url, string file, int tiemout, int maxtries)
        {
            if (File.Exists(file)) File.Delete(file);
            Stream output = null;
            if (file != null) output = File.Create(file);
            bool close = true;
            int tries = 0;
            while (true) 
            {
                try
                {
                    tries++;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = tiemout;
                    request.Proxy = null;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    Stream resStream = response.GetResponseStream();

                    if ((this.updwindow != null) && (tries == 1)) this.updwindow.Invoke(updwindow.newprogress, "");

                    int count = 0;
                    int totalread = 0;
                    byte[] buffer = new byte[8192];
                    do
                    {
                        count = resStream.Read(buffer, 0, buffer.Length);
                        totalread = totalread + count;
                        if (response.ContentLength > 0)
                        {
                            if (this.updwindow != null) this.updwindow.Invoke(updwindow.newprogress, Math.Round(((double)totalread / (double)response.ContentLength) * 100D, 3).ToString() + " %");
                        }
                        if ((count != 0) && (output != null))
                        {
                            output.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    break;
                }
                catch (Exception e)
                {
                    close = false;
                    if (output != null) output.Close();
                    if ((this.updwindow != null) && (tries == 1)) this.updwindow.Invoke(updwindow.newprogress, "");
                    if (tries > maxtries) throw new Exception(e.Message, e);
                    else
                    {
                        File.Delete(file);
                        if (file != null) output = File.Create(file);
                        close = true;
                    }
                    ConsoleLogger.LogData("Download failo: " + e.Message, "GetHTTPFile");
                }
            }
            if (close)
            {
                if (output != null) output.Close();
                if (this.updwindow != null) this.updwindow.Invoke(updwindow.newprogress, "");
            }
        }

        public void DownloadFile(string file, string ofile, bool switchmirrors)
        {
            if (ofile == null) ofile = file;
            int timeout = 2147483647;
            if (this.GetVar("TIMEOUT") != "0") timeout = int.Parse(this.GetVar("TIMEOUT")) * 1000;
            while (true)
            {
                try
                {
                    string link = this.GetCurrentMirror() + this.GetCurrentVersion() + "/" + file;
                    if (overrides.ContainsKey(file))
                    {
                        link = overrides[file];
                        switchmirrors = false;
                    }
                    this.UpdateStatus("Downloadam: " + link);
                    this.GetHTTPFile(link, ofile, timeout);
                    break;
                }
                catch (Exception e)
                {
                    if (this.updwindow != null) this.updwindow.Invoke(updwindow.newprogress, "");
                    // Pick a new mirror, and retry!
                    if (switchmirrors)
                    {
                        this.UpdateStatus("Download failo, poskusam drugi mirror!");
                        System.Threading.Thread.Sleep(1000);
                        File.Delete(ofile);
                        this.mirrors[this.GetVar("MIRROR")] = "";
                        bool found = false;
                        for (int i = 0; i < this.mirrorlist.Count; i++)
                        {
                            if (this.mirrors[this.mirrorlist[i]] != "")
                            {
                                this.SetVar("MIRROR", this.mirrorlist[i]);
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            this.UpdateStatus("Novi mirror: " + this.GetCurrentMirror());
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }
                        this.UpdateStatus("Ostal brezmirrorjev!");
                        System.Threading.Thread.Sleep(1000);
                    }
                    throw new Exception(e.Message, e);
                }
            }
        }

        public void DownloadFile(string file, string ofile)
        {
            DownloadFile(file, ofile, true);
        }

        public int FindMirrorIndex(string name)
        {
            return this.mirrorlist.FindIndex(delegate(string s) { return s == name; });
        }

        public void UpdateMinecraft()
        {
            this.UpdateStatus("Updajtam iz verzije: " + this.GetCurrentVersion());
            this.DownloadFile("ver", ".temp");
            this.UpdateStatus("Zaganjam update file...");
            overrides.Clear();
            this.progress = true;
            ExecuteCode(".temp");
            File.Delete(".temp");
            this.updwindow = null;
        }

        public void ExecuteCode(string file, bool update_ticks, bool close_window)
        {
            string[] lines = File.ReadAllLines(file);
            if (progress)
            {
                if (update_ticks) this.updwindow.Invoke(updwindow.maxticks, lines.Length);
                else this.updwindow.Invoke(updwindow.addticks, lines.Length);
            }
            this.ExecuteCode(lines, close_window);
        }

        public void ExecuteCode(string file)
        {
            this.ExecuteCode(file, true, true);
        }

        public static List<string> TokenizeLine(string line)
        {
            List<string> result = new List<string>();
            string tmpres = "";
            bool isinsidequotes = false;
            bool searchingbegin = true;
            for (int i = 0; i < line.Length; i++)
            {
				if ((line[i] == '\\') && (isinsidequotes)) 
				{
				      i++;
					  tmpres = tmpres + line[i];
					  continue;
				}
                if ((line[i] == ' ') && (searchingbegin)) continue;
                if ((line[i] == ' ') && (!searchingbegin) && (!isinsidequotes))
                {
                    searchingbegin = true;
                    result.Add(tmpres);
                    tmpres = "";
                    continue;
                }
                searchingbegin = false;
                if (line[i] == '"') isinsidequotes = !isinsidequotes;
                else tmpres = tmpres + line[i];
            }
            result.Add(tmpres);
            if (result.Count == 0) result.Add("--");
            return result;
        }

        public static bool IsLoonix()
        {
            return Program.isloonix;
        }

        public void UpdateStatus(string status)
        {
            if (this.updwindow != null)
            {
                try
                {
                    this.updwindow.Invoke(updwindow.setstatus, status);
                }
                catch (Exception e) { }
            }
            ConsoleLogger.LogData(status, 2);
        }

        public void ExecuteCode(string[] code, bool close_window)
        {
            foreach (string line in code)
            {
                if (progress) this.updwindow.Invoke(updwindow.newtick);
                List<string> commands = TokenizeLine(line);
                if (commands[0] == "--") continue;
                bool canfail = false;
                if (commands[0] == "CANFAIL")
                {
                    canfail = true;
                    commands.RemoveAt(0);
                }
                if (commands[0] == "IFLOONIX")
                {
                    commands.RemoveAt(0);
                    if (!Updater.IsLoonix()) continue;
                }
                if (commands[0] == "IFWIN")
                {
                    commands.RemoveAt(0);
                    if (Updater.IsLoonix()) continue;
                }
                try
                {
                    if (commands[0] == "EXECUTE")
                    {
                        this.UpdateStatus("Zaganjam command file " + commands[1]);
                        this.ExecuteCode(commands[1], false, false);
                        if ((progress) && (this.updwindow.update_error != ""))
                            throw new Exception(this.updwindow.update_error);
                    }
                    if (commands[0] == "VERIFY")
                    {
                        this.UpdateStatus("Verifying file " + commands[1]);
                        if (this.GetSHAHashFromFile(commands[1]) != commands[2])
                            throw new Exception("Datoteka " + commands[1] + " zgleda da je bila nepričakovano spremenjena!");
                    }
                    if (commands[0] == "EXTRACT")
                    {
                        this.UpdateStatus("Extractam file '" + commands[2] + "' iz '" + commands[1] + "' v '" + commands[3] + "'");
                        ZipStorer zip = ZipStorer.Open(commands[1], FileAccess.Read);
                        List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();
                        bool success = false;
                        foreach (ZipStorer.ZipFileEntry entry in dir)
                        {
                            if (entry.FilenameInZip == commands[2])
                            {
                                zip.ExtractFile(entry, commands[3]);
                                success = true;
                                break;
                            }
                        }
                        if (!success) throw new Exception("Datoteka " + commands[1] + "/" + commands[2] + " ne obstaja!");
                        zip.Close();
                    }
                    if (commands[0] == "ADD")
                    {
                        bool shamatch = false;
                        if (File.Exists(commands[1]))
                        {
                            string shasum = this.GetSHAHashFromFile(commands[1]);
                            if (shasum == commands[2]) shamatch = true;
                        }
                        if (shamatch)
                        {
                            this.UpdateStatus("Ze imam fajl: " + commands[1] + ", skip...");
                            continue;
                        }
                        DownloadFile(commands[1], commands[1]);
                    }
                    if (commands[0] == "SET")
                    {
                        this.SetVar(commands[1], commands[2]);
                    }
                    if (commands[0] == "CLEARMIRRORS")
                    {
                        this.UpdateStatus("Spucal vse mirrorje");
                        this.mirrorlist.Clear();
                        this.mirrors.Clear();
                    }
                    if (commands[0] == "MIRROR")
                    {
                        this.mirrors.Add(commands[1], commands[2]);
                        this.mirrorlist.Add(commands[1]);
                        this.UpdateStatus("Dodajam nov mirror: " + commands[2] + ", ID: " + this.FindMirrorIndex(commands[1]).ToString());
                    }
                    if (commands[0] == "MKDIR")
                    {
                        this.UpdateStatus("Ustvarjam mapo " + commands[1]);
                        Directory.CreateDirectory(commands[1]);
                    }
                    if (commands[0] == "DEL")
                    {
                        this.UpdateStatus("Brisem " + commands[1]);
                        if (Directory.Exists(commands[1])) Directory.Delete(commands[1], true);
                        else File.Delete(commands[1]);
                    }
                    if (commands[0] == "COMMIT")
                    {
                        this.UpdateStatus("Koncujem update...");
                        File.WriteAllText("cversion", this.GetVar("VERSION"));
                        this.UpdateStatus("Done");
                    }
                    if (commands[0] == "OVERRIDE")
                    {
                        try
                        {
                            this.UpdateStatus("Dodajam override za " + commands[1] + "...");
                            overrides.Add(commands[1], commands[2]);
                        }
                        catch { };
                    }
    				if (commands[0] == "SETCFG")
    				{
    				    Form1.config.SetValue(commands[1], commands[2]);
    					Form1.config.FlushConfig();
    				}
                    if (commands[0] == "RENAME")
                    {
                        File.Move(commands[1], commands[2]);
                    }
                    if (commands[0] == "RESTART")
                    {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.FileName = commands[1];
                        startInfo.UseShellExecute = true;
                        process.StartInfo = startInfo;
                        process.Start();
                        Environment.Exit(0);
                    }
                }
                catch (Exception e)
                {
                    if (progress) this.UpdateStatus("ERROR: " + e.Message);
                    if (!canfail)
                    {
                        System.Threading.Thread.Sleep(1000);
                        if (progress)
                        {
                            this.progress = false;
                            this.updwindow.Invoke(updwindow.weredone);
                        }
                        this.updwindow.update_error = e.Message;
                        return;
                    }
                }
            }
            if ((progress) && (close_window))
            {
                System.Threading.Thread.Sleep(1000);
                this.updwindow.update_success = true;
                this.updwindow.Invoke(updwindow.weredone);
            }
        }

        public void SetVar(string key, string value)
        {
            string spam;
            if (this.vars.TryGetValue(key, out spam))
            {
                ConsoleLogger.LogData(key + "=" + value);
                this.vars[key] = value;
            }
            else
            {
                ConsoleLogger.LogData("[NEW] " + key + "=" + value);
                this.vars.Add(key, value);
            }
        }

        public string GetVar(string key)
        {
            string data;
            if (this.vars.TryGetValue(key, out data)) return data;
            else return "";
        }

        public void ResetMirrors()
        {
            ConsoleLogger.LogData("Restart mirrorjev, default!");
            mirrors = new Dictionary<string, string>();
            mirrors.Add("main", "https://minecraft.knuples.net/updates/");
            this.SetVar("TIMEOUT", "0");
            this.SetVar("MIRROR", "main");
        }

        public string GetCurrentVersion()
        {
            try
            {
                if (this.GetVar("VERSION") != "") return this.GetVar("VERSION");
                return File.ReadAllText("cversion");
            }
            catch
            {
                return "0";
            }
        }
    }
}
