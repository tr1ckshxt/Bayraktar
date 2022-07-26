﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Serialization;
using BayraktarClient;
using BayraktarGame;

namespace Bayraktar
{
    public partial class MainMenu : UserControl
    {
        private DispatcherTimer cloudLeftTimer = new DispatcherTimer();
        private DispatcherTimer cloudRightTimer = new DispatcherTimer();
        private GameClient _client;
        private void SetTimer(DispatcherTimer timer, int seconds)
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.Tick -= CloudTimer_Tick;
            timer.Tick += CloudTimer_Tick;
            timer.Start();
        }
        public MainMenu()
        {
            InitializeComponent();
            
            Focus();
            SetCloudAnimation(cloudLeftTimer, CloudLeft, true);
            SetCloudAnimation(cloudRightTimer, CloudRight, true);
        }

        Random rnd = new Random();

        #region CloudsAnimation

        private void SetCloudAnimation(DispatcherTimer timer, Image cloud, bool access)
        {
            if (access || rnd.Next(0, 100) % 2 == 0)
            {
                DoubleAnimation DA = new DoubleAnimation
                {
                    From = -200,
                    To = SystemParameters.PrimaryScreenHeight + 100,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                short seconds = (short)rnd.Next(5, 15);
                DA.Duration = TimeSpan.FromSeconds(seconds);

                SetTimer(timer, seconds);

                cloud.BeginAnimation(Canvas.TopProperty, DA);

                RotateTransform rotate = new RotateTransform(rnd.Next(1, 3) == 2 ? 270 : 90);
                cloud.RenderTransform = rotate;
            }
        }

        private void CloudTimer_Tick(object sender, EventArgs e)
        {
            if (sender != null && sender is DispatcherTimer)
            {
                DispatcherTimer tempTimer = sender as DispatcherTimer;

                if (tempTimer == cloudLeftTimer)
                {
                    if (rnd.Next(0, 100) % 2 == 0)
                        CloudLeft.Visibility = CloudLeft.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;

                    if (CloudRight.Visibility != Visibility.Hidden)
                    {
                        Canvas.SetLeft(CloudLeft, rnd.Next(-100, 100));
                        SetCloudAnimation(tempTimer, CloudLeft, false);
                    }
                }
                else
                {
                    if (rnd.Next(0, 100) % 5 == 0)
                        CloudRight.Visibility = CloudRight.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
                    if (CloudRight.Visibility != Visibility.Hidden)
                    {
                        Canvas.SetLeft(CloudRight, rnd.Next(-100, 100));
                        SetCloudAnimation(tempTimer, CloudRight, false);
                    }
                }
            }

        }

        private void removeClouds()
        {
            cloudLeftTimer.Stop();
            cloudRightTimer.Stop();

          //  CloudLeft.BeginAnimation(TopProperty, new DoubleAnimation());
            //CloudRight.BeginAnimation(TopProperty, new DoubleAnimation());
        }

        private void setClouds()
        {
            Canvas.SetTop(CloudLeft, -200);
            Canvas.SetTop(CloudRight, -200);
            SetCloudAnimation(cloudLeftTimer, CloudLeft, true);
            SetCloudAnimation(cloudRightTimer, CloudRight, true);
        }

            #endregion
        

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                removeClouds();
                switch (button.Name)
                {
                    case "Rating":
                        ((Window)Parent).Content= new RatingWindow();
                        break;
                    case "Start":
                        CurrentClient.Instance.StartSingleGame();
                        break;
                    case "StartMultiplayer":
                        CurrentClient.Instance.StartMultiGame();

                        break;

                    default:
                        _exit();
                        break;
                }
                setClouds();
            }

        }

        private void _exit()
        {
            Application.Current.Shutdown();
        }

       

        private void MainMenu_OnKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
                _exit();
        }
        
    }
}
