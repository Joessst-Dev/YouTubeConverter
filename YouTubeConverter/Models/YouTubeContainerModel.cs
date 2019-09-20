using System.Collections.Generic;
using YoutubeExplode.Models;
using YoutubeExplode.Models.ClosedCaptions;
using YoutubeExplode.Models.MediaStreams;

namespace YouTubeConverter.Models
{
    public class YouTubeContainerModel
    {
        public Video Video { get; set; }
        public Channel Channel { get; set; }
        public MediaStreamInfoSet MediaStreamInfos { get; set; }
        public IReadOnlyList<ClosedCaptionTrackInfo> ClosedCaptionTrackInfos { get; set; }
        public bool IsDataAvailable => Video != null && Channel != null 
                                       && MediaStreamInfos != null && ClosedCaptionTrackInfos != null;
    }
}
