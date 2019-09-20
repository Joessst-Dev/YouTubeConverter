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
    //https://www.youtube.com/watch?v=LDS8SeO6hyg

    public class MainViewModel : ViewModelBase
    {
        private YoutubeClient _client;
        private IEnumerable<YouTubeContainerModel> _videos;
        private string _query;
        private bool _isBusy;
        private bool _isProgressIndeterminate;

        public IEnumerable<YouTubeContainerModel> Videos
        {
            get => _videos;
            private set
            {
                Set(ref _videos, value);
                RaisePropertyChanged(() => VideosAvailable);
            }
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

        public bool VideosAvailable => Videos != null 
            ? !Videos.Select(v => v.IsDataAvailable).Any(v => v == false) 
            : false;

        public RelayCommand FetchVideoInfoCommand { get; }
        public RelayCommand DownloadMediaCommand { get; }
        public RelayCommand<YouTubeContainerModel> RemoveVideoCommand { get; }

        public MainViewModel()
        {
            _client = new YoutubeClient();

            //Commands
            FetchVideoInfoCommand = new RelayCommand(FetchVideoInfo, 
                () => !IsBusy && !string.IsNullOrWhiteSpace(Query));
            DownloadMediaCommand = new RelayCommand(DownloadMedia, 
                () => !IsBusy);
            RemoveVideoCommand = new RelayCommand<YouTubeContainerModel>(RemoveVideo,
                _ => !IsBusy);
        }

        private string PromptSaveLocationDialog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Choose the location where your videos should be saved",
                SelectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                return dialog.SelectedPath;

            return null;
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

        private void RemoveVideo(YouTubeContainerModel video)
        {
            Videos = Videos.Where(v => v != video);
        }

        private async void FetchVideoInfo()
        {
            try
            {
                IsBusy = true;
                IsProgressIndeterminate = true;

                YouTubeContainerModel video = new YouTubeContainerModel();

                var videoId = NormalizeVideoId(Query);

                IEnumerable<Task> tasks = new List<Task>()
                {
                    _client.GetVideoAsync(videoId)
                        .ContinueWith(t => video.Video = t.Result),
                    _client.GetVideoAuthorChannelAsync(videoId)
                        .ContinueWith(t => video.Channel = t.Result),
                    _client.GetVideoMediaStreamInfosAsync(videoId)
                        .ContinueWith(t => video.MediaStreamInfos = t.Result),
                    _client.GetVideoClosedCaptionTrackInfosAsync(videoId)
                        .ContinueWith(t => video.ClosedCaptionTrackInfos = t.Result)
                };

                await Task.WhenAll(tasks);

                Videos = Videos != null 
                    ? Videos.Concat(new List<YouTubeContainerModel> { video }) 
                    : new List<YouTubeContainerModel> { video };
            }
            finally
            {
                IsBusy = false;
                IsProgressIndeterminate = false;
                Query = string.Empty;
            }
        }

        private async void DownloadMedia()
        {
            try
            {
                IsBusy = true;

                string folderPath = PromptSaveLocationDialog();

                if (folderPath is null)
                    return;

                IList<Task> tasks = new List<Task>();

                foreach (var video in Videos)
                {
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
                            ),
                            progressHandler
                        ).ContinueWith(_ => {
                            video.Progress = 0;
                        })
                    );

                    await Task.WhenAll(tasks);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
