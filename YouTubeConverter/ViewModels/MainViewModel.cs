using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YouTubeConverter.Models;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeConverter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private YoutubeClient _client;
        private IEnumerable<YouTubeContainerModel> _videos;
        private YouTubeContainerModel _video;
        private bool _videosAvailable;

        public IEnumerable<YouTubeContainerModel> Videos
        {
            get => _videos;
            private set
            {
                Set(ref _videos, value);
                DownloadMediaCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(() => VideosAvailable);
            }
        }

        public YouTubeContainerModel Video
        {
            get => _video;
            private set
            {
                Set(ref _video, value);
                RaisePropertyChanged(() => IsFetchable);
                FetchVideoInfoCommand.RaiseCanExecuteChanged();
            }
        }

        public bool VideosAvailable => Videos.Select(v => v.IsDataAvailable).Any(v => v == false);

        public bool IsFetchable => !string.IsNullOrWhiteSpace(Video.Query) && !Video.IsBusy;

        public RelayCommand FetchVideoInfoCommand { get; }
        public RelayCommand DownloadMediaCommand { get; }

        public MainViewModel()
        {
            _client = new YoutubeClient();

            //Commands
            FetchVideoInfoCommand = new RelayCommand(FetchVideoInfo, () => IsFetchable);
            DownloadMediaCommand = new RelayCommand(DownloadMedia, () => VideosAvailable);
        }

        private string PromptSaveLocationDialog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Choose the location where your videos should be saved",
                RootFolder = Environment.SpecialFolder.MyDocuments,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                return dialog.SelectedPath;

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private string NormalizeVideoId(string input)
        {
            return YoutubeClient.TryParseVideoId(input, out var videoId)
                ? videoId
                : input;
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            return fileName;
        }

        private async void FetchVideoInfo()
        {
            try
            {
                Video.IsBusy = true;
                Video.IsProgressIndeterminate = true;

                Video.Video = null;
                Video.Channel = null;
                Video.MediaStreamInfos = null;
                Video.ClosedCaptionTrackInfos = null;

                var videoId = NormalizeVideoId(Video.Query);

                IEnumerable<Task> tasks = new List<Task>()
                {
                    _client.GetVideoAsync(videoId)
                        .ContinueWith(t => Video.Video = t.Result),
                    _client.GetChannelAsync(videoId)
                        .ContinueWith(t => Video.Channel = t.Result),
                    _client.GetVideoMediaStreamInfosAsync(videoId)
                        .ContinueWith(t => Video.MediaStreamInfos = t.Result),
                    _client.GetVideoClosedCaptionTrackInfosAsync(videoId)
                        .ContinueWith(t => Video.ClosedCaptionTrackInfos = t.Result)
                };

                await Task.WhenAll(tasks);
            }
            finally
            {
                Video.IsBusy = false;
                Video.IsProgressIndeterminate = false;

                Videos = Videos.Concat(new List<YouTubeContainerModel> { Video });
            }
        }

        private async void DownloadMedia()
        {
            string folderPath = PromptSaveLocationDialog();
            IList<Task> tasks = new List<Task>();

            foreach (var video in Videos)
            {
                video.IsBusy = true;
                video.Progress = 0;

                var streamInfo = video.MediaStreamInfos.Muxed.WithHighestVideoQuality();

                var fileExt = streamInfo.Container.GetFileExtension();

                Progress<double> progressHandler = new Progress<double>(p => video.Progress = p);

                tasks.Add(
                    _client.DownloadMediaStreamAsync(
                        streamInfo,
                        Path.Combine(
                            folderPath,
                            SanitizeFileName($"{video.Video.Title}.{fileExt}")
                        )
                    ).ContinueWith(_ =>
                    {
                        video.IsBusy = false;
                        video.Progress = 0;
                    })
                );
            }

            await Task.WhenAll(tasks);
        }
    }
}
