using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using ShinenginePlus;
using ShinenginePlus.DrawableControls;
using SolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;

namespace SerialOsiGUI
{

    class WaveLine : RenderableObject
    {
        async public Task FillInTime(UInt16[] Data,TimeSpan Time)
        {
            var RealTime = Time / Data.Length;
            RealTime *= 60;

            int Point = 0;
            await Task.Run(async () =>
            {
                while(true)
                {
                    for (int i = Point; Point - i < 60; Point++)
                    {
                        if (Point >= Data.Length) return;
                        Y_Axis[Current_Point] = RateToAxis((Data[Point] - 2048) / 2060.0);
                        CircleRaise();
                    }
              //      await Task.Delay(RealTime.Milliseconds);
                }
            });
        }

        static public float RateToAxis(double Rate)
        {
            return (float)(540 + (540 * Rate));
        }

        static public double AxisToRate(float Axis)
        {
            Axis -= 540;
            return Axis / 540d;
        }

        public float[] Y_Axis = new float[1920];
        public int Current_Point = 0;
        public void CircleRaise()
        {
            Current_Point++;
            if (Current_Point == 1920) Current_Point = 0;
        }
        public WaveLine(DeviceContext DC) : base(DC)
        {
            for (int i = 0; i < 1920; i++)
            {
                Y_Axis[i] = RateToAxis(new Random().Next(0, 10000) / 10000d * 2 - 1);
            }
        }

        public override void Render()
        {
            using (SolidColorBrush sb = new SharpDX.Direct2D1.SolidColorBrush(HostDC, new RawColor4(1, 1, 0, 1)))
                for (int x = 1; x < 1920; x++)
                {
                    HostDC.DrawLine(new RawVector2(x -1, Y_Axis[x-1]), new RawVector2(x, Y_Axis[x]), sb, 2.5f);
                }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool FullScreen = false;
        SerialPort COM = null;

        int Count = 0;
        double ADCToRate(int ADC)
        {
            ADC -= 2048;
            return ADC / 2048d;
        }

        byte[] ReadForSize(SerialPort COM, int Size)
        {
            byte[] Buffer = new byte[Size];
            int ReadedSize = 0;

            do
            {
                int sc = COM.Read(Buffer, ReadedSize, Size - ReadedSize);
                ReadedSize += sc;
            } while (ReadedSize < Size);

            return Buffer;
        }

        BackGroundLayer BackGround = null;
        WaveLine MainLine = null;

        byte[] Str4096 = Encoding.ASCII.GetBytes("96000");


        public MainWindow()
        {
            InitializeComponent();
          

            Loaded += (e, v) =>
            {
                try
                {
                    COM = new SerialPort("COM19", 15000000);
                    COM.Open();
                    COM.Write(Str4096, 0, 5);
                }
                catch(Exception)
                {
                    MessageBox.Show("Serial Port was Failed to Open");
                    Close();
                }

                var WindowHandle = new WindowInteropHelper(this).Handle;
                
                var DX = new Direct2DWindow(new System.Drawing.Size(1920, 1080), WindowHandle, FullScreen);
                BackGround = new BackGroundLayer(new System.Drawing.Size(1920, 1080), this, new RawRectangleF(0, 0, 1920f, 1080f), DX.DC)
                {
                    Color = new RawColor4(0, 0, 0, 1),
                    Range = new RawRectangleF(0, 0, 1920f, 1080f)
                };
                
                MainLine = new WaveLine(DX.DC);
                MainLine.PushTo(BackGround);
                
                DX.DrawProc += (dc) =>
                {
                    dc.Clear(new RawColor4(1, 1, 1, 1));

                    BackGround.Render();
                    BackGround.Update();

                    return DrawResultW.Commit;
                };
                DX.Run();

                if(!FullScreen)
                {
                    this.Height = 576;
                    this.Width = 972;
                }

                byte[] Buf = null;
                bool Prepaired = false;
                Task.Run(async () =>
                {
                    while(true)
                    {
                        Buf = ReadForSize(COM, 1920 * 2);
                        COM.DiscardInBuffer();
                        Prepaired = true;
                    }
                });

                Task.Run(async () =>
                {
                    while (true)
                    {
                        if(Prepaired)
                        {
                            List<ushort> db = new List<ushort>();
                            for (int i = 0; i < 1920 * 2; i+=2)
                            {
                                db.Add((ushort)(4096 - BitConverter.ToUInt16(Buf, i)));
                            }
                            if (status)
                                MainLine.FillInTime(db.ToArray(), TimeSpan.FromSeconds(1));
                            Prepaired = false;
                        }
                    }
                });
            };

            Closed += (e, v) =>
            {
            };
        }
        bool status = true;
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) status = !status;
        }
    }
}
