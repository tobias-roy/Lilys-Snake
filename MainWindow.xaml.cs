using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Snaek
{
    public partial class MainWindow : Window
    {
        private System.Windows.Threading.DispatcherTimer gameTickTimer = new System.Windows.Threading.DispatcherTimer();
        const int SnakeSquareSize = 40;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;
        const int MaxHighscoreListEntryCount = 3;

        private UIElement snakeFood = null;
        private SolidColorBrush foodBrush = Brushes.Red;
        private SolidColorBrush snakeBodyBrush = new SolidColorBrush(Color.FromRgb(94, 135, 214));
        private SolidColorBrush snakeHeadBrush = new SolidColorBrush(Color.FromRgb(15, 61, 196));
        private List<SnakePart> snakeParts = new List<SnakePart>();

        public enum SnakeDirection { Left, Right, Up, Down }
        private SnakeDirection snakeDirection = SnakeDirection.Up;
        private int snakeLength;
        private int currentScore = 0;
        private Random rnd = new Random();
        private bool paused = false;
        private bool isGamePlaying = false;
        private bool directionChosen = false;

        public AudioPlayer effectPlayer = new();
        public AudioPlayer musicPlayer = new();
 

        public MainWindow()
        {
            musicPlayer.PlayLoop(@"resources/audio/music/backgroundMusic.wav", 10);
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
            LoadHighscoreList();
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
            directionChosen = false;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Visible;
        }

        private void StartNewGame()
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;
            //Remove dead snakeparts and leftover old food
            foreach (SnakePart snakeBodyPart in snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
            }
            snakeParts.Clear();
            if (snakeFood != null)
                GameArea.Children.Remove(snakeFood);

            //Reset the game
            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Up;
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            //Draw the snake again and new food
            isGamePlaying = true;
            DrawSnake();
            DrawSnakeFood();

            //Update status
            UpdateGameStatus();

            //Go!
            gameTickTimer.IsEnabled = true;
        }

        public ObservableCollection<SnakeHighscore> HighscoreList { get; set; } = new ObservableCollection<SnakeHighscore>();

        public class SnakeHighscore
        {
            public string PlayerName { get; set; }
            public int Score { get; set; }
        }





        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = rnd.Next(0, maxY) * SnakeSquareSize;

            foreach (SnakePart snakePart in snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }
            return new Point(foodX, foodY);
        }



        private void PauseMenu()
        {
            if (!paused && isGamePlaying)
            {
                bdrPauseMenu.Visibility = Visibility.Visible;
                gameTickTimer.Stop();
                paused = !paused;
            } else if (paused && isGamePlaying)
            {
                bdrPauseMenu.Visibility = Visibility.Collapsed;
                gameTickTimer.Start();
                paused = !paused;
            }
        }


        private void EatSnakeFood()
        {
            snakeLength++;
            currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(snakeFood);

            //Make this random from various strings of audio
            List<string> eatingSounds = new()
            {
                "resources/audio/snake/eating/Mmh.wav",
                "resources/audio/snake/eating/Laekkert.wav",
                "resources/audio/snake/eating/haps.wav",
                "resources/audio/snake/eating/SaftigtAeble.wav"
            };
            effectPlayer.Play(eatingSounds[rnd.Next(0, 4)], 50);

            DrawSnakeFood();
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            this.tbStatusScore.Text = currentScore.ToString();
        }

        private void EndGame()
        {
            isGamePlaying = false;
            bool isNewHighscore = false;
            if (currentScore > 0)
            {
                int lowestHighscore = (this.HighscoreList.Count > 0 ? this.HighscoreList.Min(x => x.Score) : 0);
                if ((currentScore > lowestHighscore) || (this.HighscoreList.Count < MaxHighscoreListEntryCount))
                {
                    List<string> highscoreEnding = new()
                    {
                        "resources/audio/snake/highscore/FlotKlaret.wav",
                        "resources/audio/snake/highscore/Tillykke.wav",
                    };
                    effectPlayer.Play(highscoreEnding[rnd.Next(0, 2)], 50);
                    bdrInsertPlayerName.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }
            if (!isNewHighscore)
            {
                List<string> loosingEnding = new()
                {
                    "resources/audio/snake/loosing/Av.wav",
                    "resources/audio/snake/loosing/AvForSoeren.wav",
                    "resources/audio/snake/loosing/DinKlovn.wav",
                    "resources/audio/snake/loosing/Hovsa.wav",
                };
                effectPlayer.Play(loosingEnding[rnd.Next(0, 4)], 50);
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            gameTickTimer.IsEnabled = false;
        }


        //UI Drawing
        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    System.Windows.Shapes.Path snakeface = new();
                    string snakefaceDataUp = "M 9 40 L 30 40 L 33 39 L 34 38 L 35 35 L 35 33 L 34 19 L 35 18 L 35 17 L 34 16 L 32 10 L 31 9 L 29 8 L 25 7 L 24 7 L 20 7 L 20 5 L 26 0 L 24 1 L 20 4 L 19 4 L 15 1 L 13 0 L 19 5 L 19 7 L 15 7 L 14 7 L 10 8 L 8 9 L 7 10 L 5 16 L 4 17 L 4 18 L 5 19 L 4 33 L 4 35 L 5 38 L 6 39 L 9 40";
                    string snakefaceDataRight = "M -0 10 L 0 31 L 1 34 L 2 35 L 5 36 L 7 36 L 21 35 L 22 36 L 23 36 L 24 35 L 30 33 L 31 32 L 32 30 L 33 26 L 33 25 L 33 21 L 35 21 L 40 27 L 39 25 L 36 21 L 36 20 L 39 16 L 40 14 L 35 20 L 33 20 L 33 16 L 33 15 L 32 11 L 31 9 L 30 8 L 24 6 L 23 5 L 22 5 L 21 6 L 7 5 L 5 5 L 2 6 L 1 7 L -0 10";
                    string snakefaceDataDown = "M 30 0 L 9 0 L 6 1 L 5 2 L 4 5 L 4 7 L 5 21 L 4 22 L 4 23 L 5 24 L 7 30 L 8 31 L 10 32 L 14 33 L 15 33 L 19 33 L 19 35 L 13 40 L 15 39 L 19 36 L 20 36 L 24 39 L 26 40 L 20 35 L 20 33 L 24 33 L 25 33 L 29 32 L 31 31 L 32 30 L 34 24 L 35 23 L 35 22 L 34 21 L 35 7 L 35 5 L 34 2 L 33 1 L 30 0";
                    string snakefaceDataLeft = "M 40 30 L 40 9 L 39 6 L 38 5 L 35 4 L 33 4 L 19 5 L 18 4 L 17 4 L 16 5 L 10 7 L 9 8 L 8 10 L 7 14 L 7 15 L 7 19 L 5 19 L 0 13 L 1 15 L 4 19 L 4 20 L 1 24 L 0 26 L 5 20 L 7 20 L 7 24 L 7 25 L 8 29 L 9 31 L 10 32 L 16 34 L 17 35 L 18 35 L 19 34 L 33 35 L 35 35 L 38 34 L 39 33 L 40 30";
                    var converter = TypeDescriptor.GetConverter(typeof(Geometry));
                    switch (snakeDirection)
                    {
                        case SnakeDirection.Left:
                            snakeface.Data = (Geometry)converter.ConvertFrom(snakefaceDataLeft);
                            break;
                        case SnakeDirection.Right:
                            snakeface.Data = (Geometry)converter.ConvertFrom(snakefaceDataRight);
                            break;
                        case SnakeDirection.Up:
                            snakeface.Data = (Geometry)converter.ConvertFrom(snakefaceDataUp);
                            break;
                        case SnakeDirection.Down:
                            snakeface.Data = (Geometry)converter.ConvertFrom(snakefaceDataDown);
                            break;
                    }
                    
                    snakePart.UiElement = new System.Windows.Shapes.Path()
                    {
                        Data = snakeface.Data,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }
        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            System.Windows.Shapes.Path applePath = new();
            string appleData = "M 22,8 C 19.679,8 18.035,9.009 17.062,9.857 17.47,5.698 20,4 20,4 L 18,3 C 18,3 15.42,5.587 15.048,9.955 14.09,9.089 12.411,8 10,8 6,8 3,11 4,17 c 1.315,7.892 5,11 8,11 2,0 4,-2 4,-2 0,0 2,2 4,2 3,0 6.685,-3.108 8,-11 1,-6 -2,-9 -6,-9 z";
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            applePath.Data = (Geometry)converter.ConvertFrom(appleData);
            snakeFood = new System.Windows.Shapes.Path()
            {
                Data = applePath.Data,
                Fill = foodBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };


            GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }
        
        //Movement
        private void MoveSnake()
        {
            //Remove the last part of the snake, in preparation of the new part added below
            while (snakeParts.Count >= snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }
            //Next up, we'll add a new element to the snake, which will be the (new) head
            //Therefore, we mark all existing parts as non-head (body) elements and then
            //we make sure that they use the body brush
            System.Windows.Shapes.Path snakebody = new();
            string snakebodyData = "M 0 13 L 0 27 L 5 35 L 13 40 L 27 40 L 35 35 L 40 27 L 40 13 L 35 5 L 27 0 L 13 0 L 20 15 L 25 15 L 25 25 L 15 25 L 15 15 L 20 15 L 13 0 L 5 5 L 0 13";
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            snakebody.Data = (Geometry)converter.ConvertFrom(snakebodyData);

            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.UiElement as System.Windows.Shapes.Path).Data = snakebody.Data;
                (snakePart.UiElement as System.Windows.Shapes.Path).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }
            //Determine in which direction to expand the snake, based on the current direction
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize; break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize; break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize; break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize; break;
            }
            //Now add the new head part to our list of snake parts...
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });
            //And then draw the motherfucker
            DrawSnake();
            DoCollisionCheck();
        }
        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

            if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood();
                return;
            }

            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) ||
                (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame();
            }

            foreach (SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                {
                    EndGame();
                }
            }
        }



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

            public void Mute()
            {
                m_player.Stop();
            }

            private void RepeatAudio(TimeSpan duration)
            {
                while (true)
                {
                    if(m_player.Position >= duration)
                    {
                        m_player.Position = TimeSpan.FromMilliseconds(1);
                    }
                }
            }
        }
    
        //Highscore list
        private void LoadHighscoreList()
        {
            if (File.Exists("snake_highscorelist.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
                using (Stream reader = new FileStream("snake_highscorelist.xml", FileMode.Open))
                {
                    List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader);
                    this.HighscoreList.Clear();
                    foreach (var item in tempList.OrderByDescending(x => x.Score))
                        this.HighscoreList.Add(item);
                }
            }
        }
        private void SaveHighscoreList()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
            using (Stream writer = new FileStream("snake_highscorelist.xml", FileMode.Create))
            {
                serializer.Serialize(writer, HighscoreList);
            }
        }
        
        //Click events
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                    if (snakeDirection != SnakeDirection.Down && !directionChosen)
                        snakeDirection = SnakeDirection.Up; directionChosen = true;
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up && !directionChosen)
                        snakeDirection = SnakeDirection.Down; directionChosen = true;
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right && !directionChosen)
                        snakeDirection = SnakeDirection.Left; directionChosen = true;
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left && !directionChosen)
                        snakeDirection = SnakeDirection.Right; directionChosen = true;
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
                case Key.Tab:
                    PauseMenu();
                    break;
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            //Show some awesome list of players that are good at this game!
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
        private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;
            //Determine where to insert the entry
            if ((this.HighscoreList.Count > 0) && (currentScore < this.HighscoreList.Max(x => x.Score)))
            {
                SnakeHighscore justAbove = this.HighscoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
                if (justAbove != null)
                    newIndex = this.HighscoreList.IndexOf(justAbove) + 1;
            }

            //Create & insert the entry
            this.HighscoreList.Insert(newIndex, new SnakeHighscore()
            {
                PlayerName = txtPlayerName.Text,
                Score = currentScore
            });

            //Make sure you don't have too many entries
            while (this.HighscoreList.Count > MaxHighscoreListEntryCount)
                this.HighscoreList.RemoveAt(MaxHighscoreListEntryCount);

            SaveHighscoreList();

            bdrInsertPlayerName.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
        private void BtnHideHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Visible;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMuteBackgroundMusic_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke((Action)(() => { musicPlayer.Mute(); }));
        }
    }
}
