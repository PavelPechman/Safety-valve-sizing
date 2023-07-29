using Jace;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnitsNet;

namespace API520.ViewModel
{
    public class APISteamReliefSizingViewModel : BaseViewModel, INotifyPropertyChanged
    {
        #region Constants
        private readonly Pressure AtmosphericPressure = Pressure.FromKilopascals(101);
        private double AllowableOverpressure = 1.1;
        private string PythonDllPath = @"C:\Users\pechm\AppData\Local\Programs\Python\Python311\python311.dll";
        #endregion

        #region DataStorage
        private Temperature _temperature = new Temperature(0, UnitsNet.Units.TemperatureUnit.DegreeCelsius);
        private MassFlow _massFlow = new MassFlow();
        private Pressure _p1 = new Pressure();
        private Pressure _pSet = new Pressure();
        private SpecificEnergy _enthalpy = new SpecificEnergy();
        private double _vapourFraction;
        private double _kd = 0.975;
        private double _kb = 1.0;
        private double _kc = 1.0;
        private double _kN;
        private KSH _kSH = new KSH();
        private Area _dischargeArea = new Area();
        private bool _isSaturated = false;
        #endregion

        #region Properties
        public double Temperature_SI
        {
            get
            {
                return _temperature.DegreesCelsius;
            }
            set
            {
                _temperature = Temperature.FromDegreesCelsius(value);
                KSH = _kSH.GetKSH(P1_SI, Temperature_SI);
                InvokeChange(nameof(Temperature_SI));

                if(!EnthalpySetManually)
                    Enthalpy_SI = (double)GetSteamTable().h_pt(PSet_SI + 1, Temperature_SI);
                else
                    EnthalpySetManually = false;
            }
        } // Valve inlet steam temperature [°C]
        public double MassFlow_SI
        {
            get
            {
                return _massFlow.KilogramsPerSecond;
            }
            set
            {
                _massFlow = MassFlow.FromKilogramsPerSecond(value);
                InvokeChange(nameof(MassFlow_SI));
            }
        } // Required flow rate [kg/s]
        public double P1_SI
        {
            get
            {
                return _p1.Bars;
            }
            set
            {
                _p1 = Pressure.FromBars(value);
                KN = GetKN(P1_SI);
                InvokeChange(nameof(P1_SI));
            }
        } // upstream relieving pressure [bar(a)] (set pressure + atmospheric pressure + allowable overpressure)
        public double Enthalpy_SI
        {
            get
            {
                return _enthalpy.KilojoulesPerKilogram;
            }
            set
            {
                if (!double.IsNaN(value))
                {
                    _enthalpy = SpecificEnergy.FromKilojoulesPerKilogram(value);
                    VapourFraction = (double)GetSteamTable().x_ph(PSet_SI + 1, Enthalpy_SI);
                    if (EnthalpySetManually)
                        Temperature_SI = (double)GetSteamTable().t_ph(PSet_SI + 1, Enthalpy_SI);
                }
                else
                {
                    _enthalpy = SpecificEnergy.FromKilojoulesPerKilogram(0);
                }
                InvokeChange(nameof(Enthalpy_SI));
            }
        } // upstream relieving enthalpy [kJ/kg] 
        public double VapourFraction
        {
            get
            {
                return _vapourFraction;
            }
            set
            {
                _vapourFraction = value;
                InvokeChange(nameof(VapourFraction));
            }
        } // steam dryness factor [-] 
        public double Kd
        {
            get
            {
                return _kd;
            }
            set
            {
                _kd = value;
                InvokeChange(nameof(Kd));
            }
        } // effective coefficient of discharge (0.65 for rupture disc)
        public double Kb
        {
            get
            {
                return _kb;
            }
            set
            {
                _kb = value;
                InvokeChange(nameof(Kb));
            }
        } // capacity correction factor due to backpressure (different only if bellows are used)
        public double Kc
        {
            get
            {
                return _kc;
            }
            set
            {
                _kc = value;
                InvokeChange(nameof(Kc));
            }
        } // combination correction factor for installations with a rupture disk upstream of the PRV (0.9 with RD)
        public double KN
        {
            get
            {
                return _kN;
            }
            set
            {
                _kN = value;
                InvokeChange(nameof(KN));
            }
        } // correction factor for the Napier equation
        public double KSH
        {
            get
            {
                return _kSH.GetKSH(P1_SI, Temperature_SI);
            }
            set
            {
                //_kSH = value;
                InvokeChange(nameof(KSH));
            }
        } // superheat correction factor
        public double PSet_SI
        {
            get
            { 
                return _pSet.Bars;
            }
            set
            {
                _pSet = Pressure.FromBars(value);
                P1_SI = (value + AtmosphericPressure.Bars) * AllowableOverpressure;
                KSH = _kSH.GetKSH(PSet_SI, Temperature_SI);
                InvokeChange(nameof(PSet_SI));

                var gil = PyInit(PythonDllPath);
                try
                {
                    Enthalpy_SI = (double)GetSteamTable().h_pt(PSet_SI + 1, Temperature_SI);
                }
                finally
                {
                    gil.Dispose();
                }
            }
        } // set pressure bar(g)
        public double DischargeArea_SI
        {
            get
            {
                return _dischargeArea.SquareMillimeters;
            }
            set
            {
                _dischargeArea = Area.FromSquareMillimeters(value);
                InvokeChange(nameof(DischargeArea_SI));
            }
        }
        public bool IsSaturated
        {
            get
            {
                return _isSaturated;
            }
            set
            { 
                _isSaturated = value;
                InvokeChange(nameof(IsSaturated));
            }
        }
        public bool EnthalpySetManually = false;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        private void APISteamReliefSizingViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update of discharge area
            if(e.PropertyName != nameof(DischargeArea_SI))
                try
                {
                    DischargeArea_SI = GetDischargeArea();
                }
                catch (Exception) { }

