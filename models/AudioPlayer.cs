using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Snaek.models
{
    public class AudioPlayer
    {
        private MediaPlayer m_player;

        /// <summary>
        /// Plays the audio file at path <paramref name="filename"/>
        /// with the given volume <paramref name="volume"/>
        /// </summary>
        /// <param name="filename">Relative URI</param>
        /// <param name="volume">Volume in int</param>
        public void Play(string filename, int volume)
        {
            m_player = new MediaPlayer();
            m_player.Volume = volume / 100.0f;
            m_player.Open(new Uri(filename, UriKind.Relative));
            m_player.Play();
        }

        public void PlayLoop(string filename, int volume)
        {
            Task.Run(() =>
            {
                m_player = new MediaPlayer();
                m_player.Open(new Uri(filename, UriKind.Relative));
                m_player.Volume = volume / 100.0f;
                m_player.Play();
                TimeSpan duration = TimeSpan.FromSeconds(24);
                RepeatAudio(duration);
            });
        }

        private void RepeatAudio(TimeSpan duration)
        {
            while (true)
            {
                if (m_player.Position >= duration)
                {
                    m_player.Position = TimeSpan.FromMilliseconds(1);
                }
            }
        }
    }
}
