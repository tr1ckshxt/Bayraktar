﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BayraktarClient;
using BayraktarGame;
using Message;
using Microsoft.FSharp.Core;

namespace Bayraktar
{

    public partial class Game : UserControl
    {

        //Attack or Defense
        private GameRole _gameRole;
        private GameClient _client;
        Window _window => Parent as Window;
        public User User { get; set; }


        private Game()
        {
            InitializeComponent();
            Cursor = new Cursor(System.IO.Path.GetFullPath(@"../Data/Pictures/curOfBayraktar.cur"));
        }
        public Game(GameClient client, GameRole gameRole, int gameWidth = 1920) : this()
        {
            Focus();
            _client = client;
            _client.HealthChanged += HealthChanged;
            _client.DestroyUnit += DestroyUnit;
            _client.GameOver += GameOver;
            _client.SetUnitAction += SetUnitAction;
            _gameRole = gameRole;
            lblRole.Content = _gameRole.ToString();
            switch (gameRole)
            {
                case GameRole.Attack:
                    MouseUp += AddUnit;
                    _client.ScoreAttackChanged += ScoreChanged;
                    break;
                case GameRole.Defense:
                    _client.ScoreDefenseChanged += ScoreChanged;
                    break;
            }

        }

        private void DestroyUnit(int tag)
        {
            _invoke(() =>
            {
                foreach (UserControl control in cnv.Children)
                {
                    if (!(control is MilitaryUnit unit))
                        continue;
                    if ((int)unit.Tag == tag)
                    {
                        unit.Destroy();
                    }
                }
            });
        }

        private void GameOver(GameClient client)
        {
            var result = "Game over";

            _invoke(() =>
            {
                new MessageBox(result) { Owner = Parent as Window }.ShowDialog();
                _exit();
            });
        }

        private void SetUnitAction(MessageUnit unitData)
        {
            _invoke(() => _setUnit(unitData));
        }
        private void _setUnit(MessageUnit unitData)
        {
            MilitaryUnit unit = new MilitaryUnit(unitData.Unit)
            {
                Tag = unitData.Id
            };
            unit.Destroyed += IsDestroyed;
            if (_gameRole == GameRole.Defense)
            {
                unit.MouseLeftButtonUp += UnitDestroy_MouseUp;
            }
            DoubleAnimation da = new DoubleAnimation
            {
                From = -unit.UnitHeight,
                To = SystemParameters.PrimaryScreenHeight + unit.UnitHeight,
                Duration = TimeSpan.FromSeconds(unitData.Speed)
            };
            var x = (unitData.Coords.X > _window.Width - unit.UnitWidth) ? (int)(_window.Width - unit.UnitWidth) : unitData.Coords.X;

            da.Completed += (s, e) =>
            {
                if (!unit.IsDestroying)
                    _hit(unit);
            };
            unit.Animation = da;
            var clock = da.CreateClock();

            Canvas.SetLeft(unit, x);
            unit.ApplyAnimationClock(Canvas.TopProperty, clock);
            cnv.Children.Add(unit);


        }

        private async void IsDestroyed(MilitaryUnit unit)
        {
            _invoke(() =>
            {
                DoubleAnimation da = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(3000)
                };
                unit.ApplyAnimationClock(Canvas.TopProperty, da.CreateClock());

            });
            await Task.Delay(3000);
            cnv.Children.Remove(unit);
        }

        private void UnitDestroy_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is MilitaryUnit unit)
            {
                _client.UnitDestroy((int)unit.Tag);
                unit.MouseLeftButtonUp -= UnitDestroy_MouseUp;
            }
        }

        private void _hit(MilitaryUnit unit)
        {
            _client.Shoot();
            cnv.Children.Remove(unit);
        }

        private void ScoreChanged(int score)
        {
            _invoke(() => lblScore.Content = score);
        }
        private void HealthChanged(int hp)
        {
            _invoke(() => HealthText.Content = hp);
        }



        private void _invoke(Action action)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(action);
            else
                action();
        }


        private void PauseFunc(string title, bool isDefeat)
        {
            PauseWindow pauseWindow = new PauseWindow(title, isDefeat);
            pauseWindow.ShowDialog();
            if (pauseWindow.DialogResult.HasValue && pauseWindow.DialogResult.Value)
            {
                _client.Exit();
            }

        }

        private void _exit()
        {

            try
            {
                _window.Content = new MainMenu();

            }
            catch (Exception e)
            {

                new MessageBox("Something went wrong").ShowDialog();
                Application.Current.Shutdown();
            }
        }

        private void _pause()
        {
            PauseFunc("На паузі", false);
        }

        private void Pause_Key(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && e.IsRepeat == false)
                _pause();
        }

        private bool _onCooldown = false;
        private void AddUnit(object sender, MouseButtonEventArgs e)
        {
            if (_onCooldown)
                return;
            var p = e.GetPosition(this);
            _client.SetUnit((int)p.X);
            Cooldown();
        }

        private async void Cooldown()
        {
            _onCooldown = true;
            await Task.Delay(1000);
            _onCooldown = false;
        }
    }
}