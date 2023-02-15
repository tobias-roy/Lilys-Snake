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
        /// Plays the audio file at the relative path <paramref name="filename"/>
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

        ///<summary>
        /// Plays the audio file at the relative path <paramref name="filename"/>
        /// with the given volume <paramref name="volume"/>
        /// on a seperate thread.
        /// </summary>
        /// <param name="filename">Relative URI</param>
        /// <param name="volume">Volume in int</param>
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


        /// <summary>
        /// Repeats the audio the mediaplayer is playing.
        /// </summary>
        /// <param name="duration">duration of audio file</param>
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
