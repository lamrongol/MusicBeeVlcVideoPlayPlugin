using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MusicBeePlugin
{
    public class Settings
    {
        private string vlcPath;
        private bool isFullScreen;

        public string VlcPath
        {
            get { return vlcPath; }
            set { vlcPath = value; }
        }
        public bool IsFullScreen
        {
            get { return isFullScreen; }
            set { isFullScreen = value; }
        }

        public Settings(String vlcPath, bool isFullScreen)
        {
            this.vlcPath = vlcPath;
            this.isFullScreen = isFullScreen;
        }
    }

    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Video Continuous Play";
            about.Description = "Play music and music video continuously";
            about.Author = "Tomohide Fujikawa";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.VideoPlayer; 
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 100;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            string datafile = dataPath + SETTING_FILE_NAME;
            try
            {
                //＜Read from XML file＞
                //Make XmlSerializer object
                System.Xml.Serialization.XmlSerializer serializer2 =
                    new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                //Open file
                System.IO.FileStream fs2 =
                    new System.IO.FileStream(datafile, System.IO.FileMode.Open);
                //unserialize from xml file
                Settings appSettings =
                    (Settings)serializer2.Deserialize(fs2);
                vlcPath = appSettings.VlcPath;

                isFullScreen = appSettings.IsFullScreen;
                fs2.Close();
            }
            catch (Exception)
            {
                //Set Default Setting
                if (String.IsNullOrEmpty(vlcPath)) vlcPath = GetDefaultVlcPath() + @"\vlc.exe";
            }
            if (!File.Exists(vlcPath)) vlcPath = "";

            return about;
        }

        private string GetDefaultVlcPath()
        {
            String programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            String path = programFiles + @"\VideoLAN\VLC";
            if (!Directory.Exists(path))
            {
                path = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\VideoLAN\VLC";
            }
            return path;
        }

        private string vlcPath;
        private bool isFullScreen;
        private Button vlcFileSelectButton;
        private System.Windows.Forms.TextBox vlcFilePathTextBox;
        private CheckBox fullScreenCheckBox;

        public bool Configure(IntPtr panelHandle)
        {
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "Vlc Path:";
                vlcFilePathTextBox = new System.Windows.Forms.TextBox();
                vlcFilePathTextBox.Text = vlcPath;
                vlcFilePathTextBox.Bounds = new Rectangle(60, 0, 300, vlcFilePathTextBox.Height);

                vlcFileSelectButton = new Button();
                //vlcFileSelectButton.AutoSize = true;
                vlcFileSelectButton.Text = "..";
                vlcFileSelectButton.Bounds = new Rectangle(vlcFilePathTextBox.Right + 2, vlcFilePathTextBox.Top, 30, vlcFilePathTextBox.Height);
                vlcFileSelectButton.Click += new EventHandler(VlcFileSelectButton_Clicked);

                fullScreenCheckBox = new CheckBox();
                fullScreenCheckBox.Checked = isFullScreen;
                fullScreenCheckBox.Text = "FullScreen";
                fullScreenCheckBox.Location = new Point(0, vlcFilePathTextBox.Height + 5);
                configPanel.Controls.AddRange(new Control[] { prompt, vlcFilePathTextBox, fullScreenCheckBox, vlcFileSelectButton });
            }
            return false;
        }
        private const string SETTING_SUB_FOLDER = @"\mb_VlcVideoPlay";
        private const string SETTING_FILE_NAME = SETTING_SUB_FOLDER + @"\VlcVideoPlayPlugin.xml";


        private void VlcFileSelectButton_Clicked(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = GetDefaultVlcPath();
            openFileDialog1.FileName = "vlc.exe";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //When the OK button is clicked, showing a selected file name.
                vlcFilePathTextBox.Text = openFileDialog1.FileName;
            }
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            if (vlcFilePathTextBox != null) vlcPath = vlcFilePathTextBox.Text;
            if (fullScreenCheckBox != null) isFullScreen = fullScreenCheckBox.Checked;

            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            string datafile = dataPath + SETTING_FILE_NAME;

            try
            {
                System.Xml.Serialization.XmlSerializer serializer1 =
                    new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                string subFolder = dataPath + SETTING_SUB_FOLDER;
                if (!System.IO.File.Exists(subFolder))
                {
                    System.IO.Directory.CreateDirectory(subFolder);
                }


                //Open file
                System.IO.FileStream fs1 =
                    new System.IO.FileStream(datafile, System.IO.FileMode.Create);
                //Selialkize and save to xml file
                serializer1.Serialize(fs1, new Settings(vlcPath, isFullScreen));
                fs1.Close();
            }
            catch (Exception)
            {
                //MessageBox.Show(ignored.Message);
            }

        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        private System.Diagnostics.Process vlcProcess;
        private PlayState prePlayState;
        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.TrackChanged:
                    if (!IsVideo(sourceFileUrl))
                    {
                        StopCurrentVlc();
                    }
                    break;
                case NotificationType.PlayStateChanged:
                    //Only when stop event occurs after video play started(and stop event occured)
                    if (prePlayState == PlayState.Stopped && mbApiInterface.Player_GetPlayState() == PlayState.Stopped) StopCurrentVlc();
                    prePlayState = mbApiInterface.Player_GetPlayState();
                    break;
            }
        }

        private void StopCurrentVlc()
        {
            if (vlcProcess != null && !vlcProcess.HasExited)
            {
                vlcProcess.Exited -= new EventHandler(VlcProcess_Exited);
                vlcProcess.CloseMainWindow();
            }

        }

        //private TimeSpan duration;
        //private DateTime start;
        public bool PlayVideo(string[] urls)
        {
            if (String.IsNullOrEmpty(vlcPath))
            {
                MessageBox.Show("Video Continuous Play plugin: VLC path is not set");
                //mbApiInterface.Player_PlayNextTrack();
                return false;
            }
            StopCurrentVlc();
            String fileUrl = urls[0];

            //start = DateTime.Now;
            //Dont work for some files.
            /*
                        int currentDuration = mbApiInterface.NowPlaying_GetDuration();
                        MessageBox.Show(currentDuration+"");
            */
            //Dont work for some files.
            /*
                        string durationStr = mbApiInterface.Library_GetFileProperty(fileUrl, FilePropertyType.Duration);
                        string[] durationArray = durationStr.Split(':');
                        int durationInt = 0;
                        try
                        {
                            foreach (String str in durationArray)
                            {
                                durationInt *= 60;
                                durationInt += int.Parse(str);
                            }
                            duration = TimeSpan.FromSeconds(durationInt);
                        }
                        catch
                        {
                            duration = TimeSpan.Zero;
                        }
                        MessageBox.Show(duration + "");
            */

            string vlcCommand = "--rate=1.0 --play-and-exit --video-on-top ";
            if (isFullScreen) vlcCommand += " --fullscreen ";
            vlcCommand += " \"" + fileUrl + "\"";

            IntPtr currentWindow = GetForegroundWindow();
            vlcProcess = new System.Diagnostics.Process();
            vlcProcess.StartInfo.FileName = vlcPath;
            vlcProcess.StartInfo.Arguments = vlcCommand;
            vlcProcess.Exited += new EventHandler(VlcProcess_Exited);
            vlcProcess.EnableRaisingEvents = true;
            vlcProcess.Start();
            vlcProcess.WaitForInputIdle();
            string strTitleContains = Path.GetFileName(fileUrl);
            ReactivateCurrentWindow(vlcProcess, strTitleContains, currentWindow);

            return true;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static async void ReactivateCurrentWindow(Process newProcess, string strTitleContains, IntPtr currentWindow)
        {
            for(int i = 0; i<100; i++)
            {
                newProcess.Refresh();
                //Console.WriteLine("MainWindowHandle:" + newProcess.MainWindowHandle);
                if (!newProcess.MainWindowHandle.Equals(IntPtr.Zero))
                {
                    if (newProcess.MainWindowTitle.Contains(strTitleContains))
                    {
                        SetForegroundWindow(currentWindow);
                        //Console.WriteLine("Title:" + newProcess.MainWindowTitle);
                        return;
                    }
                }
                await Task.Delay(10);
            }
        }

        private void VlcProcess_Exited(object sender, EventArgs e)
        {
            if (mbApiInterface.Player_GetStopAfterCurrentEnabled())
            {
                mbApiInterface.Player_StopAfterCurrent();
                return;
            }
            //Can't be used unlesss duration can be calculated.
            /*
            DateTime end = DateTime.Now;
            TimeSpan ts = end - start;
            //MessageBox.Show(duration.TotalSeconds + " " + ts.TotalSeconds);
            //Stop playback when VLC is exited much earlier than the video playback time because assuming that it was forcibly stopped.
            if (duration > TimeSpan.Zero && (duration - ts) > TimeSpan.FromSeconds(10)) return;
            */

            mbApiInterface.Player_PlayNextTrack();
        }

        //Video File Formats <http://www.fileinfo.com/filetypes/video>
        private HashSet<String> VIDEO_EXT_SET = new HashSet<string>() { "aep", "rms", "dzm", "wpl", "veg", "sfd", "psh", "wp3", "mpeg", "piv", "scm", "dir", "trp", "swf", "bik", "otrkey", "webm", "3gp2", "bdmv", "dzt", "fcp", "gfp", "m21", "mvp", "nvc", "rdb", "rec", "rmp", "rv", "screenflow", "swt", "usm", "vc1", "vcpf", "viewlet", "wvx", "vob", "mswmm", "wlmp", "avi", "srt", "mkv", "3gp", "ts", "wmv", "m2p", "vro", "msdvd", "fbr", "dzp", "mp4infovid", "asf", "m4v", "aepx", "mani", "mnv", "mproj", "sbk", "bu", "kmv", "bin", "swi", "meta", "mts", "amx", "prproj", "r3d", "ifo", "mpg", "hdmov", "pds", "amc", "tp", "wmd", "wmx", "mmv", "mob", "vp3", "mp4", "3g2", "lrv", "scc", "bnp", "dv4", "mov", "stx", "xvid", "yuv", "890", "avchd", "dmx", "roq", "wve", "3mm", "dnc", "f4f", "inp", "ivf", "k3g", "lsx", "lvix", "moff", "qt", "spl", "vcr", "wm", "f4v", "dvr", "dat", "cpi", "ogv", "trec", "vgz", "dxr", "flv", "dcr", "m2t", "pmf", "camproj", "dvdmedia", "fcproject", "ism", "ismv", "tix", "clpi", "f4p", "fli", "hdv", "rsx", "dav", "m15", "rmvb", "vp6", "str", "video", "264", "bdm", "divx", "3gpp", "mvp", "smv", "gvi", "mpeg4", "mod", "aetx", "playlist", "dcr", "rm", "sfera", "h264", "ajp", "vpj", "ale", "avp", "bsf", "dash", "dmsm", "dream", "imovieproj", "smil", "3p2", "aaf", "arcut", "avb", "avv", "bdt3", "bmc", "ced", "cine", "cip", "cmmp", "cmmtpl", "cmrec", "cst", "d2v", "d3v", "dce", "dck", "dmsd", "dmss", "dpa", "eyetv", "fbz", "ffm", "flc", "flh", "fpdx", "ftc", "gcs", "gifv", "gts", "hkm", "imoviemobile", "imovieproject", "ircp", "ismc", "ivr", "izz", "izzy", "jss", "jts", "jtv", "kdenlive", "m1pg", "m21", "m2ts", "m2v", "mgv", "mj2", "mk3d", "mp21", "mpgindex", "mpls", "mpv", "mse", "mtv", "mvd", "mve", "mvy", "mxv", "ncor", "nsv", "nuv", "ogm", "ogx", "pac", "photoshow", "plproj", "ppj", "pro", "prtl", "pxv", "qtl", "qtz", "rcd", "rum", "rvid", "rvl", "sdv", "sedprj", "seq", "sfvidcap", "siv", "smi", "smk", "stl", "svi", "tda3mt", "thp", "tivo", "tod", "tp0", "tpd", "tpr", "tsp", "ttxt", "tvlayer", "tvshow", "usf", "vbc", "vcv", "vdo", "vdr", "vfz", "vlab", "vsp", "wcp", "wmmp", "xej", "xesc", "xfl", "xlmv", "y4m", "zm1", "zm2", "zm3", "lrec", "mp4v", "mpe", "mys", "aqt", "gom", "orv", "ssm", "zeg", "camrec", "mxf", "zmv", "aec", "box", "dpg", "tvs", "vep", "db2", "arf", "moi", "rcproject", "vf", "60d", "vid", "dvr-ms", "bmk", "edl", "snagproj", "sqz", "dv", "dv-avi", "eye", "mp21", "pgi", "rmd", "avs", "int", "mp2v", "scn", "tdt", "ismclip", "m4e", "mpl", "avs", "evo", "smi", "vivo", "asx", "movie", "irf", "axm", "cmproj", "dmsd3d", "dvx", "ezt", "mjp", "mqv", "prel", "vp7", "xel", "aet", "anx", "avc", "avd", "awlive", "axv", "bdt2", "bs4", "bvr", "byu", "camv", "clk", "cx3", "ddat", "dlx", "dmb", "dmsm3d", "fbr", "ffd", "flx", "gvp", "imovielibrary", "iva", "jmv", "ktn", "m1v", "m2a", "m4u", "mjpg", "mpsub", "mvc", "mvex", "osp", "par", "pns", "pro4dvd", "pro5dvd", "proqc", "pssd", "pva", "qtch", "qtindex", "qtm", "rp", "rts", "sbt", "sml", "theater", "tid", "tvrecording", "vem", "vfw", "vix", "vs4", "vse", "w32", "wot", "yog", "787", "ssf", "mpg2", "wtv", "amv", "mpl", "xmv", "dif", "modd", "vft", "vmlt", "grasp", "3gpp2", "moov", "pvr", "vmlf", "am", "anim", "bix", "cel", "cvc", "dsy", "gl", "ivs", "lsf", "m75", "mpeg1", "mpf", "mpv2", "msh", "mvb", "nut", "pjs", "pmv", "psb", "rmd", "rmv", "rts", "scm", "sec", "tdx", "vdx", "viv" };

        private bool IsVideo(String fileUrl)
        {
            string ext = Path.GetExtension(fileUrl);
            ext = ext.Substring(1).ToLower();
            return VIDEO_EXT_SET.Contains(ext);
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        //public string[] GetProviders()
        //{
        //    return null;
        //}

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        //public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        //{
        //    return null;
        //}

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        //public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        //{
        //    //Return Convert.ToBase64String(artworkBinaryData)
        //    return null;
        //}

        //  presence of this function indicates to MusicBee that this plugin has a dockable panel. MusicBee will create the control and pass it as the panel parameter
        //  you can add your own controls to the panel if needed
        //  you can control the scrollable area of the panel using the mbApiInterface.MB_SetPanelScrollableArea function
        //  to set a MusicBee header for the panel, set about.TargetApplication in the Initialise function above to the panel header text
        //public int OnDockablePanelCreated(Control panel)
        //{
        //  //    return the height of the panel and perform any initialisation here
        //  //    MusicBee will call panel.Dispose() when the user removes this panel from the layout configuration
        //  //    < 0 indicates to MusicBee this control is resizable and should be sized to fill the panel it is docked to in MusicBee
        //  //    = 0 indicates to MusicBee this control resizeable
        //  //    > 0 indicates to MusicBee the fixed height for the control.Note it is recommended you scale the height for high DPI screens(create a graphics object and get the DpiY value)
        //    float dpiScaling = 0;
        //    using (Graphics g = panel.CreateGraphics())
        //    {
        //        dpiScaling = g.DpiY / 96f;
        //    }
        //    panel.Paint += panel_Paint;
        //    return Convert.ToInt32(100 * dpiScaling);
        //}

        // presence of this function indicates to MusicBee that the dockable panel created above will show menu items when the panel header is clicked
        // return the list of ToolStripMenuItems that will be displayed
        //public List<ToolStripItem> GetHeaderMenuItems()
        //{
        //    List<ToolStripItem> list = new List<ToolStripItem>();
        //    list.Add(new ToolStripMenuItem("A menu item"));
        //    return list;
        //}

        //private void panel_Paint(object sender, PaintEventArgs e)
        //{
        //    e.Graphics.Clear(Color.Red);
        //    TextRenderer.DrawText(e.Graphics, "hello", SystemFonts.CaptionFont, new Point(10, 10), Color.Blue);
        //}

    }
}