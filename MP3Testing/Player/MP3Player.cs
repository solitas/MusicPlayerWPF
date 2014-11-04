using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using WPFSoundVisualizationLib;

namespace MP3Testing.Player
{
    public class PlayOrder
    {
        public int CurrentIndex = -1;
        public int OldIndex = -1;
        public int NextIndex = -1;
    }
    public enum RepeatState
    {
        Repeat, UnRepeat
    }
    public class Mp3Player : IPlayer, IPlaybackContext, ISpectrumPlayer, IWaveformPlayer
    {
        private readonly int fftDataSize = (int)FFTDataSize.FFT2048;

        public event NextPlayDelegate NextPlayEvent;
        public Action<float> SetVolumeDelegate;
        
        private readonly Playlist _playlist;    // 재생 목록 리스트

        private readonly PlayOrder _order;  // 재생 순서 인덱스

        /* NAudio 객체 */
        private IWavePlayer _wavePlayer;
    
        private BlockAlignReductionStream _stream;
        
        /// <summary>
        /// playback 상태 객체
        /// </summary>
        private IPlaybackState _currentState;

        /// <summary>
        /// 반복 상태 객체
        /// </summary>
        private RepeatState _repeatState = RepeatState.UnRepeat;
        
        /// <summary>
        /// 자동 곡 변경 확인 
        /// </summary>
        private bool _changeSong = true;

        private float _volumn = 1.0f;
        #region Properties
        public IPlaybackState State
        {
            get { return _currentState; }
        }
        
