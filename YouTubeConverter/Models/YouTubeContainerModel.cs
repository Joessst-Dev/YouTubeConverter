using System.Collections.Generic;
using System.IO;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeConverter.Models
{
    public class YouTubeContainerModel
    {
        public bool IsBusy { get; set; }
        public string Query { get; set; }
        public Video Video { get; set; }
        public Channel Channel { get; set; }
        public MediaStreamInfoSet MediaStreamInfos { get; set; }
        public IReadOnlyList<ClosedCaptionTrackInfo> ClosedCaptionTrackInfos { get; set; }
        public double Progress { get; set; }
        public bool IsProgressIndeterminate { get; set; }
        public bool IsDataAvailable => Video != null && Channel != null 
                                       && MediaStreamInfos != null && ClosedCaptionTrackInfos != null;
    }
}
