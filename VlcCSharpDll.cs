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
            about.Author = "Fujikawa Tomohide";
            about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            about.Type = PluginType.General;
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
            catch (Exception ignored)
            {
//                MessageBox.Show(ignored.Message);
            }

            return about;
        }

        private string vlcPath;
        private bool isFullScreen;

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
                System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox();
                textBox.Text = vlcPath;
                textBox.TextChanged += new EventHandler(TextBox_Changed);
                textBox.Bounds = new Rectangle(60, 0, 250, textBox.Height);

                CheckBox fullScreenCheckBox = new CheckBox();
                fullScreenCheckBox.Checked = isFullScreen;
                fullScreenCheckBox.CheckedChanged += new EventHandler(FullScreenCheckBox_Changed);
                fullScreenCheckBox.Text = "FullScreen";
                fullScreenCheckBox.Location = new Point(0, textBox.Height + 5);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox, fullScreenCheckBox });
            }
            return false;
        }
        private const string SETTING_SUB_FOLDER = @"\mb_VlcVideoPlay";
        private const String settingFileName =  SETTING_SUB_FOLDER+ @"\VlcVideoPlayPlugin.xml";


        private String tempVlcPath;
        private bool tempFullScreen;
        private void TextBox_Changed(object sender, EventArgs e)
        {
            tempVlcPath = (sender as TextBox).Text;
        }
        private void FullScreenCheckBox_Changed(object sender, EventArgs e)
        {
            tempFullScreen = (sender as CheckBox).Checked;
        }
       
        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {

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
                serializer1.Serialize(fs1, new Settings(tempVlcPath, tempFullScreen));
                vlcPath = tempVlcPath;
                isFullScreen = tempFullScreen;
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
        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.NowPlayingListChanged:
                    loadNowPlayingList();
                    break;
                case NotificationType.RatingChanged:
                    /*
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
          */
                    break;
                case NotificationType.PlayCountersChanged:
//                    MessageBox.Show("pc:"+sourceFileUrl);
                    break;
                case NotificationType.TrackChanging:
//                    MessageBox.Show("tc:"+sourceFileUrl);
                    break;
                case NotificationType.TrackChanged:
//                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
//                    MessageBox.Show(sourceFileUrl);
                    if (playVideoNext)
                    {
                        playVideoNext = false;
                        if (mbApiInterface.Player_GetPlayState() == PlayState.Playing) mbApiInterface.Player_PlayPause();
                        queueVideoFileToNext();
                        return;
                    }
//                    MessageBox.Show(vlcPath);
                    
                    if (trackList == null) loadNowPlayingList();

                    playVideoNext = false;

                    if (vlcProcess != null && !vlcProcess.HasExited)
                    {
                        vlcProcess.Exited -= new EventHandler(vlcProcess_Exited);
                        vlcProcess.CloseMainWindow();
                    }


                    string fileUrl = sourceFileUrl;//mbApiInterface.NowPlaying_GetFileUrl();
      //              MessageBox.Show(fileUrl);
                    if (isVideo(fileUrl))
                    {
                        alreadyPlayedVideoCount++;
//                        unplayedVideoList.Remove(fileUrl);
                    }
                    else alreadyPlayedMusicCount++;

             //       MessageBox.Show(mbApiInterface.NowPlayingList_GetNextIndex(1)+" "+musicCount);
                    //Add video to playlist in appropriate rate
                    //because videos are not included to playlist under normal conditions,
                    if (!String.IsNullOrEmpty(vlcPath) && mbApiInterface.NowPlayingList_GetNextIndex(1) == -1 && videoCount > 0 && musicCount > 0)
                    {
                       // MessageBox.Show(alreadyPlayedVideoCount / (alreadyPlayedMusicCount + 0.0) + " " + videoCount / (musicCount + 0.0));
                        if (alreadyPlayedMusicCount >= musicCount / (videoCount + 0.0) * (alreadyPlayedVideoCount+1))
                        {
                           // if (start == null) start = DateTime.Now;
                            int currentDuration = mbApiInterface.NowPlaying_GetDuration();
                      //      mbApiInterface.Player_StopAfterCurrent();
                            playVideoNext = true;
                        }
                    }

                    break;
            }
        }

        private void queueVideoFileToNext()
        {
            if (unplayedVideoList.Count == 0) return;

            Random randomizer = new Random();
            string[] asArray = unplayedVideoList.ToArray();
            string nextVideoFile = asArray[randomizer.Next(asArray.Length)];

            unplayedVideoList.Remove(nextVideoFile);

            string durationStr = mbApiInterface.Library_GetFileProperty(nextVideoFile, FilePropertyType.Duration);
            string[] durationArray = durationStr.Split(':');
            int durationInt = 0;
            foreach (String str in durationArray)
            {
                durationInt *= 60;
                durationInt += int.Parse(str);
            }
            duration = TimeSpan.FromSeconds(durationInt);

            if (!String.IsNullOrEmpty(vlcPath))
            {
                string vlcCommand = "--rate=1.0 --play-and-exit ";
                if (isFullScreen) vlcCommand += " --fullscreen ";
                vlcCommand += " \"" + nextVideoFile + "\"";

                vlcProcess = new System.Diagnostics.Process();
                vlcProcess.StartInfo.FileName = vlcPath;
                vlcProcess.StartInfo.Arguments = vlcCommand;
                //vlcProcess.SynchronizingObject = this;
                vlcProcess.Exited += new EventHandler(vlcProcess_Exited);
                vlcProcess.EnableRaisingEvents = true;
                start = DateTime.Now;
                vlcProcess.Start();
            }
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
        private HashSet<String> VIDEO_EXT_SET = new HashSet<string>() { "mp4", "mov", "flv", "avi", "mka", "mpg", "mpeg", "wmv", "rm" };

        private bool isVideo(String fileUrl)
        {
            string ext = Path.GetExtension(fileUrl);
            ext = ext.Substring(1);

            return VIDEO_EXT_SET.Contains(ext);
        }


        private void vlcProcess_Exited(object sender, EventArgs e)
        {
            DateTime end = DateTime.Now;
            TimeSpan ts = end - start;
            //MessageBox.Show(duration.TotalSeconds + " " + ts.TotalSeconds);
            //再生時間より遙かに早く終了した倍は強制的に停止されたと考え、再生の停止を行う
            if ((duration - ts) > TimeSpan.FromSeconds(10)) return;
            //MessageBox.Show(mbApiInterface.Player_GetPlayState() + "");
                
            //Videoファイルのみの場合動作が異なるため
            if (musicCount == 0)
            {
                queueVideoFileToNext();
                return;
            }
            else
            {
/*                if (!isVideo(mbApiInterface.NowPlaying_GetFileUrl()))
                {
                    if (mbApiInterface.Player_GetPlayState() != PlayState.Stopped) mbApiInterface.Player_PlayPause();
                    return;
                }
*/
                mbApiInterface.Player_PlayPause();
            }

//            int nextIdx = mbApiInterface.NowPlayingList_GetNextIndex(1);
            //MessageBox.Show("" + nextIdx);
//            if (nextIdx == -1) nextIdx = new System.Random().Next(trackList.Count);

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