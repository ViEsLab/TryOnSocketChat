using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;

namespace ChatoServer
{
    static class Program
    {
        static Socket clientSocket = null;
        static IPAddress ip = null;
        static IPEndPoint point = null;

        static MainForm form = null;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm(bConnectClick, bSendClick, bSelectClick, bSendFileClick, bSendImageClick);
            Application.Run(form);

        }

        static EventHandler bConnectClick = SetConnection;
        static EventHandler bSendClick = SendMsg;
        static void SetConnection(object sender, EventArgs e)
        {
            ip = IPAddress.Parse(form.GetIPText());
            point = new IPEndPoint(ip, form.GetPort());

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                //进行连接
                clientSocket.Connect(point);
                form.SetConnectionStatusLabel(true, point.ToString());
                form.SetButtonSendEnabled(true);
                form.Println($"连接 {point} 的服务器。");

                //不停的接收服务器端发送的消息
                Thread thread = new Thread(Receive);
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start(clientSocket);

            }
            catch (Exception ex) {
                form.Println("错误：" + ex.Message);
            }
        }
        static void Receive(object so)
        {
            Socket send = so as Socket;
            while (true)
            {
                try {
                    //获取发送过来的消息
                    string filePath = "";
                    byte[] buf = new byte[1024 * 1024 * 5];
                    //form.Println(buf[0].ToString());
                    int len = send.Receive(buf);
                    if (len == 0) break;
                    string s = Encoding.UTF8.GetString(buf, 0, len);
                    if (buf[0] == 1)
                    {
                        form.Println("客户端开始");
                        SaveFileDialog sfd = new SaveFileDialog();
                        sfd.Title = "保存文件";
                        sfd.InitialDirectory = @"E:\FileReceive";
                        sfd.Filter = "文本文件|*.txt|图片文件|*.jpg|视频文件|*.avi|所有文件|*.*";

                        //如果没有选择保存文件路径就一直打开保存框
                        while (true)
                        {
                            sfd.ShowDialog();  //开启保存窗口（注意线程间操作）
                            //MainForm form2 = new MainForm(bListenClick, bSendClick);
                            //form2.XShowDialog(form);
                            //sfd.XShowDialog(form);
                            filePath = sfd.FileName;
                            if (string.IsNullOrEmpty(filePath))
                            {
                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }

                        //保存接收的文件
                        using (FileStream fsWrite = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            fsWrite.Write(buf, 1, len - 1);
                        }

                        form.Println("来自" + clientSocket.RemoteEndPoint + ": 的文件接收成功");
                    }
                    else if (s.StartsWith("ip="))  //有新客户端连接
                    {
                        form.ComboBoxAddItem(s.Substring(3));
                        form.Println($"{s.Substring(3)} 上的客户端请求连接。");
                    }
                    else if (s.StartsWith("Removeip="))  //有客户端断开连接
                    {
                        form.ComboBoxRemoveItem(s.Substring(9));
                        form.Println($"{s.Substring(9)} 上的客户端断开连接。");
                    }
                    else  //是消息
                    {
                        int index = s.IndexOf("message=");
                        form.Println(s.Substring(0, index - 1) + s.Substring(index + 8));
                    }
                }
                catch (Exception e) {
                    form.SetConnectionStatusLabel(false);
                    form.SetButtonSendEnabled(false);
                    form.Println($"服务器已中断连接：{e.Message}");
                    break;
                }
            }
        }

        static void SendMsg(object sender, EventArgs e)
        {
            //if(form.GetComboBoxItem() == null)
            //{
            //    string msg = form.GetMsgText();
            //    if (msg == "") return;
            //    byte[] sendee = Encoding.UTF8.GetBytes(msg);
            //    clientSocket.Send(sendee);
            //    form.ClearMsgText();
            //    form.Println(msg);
            //}
            //else
            //{
                string msg = null;
                string sendIP = "ip=" + form.GetComboBoxItem();
                string message = "message=" + form.GetMsgText();
                if (form.GetComboBoxItem() != null && form.GetComboBoxItem().ToString() != "All")
                    msg = sendIP + message;
                else
                    msg = message;
                if (message == "") return;
                byte[] sendee = Encoding.UTF8.GetBytes(msg);
                clientSocket.Send(sendee);
                form.ClearMsgText();
                form.Println(msg);
            //}
        }

        static void bSelectClick(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择要传的文件";
            ofd.InitialDirectory = @"C:\Users\Administrator\Desktop";
            ofd.Filter = "文本文件|*.txt|图片文件|*.jpg|视频文件|*.avi|所有文件|*.*";
            ofd.ShowDialog();
            //得到选择文件的路径
            form.SetMsgText(ofd.FileName);
        }

        static void bSendFileClick(object sender, EventArgs e)
        {
            try
            {
                string filePath = form.GetMsgText();
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("请选择文件");
                    return;
                }
                //读取选择的文件
                using (FileStream fsRead = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    int r = fsRead.Read(buffer, 0, buffer.Length);
                    //获得发送的信息时候，在数组前面加上一个字节 1
                    List<byte> list = new List<byte>();
                    list.Add(1);
                    list.AddRange(buffer);
                    byte[] newBuffer = list.ToArray();
                    //将了标识字符的字节数组传递给服务端
                    clientSocket.Send(newBuffer, 0, r + 1, SocketFlags.None);
                    form.SetMsgText("");

                    string msg = null;
                    string sendIP = "TO=" + form.GetComboBoxItem();
                    string message = "message=" + form.GetMsgText();
                    if (form.GetComboBoxItem() != null && form.GetComboBoxItem().ToString() != "All")
                        msg = sendIP;
                    else
                        msg = "";
                    if (msg == "") return;
                    byte[] sendee = Encoding.UTF8.GetBytes(msg);
                    clientSocket.Send(sendee);
                    form.ClearMsgText();
                }
            }
            catch { }
        }

        static void bSendImageClick(object sender, EventArgs e)
        {
            try
            {
                string filePath = form.GetMsgText();
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("请选择文件");
                    return;
                }
                //读取选择的文件
                using (FileStream fsRead = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    int r = fsRead.Read(buffer, 0, buffer.Length);
                    //获得发送的图片时候，在数组前面加上一个字节 2
                    List<byte> list = new List<byte>();
                    list.Add(2);
                    list.AddRange(buffer);
                    byte[] newBuffer = list.ToArray();
                    //将了标识字符的字节数组传递给客户端
                    clientSocket.Send(newBuffer, 0, r + 1, SocketFlags.None);
                    form.SetMsgText("");
                }
            }
            catch { }
        }
    }
}
