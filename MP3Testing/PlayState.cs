using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Testing
{
    class PlayState : IPlaybackState
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
            // play중 정지하면?
            context.ChangePlaybackState(new StopState());
            return true;
        }

        public bool CanPause(IPlaybackContext context)
        {
            // Play중 일시정지하면?
            context.ChangePlaybackState(new PauseState());
            return true;
        }
    }
}
