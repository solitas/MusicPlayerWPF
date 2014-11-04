using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Testing
{
    class PauseState : IPlaybackState
    {
        public bool CanPlay(string resource, IPlaybackContext context)
        {
            context.ChangePlaybackState(new PlayState());
            return true;
        }

        public bool CanStop(IPlaybackContext context)
        {
            // pause중 정지하면?
            context.ChangePlaybackState(new StopState());
            return true;
        }

        public bool CanPause(IPlaybackContext context)
        {
            // pause중 일시정지하면?
            return false;
        }
    }
}
