using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP3Testing
{
    public class Playlist
    {
        private const string DefalutFilePath = "playlist.pls";
        private readonly List<string> _filePaths;
        private int _fileCount;
        public List<string> Files
        {
            get { return _filePaths; }
        }

        public List<MediaInfo> MediaList; 

        public int FileCount { get { return _fileCount; } }
        public Playlist()
        {
            _filePaths = new List<string>();
            MediaList = new List<MediaInfo>();
            _fileCount = 0;

            Initialize();
        }

        private void Initialize()
        {
            using (var fileStream = new FileStream(DefalutFilePath, FileMode.OpenOrCreate))
            using (var streamReader = new StreamReader(fileStream))
            {
                string audioFile;

                while ((audioFile = streamReader.ReadLine()) != null)
                {
                    // 한줄씩 읽어서 리스트에 추가
                    _filePaths.Add(audioFile);
                    MediaList.Add(new MediaInfo(audioFile));
                    _fileCount++;
                }
            }
        }

        public void InsertAudioFilePath(string audioFilePath)
        {
            _filePaths.Add(audioFilePath);
            MediaList.Add(new MediaInfo(audioFilePath));
            _fileCount++;
        }

        public void DeleteAudioFilePath(string audioFilePath)
        {
            _filePaths.Remove(audioFilePath);
            _fileCount--;
        }

        public void SaveFile()
        {
            using (var fileStream = new FileStream(DefalutFilePath, FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                _filePaths.ForEach(streamWriter.WriteLine);
            }
        }

        public string GetAudioFilePath(int index)
        {
            if (index < _fileCount)
            {
                return _filePaths[index];
            }
            return null;
        }
    }
}
