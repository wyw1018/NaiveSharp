﻿using NaiveSharp.Controller;
using NaiveSharp.Model;
using NaiveSharp.Controller.Extension;

using System;
using System.IO;
using System.Windows.Forms;
using NaiveSharp.ConstText;
using System.Data;

namespace NaiveSharp.View
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists(PATH.CONFIG_NODE_NS))
            {
                try
                {
                    LoadFromNs(File.ReadAllText(PATH.CONFIG_NODE_NS));
                }
                catch
                {
                    File.Delete(PATH.CONFIG_NODE_NS);
                }
            }
        }

        public void LoadFromNs(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return;
            }

            var x = ns.Trim().Split(' ');
            if (x.Length != 2)
            {
                return;
            }

            x[0] = x[0].FromBase64();

            var uri = new Uri(x[0]);

            switch (uri.Scheme)
            {
                case "https":
                    rdoHttps.Checked = true;
                    rdoQuic.Checked = false;
                    break;
                default:
                    rdoHttps.Checked = false;
                    rdoQuic.Checked = true;
                    break;
            }

            chkPadding.Checked = bool.Parse(x[1]);
            txtHost.Text = uri.Host;
            string userinfo = uri.UserInfo.Trim();
            if (string.IsNullOrWhiteSpace(userinfo))
            {
                txtPassword.Text =
                    txtUsername.Text = "";
            }
            else
            {
                var vv = userinfo.Split(':');
                switch (vv.Length)
                {
                    case 1:
                        txtUsername.Text = vv[0];
                        break;
                    case 2:
                        txtUsername.Text = vv[0].FromUrlEncode();
                        txtPassword.Text = vv[1].FromUrlEncode();
                        break;
                    default:
                        throw new DataException();
                }
            }
            if (uri.Port > 0)
            {
                txtHost.Text += ":" + uri.Port;
            }
        }

        private void MainWindows_Load(object sender, EventArgs e)
        {
            if (System.IO.File.Exists("DEBUG"))
            {
                Config.Debug = true;
                this.Text = "[DEBUG]" + this.Text;
            }
            icnNotify.Visible = true;
        }

        #region ProxyMode

        private void rdoGlobal_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoGlobal.Checked)
            {
                Config.RunMode = "global";
            }
        }

        private void rdoGfwlist_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoGfwlist.Checked)
            {
                Config.RunMode = "gfwlist";
            }
        }

        private void rdoGeoIP_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoGfwlist.Checked)
            {
                Config.RunMode = "geoip";
            }
        }

        #endregion

        #region Operation Controller

        private void lblSave_Click(object sender, EventArgs e)
        {
            Operation.Save();

            MessageBox.Show("Node information saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            /*
             * 0 -> Ok
             * 1 -> 1080
             * 2 -> 1081
             * 3 -> 1080 & 1081
             */
            int status = 0;

            if (Net.IsPortUsed(1080))
            {
                status = 1;
            }

            if (Net.IsPortUsed(1081))
            {
                if (status == 1)
                {
                    status = 3;
                }
                else
                {
                    status = 2;
                }
            }

            DialogResult result = DialogResult.OK;
            switch (status)
            {
                case 1:
                    result = MessageBox.Show("Port 1080 is in used! NaiveProxy may not work normally!\n" +
                                             "Do you still want to continue?", "Port is in used",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    break;
                case 2:
                    result = MessageBox.Show("Port 1081 is in used! HTTP proxy and padding may not work normally!\n" +
                                             "Do you still want to continue?", "Port is in used",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    break;
                case 3:
                    result = MessageBox.Show("Port 1080 is in used! NaiveProxy may not work normally!\n" +
                                             "Port 1081 is in used! HTTP proxy and padding may not work normally!\n" +
                                             "Do you still want to continue?", "Port is in used",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    break;
            }

            if (result == DialogResult.No)
            {
                return;
            }

            Operation.Run();

            MessageBox.Show("NaiveProxy runs successfully!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lblStop_Click(object sender, EventArgs e)
        {
            Operation.Stop();
            MessageBox.Show("NaiveProxy stop successfully!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lblExit_Click(object sender, EventArgs e)
        {
            Operation.Stop();
            Environment.Exit(0);
        }

        #endregion

        #region Control -> Config

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {
            Config.Username = txtUsername.Text;
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            Config.Password = txtPassword.Text;
        }

        private void txtHost_TextChanged(object sender, EventArgs e)
        {
            Config.Host = txtHost.Text;
        }

        private void chkPadding_CheckedChanged(object sender, EventArgs e)
        {
            Config.Padding = chkPadding.Checked;
        }

        private void rdoHttps_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoHttps.Checked)
            {
                Config.Scheme = "https";
            }
            else
            {
                Config.Scheme = "quic";
            }
        }

        #endregion

        #region SMI

        private void smiExit_Click(object sender, EventArgs e)
        {
            Operation.Stop();
            Environment.Exit(0);
        }

        private void smiStop_Click(object sender, EventArgs e)
        {
            Operation.Stop();
        }

        private void smiRun_Click(object sender, EventArgs e)
        {
            Operation.Run();
        }

        private void smiAbout_Click(object sender, EventArgs e)
        {
            var about = new View.About();
            about.ShowDialog();
        }

        #endregion

        private void smiCopyShareLink_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(Sharelink.Generate());
        }

        private void smiLoadShareLink_Click(object sender, EventArgs e)
        {
            string x = Clipboard.GetText();
            var y = Sharelink.LoadFromShareLink(x);
            if (!y.HasValue)
            {
                return;
            }

            switch (y.Value.Scheme)
            {
                case "https":
                    rdoHttps.Checked = true;
                    rdoQuic.Checked = false;
                    break;
                default:
                    rdoHttps.Checked = false;
                    rdoQuic.Checked = true;
                    break;
            }

            txtHost.Text = y.Value.Host;
            txtUsername.Text = y.Value.Username;
            txtPassword.Text = y.Value.Password;
            chkPadding.Checked = y.Value.Padding;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Hide();
                if (Config.IsFirstTimeHide)
                {
                    icnNotify.ShowBalloonTip(1000, "Naive # Tip", "Hey! Naive # is still running under background!", ToolTipIcon.Info);
                    Config.IsFirstTimeHide = false;
                }
                e.Cancel = true;
            }
        }

        private void cmsNotify_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void icnNotify_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            //Focus();
        }
    }
}