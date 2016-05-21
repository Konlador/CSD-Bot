using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        private readonly string _imagePath = AppDomain.CurrentDomain.BaseDirectory + "myBitmap.bmp";
        private readonly Queue _queue = new Queue();
        private readonly Bitmap _screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        private readonly Point[] _slotLocation = new Point[8];
        private readonly List<Tuple<TextBox, TextBox>> _slotInfo = new List<Tuple<TextBox, TextBox>>();

        private readonly Dictionary<string, Dish> _meniu = new Dictionary<string, Dish>();
        private readonly DishProduction[] _slots = new DishProduction[8];

        private GlobalKeyboardHook _gHook;
        private bool _preparing;
        public bool BotOn;

        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDc, int x, int y, int nWidth, int nHeight, IntPtr hSrcDc, int xSrc, int ySrc, int dwRop);

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

            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName1, SlotStatus1));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName2, SlotStatus2));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName3, SlotStatus3));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName4, SlotStatus4));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName5, SlotStatus5));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName6, SlotStatus6));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName7, SlotStatus7));
            _slotInfo.Add(new Tuple<TextBox, TextBox>(SlotName8, SlotStatus8));

            foreach (var t in _slotInfo)
            {
                t.Item2.Text = @"Empty";
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
                _slots[i] = new DishProduction();
            }

            // Read the meniu and save it in an array
            var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "Menu.txt");
            for (var i = 0; i < lines.Length; i++)
            {
                var name = lines[i].Substring(0, lines[i].IndexOf("|", StringComparison.Ordinal));
                _meniu.Add(name, new Dish());
                lines[i] = lines[i].Substring(lines[i].IndexOf("|", StringComparison.Ordinal) + 1, lines[i].Length - lines[i].IndexOf("|", StringComparison.Ordinal) - 1);
                _meniu[name].MaxStage = 0;
                _meniu[name].Preparation = new string[5];
                while (lines[i].Length > 0)
                {
                    _meniu[name].Preparation[_meniu[name].MaxStage] = lines[i].Substring(0, lines[i].IndexOf("|", StringComparison.Ordinal));
                    lines[i] = lines[i].Substring(lines[i].IndexOf("|", StringComparison.Ordinal) + 1, lines[i].Length - lines[i].IndexOf("|", StringComparison.Ordinal) - 1);
                    _meniu[name].MaxStage++;
                }
            }

            BotOn = false;
            _preparing = false;
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
                var pname = Process.GetProcessesByName("CSDSteamBuild");

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

        private void OnOffButton_Click(object sender, EventArgs e)
        {
            BotOn = !BotOn;
            if (BotOn)
            {
                OnOffButton.Text = @"Turn off (F12)";
                Run(); // Start the checking
            }
            else
            {
                OnOffButton.Text = @"Turn on (F12)";
            }
            //int a = (int) VirtualKeyCode.VK_A;
            //Console.WriteLine(a);
        }

        // Look at a slot and handle it
        private async void Prepare(int slot)
        {
            var inputSimulator = new InputSimulator();
            SendKeys.Send(slot.ToString());
            if (_slots[slot - 1].Dish == null)
            {
                await Task.Delay(50);
                Order(slot, ReadTextFromImage(_imagePath));
            }
            if (_slots[slot - 1].Dish == null)
            {
                _slots[slot - 1].Occupied = false;
                _preparing = false;
                _slotInfo[slot - 1].Item2.Text = @"Empty";
                return;
            }
            _slotInfo[slot - 1].Item2.Text = @"Preparing";
            _slots[slot - 1].Stage++;

            var instructions = _slots[slot - 1].Dish.Preparation[_slots[slot - 1].Stage - 1];
            //Console.WriteLine(@"Slot " + slot + @": Preparing " + _slots[slot - 1].Name + @"|Stage " + _slots[slot - 1].Stage + @"/" + _slots[slot - 1].MaxStage + @"|");

            for (var i = 0; i < instructions.Length; i++)
            {
                await Task.Delay(30);
                if (char.IsLower(instructions[i]))
                {
                    //inputSimulator.Keyboard.KeyPress((VirtualKeyCode) instructions[i] - 32);
                    SendKeys.Send(instructions[i].ToString());
                }
                else
                {
                    switch (instructions[i])
                    {
                        case 'E':
                            //inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                            SendKeys.Send("{ENTER}");
                            break;
                        case 'D':
                            //inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                            SendKeys.Send("{DOWN}");
                            break;
                        case 'U':
                            //inputSimulator.Keyboard.KeyPress(VirtualKeyCode.UP);
                            SendKeys.Send("{UP}");
                            break;
                        case 'L':
                            //inputSimulator.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                            SendKeys.Send("{LEFT}");
                            break;
                        case 'R':
                            //inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                            SendKeys.Send("{RIGHT}");
                            break;
                        case 'H':
                            var press = instructions[i + 1];
                            i += 2;
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
                            inputSimulator.Keyboard.KeyDown(virtualKey);
                            await Task.Delay(hTime);
                            inputSimulator.Keyboard.KeyUp(virtualKey);
                            break;
                        case 'W':
                            _preparing = false;
                            var wTime = int.Parse(instructions.Substring(i + 1, instructions.Length - i - 1));
                            _slotInfo[slot - 1].Item2.Text = @"Cooking";
                            await Task.Delay(wTime);
                            if (_slots[slot - 1].Stage == _slots[slot - 1].Dish.MaxStage)
                            {
                                SendKeys.Send(slot.ToString());
                                await Task.Delay(50);
                                _slots[slot - 1].Occupied = false;
                                _slots[slot - 1].Dish = null;
                                _slotInfo[slot - 1].Item1.Text = null;
                                _slotInfo[slot - 1].Item2.Text = @"Empty";
                            }
                            else
                            {
                                _queue.Enqueue(slot);
                                _slotInfo[slot - 1].Item2.Text = @"In queue";
                            }
                            return;
                    }
                }
            }
            await Task.Delay(100);
            _preparing = false;
            _slots[slot - 1].Occupied = false;
            _slots[slot - 1].Dish = null;
            _slotInfo[slot - 1].Item1.Text = null;
            _slotInfo[slot - 1].Item2.Text = @"Empty";
        }

        // Fills the slot info
        private void Order(int slot, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            name = Clean(name);
            Console.WriteLine(name);
            _slotInfo[slot - 1].Item2.Text = @"Searching";

            Dish dish;
            if (!_meniu.TryGetValue(name, out dish))
                return;

            _slotInfo[slot - 1].Item1.Text = name;

            _slots[slot - 1].Dish = dish;
            _slots[slot - 1].Stage = 0;
        }

        // Returns a string only in quotes
        private static string Clean(string str)
        {
            //var firstMark = str.IndexOf((char)8220);
            var secondMark = str.IndexOf((char) 8221);
            if (secondMark != -1)
            {
                str = str.Substring(1, secondMark - 1);
            }
            else
            {
                var ticket = str.IndexOf((char) 41);
                if (ticket != -1)
                {
                    str = str.Substring(0, ticket + 1);
                }
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
                    var hSrcDc = gsrc.GetHdc();
                    var hDc = gdest.GetHdc();
                    BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X, location.Y, (int) CopyPixelOperation.SourceCopy);
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

            const float scale = 0.85f;
            var scaleWidth = (int) (bmpScreenshot.Width*scale);
            var scaleHeight = (int) (bmpScreenshot.Height*scale);

            var bmpScaled = new Bitmap(scaleWidth, scaleHeight);
            var graph = Graphics.FromImage(bmpScaled);

            

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
                var modiImageObj = (Image) modiObj.Images[0];

                str = modiImageObj.Layout.Text;

                modiObj.Close();
            }
            catch (Exception)
            {
                Console.WriteLine(@"Couldn't read.");
            }
            return str;
        }

        public class Dish
        {
            public string Name { get; set; }
            public int MaxStage { get; set; }
            public string[] Preparation { get; set; }
        }

        public class DishProduction
        {
            public Dish Dish { get; set; }
            public int Stage { get; set; }
            public bool Occupied { get; set; }
        }
    }
}