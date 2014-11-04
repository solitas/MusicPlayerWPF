using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Testing
{
    class StopState : IPlaybackState
    {
        public bool CanPlay(string resource, IPlaybackContext context)
        {
            if (string.IsNullOrEmpty(resource))
            {
                return false;
            }
            context.Open(resource);
            context.ChangePlaybackState(new PlayState());
            return true;
        }

        public bool CanStop(IPlaybackContext context)
        {
            // stop중 정지하면?
            return false;
        }

        public bool CanPause(IPlaybackContext context)
        {
            // Pause중 일시정지하면?
            return false;
        }
    }
}
