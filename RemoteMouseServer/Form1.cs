using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteMouseServer
{
    public partial class Form1 : Form
    {
        string dataPosX;
        string dataPosY;
        string data;
        static int port = 10000;
        IPAddress address;
        BackgroundWorker bw;

        int posMouseX = 0;
        int posMouseY = 0;

        int mouseSensitivity = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        public Form1()
        {
            InitializeComponent();

            string host = Dns.GetHostName();
            address = Dns.GetHostAddresses(host)[0];

            var name = Dns.GetHostName();
            label1.Text = address.ToString();
            label2.Text = port.ToString();

            bw = new BackgroundWorker();
            bw.DoWork += (obj, ew) => TaskAsync(1);
            bw.RunWorkerCompleted += worker_RunWorkerCompleted;
            bw.RunWorkerAsync();
        }

        private async void TaskAsync(int i)
        {
            createSocket(port, address);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TopMost = true;
            DialogResult result = MessageBox.Show(this, "Сессия остановлена. Хотите восстановить подключение?",
                "Сессия остановлена", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Restart();
            }
            else
            {
                Application.Exit();
            }
        }

        void createSocket(int port, IPAddress address)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(address, port);
            Socket sListener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ListenSocket(sListener, ipEndPoint);
        }

        public void ListenSocket(Socket sListener, IPEndPoint ipEndPoint)
        {
            Socket handler = null;
            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(1);
                handler = sListener.Accept();
                Invoke((MethodInvoker)(() => { Opacity = 0; }));

                while (true)
                {
                    data = null;

                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);

                    if (data.Contains("<TheEnd>"))
                    {
                        byte[] msg = Encoding.UTF8.GetBytes(data);
                        handler.Send(msg);
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        Invoke((MethodInvoker)(() => {
                            Opacity = 100;
                        }));
                        break;
                    }
                    else if (data.Contains("Click"))
                    {
                        ClickMouse(data);
                    }
                    else
                    {
                        MoveCursor(data);
                    }
                }
            }
            catch (Exception ex)
            {
                data = "<The End>";
            }
        }

        private void ClickMouse(string data)
        {
            uint mouseKeyEvent = 0;

            switch (data)
            {
                case "leftClickDown":
                    mouseKeyEvent = (uint)MouseEventF.LeftDown;
                    break;
                case "leftClickUp":
                    mouseKeyEvent = (uint)MouseEventF.LeftUp;
                    break;
                case "rightClickDown":
                    mouseKeyEvent = (uint)MouseEventF.RightDown;
                    break;
                case "rightClickUp":
                    mouseKeyEvent = (uint)MouseEventF.RightUp;
                    break;
                case "middleClickDown":
                    mouseKeyEvent = (uint)MouseEventF.MiddleDown;
                    break;
                case "middleClickUp":
                    mouseKeyEvent = (uint)MouseEventF.MiddleUp;
                    break;
            }


            Input[] inputs = new Input[]
            {
                new Input
                {
                    type = (int) InputType.Mouse,
                    u = new InputUnion
                    {
                        mi = new MouseInput
                        {
                            dwFlags = mouseKeyEvent,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        private void MoveCursor(string data)
        {
            IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
            string[] firstFiltredData = data.Split('/');
            string[] positionCursor = firstFiltredData[0].Split(' ');
            dataPosX = positionCursor[0];
            dataPosY = positionCursor[1];
            if (positionCursor[2] != null)
                mouseSensitivity = Convert.ToInt32(positionCursor[2]);

            if (dataPosX.Contains(".") || dataPosY.Contains("."))
            {
                GetCursorPos(out POINT point);
                posMouseX = point.X;
                posMouseY = point.Y;
                return;
            }

            var posX = Convert.ToInt32(dataPosX, formatter);
            var posY = Convert.ToInt32(dataPosY, formatter);

            SetCursorPos(posMouseX + (posX * mouseSensitivity), posMouseY + (posY * mouseSensitivity));

        }
    }
}
