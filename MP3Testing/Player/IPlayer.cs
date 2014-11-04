using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Testing.Player
{
    public interface IPlayer
    {
        void Play(string resourceName);

        void Stop();

        void Pause();

        void Forward();

        void Backward();
    }
}