            // Check for saturation
            if (e.PropertyName == nameof(Temperature_SI) ||
                e.PropertyName == nameof(PSet_SI) ||
                e.PropertyName == nameof(Enthalpy_SI))
            {
                double x = (double)GetSteamTable().x_ph(PSet_SI + 1, Enthalpy_SI);
                if (Enthalpy_SI == 0 || (x > 0 && x < 1))
                    IsSaturated = true;
                else
                    IsSaturated = false;
            }
        }
        #endregion

        #region Constructor
        public APISteamReliefSizingViewModel()
        {
            this.PropertyChanged += APISteamReliefSizingViewModel_PropertyChanged;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes Python.Net
        /// </summary>
        /// <param name="pythonDllPath"></param>
        /// <returns></returns>
        private Py.GILState PyInit(string pythonDllPath)
        {
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);
            PythonEngine.Initialize();
            return Py.GIL();
        }

        private dynamic GetSteamTable()
        {
            dynamic xSteam = Py.Import("pyXSteam.XSteam");
            dynamic unitSystem = xSteam.XSteam.UNIT_SYSTEM_MKS;
            return xSteam.XSteam(unitSystem);
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

        /// <summary>
        /// Gets correction factor for the Napier equation
        /// </summary>
        /// <param name="P1">Set pressure [psi(a)]</param>
        /// <returns></returns>
        private double GetKN(double P1)
        {
            Pressure Pressure = Pressure.FromBars(P1);
            double psia = Pressure.PoundsForcePerSquareInch;

            if (psia <= 1500)
                return 1.0;
            else if (psia > 1500 && psia <= 3200)
                return (0.1906 * psia - 1000) / (0.2292 * psia - 1061);
            else
                return double.NaN;            
        }

        public double GetDischargeArea()
        {
            Area area;

            double W_USC = _massFlow.PoundsPerHour;
            double P1_USC = _p1.PoundsForcePerSquareInch;

            var gil = PyInit(PythonDllPath);
            try
            {

                Dictionary<string, double> variables = new Dictionary<string, double>();
                variables.Add("W", W_USC);
                variables.Add("P1", P1_USC);
                variables.Add(nameof(Kd), Kd);
                variables.Add(nameof(Kb), Kb);
                variables.Add(nameof(Kc), Kc);
                variables.Add(nameof(KN), KN);
                variables.Add(nameof(KSH), KSH);

                string AExpression = "W / (51,5 * P1 * Kd * Kb * Kc * KN * KSH)";

                CalculationEngine engine = new CalculationEngine();
                area = Area.FromSquareInches(engine.Calculate(AExpression, variables));
                return area.SquareMillimeters;
            }
            catch (Exception)
            {
                return double.NaN;
            }
            finally
            {
                gil.Dispose();
            }

        }

        public void SetSaturationTemperature()
        {
            var gil = PyInit(PythonDllPath);
            try
            {
                Temperature_SI = (double)GetSteamTable().tsat_p(PSet_SI + 1);
                IsSaturated = true;
            }
            finally
            {
                gil.Dispose();
            }
        }
        #endregion

    }
}
