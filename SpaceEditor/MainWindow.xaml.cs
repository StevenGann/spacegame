using SpaceGame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpaceEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker gameWorker = new BackgroundWorker();

        public MainWindow()
        {
            gameWorker.DoWork += GameWorker_DoWork;

            InitializeComponent();
            gameWorker.RunWorkerAsync();
        }

        private bool Update()
        {
            lock (GameManager.Instance)
            {
                if (GameManager.Instance == null) { return false; }

                dataGridObjects.ItemsSource = null;
                dataGridObjects.ItemsSource = GameManager.Instance.Objects;

                dataGridUnits.ItemsSource = null;
                dataGridUnits.ItemsSource = GameManager.Instance.Units;
            }

            lock (UiManager.Selected)
            {
                dataGridObjectsSelected.ItemsSource = null;
                dataGridObjectsSelected.ItemsSource = UiManager.Selected;
            }

            return true;
        }

        private void UpdatePropertyBox(object selected)
        {
            propertyGrid.SelectedObject = selected;
        }

        private void GameWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SpaceGame.Program.Main();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            while (!Update())
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        private void dataGridObjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePropertyBox(dataGridObjects.SelectedItem);
        }

        private void dataGridObjectsSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePropertyBox(dataGridObjectsSelected.SelectedItem);
        }

        private void dataGridUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePropertyBox(dataGridUnits.SelectedItem);
        }

        private void dataGridUnitsSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePropertyBox(dataGridUnitsSelected.SelectedItem);
        }
    }
}