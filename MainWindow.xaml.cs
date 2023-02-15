using Snaek.models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private SolidColorBrush foodBrush = new SolidColorBrush(Color.FromRgb(212, 39, 39));
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
        private bool enableFastfood = false;

        public AudioPlayer effectPlayer = new();
        public AudioPlayer musicPlayer = new();
 
        /// <summary>
        /// Starts the background music.
        /// Initializes the main window component.
        /// Creates the game ticker and loads the highscore list.
        /// </summary>
        public MainWindow()
        {
            //musicPlayer.PlayLoop(@"resources/audio/music/backgroundMusic.wav", 10);
            musicPlayer.PlayLoop(@"backgroundMusic.wav", 10);
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
            LoadHighscoreList();
        }

        //Game states
        /// <summary>
        /// Resets all game values and starts a new game, this is called by the spacebar click trigger.
        /// </summary>
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
            if(!enableFastfood)
            {
                gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
            } else
            {
                gameTickTimer.Interval = TimeSpan.FromMilliseconds(250);
            }

            //Draw the snake again and new food
            isGamePlaying = true;
            DrawSnake();
            DrawSnakeFood();

            //Update status
            UpdateGameStatus();

            //Go!
            gameTickTimer.IsEnabled = true;
        }
        
        /// <summary>
        /// Pauses and unpauses the game tick timer. Triggered by the tab key click event.
        /// </summary>
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
        
        /// <summary>
        /// Assigns the current score to the tbStatusScore text, is called each time a fruit is eaten.
        /// </summary>
        private void UpdateGameStatus()
        {
            this.tbStatusScore.Text = currentScore.ToString();
        }
        
        /// <summary>
        /// Ends the game and checks to see if the ending score is a highscore.
        /// </summary>
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
        /// <summary>
        /// Changes the snake face depending on direction input and draws the entire thing.
        /// </summary>
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
        
        /// <summary>
        /// Chooses a random position on the board and draws some food for the snake
        /// </summary>
        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            System.Windows.Shapes.Path foodPath = new();
            List<string> foodPaths = new List<string>()
            {
                //Apple
                "M 22,8 C 19.679,8 18.035,9.009 17.062,9.857 17.47,5.698 20,4 20,4 L 18,3 C 18,3 15.42,5.587 15.048,9.955 14.09,9.089 12.411,8 10,8 6,8 3,11 4,17 c 1.315,7.892 5,11 8,11 2,0 4,-2 4,-2 0,0 2,2 4,2 3,0 6.685,-3.108 8,-11 1,-6 -2,-9 -6,-9 z",
                //Chicken thigh
                "M 40.2336 2.4518 a 2.4563 2.4563 90 0 0 -1.0931 -0.6323 a 2.4582 2.4582 90 1 0 -4.4998 1.8694 L 26.496 11.8362 l -0.0045 -0.0045 a 0.64 0.64 90 0 0 -0.6688 -0.1504 L 15.4349 15.4067 a 11.7171 11.7171 90 0 1 -3.513 0.6778 a 12.4429 12.4429 90 0 0 -0.1376 24.8563 c 0.2291 0.0128 0.457 0.0192 0.6848 0.0192 A 12.4026 12.4026 90 0 0 24.8755 29.0406 a 11.7478 11.7478 90 0 1 0.6778 -3.5168 l 3.7261 -10.384 a 0.64 0.64 90 0 0 -0.1498 -0.6688 l -0.0051 -0.0045 L 37.2704 6.32 a 2.4595 2.4595 90 0 0 1.2307 0.3322 h 0.009 a 2.432 2.432 90 0 0 1.7331 -0.7168 A 2.4659 2.4659 90 0 0 40.2336 2.4518 Z M 24.3482 25.0918 A 13.0234 13.0234 90 0 0 23.5968 28.992 A 11.1821 11.1821 90 1 1 11.9718 17.3632 a 12.9965 12.9965 90 0 0 3.895 -0.7514 l 10.0045 -3.5885 l 2.0659 2.0659 Z M 39.3382 5.0304 a 1.1693 1.1693 90 0 1 -0.832 0.3418 a 1.1878 1.1878 90 0 1 -0.841 -0.3514 a 0.6586 0.6586 90 0 0 -0.905 0 L 28.224 13.5603 l -0.8205 -0.8205 L 35.9392 4.2003 a 0.64 0.64 90 0 0 0 -0.905 a 1.191 1.191 90 0 1 -0.3514 -0.8416 a 1.1616 1.1616 90 0 1 0.3418 -0.832 a 1.1859 1.1859 90 0 1 2.0166 0.7885 a 0.64 0.64 90 0 0 0.6035 0.6035 A 1.1859 1.1859 90 0 1 39.3382 5.0304 Z",
                //Fries
                "M 37.1486 13.9342 A 0.72 0.72 90 0 0 36.6 13.68 h -0.0432 a 0.72 0.72 90 0 0 -0.301 0.0655 c -0.3478 0.1606 -0.7142 0.3197 -1.0958 0.4795 V 6.48 a 0.72 0.72 90 0 0 -0.72 -0.72 H 32.28 V 3.6 a 0.72 0.72 90 0 0 -0.72 -0.72 H 29.4 V 0.72 a 0.72 0.72 90 0 0 -0.72 -0.72 H 25.8 a 0.72 0.72 90 0 0 -0.72 0.72 V 1.44 H 23.64 V 0.72 a 0.72 0.72 90 0 0 -0.72 -0.72 H 17.16 a 0.72 0.72 90 0 0 -0.72 0.72 V 1.44 H 15 V 0.72 a 0.72 0.72 90 0 0 -0.72 -0.72 H 11.4 a 0.72 0.72 90 0 0 -0.72 0.72 V 2.88 H 8.52 a 0.72 0.72 90 0 0 -0.72 0.72 V 5.76 H 5.64 a 0.72 0.72 90 0 0 -0.72 0.72 V 14.225 c -0.3816 -0.1598 -0.7481 -0.319 -1.0958 -0.4795 A 0.936 0.936 90 0 0 3.48 13.68 a 0.72 0.72 90 0 0 -0.7106 0.8359 l 5.04 30.96 A 0.72 0.72 90 0 0 8.52 46.08 H 31.56 a 0.72 0.72 90 0 0 0.7106 -0.6041 l 5.04 -30.96 A 0.72 0.72 90 0 0 37.1486 13.9342 Z M 33.72 7.2 V 14.7967 l -0.0619 0.023 c -0.4162 0.1548 -0.8482 0.3053 -1.301 0.4543 L 32.28 15.3 V 7.2 Z M 30.84 4.32 V 15.7457 l -0.1238 0.036 c -0.1231 0.036 -0.2419 0.072 -0.36 0.103 s -0.2513 0.072 -0.3737 0.1037 l -0.3312 0.0871 L 29.4 16.1395 V 4.32 Z M 26.52 1.44 h 1.44 V 16.4693 l -0.0432 0.0086 c -0.1246 0.0252 -0.2556 0.0497 -0.3852 0.072 c -0.1001 0.0187 -0.198 0.0382 -0.301 0.0569 c -0.144 0.0266 -0.2988 0.0518 -0.45 0.0778 L 26.52 16.7306 V 1.44 Z M 25.08 2.88 V 16.9366 c -0.1325 0.0173 -0.2642 0.0346 -0.3967 0.0504 c -0.2434 0.0288 -0.4867 0.0562 -0.7294 0.0806 c -0.1051 0.0108 -0.2095 0.0187 -0.3139 0.0288 V 2.88 Z M 22.2 2.16 V 17.208 h -0.0122 c -0.5098 0.0302 -0.9799 0.0504 -1.4278 0.0605 V 1.44 h 1.44 Z M 17.88 1.44 h 1.44 V 17.2714 c -0.4478 -0.0101 -0.918 -0.0302 -1.4278 -0.0605 H 17.88 V 1.44 Z M 16.44 2.88 V 17.0964 c -0.1044 -0.0101 -0.2088 -0.018 -0.3139 -0.0288 c -0.2426 -0.0245 -0.486 -0.0518 -0.7294 -0.0806 c -0.1325 -0.0158 -0.2642 -0.0331 -0.3967 -0.0504 V 2.88 Z M 12.12 1.44 h 1.44 V 16.7306 l -0.2585 -0.0439 c -0.1526 -0.0259 -0.306 -0.0511 -0.4529 -0.0778 l -0.288 -0.0547 c -0.1332 -0.0252 -0.2678 -0.0504 -0.3967 -0.0763 L 12.12 16.4693 V 1.44 Z M 9.24 4.32 h 1.44 V 16.1395 l -0.2498 -0.0641 l -0.329 -0.0871 c -0.1267 -0.0338 -0.2563 -0.072 -0.3866 -0.1073 c -0.113 -0.0317 -0.2254 -0.0626 -0.3406 -0.0965 c -0.0439 -0.0122 -0.09 -0.0266 -0.1339 -0.0396 V 4.32 Z m -1.44 2.88 V 15.3 l -0.0835 -0.0274 c -0.45 -0.149 -0.8806 -0.2988 -1.296 -0.4529 L 6.36 14.7967 V 7.2 Z M 5.3779 15.9638 c 0.3046 0.1195 0.6199 0.2354 0.9403 0.3506 c 0.6257 0.2239 1.2823 0.4421 1.9922 0.6581 h 0 q 0.4039 0.1231 0.7855 0.2326 c 0.7675 0.216 1.4746 0.401 2.1449 0.5508 c 0.8705 0.1937 1.8302 0.3665 2.9326 0.5278 c 0.9727 0.144 1.9591 0.2484 2.9326 0.3182 a 35.7509 35.7509 90 0 0 5.8637 0 c 0.9734 -0.072 1.9598 -0.1771 2.9326 -0.3182 q 0.4126 -0.0605 0.8006 -0.1231 c 0.7762 -0.126 1.4789 -0.2592 2.1319 -0.4046 h 0 c 0.2239 -0.0497 0.4507 -0.103 0.6833 -0.1598 c 0.6977 -0.1714 1.44 -0.3773 2.2493 -0.6228 c 1.0735 -0.3269 2.0333 -0.6566 2.9326 -1.008 c 0.3406 -0.1325 0.6653 -0.2635 0.9778 -0.3938 L 34.4638 23.04 H 23.64 a 0.72 0.72 90 0 0 -0.509 0.211 L 20.04 26.3419 l -3.091 -3.091 A 0.72 0.72 90 0 0 16.44 23.04 H 5.6162 L 4.4002 15.57 C 4.7126 15.7003 5.0374 15.8314 5.3779 15.9638 Z M 5.856 24.48 h 10.2888 l 3.389 3.389 a 0.72 0.72 90 0 0 1.0181 0 L 23.9381 24.48 H 34.224 L 32.352 36 H 7.728 Z M 30.9473 44.64 h -21.816 L 7.9606 37.44 H 32.1194 Z",
                //Taco
                "M 19.2 4.8 A 19.2216 19.2216 90 0 0 0 24 V 33 a 0.6 0.6 90 0 0 0.6 0.6 H 37.8 a 0.6 0.6 90 0 0 0.6 -0.6 V 24 A 19.2216 19.2216 90 0 0 19.2 4.8 Z m 0 1.2 A 18.021 18.021 90 0 1 37.1322 22.4928 a 2.3856 2.3856 90 0 0 -0.6222 -0.24 A 2.403 2.403 90 0 0 34.2 19.2 h -0.0162 l -0.0468 -0.06 A 2.3652 2.3652 90 0 0 34.2 18.6 a 2.4 2.4 90 0 0 -2.4 -2.4 a 2.4 2.4 90 0 0 -3.054 -2.3088 A 2.4042 2.4042 90 0 0 26.4 12 a 2.3688 2.3688 90 0 0 -1.3344 0.4074 A 2.4048 2.4048 90 0 0 22.8 10.8 a 2.3772 2.3772 90 0 0 -1.5438 0.5628 a 2.4 2.4 90 0 0 -4.1124 0 A 2.3772 2.3772 90 0 0 15.6 10.8 a 2.4048 2.4048 90 0 0 -2.2656 1.6074 A 2.3688 2.3688 90 0 0 12 12 a 2.4042 2.4042 90 0 0 -2.346 1.8912 A 2.4 2.4 90 0 0 6.6 16.2 a 2.4 2.4 90 0 0 -2.4 2.4 a 2.3652 2.3652 90 0 0 0.063 0.54 l -0.0468 0.06 A 2.403 2.403 90 0 0 1.89 22.2546 a 2.3856 2.3856 90 0 0 -0.6222 0.24 A 18.021 18.021 90 0 1 19.2 6 Z M 2.4468 23.406 H 2.46 l 0.0864 0.0096 a 0.6 0.6 90 0 0 0.4254 -0.1362 a 19.0758 19.0758 90 0 0 -1.2474 2.313 a 0.4518 0.4518 90 0 0 -0.0336 -0.0342 A 1.1868 1.1868 90 0 1 1.2 24.6 A 1.206 1.206 90 0 1 2.4468 23.406 Z m 0.7548 -1.14 A 1.1742 1.1742 90 0 1 3 21.6 a 1.1928 1.1928 90 0 1 1.38 -1.1802 a 0.5964 0.5964 90 0 0 0.5898 -0.2304 c 0.1374 -0.18 0.2814 -0.36 0.4248 -0.5328 a 0.6 0.6 90 0 0 0.096 -0.6066 a 1.2 1.2 90 0 1 1.56 -1.56 a 0.5904 0.5904 90 0 0 0.6414 -0.1272 c 0.0132 -0.0114 0.0564 -0.0504 0.0684 -0.063 a 0.6 0.6 90 0 0 0.129 -0.6432 A 1.23 1.23 90 0 1 7.8 16.2 a 1.2 1.2 90 0 1 2.0298 -0.8658 a 0.6234 0.6234 90 0 0 1.02 -0.6108 A 1.2294 1.2294 90 0 1 10.8 14.4 a 1.1958 1.1958 90 0 1 2.2278 -0.609 a 0.6 0.6 90 0 0 0.7032 0.2604 l 0.2778 -0.0906 a 0.6 0.6 90 0 0 0.4062 -0.63 l -0.0084 -0.0702 c 0 -0.0198 -0.006 -0.0402 -0.0066 -0.06 a 1.1964 1.1964 90 0 1 2.3466 -0.3336 A 0.6426 0.6426 90 0 0 18 12.6 a 1.2 1.2 90 0 1 2.4 -0.0132 c 0 0.0192 -0.006 0.084 -0.006 0.1032 a 0.6258 0.6258 90 0 0 0.5922 0.6 a 0.6 0.6 90 0 0 0.6672 -0.42 A 1.1964 1.1964 90 0 1 24 13.2 c 0 0.021 -0.0042 0.0414 -0.0066 0.06 l -0.0084 0.0702 a 0.6 0.6 90 0 0 0.4062 0.63 l 0.2778 0.0906 a 0.6 0.6 90 0 0 0.7032 -0.2604 A 1.1958 1.1958 90 0 1 27.6 14.4 a 1.2294 1.2294 90 0 1 -0.051 0.3234 a 0.6072 0.6072 90 0 0 0.2754 0.6732 a 0.6 0.6 90 0 0 0.7458 -0.06 A 1.2 1.2 90 0 1 30.6 16.2 a 1.23 1.23 90 0 1 -0.0888 0.4554 a 0.6 0.6 90 0 0 0.129 0.6432 c 0.021 0.021 0.0444 0.042 0.0636 0.06 a 0.6 0.6 90 0 0 0.6462 0.1326 a 1.2 1.2 90 0 1 1.56 1.56 a 0.6 0.6 90 0 0 0.096 0.6066 c 0.1434 0.1716 0.2874 0.3498 0.4248 0.5328 a 0.5964 0.5964 90 0 0 0.5898 0.2304 A 1.191 1.191 90 0 1 35.4 21.6 a 1.1742 1.1742 90 0 1 -0.2016 0.6642 a 0.6 0.6 90 0 0 -0.0408 0.606 l 0.003 0.0066 a 19.2642 19.2642 90 0 0 -31.92 0 l 0 -0.0066 A 0.6 0.6 90 0 0 3.2016 22.2642 Z M 36.7104 25.56 a 0.4518 0.4518 90 0 0 -0.0336 0.0342 A 18.9732 18.9732 90 0 0 35.43 23.28 a 0.5958 0.5958 90 0 0 0.4248 0.135 L 36 23.4 a 1.2 1.2 90 0 1 1.2 1.2 A 1.1868 1.1868 90 0 1 36.7104 25.56 Z M 19.2 15.6 A 17.9124 17.9124 90 0 1 35.2932 25.488 a 19.2492 19.2492 90 0 0 -32.1864 0 A 17.9124 17.9124 90 0 1 19.2 15.6 Z M 1.56 32.4 a 18.006 18.006 90 0 1 35.28 0 Z",
                //Hotdog
                "M 21.9616 2.1732 a 3.7974 3.7974 90 0 0 -6.5868 -0.4902 a 3.5526 3.5526 90 0 0 -0.258 3.3954 A 34.3356 34.3356 90 0 1 18.1378 19.2 a 34.3356 34.3356 90 0 1 -3.021 14.1216 a 3.5526 3.5526 90 0 0 0.258 3.3954 a 3.7974 3.7974 90 0 0 6.5868 -0.4902 A 41.3874 41.3874 90 0 0 25.6 19.2 A 41.3874 41.3874 90 0 0 21.9616 2.1732 Z M 20.8654 35.7366 a 2.5974 2.5974 90 0 1 -4.4832 0.3282 a 2.3088 2.3088 90 0 1 -0.3 -1.8648 H 18.4 a 0.6 0.6 90 0 0 0 -1.2 h -1.848 a 36.0246 36.0246 90 0 0 1.2894 -3.6 H 19.6 a 0.6 0.6 90 0 0 0 -1.2 H 18.1828 a 35.4834 35.4834 90 0 0 0.7452 -3.6144 A 0.5664 0.5664 90 0 0 19 24.6 h 1.8 a 0.6 0.6 90 0 0 0 -1.2 H 19.0846 a 35.8674 35.8674 90 0 0 0.24 -3.6 H 20.8 a 0.6 0.6 90 0 0 0 -1.2 H 19.3222 a 35.8674 35.8674 90 0 0 -0.24 -3.6 H 20.8 a 0.6 0.6 90 0 0 0 -1.2 H 19 a 0.5664 0.5664 90 0 0 -0.072 0.0144 A 35.4834 35.4834 90 0 0 18.1828 10.2 H 19.6 a 0.6 0.6 90 0 0 0 -1.2 H 17.8408 a 36.0246 36.0246 90 0 0 -1.2894 -3.6 H 18.4 a 0.6 0.6 90 0 0 0 -1.2 H 16.0804 a 2.3088 2.3088 90 0 1 0.3 -1.8648 a 2.5974 2.5974 90 0 1 4.4832 0.3282 A 40.1814 40.1814 90 0 1 24.4 19.2 A 40.1814 40.1814 90 0 1 20.8654 35.7366 Z",
                //Popcorn
                "M 35.727 2.886 A 4.2462 4.2462 90 0 0 31.8 0 a 4.0914 4.0914 90 0 0 -3.2928 1.6974 A 4.029 4.029 90 0 0 25.8 0.645 a 4.0746 4.0746 90 0 0 -3.06 1.4112 a 4.0704 4.0704 90 0 0 -7.074 0 A 4.0746 4.0746 90 0 0 12.6 0.645 a 4.029 4.029 90 0 0 -2.7072 1.0524 A 4.0914 4.0914 90 0 0 6.6 0 A 4.2462 4.2462 90 0 0 2.673 2.886 A 4.4472 4.4472 90 0 0 0 7.0506 a 4.3938 4.3938 90 0 0 3.6336 4.4298 a 4.5894 4.5894 90 0 0 1.17 2.544 c 0 0.0258 -0.0036 0.0504 -0.0036 0.0756 c 0 0.042 0 0.063 2.403 23.76 A 0.6 0.6 90 0 0 7.8 38.4 H 30.6 A 0.6 0.6 90 0 0 31.2 37.86 l 2.4 -23.7 c 0 -0.0204 0.003 -0.0408 0.003 -0.06 s -0.003 -0.0498 -0.0036 -0.0756 a 4.5894 4.5894 90 0 0 1.17 -2.544 A 4.3938 4.3938 90 0 0 38.4 7.0506 A 4.4472 4.4472 90 0 0 35.727 2.886 Z M 8.34 37.2 c -0.3546 -3.507 -2.3184 -22.8678 -2.34 -23.1 c 0 -0.0936 0.0072 -0.186 0.0138 -0.2766 A 3.3168 3.3168 90 0 1 9.3 10.8 a 3.2832 3.2832 90 0 1 2.7918 1.5474 a 0.6264 0.6264 90 0 0 0.0942 0.1152 L 13.1754 37.2 Z m 5.8734 -25.932 c 0.0696 -0.042 0.141 -0.0792 0.2136 -0.1158 c 0.048 -0.0246 0.0972 -0.048 0.147 -0.0702 c 0.06 -0.027 0.12 -0.0522 0.1842 -0.075 s 0.1332 -0.0462 0.201 -0.066 c 0.0528 -0.0162 0.1062 -0.0324 0.1602 -0.0456 c 0.0828 -0.0198 0.1674 -0.0348 0.2526 -0.0486 c 0.0432 -0.0072 0.0858 -0.0162 0.1302 -0.0216 a 3.2094 3.2094 90 0 1 0.8076 0 c 0.0474 0.006 0.093 0.018 0.1392 0.0258 c 0.0876 0.015 0.1752 0.0288 0.2604 0.0498 c 0.0546 0.0144 0.1068 0.0336 0.1608 0.0504 c 0.0744 0.0228 0.1494 0.0444 0.222 0.0726 c 0.0558 0.0222 0.1098 0.0492 0.1644 0.0738 c 0.0672 0.0306 0.1344 0.06 0.1992 0.0948 c 0.0546 0.0294 0.1062 0.063 0.1596 0.0954 s 0.12 0.0744 0.18 0.1164 s 0.099 0.0762 0.1482 0.1152 s 0.1128 0.0894 0.1662 0.1386 s 0.0894 0.087 0.1332 0.132 s 0.1026 0.105 0.1506 0.1614 c 0.0402 0.0474 0.0786 0.0972 0.1164 0.147 c 0.03 0.0396 0.06 0.0798 0.087 0.12 V 37.2 H 14.3766 L 13.368 12 a 3.3294 3.3294 90 0 1 0.7698 -0.6822 C 14.1636 11.301 14.1888 11.2836 14.2152 11.268 Z M 19.8 12.2196 c 0.0282 -0.0408 0.057 -0.081 0.087 -0.12 c 0.0378 -0.0498 0.0762 -0.0996 0.1164 -0.147 c 0.048 -0.0564 0.099 -0.1092 0.1506 -0.1614 s 0.087 -0.09 0.1332 -0.132 s 0.1098 -0.0936 0.1662 -0.1386 s 0.0972 -0.0792 0.1482 -0.1152 s 0.12 -0.0786 0.18 -0.1164 s 0.105 -0.066 0.1596 -0.0954 c 0.0648 -0.0348 0.132 -0.0642 0.1992 -0.0948 c 0.0546 -0.0246 0.1086 -0.0516 0.1644 -0.0738 c 0.0726 -0.0282 0.1476 -0.0498 0.222 -0.0726 c 0.054 -0.0168 0.1062 -0.036 0.1608 -0.0504 c 0.0852 -0.021 0.1728 -0.0348 0.2604 -0.0498 c 0.0462 -0.0078 0.0918 -0.0198 0.1392 -0.0258 a 3.2094 3.2094 90 0 1 0.8076 0 c 0.0438 0.0054 0.087 0.0144 0.1302 0.0216 c 0.0852 0.0138 0.1698 0.0288 0.2526 0.0486 c 0.054 0.0132 0.1074 0.03 0.1602 0.0456 c 0.0678 0.0198 0.135 0.0414 0.201 0.066 s 0.1236 0.048 0.1842 0.075 c 0.0498 0.0222 0.099 0.0456 0.147 0.0702 c 0.0726 0.0366 0.144 0.0738 0.2136 0.1158 c 0.0264 0.0156 0.0516 0.033 0.0774 0.0498 A 3.3294 3.3294 90 0 1 25.032 12 L 24.0234 37.2 H 19.8 Z M 30.06 37.2 H 25.2246 L 26.214 12.4626 a 0.6264 0.6264 90 0 0 0.0942 -0.1152 A 3.2832 3.2832 90 0 1 29.1 10.8 a 3.3168 3.3168 90 0 1 3.2856 3.0222 c 0.0066 0.0828 0.0132 0.165 0.0144 0.2496 Z M 34.2 10.32 a 0.6 0.6 90 0 0 -0.6 0.6 a 3.4872 3.4872 90 0 1 -0.3336 1.4922 c -0.0084 -0.0216 -0.0216 -0.0408 -0.0306 -0.06 A 4.614 4.614 90 0 0 33 11.88 c -0.0168 -0.0288 -0.0318 -0.06 -0.0492 -0.0876 a 4.5816 4.5816 90 0 0 -0.3288 -0.4704 c -0.0258 -0.033 -0.0528 -0.0642 -0.0798 -0.096 A 4.521 4.521 90 0 0 32.1444 10.8 l -0.0036 -0.0036 a 4.5564 4.5564 90 0 0 -0.459 -0.3654 c -0.0342 -0.024 -0.0678 -0.048 -0.1026 -0.0714 a 4.4568 4.4568 90 0 0 -0.4968 -0.2904 c -0.0282 -0.0138 -0.057 -0.0252 -0.0852 -0.0384 a 4.4364 4.4364 90 0 0 -0.495 -0.1986 c -0.0306 -0.0102 -0.06 -0.0222 -0.0918 -0.0318 a 4.4004 4.4004 90 0 0 -0.57 -0.1344 c -0.0426 -0.0072 -0.0846 -0.0126 -0.1278 -0.0186 A 4.3944 4.3944 90 0 0 29.1 9.6 a 4.656 4.656 90 0 0 -0.486 0.0264 c -0.0552 0.006 -0.1092 0.018 -0.1638 0.0258 c -0.1038 0.015 -0.2076 0.0294 -0.3096 0.0516 c -0.0648 0.0138 -0.1278 0.0342 -0.192 0.051 c -0.0888 0.0234 -0.18 0.0456 -0.2658 0.075 c -0.0672 0.0222 -0.1326 0.0498 -0.1986 0.075 c -0.081 0.0312 -0.162 0.06 -0.24 0.0972 c -0.0666 0.0306 -0.1308 0.0654 -0.1956 0.0984 c -0.075 0.0384 -0.15 0.0768 -0.2226 0.12 s -0.126 0.0798 -0.1884 0.12 s -0.138 0.0912 -0.2052 0.141 s -0.12 0.0924 -0.18 0.1404 s -0.1278 0.1062 -0.189 0.1626 s -0.1092 0.105 -0.1626 0.1596 c -0.0336 0.0342 -0.0702 0.0648 -0.1026 0.1002 a 4.5048 4.5048 90 0 0 -0.8034 -0.6828 c -0.0186 -0.0126 -0.0378 -0.024 -0.0564 -0.036 c -0.1152 -0.075 -0.2328 -0.144 -0.354 -0.2076 c -0.0426 -0.0228 -0.0864 -0.0444 -0.1302 -0.066 c -0.1002 -0.048 -0.2016 -0.093 -0.3054 -0.1338 c -0.057 -0.0228 -0.114 -0.045 -0.1716 -0.0654 c -0.0972 -0.0336 -0.195 -0.06 -0.2946 -0.0888 c -0.06 -0.0174 -0.1242 -0.036 -0.1878 -0.0504 c -0.1062 -0.0234 -0.2136 -0.0408 -0.3216 -0.057 c -0.057 -0.0084 -0.1134 -0.0204 -0.1704 -0.027 a 4.266 4.266 90 0 0 -1.0344 0.0042 c -0.0564 0.0066 -0.111 0.0174 -0.1668 0.0264 c -0.12 0.018 -0.2358 0.0396 -0.3516 0.0672 c -0.06 0.015 -0.1236 0.0318 -0.1848 0.0492 c -0.1098 0.0312 -0.2172 0.0666 -0.3234 0.1062 c -0.06 0.021 -0.1164 0.042 -0.174 0.0654 c -0.12 0.0492 -0.231 0.1044 -0.3432 0.1632 c -0.0408 0.021 -0.0828 0.039 -0.12 0.06 a 4.4748 4.4748 90 0 0 -0.4308 0.2748 c -0.0366 0.0264 -0.0702 0.057 -0.1062 0.0846 c -0.1014 0.078 -0.2004 0.159 -0.2952 0.246 c -0.0468 0.0426 -0.0906 0.0876 -0.135 0.132 s -0.0936 0.087 -0.1368 0.1338 c -0.0432 -0.0468 -0.0912 -0.0888 -0.1368 -0.1338 s -0.0882 -0.0894 -0.135 -0.132 c -0.0948 -0.087 -0.1944 -0.1686 -0.2958 -0.2466 c -0.0354 -0.027 -0.069 -0.06 -0.105 -0.0834 a 4.4478 4.4478 90 0 0 -0.4314 -0.2754 c -0.0396 -0.0228 -0.0816 -0.0408 -0.12 -0.06 c -0.1122 -0.06 -0.2262 -0.114 -0.3432 -0.1632 c -0.06 -0.0234 -0.1158 -0.0444 -0.174 -0.0654 c -0.1062 -0.0396 -0.2136 -0.075 -0.3234 -0.1062 c -0.06 -0.0174 -0.12 -0.0342 -0.1848 -0.0492 c -0.1158 -0.0276 -0.2334 -0.0492 -0.3516 -0.0678 c -0.0558 -0.0084 -0.1104 -0.0192 -0.1668 -0.0258 a 4.266 4.266 90 0 0 -1.0344 -0.0042 c -0.06 0.0066 -0.1134 0.0186 -0.1704 0.027 c -0.108 0.0162 -0.2154 0.033 -0.3216 0.057 c -0.0636 0.0144 -0.1254 0.033 -0.1884 0.0504 c -0.099 0.027 -0.1974 0.0552 -0.294 0.0888 c -0.06 0.0204 -0.1146 0.0426 -0.1716 0.0654 c -0.1038 0.0408 -0.2052 0.0858 -0.3054 0.1338 c -0.0438 0.0216 -0.0876 0.0432 -0.1302 0.066 q -0.18 0.0954 -0.3534 0.2076 c -0.0192 0.012 -0.0384 0.0234 -0.06 0.036 A 4.5048 4.5048 90 0 0 12.6 11.0442 c -0.0324 -0.0354 -0.069 -0.066 -0.1026 -0.1002 c -0.0534 -0.0546 -0.1068 -0.108 -0.1626 -0.1596 s -0.1248 -0.1098 -0.189 -0.1626 s -0.1164 -0.0954 -0.177 -0.1404 s -0.1362 -0.0954 -0.2058 -0.141 s -0.1236 -0.0828 -0.1878 -0.12 s -0.1476 -0.081 -0.2226 -0.12 c -0.0648 -0.033 -0.129 -0.0678 -0.1956 -0.0984 c -0.0792 -0.0354 -0.1602 -0.066 -0.24 -0.0972 c -0.066 -0.0252 -0.1314 -0.0528 -0.1986 -0.075 c -0.087 -0.0294 -0.177 -0.0516 -0.2664 -0.075 c -0.0642 -0.0174 -0.1266 -0.0372 -0.1914 -0.051 c -0.102 -0.0222 -0.2058 -0.0366 -0.3096 -0.0516 c -0.0552 -0.0078 -0.1086 -0.0198 -0.1638 -0.0258 A 4.656 4.656 90 0 0 9.3 9.6 a 4.3944 4.3944 90 0 0 -0.6126 0.0474 c -0.0432 0.006 -0.0852 0.0114 -0.1278 0.0186 a 4.4004 4.4004 90 0 0 -0.57 0.1344 c -0.0312 0.0096 -0.06 0.0216 -0.0924 0.0318 a 4.5942 4.5942 90 0 0 -0.4944 0.1986 c -0.0282 0.0132 -0.057 0.0246 -0.0852 0.039 a 4.5072 4.5072 90 0 0 -0.4968 0.2898 c -0.0348 0.0234 -0.0684 0.0474 -0.1026 0.0714 a 4.5 4.5 90 0 0 -0.4584 0.3654 L 6.2544 10.8 a 4.56 4.56 90 0 0 -0.3972 0.42 c -0.027 0.0324 -0.054 0.0642 -0.0804 0.0972 a 4.5552 4.5552 90 0 0 -0.3276 0.4692 c -0.018 0.0294 -0.033 0.06 -0.0498 0.0888 a 4.5 4.5 90 0 0 -0.2358 0.4722 c -0.009 0.021 -0.0216 0.0396 -0.03 0.06 A 3.4872 3.4872 90 0 1 4.8 10.92 a 0.6 0.6 90 0 0 -0.6 -0.6 a 3.1476 3.1476 90 0 1 -3 -3.27 A 3.2694 3.2694 90 0 1 3.3408 3.9174 a 0.6 0.6 90 0 0 0.3924 -0.4074 A 3.0258 3.0258 90 0 1 6.6 1.2 a 2.9736 2.9736 90 0 1 2.655 1.7526 a 0.6 0.6 90 0 0 1.02 0.099 A 2.901 2.901 90 0 1 12.6 1.845 a 2.9736 2.9736 90 0 1 2.6364 1.7106 a 0.588 0.588 90 0 0 0.5886 0.3294 a 0.6 0.6 90 0 0 0.5226 -0.426 a 2.9304 2.9304 90 0 1 5.7048 0 a 0.6 0.6 90 0 0 0.5226 0.426 a 0.5856 0.5856 90 0 0 0.5886 -0.3294 A 2.9736 2.9736 90 0 1 25.8 1.845 a 2.901 2.901 90 0 1 2.3244 1.2066 a 0.6048 0.6048 90 0 0 0.54 0.237 a 0.6 0.6 90 0 0 0.48 -0.336 A 2.9736 2.9736 90 0 1 31.8 1.2 a 3.0258 3.0258 90 0 1 2.8668 2.31 a 0.6 0.6 90 0 0 0.3924 0.4074 A 3.2694 3.2694 90 0 1 37.2 7.0506 A 3.1476 3.1476 90 0 1 34.2 10.32 Z",
            };
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            if (!enableFastfood)
            {
                foodPath.Data = (Geometry)converter.ConvertFrom(foodPaths[0]);

            } else
            {
                foodPath.Data = (Geometry)converter.ConvertFrom(foodPaths[rnd.Next(1, 6)]);
            }
            snakeFood = new System.Windows.Shapes.Path()
            {
                Data = foodPath.Data,
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

        //Highscore list
        public ObservableCollection<SnakeHighscore> HighscoreList { get; set; } = new ObservableCollection<SnakeHighscore>();
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
        
        //Events
        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
            directionChosen = false;
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Visible;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //Implements a secret :-)
            if(Keyboard.IsKeyDown(Key.Up) && Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                this.CbEnableFastfood.Visibility = Visibility.Visible; 
            }
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
        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.IsChecked == true)
            {
                Debug.Write("im checked");
                enableFastfood = true;
            } else
            {
                Debug.Write("im unchecked");

                enableFastfood = false;
            }
        }
    }
}
