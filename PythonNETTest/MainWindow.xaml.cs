using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Diagnostics;
using Jace.Operations;
using Jace;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Data.Analysis;
using Apache.Arrow.Types;
using System.Runtime.Remoting.Messaging;
using System.Xml.Serialization;
using System.Windows.Markup;
using System.IO.Pipes;
using System.Reflection;
using UnitsNet;

namespace PythonNETTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public API520Calc APICalc;

        public MainWindow()
        {
            InitializeComponent();
            APICalc = new API520Calc();
            DataContext = APICalc;

            APICalc.PropertyChanged += APICalc_PropertyChanged;
        }

        private void APICalc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "H_SI" && double.IsNaN(APICalc.H_SI))
                EnthalpyTBox.IsEnabled = true;
            else if (e.PropertyName == "H_SI" && !double.IsNaN(APICalc.H_SI))
                EnthalpyTBox.IsEnabled = false;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9,-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Pressure.FromKilopascals(101).Bars.ToString());
        }
    }
}
