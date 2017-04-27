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
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace hddiag
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        private bool RunAutomaticDiagnoses()
        {
            bool issuesDetected = false;

            NetworkInterface wifiAdapter = Diagnosis.GetAdapter("Wi-Fi");

            if(wifiAdapter == null)
            {
                string message =
                    "Detected: Failed to get the wi-fi adapter.\n\n" +
                    "The interface may be offline or nonexistant.\n\n" +
                    "In the main dialog, choose \"Manage Network Adapters\"" +
                    "and ensure the adapter named \"Wi-Fi\" exists, and is enabled.\n\n" +
                    "If the problem persists, create a report to send\n" +
                    "to your support staff by choosing \"Generate Text Report.\"";

                MessageBox.Show(message, "HD Diagnostic Utility");

                //if we can't get the interface, there's not
                //much other diagnosis we can do
                return true;
            }

            if(Diagnosis.DiagnoseIllegalDNSServers(wifiAdapter))
            {
                string message =
                    "Detected: DNS servers are not set to be automatically acquired.\n\n" +
                    "In the main dialog, choose \"Reset DNS Servers\" under\n" +
                    "\"Troubleshooting Options.\"";

                MessageBox.Show(message, "HD Diagnostic Utility");

                issuesDetected = true;
            }

            return issuesDetected;
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
            button6.BackColor = Color.Orange;
            button8.BackColor = Color.Orange;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
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
            button8.BackColor = Color.Yellow;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            label4.Visible = false;
        }

        /*
         * Start cmd.exe with the given command as
         * an argument.
         */
        private void ExecuteCommand(string cmd)
        {
            Process proc = beginCommand();
            proc.StartInfo.Arguments = "/C " + cmd;
            proc.Start();
            proc.WaitForExit();
        }

        /*
         * The parameter to this function will be
         * the first line in helpdesk.txt. If the file
         * exists already, it will be overwritten;
         * if it does not exist, it will be created.
         */
        private void InitializeLog(string cmd)
        {
            Process proc = beginCommand();
            proc.StartInfo.Arguments = "/C " + cmd + " > \"%userprofile%\\Desktop\\helpdesk.txt\" 2>&1";
            proc.Start();
            proc.WaitForExit();
        }

        /*
         * Start cmd.exe with the given command as
         * an argument, and pipe the output into
         * helpdesk.txt.
         */
        private void ExecuteLoggedCommand(string cmd)
        {
            Process proc = beginCommand();
            proc.StartInfo.Arguments = "/C " + cmd + " >> \"%userprofile%\\Desktop\\helpdesk.txt\" 2>&1";
            proc.Start();
            proc.WaitForExit();
        }

        //bump up rpi_wpa2 and eduroam priority
        private void button1_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            ExecuteCommand("netsh wlan set profileorder name=\"rpi_wpa2\" interface=\"Wi-Fi\" priority=1");
            ExecuteCommand("netsh wlan set profileorder name=\"eduroam\" interface=\"Wi-Fi\" priority=2");

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.", "HD Diagnostic Utility");

            EnableAllButtons();
        }

        //disable tunnelling
        private void button2_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            
            ExecuteCommand("netsh int ipv6 isatap set state disabled");
            ExecuteCommand("netsh int ipv6 6to4 set state disabled");
            ExecuteCommand("netsh int teredo set state disabled");

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.", "HD Diagnostic Utility");

            EnableAllButtons();
        }

        //disable ipv6 privacy
        private void button3_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            
            ExecuteCommand("netsh interface ipv6 set privacy state=disabled");

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.", "HD Diagnostic Utility");

            EnableAllButtons();
        }

        //flush DNS and renew DHCP lease
        private void button4_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            ExecuteCommand("ipconfig /flushdns");
            ExecuteCommand("ipconfig /release");
            ExecuteCommand("ipconfig /renew");

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.", "HD Diagnostic Utility");

            EnableAllButtons();
        }

        //set DNS servers to be acquired via DHCP
        private void button5_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            
            ExecuteCommand("netsh interface ip set dns \"Wi-Fi\" dhcp");

            GreyProgressText();

            MessageBox.Show(this, "Command completed successfully.", "HD Diagnostic Utility");

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
                "net use",
                "net user",
                "net view 127.0.0.1",
                "net session",
                "nbtstat -n",
                "nbtstat -S",
                "netsh interface ipv6 show route",
                "netsh interface ipv6 show neighbors",
                "nslookup www.rpi.edu",
                "nslookup www.cnn.com",
                "netsh wlan show profiles",
                "date /t",
                "time /t"
            };

            //initialize the progress bar, maximum steps
            //will equal number of commands
            progressBar1.Maximum = commands.Length;
            progressBar1.Step = 1;

            //begin the log
            InitializeLog("echo HD Diagnostic Utility 0.2a");
            
            foreach(string cmd in commands)
            {
                //before we run the actual command, print what the command is
                ExecuteLoggedCommand("echo.");
                string echocmd = "echo -+ RUNNING \"" + cmd + "\"";
                ExecuteLoggedCommand(echocmd);

                //now run the command
                ExecuteLoggedCommand(cmd);

                //progress bar is stepped each command
                progressBar1.PerformStep();
            }

            GreyProgressText();

            //verify that the file was actually created
            string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if(File.Exists(Path.Combine(desktopFolder, "helpdesk.txt")))
            {
                MessageBox.Show(this, "Successfully generated text report.\nThe report has been placed on your desktop.", "HD Diagnostic Utility");
            }
            else
            {
                MessageBox.Show(this, "Error: Failed to generate text report.\nEnsure you have write permissions for the desktop.", "HD Diagnostic Utility");
            }

            //hide the progress bar again
            progressBar1.Value = 0;
            progressBar1.Visible = false;

            EnableAllButtons();
        }

        //manage network adapters
        private void button7_Click(object sender, EventArgs e)
        {
            //just launch ncpa.cpl, there is probably
            //a better way to do this, but it works, so who cares
            Process.Start("ncpa.cpl");
        }

        //automatic diagnosis
        private void button8_Click(object sender, EventArgs e)
        {
            DisableAllButtons();

            bool issuesDetected = RunAutomaticDiagnoses();

            GreyProgressText();

            if(!issuesDetected)
            {
                string message =
                    "The automatic diagnosis did not detect any issues.\n\n" +
                    "If you are still experiencing connectivity problems,\n" +
                    "create a report to send to your support staff by\n" +
                    "choosing \"Generate Text Report.\"";

                MessageBox.Show(message);
            }

            EnableAllButtons();
        }
    }
}
