using API520.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using UnitsNet;
using System.ComponentModel;
using System.Diagnostics;

namespace API520.View
{
    /// <summary>
    /// Interaction logic for API520SteamReliefSizingSheetView.xaml
    /// </summary>
    public partial class API520SteamReliefSizingSheetView : UserControl
    {
        public APISteamReliefSizingViewModel APICalc;

        public API520SteamReliefSizingSheetView()
        {
            InitializeComponent();
            APICalc = new APISteamReliefSizingViewModel();
            DataContext = APICalc;

            APICalc.PropertyChanged += APICalc_PropertyChanged;
        }

        private void APICalc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(APICalc.IsSaturated))
                EnthalpyTBox.IsEnabled = APICalc.IsSaturated;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9,-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SaturationTemperature_Button_Click(object sender, RoutedEventArgs e)
        {
            APICalc.SetSaturationTemperature();
        }

        private void EnthalpyTBox_LostFocus(object sender, RoutedEventArgs e)
        {
            APICalc.EnthalpySetManually = true;
        }
    }
}
