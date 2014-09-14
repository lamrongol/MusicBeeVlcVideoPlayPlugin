using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MusicBeePlugin
{
    public class Settings
    {
    private string vlcPath;
    private bool isFullScreen;

    public string VlcPath
    {
        get {return vlcPath;}
        set {vlcPath = value;}
    }
    public bool IsFullScreen
    {
        get {return isFullScreen;}
        set {isFullScreen = value;}
    }

    public Settings()
    {
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
            string datafile = dataPath + settingFileName;
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
            catch (Exception e)
            {
                //Set Default Setting
                if (String.IsNullOrEmpty(vlcPath)) vlcPath = getDefaultVlcPath() + @"\vlc.exe";
            }
            if (!File.Exists(vlcPath)) vlcPath = "";

            return about;
        }

        private string getDefaultVlcPath()
        {
            String programFiles = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            return programFiles + @"\VideoLAN\VLC";
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
//                vlcFileSelectButton.AutoSize = true;
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
        private const String settingFileName =  SETTING_SUB_FOLDER+ @"\VlcVideoPlayPlugin.xml";


         private void VlcFileSelectButton_Clicked(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = getDefaultVlcPath();
            openFileDialog1.FileName = "vlc.exe";


            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき
                //選択されたファイル名を表示する
                vlcFilePathTextBox.Text = openFileDialog1.FileName;
            }
        }
       
        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            if(vlcFilePathTextBox!=null) vlcPath = vlcFilePathTextBox.Text;
            if (fullScreenCheckBox != null) isFullScreen = fullScreenCheckBox.Checked;

            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            string datafile = dataPath+settingFileName;

            try{
                System.Xml.Serialization.XmlSerializer serializer1 =
                    new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                string subFolder =dataPath+ SETTING_SUB_FOLDER;
                if(!System.IO.File.Exists(subFolder)){
                    System.IO.Directory.CreateDirectory(subFolder);
                 }

                
                //Open file
                System.IO.FileStream fs1 =
                    new System.IO.FileStream(datafile, System.IO.FileMode.Create);
                //Selialkize and save to xml file
                serializer1.Serialize(fs1, new Settings(vlcPath, isFullScreen));
                fs1.Close();
            }
            catch (Exception ignored)
            {
 //               MessageBox.Show(ignored.Message);
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
        private HashSet<int> alreadyPlayedIdx = new HashSet<int>();
        private int trackCount = 0;
        private int musicCount = 0;
        private int videoCount = 0;
        private int alreadyPlayedMusicCount = 0;
        private int alreadyPlayedVideoCount = 0;
        List<String> unplayedVideoList = null;
        List<String> trackList = null;
        private TimeSpan duration;
        private DateTime start;
        private bool playVideoNext = false;
        private PlayState prePlayState;
        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                /*
                case NotificationType.NowPlayingListChanged:
                    loadNowPlayingList();
                    break;
                */
                /*
                case NotificationType.RatingChanged:
                    string ratingStr = mbApiInterface.Library_GetFileTag(sourceFileUrl, MetaDataType.Rating);

                    MessageBox.Show(sourceFileUrl + " " + ratingStr);

                    double rating = double.Parse(ratingStr);

                    TagLib.Id3v2.Tag.DefaultVersion = 3;
                    TagLib.Id3v2.Tag.ForceDefaultVersion = true;

                    TagLib.File file = TagLib.File.Create(sourceFileUrl);

                    TagLib.Tag Tag = file.GetTag(TagTypes.Id3v2);
                    TagLib.Id3v2.PopularimeterFrame frame = TagLib.Id3v2.PopularimeterFrame.Get((TagLib.Id3v2.Tag)Tag, "WindowsUser", true);
                    frame.Rating = (byte)(255 * rating/5.0);
                    file.Save();
                    MessageBox.Show(sourceFileUrl + " " + ratingStr);
                    break;
                */
                case NotificationType.TrackChanged:
//                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
//                    MessageBox.Show(sourceFileUrl);

                    if (!isVideo(sourceFileUrl))
                    {
                        stopCurrentVlc();
                        alreadyPlayedMusicCount++;

                        return;
                    }

                    break;
                case NotificationType.PlayStateChanged:
                    //MessageBox.Show("PlayStateChanged:" + mbApiInterface.Player_GetPlayState());
                    //Only when stop event occurs after video play started(and stop event occured)
                    if (prePlayState == PlayState.Stopped && mbApiInterface.Player_GetPlayState() == PlayState.Stopped) stopCurrentVlc();
                    prePlayState = mbApiInterface.Player_GetPlayState();
                    // MessageBox.Show(mbApiInterface.Player_GetPlayState() + "");
                    break;
            }
        }

        private void stopCurrentVlc()
        {
            if (vlcProcess != null && !vlcProcess.HasExited)
            {
                vlcProcess.Exited -= new EventHandler(vlcProcess_Exited);
                vlcProcess.CloseMainWindow();
            }

        }

        
        public bool PlayVideo(string[] urls)
        {
            if (String.IsNullOrEmpty(vlcPath))
            {
                MessageBox.Show("Video Continuous Play plugin: VLC path is not set");
                mbApiInterface.Player_PlayNextTrack();
                return false;
            }
//            MessageBox.Show("playVideo:" + urls[0]);
            stopCurrentVlc();

            alreadyPlayedVideoCount++;

            String fileUrl = urls[0];

//            int currentDuration = mbApiInterface.NowPlaying_GetDuration();
//            MessageBox.Show(currentDuration+"");

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
            */

            string vlcCommand = "--rate=1.0 --play-and-exit ";
            if (isFullScreen) vlcCommand += " --fullscreen ";
            vlcCommand += " \"" + fileUrl + "\"";

            vlcProcess = new System.Diagnostics.Process();
            vlcProcess.StartInfo.FileName = vlcPath;
            vlcProcess.StartInfo.Arguments = vlcCommand;
            //vlcProcess.SynchronizingObject = this;
            vlcProcess.Exited += new EventHandler(vlcProcess_Exited);
            vlcProcess.EnableRaisingEvents = true;
            start = DateTime.Now;
            vlcProcess.Start();
            return true;
        }

        private void vlcProcess_Exited(object sender, EventArgs e)
        {
            if (mbApiInterface.Player_GetStopAfterCurrentEnabled())
            {
                mbApiInterface.Player_StopAfterCurrent();
                return;
            }
            /*
            if (mbApiInterface.Player_GetPlayState() != PlayState.Playing)
            {
                return;
            }
            */
            /*
            DateTime end = DateTime.Now;
            TimeSpan ts = end - start;
            //MessageBox.Show(duration.TotalSeconds + " " + ts.TotalSeconds);
            //再生時間より遙かに早く終了した倍は強制的に停止されたと考え、再生の停止を行う
            if (duration > TimeSpan.Zero && (duration - ts) > TimeSpan.FromSeconds(10)) return;
            */

            mbApiInterface.Player_PlayNextTrack();
        }

        private HashSet<String> VIDEO_EXT_SET = new HashSet<string>() { "mp4", "mov", "flv", "avi", "mka", "mpg", "mpeg", "wmv", "rm" };

        private bool isVideo(String fileUrl)
        {
            string ext = Path.GetExtension(fileUrl);
            ext = ext.Substring(1).ToLower();
            return VIDEO_EXT_SET.Contains(ext);
        }



        private void queueVideoFileToNext()
        {
            if (unplayedVideoList.Count == 0) return;

            Random randomizer = new Random();
            string[] asArray = unplayedVideoList.ToArray();
            string nextVideoFile = asArray[randomizer.Next(asArray.Length)];

            unplayedVideoList.Remove(nextVideoFile);
           
        }

        private void loadNowPlayingList()
        {
            mbApiInterface.NowPlayingList_QueryFiles(null);
            trackList = new List<String>();
            unplayedVideoList = new List<String>();

            musicCount = 0;
            videoCount = 0;

            while (true)
            {
                string playListTrack = mbApiInterface.NowPlayingList_QueryGetNextFile();
                if (String.IsNullOrEmpty(playListTrack))
                {
                    break;
                }

                if (isVideo(playListTrack))
                {
                    videoCount++;
                    unplayedVideoList.Add(playListTrack);
                }
                else musicCount++;

                trackList.Add(playListTrack);
            }
            //       MessageBox.Show(musicCount + " " + videoCount);
            alreadyPlayedVideoCount = 0;
            alreadyPlayedMusicCount = 0;

            trackCount = trackList.Count;

            //Only Video
            if (musicCount == 0)
            {
                queueVideoFileToNext();
                return;
            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        {
            return null;
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            //Return Convert.ToBase64String(artworkBinaryData)
            return null;
        }
    }
}