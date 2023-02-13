using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        private int changedDirection = 0;

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
            Sound music = new();
            music.PlayLoop(@"resources/audio/music/backgroundMusic.wav", 10);
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            changedDirection = 0;
            MoveSnake();
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
                    snakePart.UiElement = new Ellipse()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
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
            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.UiElement as Ellipse).Fill = snakeBodyBrush;
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
                    if (changedDirection < 2 && snakeDirection != SnakeDirection.Down)
                        snakeDirection = SnakeDirection.Up; changedDirection++; break;
                case Key.Down:
                    if (changedDirection < 2 && snakeDirection != SnakeDirection.Up)
                        snakeDirection = SnakeDirection.Down; changedDirection++; break;
                case Key.Left:
                    if (changedDirection < 2 && snakeDirection != SnakeDirection.Right)
                        snakeDirection = SnakeDirection.Left; changedDirection++; break;
                case Key.Right:
                    if (changedDirection < 2 && snakeDirection != SnakeDirection.Left)
                        snakeDirection = SnakeDirection.Right; changedDirection++; break;
                case Key.Space:
                    StartNewGame();
                    break;
                    //case Key.Tab:
                    //Implement scoreboard viewing
            }
            if (snakeDirection != originalSnakeDirection)
            {
                MoveSnake();
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
