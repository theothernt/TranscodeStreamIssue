using System;
using System.Diagnostics;

using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Media.Transcoding;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace TranscodeStreamIssue
{
    public sealed partial class MainPage : Page
    {
       
        private readonly Uri _dashStreamUrl = new Uri("https://bitmovin-a.akamaihd.net/content/MI201109210084_1/mpds/f08e80da-bf1d-4e3d-8899-f0f6155f6efa.mpd");
        private readonly Uri _hlsStreamUrl = new Uri("https://mnmedias.api.telequebec.tv/m3u8/29880.m3u8");

        private readonly string _mp4ContentType = "video/mp4";

        private readonly MediaTranscoder _transcoder = new MediaTranscoder();
        private MediaEncodingProfile _profile;

        private IRandomAccessStream _memoryStream = new InMemoryRandomAccessStream();

        public MainPage()
        {
            this.InitializeComponent();
            Status("Waiting...");
            SetupEncodingProfile();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Status("Starting...");
            _ = GenerateHlsPreview();
        }

        private void Status(string message) => StatusMessage.Text = message;

        private void SetupEncodingProfile()
        {
            var quality = VideoEncodingQuality.HD720p;
            _profile = MediaEncodingProfile.CreateMp4(quality);
        }

        private async Task GenerateHlsPreview()
        {
            var hlsSource = await BuildHlsMediaSource();

            var hlsMediaSource = MediaSource.CreateFromAdaptiveMediaSource(hlsSource);

            // hlsSource = InvalidCastException
            // hlsMediaSource.MediaStreamSource = NullReferenceException
            await TranscodeMediaSourceStream(hlsSource);

            var source = MediaSource.CreateFromStream(_memoryStream, _mp4ContentType);
            PlayMedia(source);
        }

        private async Task TranscodeMediaSourceStream(IMediaSource source)
        {
            _transcoder.TrimStartTime = new TimeSpan(0, 0, 0);
            _transcoder.TrimStopTime = new TimeSpan(0, 0, 5);

            var result = await _transcoder.PrepareMediaStreamSourceTranscodeAsync(source, _memoryStream, _profile);

            if (result.CanTranscode)
            {
                var progress = new Progress<double>(TranscodeProgress);
                await result.TranscodeAsync().AsTask(progress);
            }
            else
            {
                Status(result.FailureReason.ToString());
            }
        }

        private void TranscodeProgress(double percent) => Status("Progress:  " + percent.ToString().Split('.')[0] + "%");

        private async Task<AdaptiveMediaSource> BuildHlsMediaSource()
        {
            var client = new HttpClient();
            // Might want custom headers
            //client.DefaultRequestHeaders.Add("Referer", "xyz");
            var result = await AdaptiveMediaSource.CreateFromUriAsync(_dashStreamUrl, client);

            if (result.Status == AdaptiveMediaSourceCreationStatus.Success)
            {
                Debug.WriteLine($"Success: got manifest");
                return result.MediaSource;
            }
            else
            {
                Debug.WriteLine($"Error: {result.Status}");
                return null;
            }
        }

        private void PlayMedia(IMediaPlaybackSource source)
        {
            Player.AreTransportControlsEnabled = true;
            Player.AutoPlay = true;
            Player.Source = source;
            Player.MediaPlayer.IsLoopingEnabled = true;
        }
    }
    
}
