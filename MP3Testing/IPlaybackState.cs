using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Testing
{
    public interface IPlaybackContext
    {
        void ChangePlaybackState(IPlaybackState newState);
        void Open(string resourceName);
    }

    public interface IPlaybackState
    {
        // 동작을 정의
        bool CanPlay(string resource, IPlaybackContext context);
        bool CanStop(IPlaybackContext context);
        bool CanPause(IPlaybackContext context);
    }
}
