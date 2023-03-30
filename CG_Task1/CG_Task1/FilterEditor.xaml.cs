using CG_Task1.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CG_Task1
{
    /// <summary>
    /// Interaction logic for FilterEditor.xaml
    /// </summary>
    public partial class FilterEditor : Window
    {
        public FilterEditor()
        {
            InitializeComponent();
            
            List<FunctionalFilter> functionalFilters = new List<FunctionalFilter>();

            // TO DO: Read filters from 'Filters' folder
            FunctionalFilter testFilter = new FunctionalFilter("Test filter");

            testFilter.AddPoint(new System.Windows.Point(128, 200));

            functionalFilters.Add(testFilter);

            FilterListBox.ItemsSource = functionalFilters;

        }

        private void FilterListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(FilterListBox.SelectedItem != null)
            {
                Polyline line = new Polyline();

                SolidColorBrush whiteBrush = new SolidColorBrush(Colors.Pink);
                line.Points = TranslatePoints((FilterListBox.SelectedItem as FunctionalFilter).Points);
                line.Stroke = whiteBrush;
                line.StrokeThickness= 1;

                FilterGraph.Children.Add(line);

                foreach (var point in TranslatePoints((FilterListBox.SelectedItem as FunctionalFilter).Points))
                {
                    Ellipse circle = new Ellipse();
                    circle.Width = 7;
                    circle.Height = 7;
                    circle.HorizontalAlignment = HorizontalAlignment.Center;
                    circle.VerticalAlignment = VerticalAlignment.Center;
                    circle.Fill = whiteBrush;
                    circle.Cursor = Cursors.Hand;
                    circle.Tag = point;
                    circle.MouseLeftButtonDown += Circle_MouseLeftButtonDown;

                    FilterGraph.Children.Add(circle);

                    Canvas.SetTop(circle, point.Y - 4);
                    Canvas.SetLeft(circle, point.X - 3);

                }
            }
        }

        private void Circle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse circle = (Ellipse)sender;
            if(((System.Windows.Point)circle.Tag).X != 0 || ((System.Windows.Point)circle.Tag).X != 255)
            {
                Canvas.SetLeft(circle, e.GetPosition(FilterGraph).X - 4);
                Canvas.SetTop(circle, e.GetPosition(FilterGraph).Y - 3);

            }
        }


        private PointCollection TranslatePoints(PointCollection points)
        {
            PointCollection newPoints = new PointCollection();

            foreach (System.Windows.Point point in points)
            {
                newPoints.Add(new System.Windows.Point(point.X, 255 - point.Y));
            }

            return newPoints;
        }
    }
}
