using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Diagnostics;
//using System.Threading;

namespace SocketUdpClient
{
    static class Program
    {
        [STAThread]

        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MyApplication());
        }
    }


    public class MyApplication : ApplicationContext
    {
        Listener listener = new Listener();

        public MyApplication()
        {
            Task listen = new Task(listener.Listen);
            listen.Start();
        }

        //public static void Exit()
        //{
        //    listener.Close();
        //    Application.Exit();
        //}
    }

    public class Notifier
    {
        private NotifyIcon icon;
        public bool IsCalling;
        private Timer timer = new Timer();

        public Notifier()
        {
            icon = new NotifyIcon()
            {
                Icon = new Icon("C:\\MyAssets\\call.ico"), ///Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Exit", Exit),
                    new MenuItem("Mute", Mute)
                }),
                Visible = true
            };

            icon.BalloonTipClicked += Mute;
            icon.Click += Mute;

            timer.Interval = 300;
            timer.Enabled = true;
            timer.Tick += Bell;
            timer.Start();

            //Task bell = new Task(Bell);
            //bell.Start();
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            icon.Visible = false;

            Application.Exit();
        }

        void Mute(object sender, EventArgs e)
        {
            IsCalling = false;
        }

        public void Notify(string title, string text)
        {
            //icon.BalloonTipIcon = ToolTipIcon.Info;

            icon.BalloonTipText = text;

            icon.BalloonTipTitle = title;

            icon.ShowBalloonTip(20000);
        }

        private void Bell(object sender, EventArgs e)
        {
            if (IsCalling)
            {
                Console.Beep(700, 250);
            }
        }
    }

    public class Listener
    {
        static Socket listeningSocket;
        private int localPort = 25255; // порт приема сообщений
        static Notifier notifier;
        private string IP = "192.168.0.123";

        public Listener()
        {
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Прослушиваем по адресу
            IPEndPoint localIP = new IPEndPoint(IPAddress.Parse(IP), localPort);
            listeningSocket.Bind(localIP);

            notifier = new Notifier();
        }

        public void Listen()
        {
            if (listeningSocket is null)
            {
                return;
            }

            try
            {
                while (true)
                {
                    // получаем сообщение
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0; // количество полученных байтов
                    byte[] data = new byte[256]; // буфер для получаемых данных

                    // адрес, с которого пришли данные
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        bytes = listeningSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    }
                    while (listeningSocket.Available > 0);

                    // получаем данные о подключении
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                    string message = builder.ToString();

                    // выводим сообщение
                    Console.WriteLine("{0}:{1} - {2}", remoteFullIp.Address.ToString(), remoteFullIp.Port, message);

                    if (message.Split(':')[0] == "incoming_call")
                    {
                        notifier.Notify("Incoming Call", message.Split(':')[1] + "\n" + message.Split(':')[2]);
                        notifier.IsCalling = true;
                    }
                    else if (message.Split(':')[0] == "active_call")
                    {
                        notifier.IsCalling = false;
                    }
                    else if (message.Split(':')[0] == "missed_call")
                    {
                        notifier.Notify("Missed Call!", message.Split(':')[1] + "\n" + message.Split(':')[2]);
                        notifier.IsCalling = false;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        // закрытие сокета
        public void Close()
        {
            if (listeningSocket != null)
            {
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
                listeningSocket = null;
            }
        }
    }
}