        public RepeatState Repeat
        {
            set
            {
                _repeatState = value;
            }
        }
        public List<string> PlayList
        {
            get
            {
                return _playlist.Files;
            }
        }
        public TimeSpan TotalTime
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.TotalTime;
                }
                return TimeSpan.Zero;
            }
        }

        public long Position
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.Position;
                }
                return 0;
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.CurrentTime;
                }
                return TimeSpan.Zero;
            }
        }

        public long TotalLength
        {
            get
            {
                if (_stream != null)
                {
                    return _stream.Length;
                }
                return 0;
            }
        }

        [DefaultValue(false)]
        public bool RandomState { get; set; }

        #endregion
        public Mp3Player()
        {
            _currentState = new StopState();
            _playlist = new Playlist();
            _order = new PlayOrder();
        }

        public void Close()
        {
            DisposeWave();
            _playlist.SaveFile();
        }

        public void AddFolderList(List<FileInfo> files)
        {
            foreach (var fileInfo in files.Where(fileInfo => fileInfo.Extension.Equals(".mp3")))
            {
                _playlist.InsertAudioFilePath(fileInfo.FullName);
            }
        }

        public void Play(string resourceName)
        {
            if (_currentState.CanPlay(resourceName, this))
            {
                _wavePlayer.Play();
            }
        }

        
        // 선택 재생 메소드
        public void Play(int index)
        {
            if (_wavePlayer != null)
            {
                // 이미 플레이어 객체가 만들어 진 상태에서 이 메소드를 호출한 건 
                // 음악을 바꾸려고 인터럽트 걸었던 것 
                // 따라서 자동 음악 변경은 false
                _changeSong = false;
            }

            UpdatePlayIndex(index);

            var resource = _playlist.MediaList[index].FilePath;

            if (_currentState.CanPlay(resource, this))
            {
                if (_wavePlayer != null)
                    _wavePlayer.Play();
                IsPlaying = true;
            }
        }

        public void Stop()
        {
            if (_currentState.CanStop(this))
            {
                _wavePlayer.Stop();
            }
        }

        public void Pause()
        {
            if (_currentState.CanPause(this))
            {
                _wavePlayer.Pause();
            }
        }

        public void Forward()
        {

        }

        public void Backward()
        {

        }

        public void ChangePlaybackState(IPlaybackState newState)
        {
            _currentState = newState;
        }

        public void Open(string selectedFile)
        {
            DisposeWave();

            WaveStream pcm;

            if (selectedFile.EndsWith(".mp3"))
            {
                pcm = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(selectedFile));
                _stream = new BlockAlignReductionStream(pcm);

            }
            else if (selectedFile.EndsWith(".wav"))
            {
                pcm = new WaveChannel32(new WaveFileReader(selectedFile));
                _stream = new BlockAlignReductionStream(pcm);
            }
            else
                throw new InvalidOperationException("Not a correct audio file type");

            var channel = new WaveChannel32(_stream);
            channel.PadWithZeroes = false;
            channel.Sample += ChannelOnSample;
            // Volume 설정
            SetVolumeDelegate = vol =>
            {
                _volumn = vol;
                channel.Volume = _volumn;
            };
            SetVolumeDelegate(_volumn);

            _sampleAggregator = new SampleAggregator(fftDataSize);
            
            _wavePlayer = new DirectSoundOut();
            _wavePlayer.Init(channel);
            _wavePlayer.PlaybackStopped += WavePlayerOnPlaybackStopped;
        }


        private const int repeatThreshold = 200;
        private void ChannelOnSample(object sender, SampleEventArgs sampleEventArgs)
        {
            _sampleAggregator.Add(sampleEventArgs.Left, sampleEventArgs.Right);
            long repeatStartPosition = (long)((SelectionBegin.TotalSeconds / _stream.TotalTime.TotalSeconds) * _stream.Length);
            long repeatStopPosition = (long)((SelectionEnd.TotalSeconds / _stream.TotalTime.TotalSeconds) * _stream.Length);
            if (((SelectionEnd - SelectionBegin) >= TimeSpan.FromMilliseconds(repeatThreshold)) &&
                _stream.Position >= repeatStopPosition)
            {

                _sampleAggregator.Clear();
            }
        }

        public void Seek(long pos)
        {
            if (_stream != null)
            {
                if ((pos % _stream.BlockAlign) != 0)
                    pos -= pos % _stream.BlockAlign;
                // Force new position into valid range
                pos = Math.Max(0, Math.Min(_stream.Length, pos));
                // set position
                _stream.Position = pos;
            }
        }

        public MediaInfo GetMediaInfo(int index)
        {
            return _playlist.MediaList[index];
        }
        private void UpdatePlayIndex(int index)
        {
            _order.OldIndex = _order.CurrentIndex;
            _order.CurrentIndex = index;

            if (++index < _playlist.FileCount)
            {
                _order.NextIndex = index;
            }

            if (RandomState)
            {
                var r = new Random(index);

                int currentIndex = r.Next(0, _playlist.FileCount);
                int nextIndex = r.Next(0, _playlist.FileCount);

                _order.NextIndex = nextIndex;
                _order.CurrentIndex = currentIndex;
            }
        }

        private void WavePlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            if (_changeSong)
            {
                // 한 곡 플레이가 인터럽트 없이 끝나면 처리
                if (_order.NextIndex > 0)
                {
                    if (NextPlayEvent != null)
                    {
                        int playIndex = 0;

                        UpdatePlayIndex(playIndex); // 인덱스 업데이트

                        playIndex = _repeatState == RepeatState.Repeat ? _order.CurrentIndex : _order.NextIndex;

                        var resource = _playlist.MediaList[playIndex].FilePath;

                        if (_currentState.CanPlay(resource, this))
                        {
                            _wavePlayer.Play();
                        }

                        NextPlayEvent(playIndex, GetMediaInfo(playIndex));
                    }
                }
            }
            else
            {
                _changeSong = true;
            }
        }
        private void DisposeWave()
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }

            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
        #region INotifyPropertyChanged

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
        

        public event PropertyChangedEventHandler PropertyChanged;

        public bool GetFFTData(float[] fftDataBuffer)
        {
            _sampleAggregator.GetFFTResults(fftDataBuffer);
            return IsPlaying;
        }

        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (_stream != null)
                maxFrequency = _stream.WaveFormat.SampleRate / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate.
            return (int)((frequency / maxFrequency) * (fftDataSize / 2));
        }

        private bool isPlaying;
        public bool IsPlaying
        {
            get
            {
                if (_wavePlayer == null) return false;
                return _wavePlayer.PlaybackState == PlaybackState.Playing;
            }
            protected set
            {
                bool oldValue = isPlaying;
                isPlaying = value;
                if (oldValue != isPlaying)
                    NotifyPropertyChanged("IsPlaying");
            }
        }

        private SampleAggregator _sampleAggregator;
        private double channelLength;
        public double ChannelLength
        {
            get { return channelLength; }
            protected set
            {
                double oldValue = channelLength;
                channelLength = value;
                if (oldValue != channelLength)
                    NotifyPropertyChanged("ChannelLength");
            }
        }

        private double channelPosition;
        private bool inChannelTimerUpdate;
        public double ChannelPosition
        {
            get { return channelPosition; }
            set
            {
                if (!inChannelSet)
                {
                    inChannelSet = true; // Avoid recursion
                    double oldValue = channelPosition;
                    double position = Math.Max(0, Math.Min(value, ChannelLength));
                    if (!inChannelTimerUpdate && _stream != null)
                        _stream.Position = (long)((position / _stream.TotalTime.TotalSeconds) * _stream.Length);
                    channelPosition = position;
                    if (oldValue != channelPosition)
                        NotifyPropertyChanged("ChannelPosition");
                    inChannelSet = false;
                }
            }

        }

        private TimeSpan repeatStart;
        private TimeSpan repeatStop;
        private bool inRepeatSet;
        private bool inChannelSet;
        public TimeSpan SelectionBegin
        {
            get { return repeatStart; }
            set
            {
                if (!inRepeatSet)
                {
                    inRepeatSet = true;
                    TimeSpan oldValue = repeatStart;
                    repeatStart = value;
                    if (oldValue != repeatStart)
                        NotifyPropertyChanged("SelectionBegin");
                    inRepeatSet = false;
                }
            }

        }

        public TimeSpan SelectionEnd
        {
            get { return repeatStop; }
            set
            {
                if (!inChannelSet)
                {
                    inRepeatSet = true;
                    TimeSpan oldValue = repeatStop;
                    repeatStop = value;
                    if (oldValue != repeatStop)
                        NotifyPropertyChanged("SelectionEnd");
                    inRepeatSet = false;
                }
            }

        }

        private float[] waveformData;
        public float[] WaveformData
        {
            get { return waveformData; }
            protected set
            {
                float[] oldValue = waveformData;
                waveformData = value;
                if (oldValue != waveformData)
                    NotifyPropertyChanged("WaveformData");
            }
        }
    }
}
