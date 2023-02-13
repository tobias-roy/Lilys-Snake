using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

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

        public Sound snakeVoice = new();
        public Sound music = new();
 

        public MainWindow()
        {
            Task.Run(() => BackgroundMusicAsync());
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
            LoadHighscoreList();
        }

        private async Task BackgroundMusicAsync()
        {
            await Task.Delay(500);
            music.PlayLoop(@"resources/audio/music/backgroundMusic.wav", 10);
           
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

        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    System.Windows.Shapes.Path snakeface = new();
                    string snakefaceDataUp = "M 12 40 L 26 40 L 29 39 L 30 38 L 31 35 L 31 33 L 30 19 L 31 18 L 31 17 L 30 16 L 28 10 L 27 9 L 25 8 L 21 7 L 20 7 L 19 7 L 22 0 L 21 1 L 19 5 L 17 1 L 16 0 L 19 7 L 18 7 L 17 7 L 13 8 L 11 9 L 10 10 L 8 16 L 7 17 L 7 18 L 8 19 L 7 33 L 7 35 L 8 38 L 9 39 L 12 40";
                    string snakefaceDataRight = "M 0 12 L 0 26 L 1 29 L 2 30 L 5 31 L 7 31 L 21 30 L 22 31 L 23 31 L 24 30 L 30 28 L 31 27 L 32 25 L 33 21 L 33 20 L 33 19 L 40 22 L 39 21 L 35 19 L 39 17 L 40 16 L 33 19 L 33 18 L 33 17 L 32 13 L 31 11 L 30 10 L 24 8 L 23 7 L 22 7 L 21 8 L 7 7 L 5 7 L 2 8 L 1 9 L 0 12";
                    string snakefaceDataDown = "M 29 0 L 15 0 L 12 1 L 11 2 L 10 5 L 10 7 L 11 21 L 10 22 L 10 23 L 11 24 L 13 30 L 14 31 L 16 32 L 20 33 L 21 33 L 22 33 L 19 40 L 20 39 L 22 35 L 24 39 L 25 40 L 22 33 L 23 33 L 24 33 L 28 32 L 30 31 L 31 30 L 33 24 L 34 23 L 34 22 L 33 21 L 34 7 L 34 5 L 33 2 L 32 1 L 29 0";
                    string snakefaceDataLeft = "M 40 27 L 40 13 L 39 10 L 38 9 L 35 8 L 33 8 L 19 9 L 18 8 L 17 8 L 16 9 L 10 11 L 9 12 L 8 14 L 7 18 L 7 19 L 7 20 L 0 17 L 1 18 L 5 20 L 1 22 L 0 23 L 7 20 L 7 21 L 7 22 L 8 26 L 9 28 L 10 29 L 16 31 L 17 32 L 18 32 L 19 31 L 33 32 L 35 32 L 38 31 L 39 30 L 40 27";
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
            string snakebodyLeftRight = "M 0 27 L 40 27 L 40 13 L 0 13 L 0 27";
            string snakebodyUpDown = "M 13 0 L 13 40 L 27 40 L 27 0 L 13 0";
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));

            foreach (SnakePart snakePart in snakeParts)
            {
                switch (snakePart.Direction)
                {
                    case Direction.Up or Direction.Down:
                        snakebody.Data = (Geometry)converter.ConvertFrom(snakebodyUpDown);
                        (snakePart.UiElement as System.Windows.Shapes.Path).Data = snakebody.Data;
                        break;

                    case Direction.Left or Direction.Right:
                        snakebody.Data = (Geometry)converter.ConvertFrom(snakebodyLeftRight);
                        (snakePart.UiElement as System.Windows.Shapes.Path).Data = snakebody.Data;
                        break;
                }
                (snakePart.UiElement as System.Windows.Shapes.Path).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }
            //Determine in which direction to expand the snake, based on the current direction
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            Direction dir = snakeHead.Direction;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    dir = Direction.Left;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    dir = Direction.Right;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    dir = Direction.Up;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    dir = Direction.Down;
                    break;
            }
            //Now add the new head part to our list of snake parts...
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true,
                Direction = dir
            });
            //And then draw the motherfucker
            DrawSnake();
            DoCollisionCheck();
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
            snakeVoice.Play(eatingSounds[rnd.Next(0, 4)], 50);

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
                    snakeVoice.Play(highscoreEnding[rnd.Next(0, 2)], 50);
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
                snakeVoice.Play(loosingEnding[rnd.Next(0, 4)], 50);
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            gameTickTimer.IsEnabled = false;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            //Show some awesome list of players that are good at this game!
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        private void BtnHideHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Visible;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
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

        public class Sound
        {
            private MediaPlayer m_player;

            public void Play(string filename, int volume)
            {
                m_player = new MediaPlayer();
                m_player.Volume = volume / 100.0f;
                m_player.Open(new Uri(filename, UriKind.Relative));
                m_player.Play();
            }

            public void PlayLoop(string filename, int volume)
            {
                m_player = new MediaPlayer();
                m_player.Open(new Uri(filename, UriKind.Relative));
                m_player.Volume = volume / 100.0f;
                m_player.Play();
                TimeSpan duration = TimeSpan.FromSeconds(24);
                RepeatAudio(duration);
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
    }
}
