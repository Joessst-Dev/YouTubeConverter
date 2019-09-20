using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeConverter.Models
{
    public class YouTubeContainerModel : ObservableObject
    {
        private double _progress;
        private Video _video;
        private Channel _channel;
        private MediaStreamInfoSet _mediaStreamInfos;
        private IReadOnlyList<ClosedCaptionTrackInfo> _closedCaptionTrackInfos;

        public double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        public Video Video
        {
            get => _video;
            set
            {
                Set(ref _video, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public Channel Channel
        {
            get => _channel;
            set
            {
                Set(ref _channel, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public MediaStreamInfoSet MediaStreamInfos
        {
            get => _mediaStreamInfos;
            set
            {
                Set(ref _mediaStreamInfos, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public IReadOnlyList<ClosedCaptionTrackInfo> ClosedCaptionTrackInfos
        {
            get => _closedCaptionTrackInfos;
            set
            {
                Set(ref _closedCaptionTrackInfos, value);
                RaisePropertyChanged(() => IsDataAvailable);
            }
        }

        public bool IsDataAvailable => Video != null && Channel != null 
                                       && MediaStreamInfos != null && ClosedCaptionTrackInfos != null;
    }
}
