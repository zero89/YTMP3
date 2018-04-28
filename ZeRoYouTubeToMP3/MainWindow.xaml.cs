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
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;
using System.IO;
using System.Threading;
using Id3;

namespace ZeRoYouTubeToMP3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_browse_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                input_outputfolder.Text = dialog.SelectedPath;
            }

        }

        private void button_convert_Click(object sender, RoutedEventArgs e)
        {

            string videoURL     = input_video.Text;
            string outDir       = input_outputfolder.Text;

            if(string.IsNullOrEmpty(videoURL))
            {
                MessageBox.Show("Please enter a video URL", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(outDir))
            {
                MessageBox.Show("Please enter a output directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if(!videoURL.Contains("youtube.com"))
            {
                MessageBox.Show("This application only accepts Youtube links", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            progress.Visibility = Visibility.Visible;
            progress.IsIndeterminate = true;
            button_convert.IsEnabled = false;
            button_browse.IsEnabled = false;
            input_video.IsEnabled = false;
            input_outputfolder.IsEnabled = false;

            Thread convertThread = new Thread(new ThreadStart(() =>
            {
                ConvertVideo(videoURL, outDir);
            }));

            convertThread.Start();

        }

        private async void ConvertVideo(string url, string outdir)
        {
            if (!Directory.Exists(outdir))
            {
                MessageBox.Show("Directory: " + outdir + " does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {

                YouTube _youtube        = YouTube.Default;
                Video requestedVideo    = await _youtube.GetVideoAsync(url);

                File.WriteAllBytes(string.Format("{0}\\convert.tmp", outdir), await requestedVideo.GetBytesAsync());

                string inputFile        = string.Format("{0}\\convert.tmp", outdir);
                string outFileDir       = string.Format("{0}\\{1}.mp3", outdir, GetFileName(requestedVideo.FullName));

                outFileDir = outFileDir.Replace(".mp4", "");
                outFileDir = outFileDir.Replace("YouTube", "");

                MediaFile Src = new MediaFile { Filename = inputFile };
                MediaFile Dst = new MediaFile { Filename = outFileDir };

                Engine converter = new Engine();
                converter.GetMetadata(Src);
                converter.Convert(Src, Dst);

                //Add ID3 Tags if we can.
                if (requestedVideo.FullName.Contains("-"))
                {
                    string fullname = requestedVideo.FullName;
                    fullname = fullname.Replace(".mp4", "");
                    fullname = fullname.Replace("YouTube", "");
                    string[] details = fullname.Split('-');
                    string artist = null;
                    string title = null;
                    bool doTag = false;

                    if (details[0] != null)
                    {
                        artist = details[0];
                    }

                    if (details[1] != null)
                    {
                        title = details[1];
                    }

                    if (artist == null || title == null)
                    {
                        doTag = true;
                    }

                    if (doTag)
                    {
                        //To-do later when I can be arsed.
                    }

                }

                File.Delete(inputFile);

                Dispatcher.Invoke(() =>
                {
                    progress.Visibility = Visibility.Hidden;
                    progress.IsIndeterminate = false;
                    button_convert.IsEnabled = true;
                    button_browse.IsEnabled = true;
                    input_video.IsEnabled = true;
                    input_video.Text = "";
                    input_outputfolder.IsEnabled = true;
                });

                MessageBox.Show("Video Conversion Completed!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);


            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to convert video: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private string GetFileName(string file)
        {
            Array.ForEach(System.IO.Path.GetInvalidFileNameChars(),
                  c => file = file.Replace(c.ToString(), String.Empty));

            return file;

        }

    }

    class MusicID3Tag
    {

        public byte[] TAGID = new byte[3];      //  3
        public byte[] Title = new byte[30];     //  30
        public byte[] Artist = new byte[30];    //  30 
        public byte[] Album = new byte[30];     //  30 
        public byte[] Year = new byte[4];       //  4 
        public byte[] Comment = new byte[30];   //  30 
        public byte[] Genre = new byte[1];      //  1

    }

}


