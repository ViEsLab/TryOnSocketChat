using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChatoServer
{
    public partial class MainForm : Form
    {
        public MainForm(EventHandler b1Click, EventHandler b2Click, EventHandler b3Click, EventHandler b4Click, EventHandler b5Click)
        {
            InitializeComponent();
            this.buttonConnect.Click += b1Click;
            this.buttonSend.Click += b2Click;
            this.btnSelect.Click += b3Click;
            this.btnSendFile.Click += b4Click;
            this.btnSendImage.Click += b5Click;
            this.comboBoxAllClients.Items.Add("All");
        }
        
        public string GetIPText()
        {
            return this.textBoxIP.Text;
        }
        
        public int GetPort()
        {
            return (int)this.numericUpDownPort.Value;
        }

        public string GetMsgText()
        {
            return this.textBoxSendee.Text.Trim();
        }

        public void SetMsgText(string str)
        {
            this.textBoxSendee.Text = str;
        }
         
        public void ClearMsgText()
        {
            this.textBoxSendee.Clear();
        }

        delegate void VoidString(string s);
        public void Println(string s)
        {
            if (this.textBoxMsg.InvokeRequired) {
                VoidString println = Println;
                this.textBoxMsg.Invoke(println, s);
            }
            else {
                this.textBoxMsg.AppendText(s + Environment.NewLine);
            }
        }

        delegate void VoidBoolString(bool b, string s);
        public void SetConnectionStatusLabel(bool isConnect, string point = null)
        {
            if (this.labelStatus.InvokeRequired) {
                VoidBoolString scsl = SetConnectionStatusLabel;
                this.labelStatus.Invoke(scsl, isConnect, point);
            }
            else {
                if (isConnect) {
                    this.labelStatus.ForeColor = Color.Green;
                    this.labelStatus.Text = point;
                }
                else {
                    this.labelStatus.ForeColor = Color.Red;
                    this.labelStatus.Text = "未连接";
                }
            }
        }

        delegate void VoidBool(bool b);
        public void SetButtonSendEnabled(bool enabled)
        {
            if (this.buttonSend.InvokeRequired)
            {
                VoidBool sbse = SetButtonSendEnabled;
                this.textBoxMsg.Invoke(sbse, enabled);
            }
            else
            {
                this.buttonSend.Enabled = enabled;
                this.btnSelect.Enabled = enabled;
                this.btnSendFile.Enabled = enabled;
                this.btnSendImage.Enabled = enabled;
            }
        }

        public void ComboBoxAddItem(string s)
        {
            if (this.comboBoxAllClients.InvokeRequired)
            {
                VoidString cbAddItem = ComboBoxAddItem;
                this.textBoxMsg.Invoke(cbAddItem, s);
            }
            else
            {
                this.comboBoxAllClients.Items.Add(s);
            }
        }
        public void ComboBoxRemoveItem(string s)
        {
            if (this.comboBoxAllClients.InvokeRequired)
            {
                VoidString cbRmItem = ComboBoxRemoveItem;
                this.textBoxMsg.Invoke(cbRmItem, s);
            }
            else
            {
                this.comboBoxAllClients.Items.Remove(s);
            }
        }

        public string GetComboBoxItem()
        {
            if (this.comboBoxAllClients.SelectedItem == null)
                return null;
            else
                return this.comboBoxAllClients.SelectedItem.ToString();
        }

    }
}
