using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using MODI;
using Image = MODI.Image;

namespace CSD_Bot
{
    public partial class Form1 : Form
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        public bool BotOn;

        private GlobalKeyboardHook _gHook;
        private readonly string _imagePath = AppDomain.CurrentDomain.BaseDirectory + "myBitmap.bmp";
        private readonly Dish[] _meniu = new Dish[1000];
        private int _meniuSize;
        private bool _preparing;
        private readonly Queue _queue = new Queue();
        private readonly Dish[] _slots = new Dish[8];
        private readonly Point[] _slotLocation = new Point[8];
        private readonly Bitmap _screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

        private readonly List<TextBox> _slotBox = new List<TextBox>();
        private readonly List<TextBox> _slotStatus = new List<TextBox>();

        public Form1()
        {
            InitializeComponent();
        }

        // Default settings
        private void Form1_Load(object sender, EventArgs e)
        {
            // Windowed
            _slotLocation[0].X = 30;
            _slotLocation[0].Y = 190;
            _slotLocation[1].X = 40;
            _slotLocation[1].Y = 240;
            _slotLocation[2].X = 40;
            _slotLocation[2].Y = 340;
            _slotLocation[3].X = 40;
            _slotLocation[3].Y = 415;
            _slotLocation[4].X = 40;
            _slotLocation[4].Y = 495;
            _slotLocation[5].X = 40;
            _slotLocation[5].Y = 575;
            _slotLocation[6].X = 32;
            _slotLocation[6].Y = 650;
            _slotLocation[7].X = 33;
            _slotLocation[7].Y = 722;

            _slotBox.Add(SlotBox1);
            _slotBox.Add(SlotBox2);
            _slotBox.Add(SlotBox3);
            _slotBox.Add(SlotBox4);
            _slotBox.Add(SlotBox5);
            _slotBox.Add(SlotBox6);
            _slotBox.Add(SlotBox7);
            _slotBox.Add(SlotBox8);

            _slotStatus.Add(SlotStatus1);
            _slotStatus.Add(SlotStatus2);
            _slotStatus.Add(SlotStatus3);
            _slotStatus.Add(SlotStatus4);
            _slotStatus.Add(SlotStatus5);
            _slotStatus.Add(SlotStatus6);
            _slotStatus.Add(SlotStatus7);
            _slotStatus.Add(SlotStatus8);

            foreach (TextBox t in _slotStatus)
            {
                t.Text = @"Empty";
            }

            _gHook = new GlobalKeyboardHook(); // Create a new GlobalKeyboardHook
            _gHook.KeyDown += gHook_KeyDown; // Declare a KeyDown Event

            // Add the keys you want to hook to the HookedKeys list
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key.GetHashCode() >= 112 && key.GetHashCode() <= 119)
                {
                    _gHook.HookedKeys.Add(key);
                }
            }
            _gHook.hook(); // Start the keylogger

            // Empty the slot array
            for (var i = 0; i < 8; i++)
            {
                _slots[i].Name = null;
                _slots[i].Preparation = new string[10];
                _slots[i].Stage = 0;
                _slots[i].MaxStage = 0;
            }

            // Read the meniu and save it in an array
            var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "Meniu.txt");
            for (var i = 0; i < lines.Length; i++)
            {
                _meniuSize++;
                _meniu[i].Name = lines[i].Substring(0, lines[i].IndexOf("|", StringComparison.Ordinal));
                lines[i] = lines[i].Substring(lines[i].IndexOf("|", StringComparison.Ordinal) + 1, lines[i].Length - lines[i].IndexOf("|", StringComparison.Ordinal) - 1);
                var d = 0;
                _meniu[i].Preparation = new string[5];
                while (lines[i].Length > 0)
                {
                    _meniu[i].Preparation[d] = lines[i].Substring(0, lines[i].IndexOf("|", StringComparison.Ordinal));
                    lines[i] = lines[i].Substring(lines[i].IndexOf("|", StringComparison.Ordinal) + 1, lines[i].Length - lines[i].IndexOf("|", StringComparison.Ordinal) - 1);
                    d++;
                }
                _meniu[i].MaxStage = d;
            }
            BotOn = true;
            _preparing = false;
            Run();// Start the checking
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _gHook.unhook();
        }

        // Core
        private async void Run()
        {
            while (BotOn)
            {
                Process[] pname = Process.GetProcessesByName("CSDSteamBuild");

                if (pname.Length != 0)
                {
                    GameStatus.Text = @"CSD detected";
                    if (!_preparing)
                    {
                        for (var slot = 1; slot <= 8; slot++)
                        {
                            if (_slots[slot - 1].Occupied || !CheckSlot(_slotLocation[slot - 1])) continue;
                            _slots[slot - 1].Occupied = true;
                            _queue.Enqueue(slot);
                        }
                        if (_queue.Count != 0)
                        {
                            _preparing = true;
                            Prepare((int) _queue.Dequeue());
                        }
                    }
                }
                else
                {
                    GameStatus.Text = @"Can't detect CSD";
                }
                await Task.Delay(40);
            }
        }

        // Handle the KeyDown Event
        private void gHook_KeyDown(object sender, KeyEventArgs e)
        {
            var input = e.KeyValue;

            var slot = input - 111;
            _queue.Enqueue(slot);
        }

        // Look at a slot and handle it
        private async void Prepare(int slot)
        {
            var sim = new InputSimulator();
            SendKeys.Send(slot.ToString());
            if (_slots[slot - 1].Name == null)
            {
                await Task.Delay(50);
                Order(slot, ReadTextFromImage(_imagePath));
            }
            if (_slots[slot - 1].Name != null)
            {
                _slotStatus[slot - 1].Text = @"Preparing";
                _slots[slot - 1].Stage++;

                var instructions = _slots[slot - 1].Preparation[_slots[slot - 1].Stage - 1];
                Console.WriteLine(@"Slot " + slot + @": Preparing " + _slots[slot - 1].Name + @"|Stage " + _slots[slot - 1].Stage + @"/" + _slots[slot - 1].MaxStage + @"|");

                for (var i = 0; i < instructions.Length; i++)
                {
                    await Task.Delay(50);
                    if (char.IsLower(instructions[i]))
                    {
                        SendKeys.Send(instructions[i].ToString());
                    }
                    else
                    {
                        switch (instructions[i])
                        {
                            case 'E':
                                SendKeys.Send("{ENTER}");
                                break;
                            case 'D':
                                SendKeys.Send("{DOWN}");
                                break;
                            case 'U':
                                SendKeys.Send("{UP}");
                                break;
                            case 'L':
                                SendKeys.Send("{LEFT}");
                                break;
                            case 'R':
                                SendKeys.Send("{RIGHT}");
                                break;
                            case 'H':
                                var press = instructions[i+1];
                                i+=2;
                                var hTimeS = "";
                                while (instructions[i] >= 48 && instructions[i] <= 57)
                                {
                                    hTimeS += instructions[i];
                                    i++;
                                    if (i == instructions.Length)
                                    {
                                        break;
                                    }
                                }
                                i--;
                                var hTime = int.Parse(hTimeS);
                                VirtualKeyCode virtualKey = 0;
                                switch (press)
                                {
                                    case 'D':
                                        virtualKey = (VirtualKeyCode) 40;
                                        break;
                                    case 'L':
                                        virtualKey = (VirtualKeyCode) 37;
                                        break;
                                    case 'U':
                                        virtualKey = (VirtualKeyCode) 38;
                                        break;
                                    case 'R':
                                        virtualKey = (VirtualKeyCode) 39;
                                        break;
                                }
                                sim.Keyboard.KeyDown(virtualKey);
                                await Task.Delay(hTime);
                                sim.Keyboard.KeyUp(virtualKey);
                                break;
                            case 'W':
                                _preparing = false;
                                var wTime = int.Parse(instructions.Substring(i + 1, instructions.Length - i - 1));
                                _slotStatus[slot - 1].Text = @"Cooking";
                                await Task.Delay(wTime);
                                if (_slots[slot - 1].Stage == _slots[slot - 1].MaxStage)
                                {
                                    SendKeys.Send(slot.ToString());
                                    _slots[slot - 1].Occupied = false;
                                    _slots[slot - 1].Name = null;
                                    //_slots[slot - 1].Stage = 0;
                                    //_slots[slot - 1].MaxStage = 0;
                                    _slotBox[slot - 1].Text = null;
                                    _slotStatus[slot - 1].Text = @"Empty";
                                }
                                else
                                {
                                    _queue.Enqueue(slot);
                                    _slotStatus[slot - 1].Text = @"In queue";
                                }
                                return;
                        }
                    }
                }
                _preparing = false;
                _slots[slot - 1].Occupied = false;
                _slots[slot - 1].Name = null;
                //_slots[slot - 1].Stage = 0;
                //_slots[slot - 1].MaxStage = 0;
                _slotBox[slot - 1].Text = null;
                _slotStatus[slot - 1].Text = @"Empty";
            }
            else
            {
                _slots[slot - 1].Occupied = false;
                _preparing = false;
            }
        }

        // Fills the slot info
        private void Order(int slot, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            name = Clean(name);
            //Console.WriteLine(name);
            _slotStatus[slot - 1].Text = @"Searching";
            for (var i = 0; i < _meniuSize; i++)
            {
                if (_meniu[i].Name != name) continue;
                _slots[slot - 1].Name = name;
                _slotBox[slot - 1].Text = name;
                for (var a = 0; a < _meniu[i].MaxStage; a++)
                {
                    _slots[slot - 1].Preparation[a] = _meniu[i].Preparation[a];
                }
                _slots[slot - 1].Stage = 0;
                _slots[slot - 1].MaxStage = _meniu[i].MaxStage;
                return;
            }
        }

        // Returns a string only in quotes
        private static string Clean(string str)
        {
            //var firstMark = str.IndexOf((char)8220);
            var secondMark = str.IndexOf((char)8221);
            if (secondMark != -1)
            {
                str = str.Substring(1, secondMark - 1);
            }
            var ticket = str.IndexOf((char)41);
            if (ticket != -1)
            {
                str = str.Substring(0, ticket+1);
            }
            return str;
        }

        // Check if the slot icon is requesting preparation
        public bool CheckSlot(Point location)
        {
            // Getting the color
            using (var gdest = Graphics.FromImage(_screenPixel))
            {
                using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDc = gsrc.GetHdc();
                    IntPtr hDc = gdest.GetHdc();
                    int retval = BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            var c = _screenPixel.GetPixel(0, 0);
            return c.R == 255 && c.G == 255 && c.B == 255;
        }

        // Take a screenshot from primary screen
        private static Bitmap Screenshot()
        {
            // s = Screen.PrimaryScreen.Bounds.Size
            var s = new Size(800, 55);
            // Screen - 550, 55

            // This is there we will store a snapshot of the screen
            var bmpScreenshot = new Bitmap(s.Width, s.Height);
            var g = Graphics.FromImage(bmpScreenshot);

            // Take a screenshot
            g.CopyFromScreen(365, 780, 0, 0, s);
            // Screem - 365, 780// Screem - 365, 780

            //float scale = Math.Min(width / image.Width, height / image.Height);
            const float scale = 0.835f;

            var bmpScaled = new Bitmap(bmpScreenshot.Width*2, bmpScreenshot.Height*2);
            var graph = Graphics.FromImage(bmpScaled);

            var scaleWidth = (int)(bmpScreenshot.Width * scale);
            var scaleHeight = (int)(bmpScreenshot.Height * scale);

            var brush = new SolidBrush(Color.Black);
            graph.FillRectangle(brush, new RectangleF(0, 0, scaleWidth, scaleHeight));
            graph.DrawImage(bmpScreenshot, new Rectangle(0, 0, scaleWidth, scaleHeight));

            return bmpScaled;
        }

        // Read Text from Image
        private string ReadTextFromImage(string imagePath)
        {
            string str = null;
            var bmpScreenshot = Screenshot();
            PictureBox.Image = bmpScreenshot;
            GC.Collect();

            // Look at screen and read dish title
            bmpScreenshot.Save(_imagePath);
            try
            {
                // Grab Text From Image  
                var modiObj = new Document();
                modiObj.Create(imagePath);
                modiObj.OCR(MiLANGUAGES.miLANG_ENGLISH, true, true);

                //Retrieve the text gathered from the image  
                var modiImageObj = (Image)modiObj.Images[0];

                str = modiImageObj.Layout.Text;

                modiObj.Close();
            }
            catch (Exception)
            {
                Console.WriteLine(@"Couldn't read.");
            }
            return str;
        }

        public struct Dish
        {
            public string Name;
            public int Stage, MaxStage;
            public string[] Preparation;
            public bool Occupied;
        }
    }
}
