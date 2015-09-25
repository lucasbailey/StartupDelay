using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;
using System.Web.Script.Serialization;

namespace StartupDelay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class ProgramEntry
        {
            public int h = 0;
            public int m = 0;
            public int s = 0;

            public string path = "";
            public string name = "";

            private bool isCalled = false;
            private bool isErrored = false;

            private string ErrorMsg = "";

            public void setCall(bool val)
            {
                isCalled = val;
            }

            public bool getCall()
            {
                return isCalled;
            }

            public void setError(bool val)
            {
                isErrored = val;
            }

            public bool getError()
            {
                return isErrored;
            }

            public ProgramEntry(string _h, string _m, string _s, string _p)
            {
                initialize(Convert.ToInt16(_h), Convert.ToInt16(_m), Convert.ToInt16(_s), _p);
            }

            public ProgramEntry(string _h, string _m, string _s, string _p, string _n)
            {
                initialize(Convert.ToInt16(_h), Convert.ToInt16(_m), Convert.ToInt16(_s), _p, _n);
            }

            public ProgramEntry(int _h, int _m, int _s, string _p)
            {
                initialize(_h, _m, s, _p);
            }

            public ProgramEntry()
            {
                initialize();
            }

            private void initialize(int _h = 0, int _m = 0, int _s = 0, string _p = "", string _n = "")
            {
                h = Convert.ToInt16(_h);
                s = Convert.ToInt16(_s);
                m = Convert.ToInt16(_m);
                path = _p;
                name = _n;
            }

            public void SetErrMsg(String msg)
            {
                ErrorMsg = msg;
            }

            public string GetErrMsg()
            {
                return ErrorMsg;
            }

            public int getTotalSeconds()
            {
                return (h * 3600) + (m * 60) + s;
            }

            public void StartProgram()
            {
                try
                {
                    setCall(true);
                    System.Diagnostics.Process.Start(path);                    
                }
                catch (Exception e)
                {
                    setError(true);
                    SetErrMsg(e.Message);
                }
            }
        }

        public List<ProgramEntry> ProgramEntryList = new List<ProgramEntry>();
        int tickCounter = 0;
        bool LAUNCH_ONLY = false;
        bool NO_LAUNCH = false;

        readonly string BaseDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"StartupDelay\");

        public MainWindow()
        {
            Loaded += MyWindow_Loaded;
            InitializeComponent();
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            String[] arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].ToString().ToLower().Equals("-launchonly"))
                {
                    LAUNCH_ONLY = true;
                }
                else if (arguments[i].ToString().ToLower().Equals("-nolaunch"))
                {
                    NO_LAUNCH = true;
                }
            }

            if (LAUNCH_ONLY)
            {
                mw.Visibility = System.Windows.Visibility.Collapsed;
            }

            LoadJsonFile();

            if (!NO_LAUNCH)
            {
                StartDelays();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".exe";
            dlg.Filter = "Programs|*.exe";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                PathBox.Text = filename;
            }
        }

        private void DoDirectionClick(TextBox tbox, int direction)
        {
            Regex r = new Regex("([^0-9])");
            tbox.Text = r.Replace(tbox.Text, "");

            tbox.Text = (tbox.Text.Equals("") == true ? "0" : tbox.Text);

            int num = Convert.ToInt16(tbox.Text);
            num += direction;

            num = ((tbox.Name.ToLower().Contains("hour") && num > 23) || num > 59 ? 0 : num);
            num = (tbox.Name.ToLower().Contains("hour") && num < 0 ? 23 : num);
            num = (num < 0 ? 59 : num);

            tbox.Text = num.ToString("00");
        }

        private void HourUpButton_Click(object sender, RoutedEventArgs e)
        {
            DoDirectionClick(HourTBox, 1);
        }

        private void MinuteUpButton_Click(object sender, RoutedEventArgs e)
        {
            DoDirectionClick(MinuteTBox, 1);
        }

        private void SecondUpButton_Click(object sender, RoutedEventArgs e)
        {
            DoDirectionClick(SecondTBox, 1);
        }

        private void HourDownButton_Click(object sender, RoutedEventArgs e)
        {
            DoDirectionClick(HourTBox, -1);
        }

        private void MinuteDownButton_Click(object sender, RoutedEventArgs e)
        {
            DoDirectionClick(MinuteTBox, -1);
        }

        private void SecondDownButton_Click(object sender, RoutedEventArgs e)
        {
            DoDirectionClick(SecondTBox, -1);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ProgramEntry entry = new ProgramEntry(HourTBox.Text, MinuteTBox.Text, SecondTBox.Text, PathBox.Text, NameBox.Text);

            if (idBox.Text == "")
            {
                ProgramEntryList.Add(entry);
            }
            else
            {
                UpdateEntry(Convert.ToInt16(idBox.Text));
            }

            ResetInputs();

            SaveJsonFile();

            UpdateListDisplay();
        }

        private void UpdateEntry(int id)
        {
            ProgramEntryList[id].h = Convert.ToInt16(HourTBox.Text);
            ProgramEntryList[id].m = Convert.ToInt16(MinuteTBox.Text);
            ProgramEntryList[id].s = Convert.ToInt16(SecondTBox.Text);
            ProgramEntryList[id].path = PathBox.Text;
            ProgramEntryList[id].name = NameBox.Text;
        }

        private void SaveJsonFile()
        {
            String fileName = "StartupDelay.json";

            if (!Directory.Exists(BaseDirectory))
            {
                Directory.CreateDirectory(BaseDirectory);
            }

            if (File.Exists(BaseDirectory + fileName))
            {
                File.Delete(BaseDirectory + fileName);
            }

            JavaScriptSerializer serialize = new JavaScriptSerializer();
            FileStream theFile = File.OpenWrite(BaseDirectory + fileName);

            Byte[] theAry = Encoding.ASCII.GetBytes(serialize.Serialize(ProgramEntryList));

            theFile.Write(theAry, 0, theAry.Length);

            theFile.Close();
        }

        private void LoadJsonFile()
        {
            String fileName = "StartupDelay.json";
            JavaScriptSerializer serialize = new JavaScriptSerializer();

            if (File.Exists(BaseDirectory + fileName))
            {
                Byte[] theAry = File.ReadAllBytes(BaseDirectory + fileName);
                ProgramEntryList = serialize.Deserialize<List<ProgramEntry>>(Encoding.ASCII.GetString(theAry));

                if (ProgramEntryList == null)
                {
                    ProgramEntryList = new List<ProgramEntry>();
                }

                UpdateListDisplay();
            }
        }

        private void StartDelays()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            bool isDirty = false;

            for (int i = 0; i < ProgramEntryList.Count; i++)
            {
                ProgramEntry entry = ProgramEntryList[i];

                if (entry.getTotalSeconds() == tickCounter)
                {
                    entry.StartProgram();
                    UpdateListDisplay();
                }

                if (!entry.getCall() || entry.getError())
                {
                    isDirty = true;
                }
            }

            if (!isDirty)
            {
                System.Windows.Threading.DispatcherTimer dispatcherTimer = (System.Windows.Threading.DispatcherTimer)sender;
                dispatcherTimer.Stop();
                if (LAUNCH_ONLY)
                {
                    Application.Current.Shutdown();
                }
            }

            tickCounter++;
        }

        private void UpdateListDisplay()
        {
            int id = 0;

            /*
                < Grid >
                    < Grid.ColumnDefinitions >
                        <ColumnDefinition Width = "1*" />
                        <ColumnDefinition Width = "5*" />                            
                        < ColumnDefinition Width = "1*" />
                        < ColumnDefinition Width = ".5*" />
                        < ColumnDefinition Width = "1*" />
                        < ColumnDefinition Width = ".5*" />
                        < ColumnDefinition Width = "1*" />
                    </ Grid.ColumnDefinitions >
                    <Label Padding = "0" Margin="3" Name = "statusLabel1" Background="Black" Grid.Column = "0" ></Label>
                    <Label Padding = "0" Visibility="Collapsed" Name = "pathLabel1" Grid.Column = "1" >The Path</Label>        
                    < Label Padding = "0" Name = "pathLabel1" Grid.Column = "0" > Path 1 </ Label >
                    < Label Padding = "0" Name = "hourLabel1" Grid.Column = "1" > Hour </ Label >
                    < Label Padding = "0" Name = "sep1Label1" Grid.Column = "2" >:</ Label >
                    < Label Padding = "0" Name = "minLabel1" Grid.Column = "3" > Min </ Label >
                    < Label Padding = "0" Name = "sep2Label1" Grid.Column = "4" >:</ Label >
                    < Label Padding = "0" Name = "secLabel1" Grid.Column = "5" > Sec </ Label >
                </ Grid >
            */

            stackPanelContainer.Children.Clear();

            for (int i = 0; i < ProgramEntryList.Count; i++)
            {
                ProgramEntry entry = ProgramEntryList[i];

                Grid newGrid = new Grid();
                newGrid.Name = "ProgramEntryGrid" + id.ToString();
                newGrid.MouseDown += LoadThisItem;


                ColumnDefinition c0 = new ColumnDefinition();
                c0.Width = new GridLength(1, GridUnitType.Star);

                ColumnDefinition c1 = new ColumnDefinition();
                c1.Width = new GridLength(5, GridUnitType.Star);

                ColumnDefinition c2 = new ColumnDefinition();
                c2.Width = new GridLength(1, GridUnitType.Star);

                ColumnDefinition c3 = new ColumnDefinition();
                c3.Width = new GridLength(.5, GridUnitType.Star);

                ColumnDefinition c4 = new ColumnDefinition();
                c4.Width = new GridLength(1, GridUnitType.Star);

                ColumnDefinition c5 = new ColumnDefinition();
                c5.Width = new GridLength(.5, GridUnitType.Star);

                ColumnDefinition c6 = new ColumnDefinition();
                c6.Width = new GridLength(1, GridUnitType.Star);

                newGrid.ColumnDefinitions.Add(c0);
                newGrid.ColumnDefinitions.Add(c1);
                newGrid.ColumnDefinitions.Add(c2);
                newGrid.ColumnDefinitions.Add(c3);
                newGrid.ColumnDefinitions.Add(c4);
                newGrid.ColumnDefinitions.Add(c5);
                newGrid.ColumnDefinitions.Add(c6);

                Label newStatus = new Label();
                Label newPath = new Label();
                Label newHour = new Label();
                Label newMinute = new Label();
                Label newSecond = new Label();
                Label sep1 = new Label();
                Label sep2 = new Label();
                Label newName = new Label();

                newPath.Visibility = Visibility.Collapsed;

                newName.Padding = new System.Windows.Thickness(0);
                newStatus.Padding = new System.Windows.Thickness(0);
                newPath.Padding = new System.Windows.Thickness(0);
                newHour.Padding = new System.Windows.Thickness(0);
                sep1.Padding = new System.Windows.Thickness(0);
                newMinute.Padding = new System.Windows.Thickness(0);
                sep2.Padding = new System.Windows.Thickness(0);
                newSecond.Padding = new System.Windows.Thickness(0);

                Brush statusBrush;

                if (entry.getCall())
                {
                    statusBrush = Brushes.Green;//successfully started the program
                }
                else if (entry.getError())
                {
                    statusBrush = Brushes.Red;//failed to start the program
                }
                else
                {
                    statusBrush = Brushes.White;
                }

                newStatus.Background = statusBrush;
                newStatus.Margin = new System.Windows.Thickness(3);

                newStatus.MouseEnter += DisplayStatusTooltip;

                newPath.Content = entry.path;
                newHour.Content = entry.h;
                sep1.Content = ":";
                newMinute.Content = entry.m;
                sep2.Content = ":";
                newSecond.Content = entry.s;
                newName.Content = entry.name;

                newGrid.Children.Add(newStatus);
                newGrid.Children.Add(newPath);
                newGrid.Children.Add(newHour);
                newGrid.Children.Add(sep1);
                newGrid.Children.Add(newMinute);
                newGrid.Children.Add(sep2);
                newGrid.Children.Add(newSecond);
                newGrid.Children.Add(newName);

                stackPanelContainer.Children.Add(newGrid);

                Grid.SetColumn(newPath, 0);
                Grid.SetColumn(newPath, 1);
                Grid.SetColumn(newName, 1);
                Grid.SetColumn(newHour, 2);
                Grid.SetColumn(sep1, 3);
                Grid.SetColumn(newMinute, 4);
                Grid.SetColumn(sep2, 5);
                Grid.SetColumn(newSecond, 6);

                id++;
            }
        }

        private void DisplayStatusTooltip(Object sender, MouseEventArgs e)
        {
            //Label lbl = (Label)sender;
            //Grid parent;
            //Object tempParent;

            ////while (lbl.Parent.GetType != tempParent.GetType)
            ////{
            ////    if (parent == null) {
            ////        parent = lbl.Parent;
            ////    }
            ////}

            ////lbl.ToolTip = "message";
        }

        private void LoadThisItem(object sender, MouseButtonEventArgs e)
        {
            Grid theGrid = (Grid)sender;

            Regex r = new Regex("([^0-9])");

            //get the index number
            int i = Convert.ToInt16(r.Replace(theGrid.Name, ""));

            ProgramEntry entry = ProgramEntryList[i];

            HourTBox.Text = entry.h.ToString();
            MinuteTBox.Text = entry.m.ToString();
            SecondTBox.Text = entry.s.ToString();
            PathBox.Text = entry.path;
            NameBox.Text = entry.name;

            idBox.Text = i.ToString();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            ResetInputs();
        }

        private void ResetInputs()
        {
            HourTBox.Text = "00";
            MinuteTBox.Text = "00";
            SecondTBox.Text = "00";
            PathBox.Text = "";
            NameBox.Text = "";

            idBox.Text = "";
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (idBox.Text != "")
            {
                ProgramEntryList.RemoveAt(Convert.ToInt16(idBox.Text));

                UpdateListDisplay();
                SaveJsonFile();
                ResetInputs();
            }


        }
    }
}
