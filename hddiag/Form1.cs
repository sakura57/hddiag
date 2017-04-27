using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using System.Diagnostics;

namespace hddiag
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //helper function to initialize a Process object
        //with hidden window attribute
        private Process beginCommand()
        {
            Process proc1 = new Process();
            proc1.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc1.StartInfo.FileName = "cmd.exe";
            return proc1;
        }

        //a command just started, disable all buttons
        //and show the "in progress" text
        private void DisableAllButtons()
        {
            button6.BackColor = Color.Yellow;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            label4.Visible = true;
            label4.Enabled = true;
        }

        //this is called after a command has finished
        //but before the "finished" messagebox is shown
        private void GreyProgressText()
        {
            label4.Enabled = false;
        }

        //a command just finished, enable all buttons again
        //and hide the "in progress" text
        private void EnableAllButtons()
        {
            button6.BackColor = Color.Lime;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            label4.Visible = false;
        }

        //bump up rpi_wpa2 and eduroam priority
        private void button1_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            Process proc1 = beginCommand();
            Process proc2 = beginCommand();
            proc1.StartInfo.Arguments = "/C netsh wlan set profileorder name=\"rpi_wpa2\" interface=\"Wi-Fi\" priority=1";
            proc2.StartInfo.Arguments = "/C netsh wlan set profileorder name=\"eduroam\" interface=\"Wi-Fi\" priority=2";
            proc1.Start();
            proc1.WaitForExit();
            proc2.Start();
            proc2.WaitForExit();

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.");

            EnableAllButtons();
        }

        //disable tunnelling
        private void button2_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            Process proc1 = beginCommand();
            Process proc2 = beginCommand();
            Process proc3 = beginCommand();
            proc1.StartInfo.Arguments = "/C netsh int ipv6 isatap set state disabled";
            proc2.StartInfo.Arguments = "/C netsh int ipv6 6to4 set state disabled";
            proc3.StartInfo.Arguments = "/C netsh int teredo set state disabled";
            proc1.Start();
            proc1.WaitForExit();
            proc2.Start();
            proc2.WaitForExit();
            proc3.Start();
            proc3.WaitForExit();

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.");

            EnableAllButtons();
        }

        //disable ipv6 privacy
        private void button3_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            Process proc1 = beginCommand();
            proc1.StartInfo.Arguments = "/C netsh interface ipv6 set privacy state=disabled";
            proc1.Start();
            proc1.WaitForExit();

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.");

            EnableAllButtons();
        }

        //flush DNS and renew DHCP lease
        private void button4_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            Process proc1 = beginCommand();
            Process proc2 = beginCommand();
            Process proc3 = beginCommand();
            proc1.StartInfo.Arguments = "/C ipconfig /flushdns";
            proc2.StartInfo.Arguments = "/C ipconfig /release";
            proc3.StartInfo.Arguments = "/C ipconfig /renew";
            proc1.Start();
            proc1.WaitForExit();
            proc2.Start();
            proc2.WaitForExit();
            proc3.Start();
            proc3.WaitForExit();

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.");

            EnableAllButtons();
        }

        //set DNS servers to be acquired via DHCP
        private void button5_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            Process proc1 = beginCommand();
            proc1.StartInfo.Arguments = "/C netsh interface ip set dns \"Wi-Fi\" dhcp";
            proc1.Start();
            proc1.WaitForExit();

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.");

            EnableAllButtons();
        }

        //generate a text report containing diagnostic information
        private void button6_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            progressBar1.Visible = true;

            //These commands will be sent to cmd.exe, the pipe
            //into helpdesk.txt is added automatically
            string[] commands = new string[]
            {
                "date /t",
                "time /t",
                "ipconfig /all",
                "arp -a",
                "netstat -esr",
                "netstat -na",
                "nbtstat -n",
                "net use",
                "net user",
                "net view 127.0.0.1",
                "net session",
                "nbtstat -S",
                "netsh interface ipv6 show route",
                "netsh interface ipv6 show neighbors",
                "nslookup www.rpi.edu",
                "nslookup www.cnn.com",
                "netsh wlan show profiles",
                "date /t",
                "time /t"
            };

            progressBar1.Maximum = commands.Length;
            progressBar1.Step = 1;

            Process proc = beginCommand();
            proc.StartInfo.Arguments = "/C echo HD Diagnostic Utility 0.1a > \"%userprofile%\\Desktop\\helpdesk.txt\" 2>&1";
            proc.Start();
            proc.WaitForExit();
            
            foreach(string cmd in commands)
            {
                string echocmd = "echo -+ RUNNING \"" + cmd + "\"";
                proc = beginCommand();
                proc.StartInfo.Arguments = "/C " + echocmd + " >> \"%userprofile%\\Desktop\\helpdesk.txt\" 2>&1";
                proc.Start();
                proc.WaitForExit();

                proc = beginCommand();
                proc.StartInfo.Arguments = "/C " + cmd + " >> \"%userprofile%\\Desktop\\helpdesk.txt\" 2>&1";
                proc.Start();
                proc.WaitForExit();

                progressBar1.PerformStep();
            }

            GreyProgressText();

            //verify that the file was actually created
            string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (File.Exists(Path.Combine(desktopFolder, "helpdesk.txt")))
            {
                MessageBox.Show(this, "Wrote text report successfully.");
            }
            else
            {
                MessageBox.Show(this, "Error: Failed to generate text report.\nEnsure you have write permissions for the desktop.");
            }

            progressBar1.Value = 0;
            progressBar1.Visible = false;

            EnableAllButtons();
        }
    }
}
