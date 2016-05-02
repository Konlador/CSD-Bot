﻿using System;
using System.Collections;
using System.Drawing;
using System.IO;
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
        public bool BotOn;
        private GlobalKeyboardHook _gHook;

        private readonly string _imagePath = AppDomain.CurrentDomain.BaseDirectory + "myBitmap.bmp";
        private readonly Dish[] _meniu = new Dish[1000];
        private int _meniuSize;
        private bool _preparing;
        private readonly Queue _queue = new Queue();
        private readonly Dish[] _slots = new Dish[10];



        public Form1()
        {
            InitializeComponent();
        }

        // Default settings
        private void Form1_Load(object sender, EventArgs e)
        {
            BotOn = true;
            _preparing = false;
            Run();

            _gHook = new GlobalKeyboardHook(); // Create a new GlobalKeyboardHook
            _gHook.KeyDown += gHook_KeyDown; // Declare a KeyDown Event
            // Add the keys you want to hook to the HookedKeys list
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                _gHook.HookedKeys.Add(key);
            }
            _gHook.hook(); // Start the keylogger

            // Empty the slot array
            for (var i = 0; i < 10; i++)
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
                /*if (slot1cords.pixel == white && Slots[1-1].Name == null) { Queue.add(1)}
                if (slot1cords.pixel == white && Slots[1 - 1].Name == null) { Queue.add(2)}
                if (slot1cords.pixel == white && Slots[1 - 1].Name == null) { Queue.add(3)} ...*/
                if (!_preparing && _queue.Count > 0)
                {
                    var nextSlot = (int)_queue.Dequeue();
                    Prepare(nextSlot);
                }
                await Task.Delay(100);
            }
        }

        // Handle the KeyDown Event
        private async void gHook_KeyDown(object sender, KeyEventArgs e)
        {
            var input = e.KeyValue;
            HistoryLog.Text += (char)input;

            await Task.Delay(10);
            /*
            if (input == 96)
            {
                var bmpScreenshot = Screenshot();
                PictureBox.Image = bmpScreenshot;
                GC.Collect();

                // Look at screen and read dish title
                bmpScreenshot.Save(_imagePath);

                if (Order(1, ReadTextFromImage(_imagePath))) _queue.Enqueue(1);
            }
            */

            if (input >= 12 && input <= 40)
            {
                bool ok = false;
                int slot = 0;
                switch (input)
                {
                    case 35:
                        ok = true;
                        slot = 1;
                        break;
                    case 40:
                        ok = true;
                        slot = 2;
                        break;
                    case 34:
                        ok = true;
                        slot = 3;
                        break;
                    case 37:
                        ok = true;
                        slot = 4;
                        break;
                    case 12:
                        ok = true;
                        slot = 5;
                        break;
                    case 39:
                        ok = true;
                        slot = 6;
                        break;
                    case 36:
                        ok = true;
                        slot = 7;
                        break;
                    case 38:
                        ok = true;
                        slot = 8;
                        break;
                    case 33:
                        ok = true;
                        slot = 9;
                        break;
                }
                if (ok)
                {
                    _queue.Enqueue(slot);
                }
            }
            //CurrentRecipeBox.Text = _slots[0].Name;
            StatusBox.Text = input.ToString();
        }

        // Look at a slot and handle it
        private async void Prepare(int slot)
        {
            var sim = new InputSimulator();
            _preparing = true;
            var end = true;
            var instructions = "";
            SendKeys.Send(slot.ToString());
            await Task.Delay(20);
            if (_slots[slot - 1].Name == null)
            {
                Order(slot, ReadTextFromImage(_imagePath));
            }
            if (_slots[slot - 1].Name != null)
            {
                _slots[slot - 1].Stage++;
                if (_slots[slot - 1].Stage > _slots[slot - 1].MaxStage)
                {
                    _slots[slot - 1].Name = null;
                    _slots[slot - 1].Stage = 0;
                    _slots[slot - 1].MaxStage = 0;
                    Console.WriteLine(@"Slot " + slot + @": Done");
                }
                else
                {
                    instructions = _slots[slot - 1].Preparation[_slots[slot - 1].Stage - 1];
                    Console.WriteLine(@"Slot " + slot + @": Preparing " + _slots[slot - 1].Name + @"|Stage " + _slots[slot - 1].Stage + @"/" + _slots[slot - 1].MaxStage + @"|");
                }

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
                                _preparing = false;
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
                            case 'D':
                                SendKeys.Send("{DOWN}");
                                break;
                            case 'H':
                                i++; var press = instructions[i]; i++;
                                var hTimeS = "";
                                while ((int)instructions[i] >= 48 && (int)instructions[i] <= 57)
                                { hTimeS += instructions[i]; i++; }
                                i--;
                                var hTime = int.Parse(hTimeS);
                                //HoldKey(press, hTime);
                                switch (press)
                                {
                                    case 'D':
                                        sim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
                                        await Task.Delay(hTime);
                                        sim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
                                        break;
                                    case 'U':
                                        sim.Keyboard.KeyDown(VirtualKeyCode.UP);
                                        await Task.Delay(hTime);
                                        sim.Keyboard.KeyUp(VirtualKeyCode.UP);
                                        break;
                                    case 'L':
                                        sim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
                                        await Task.Delay(hTime);
                                        sim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
                                        break;
                                    case 'R':
                                        sim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
                                        await Task.Delay(hTime);
                                        sim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
                                        break;
                                }
                                break;
                            case 'W':
                                end = false;
                                var time = int.Parse(instructions.Substring(i + 1, instructions.Length - i - 1));
                                await Task.Delay(time);
                                _queue.Enqueue(slot);
                                i = instructions.Length;
                                break;
                        }
                    }
                }
                if (end)
                {
                    _slots[slot - 1].Name = null;
                    _slots[slot - 1].Stage = 0;
                    _slots[slot - 1].MaxStage = 0;
                    Console.WriteLine(@"Slot " + slot + @": Done");
                }
            }

            _preparing = false;
        }

        // Fills the slot info
        private bool Order(int slot, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            name = Clean(name);
            var found = false;
            for (var i = 0; i < _meniuSize; i++)
            {
                if (_meniu[i].Name != name) continue;
                found = true;
                _slots[slot - 1].Name = name;
                for (var a = 0; a < _meniu[i].MaxStage; a++)
                {
                    _slots[slot - 1].Preparation[a] = _meniu[i].Preparation[a];
                }
                _slots[slot - 1].Stage = 0;
                _slots[slot - 1].MaxStage = _meniu[i].MaxStage;
                break;
            }
            return found;
        }

        // Returns a string only in quotes
        private static string Clean(string str)
        {
            var ticket = str.IndexOf((char)41);
            var firstMark = str.IndexOf((char)8220);
            var secondMark = str.IndexOf((char)8221);
            if (firstMark != -1 && secondMark != -1)
            {
                str = str.Substring(firstMark + 1, secondMark - firstMark - 1);
            }
            else if (ticket != -1)
            {
                str = str.Substring(0, ticket);
            }
            return str;
        }

        // Take a screenshot from primary screen
        private static Bitmap Screenshot()
        {
            // s = Screen.PrimaryScreen.Bounds.Size
            // Pic - 515, 40
            //Size S = new Size(515, 40);
            // Screen - 550, 55
            var s = new Size(800, 55);

            // This is there we will store a snapshot of the screen
            var bmpScreenshot = new Bitmap(s.Width, s.Height);
            var g = Graphics.FromImage(bmpScreenshot);
            // Copy from screen into he bitmap we created

            // Pic - 485, 695
            //g.CopyFromScreen(485, 695, 0, 0, S);
            // Screem - 365, 780
            g.CopyFromScreen(365, 780, 0, 0, s);

            return bmpScreenshot;
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
                //throw new Exception(ex.Message);
            }
            return str;
        }

        public struct Dish
        {
            public string Name;
            public int Stage, MaxStage;
            public string[] Preparation;
        }
    }
}