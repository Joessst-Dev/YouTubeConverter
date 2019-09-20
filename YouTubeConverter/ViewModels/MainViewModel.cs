using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YouTubeConverter.Models;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeConverter.ViewModels
{
    //https://www.youtube.com/watch?v=LDS8SeO6hyg

    public class MainViewModel : ViewModelBase
    {
        private YoutubeClient _client;
        private IEnumerable<YouTubeContainerModel> _videos;
        private string _query;
        private bool _isBusy;
        private double _progress;
        private bool _isProgressIndeterminate;
        private YouTubeContainerModel _video;

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
            private set => Set(ref _video, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                Set(ref _isBusy, value);
                FetchVideoInfoCommand.RaiseCanExecuteChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            private set => Set(ref _progress, value);
        }

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            private set => Set(ref _isProgressIndeterminate, value);
        }

        public string Query
        {
            get => _query;
            set
            {
                Set(ref _query, value);
                FetchVideoInfoCommand.RaiseCanExecuteChanged();
            }
        }

        public bool VideosAvailable => !Videos.Select(v => v.IsDataAvailable).Any(v => v == false);

        public RelayCommand FetchVideoInfoCommand { get; }
        public RelayCommand DownloadMediaCommand { get; }
        public RelayCommand<YouTubeContainerModel> RemoveVideoCommand { get; }

        public MainViewModel()
        {
            _client = new YoutubeClient();

            //Commands
            FetchVideoInfoCommand = new RelayCommand(FetchVideoInfo, 
                () => !IsBusy && !string.IsNullOrWhiteSpace(Query));
            DownloadMediaCommand = new RelayCommand(DownloadMedia, () => VideosAvailable && !IsBusy);
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
                IsBusy = true;
                IsProgressIndeterminate = true;

                Video video = null;
                Channel channel = null;
                MediaStreamInfoSet mediaStreamInfos = null;
                IReadOnlyList<ClosedCaptionTrackInfo> closedCaptionTrackInfos = null;

                var videoId = NormalizeVideoId(Query);

                IEnumerable<Task> tasks = new List<Task>()
                {
                    _client.GetVideoAsync(videoId)
                        .ContinueWith(t => video = t.Result),
                    _client.GetVideoAuthorChannelAsync(videoId)
                        .ContinueWith(t => channel = t.Result),
                    _client.GetVideoMediaStreamInfosAsync(videoId)
                        .ContinueWith(t => mediaStreamInfos = t.Result),
                    _client.GetVideoClosedCaptionTrackInfosAsync(videoId)
                        .ContinueWith(t => closedCaptionTrackInfos = t.Result)
                };

                await Task.WhenAll(tasks);

                Video = new YouTubeContainerModel
                {
                    Video = video,
                    Channel = channel,
                    MediaStreamInfos = mediaStreamInfos,
                    ClosedCaptionTrackInfos = closedCaptionTrackInfos
                };

                Videos = Videos != null 
                    ? Videos.Concat(new List<YouTubeContainerModel> { Video }) 
                    : new List<YouTubeContainerModel> { Video};
            }
            finally
            {
                IsBusy = false;
                IsProgressIndeterminate = false;
            }
        }

        //private async void DownloadMedia()
        //{
        //    string folderPath = PromptSaveLocationDialog();
        //    IList<Task> tasks = new List<Task>();

        //    foreach (var video in Videos)
        //    {
        //        video.IsBusy = true;
        //        video.Progress = 0;

        //        var streamInfo = video.MediaStreamInfos.Muxed.WithHighestVideoQuality();

        //        var fileExt = streamInfo.Container.GetFileExtension();

        //        Progress<double> progressHandler = new Progress<double>(p => video.Progress = p);

        //        tasks.Add(
        //            _client.DownloadMediaStreamAsync(
        //                streamInfo,
        //                Path.Combine(
        //                    folderPath,
        //                    SanitizeFileName($"{video.Video.Title}.{fileExt}")
        //                )
        //            ).ContinueWith(_ =>
        //            {
        //                video.IsBusy = false;
        //                video.Progress = 0;
        //            })
        //        );
        //    }

        //    await Task.WhenAll(tasks);
        //}

        private async void DownloadMedia()
        {
            try
            {
                IsBusy = true;

                string folderPath = PromptSaveLocationDialog();

                foreach (var video in Videos)
                {
                    Progress = 0;
                }
            }
            finally
            {
                IsBusy = false;
                Progress = 0;
            }
        }
    }
}
