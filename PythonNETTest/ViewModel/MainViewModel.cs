using API520.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace API520.ViewModel
{
    public class MainViewModel : BaseViewModel, INotifyPropertyChanged
    {
		private BaseViewModel _selectedViewModel;

		public event PropertyChangedEventHandler PropertyChanged;

		public BaseViewModel SelectedViewModel
		{
			get { return _selectedViewModel; }
			set 
            { 
                _selectedViewModel = value;
                InvokeChange(nameof(SelectedViewModel));
            }
		}

		public ICommand UpdateViewCommand { get; set; }

		public MainViewModel()
		{
			UpdateViewCommand = new UpdateViewCommand(this);
        }

        /// <summary>
        /// Invokes change of class property
        /// </summary>
        /// <param name="property"></param>
        protected void InvokeChange(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

    }
}
