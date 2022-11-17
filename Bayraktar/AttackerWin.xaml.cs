﻿using BayraktarGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



namespace Bayraktar {
    /// <summary>
    /// Interaction logic for AttackerWin.xaml
    /// </summary>
    public partial class AttackerWin : Window {

        List<Unit> MilitaryUnits;
        public int Money { get; set; } =0;



        public AttackerWin() {
            InitializeComponent();

            List<Unit> MilitaryUnits = new List<Unit>();
            MilitaryUnits.Add(new Unit() { Id = 0, CoolDown = 500, Name = "ArtZ", Price = 100 });

            Thread thread = new Thread(a => {
                while (true) {
                    Money += 1;
                    Thread.Sleep(50);
                    try {
                        this.Dispatcher.Invoke(() =>
                        {
                            lblMoney.Content = "Money: " + Money.ToString();
                        });
                    }
                    catch (Exception) {
                    }
                }
               
            });

            thread.Start();

            MilitaryUnitsLists.ItemsSource = MilitaryUnits; //подвязать существующих юнитов в список с сервера
        }

        private void AttackerMenu_MouseEnter(object sender, MouseEventArgs e) {
            AttackerMenuCanvas.Margin = new Thickness(245,0,0,0);
        }

        private void AttackerMenu_MouseLeave(object sender, MouseEventArgs e) {
            AttackerMenuCanvas.Margin = new Thickness(590, 0, 0, 0);
        }
       
        Random rnd = new Random(DateTime.Now.GetHashCode());
        private void MilitaryUnitsLists_MouseDoubleClick(object sender, MouseButtonEventArgs e) {


            var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null) {
                var _Unit = (Unit)MilitaryUnitsLists.SelectedItem;
                var mb = new MessageBox(_Unit.Name, true);
                
                if (Money < _Unit.Price)
                    new MessageBox("Not enough money!").ShowDialog();
                else {
                    //send message to send the Unit to the defender;

                    mb.ShowDialog();
                    _Unit.Line = mb.unitLine;
                    Money -= _Unit.Price;

                    AddMilitaryUnit(_Unit);
                }

            }
        }
        private void AddMilitaryUnit(Unit unit) {
            MilitaryUnit _unit = new MilitaryUnit(unit);

            _unit.Y = (short)rnd.Next(-50, 50);

            Canvas.SetLeft(_unit, _unit.Y);
            Canvas.SetTop(_unit, _unit.X);
            _unit.Width = Road.Width / 3;
            _unit.Height = Road.Width / 3;

            var this_ = (CMUs.Children[_unit.Unit.Line] as Border).Child;
            (this_ as Canvas).Children.Add(_unit);

            _unit.Move();
        }
    }
}