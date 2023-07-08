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
using UnitsNet;

namespace PythonNETTest
{
    public class API520Calc : INotifyPropertyChanged
    {
        #region DataStorage
        private double t_SI;
        private double w_SI;
        private double p1_SI;
        private double h_SI;
        private double x_SI;
        private double kd = 0.975;
        private double kb = 1.0;
        private double kc = 1.0;
        private double kN;
        private double kSH;
        private KSH ksh = new KSH();
        private double dischargeArea;
        private double pSet;
        private double allowableOverpressure = 1.1;
        Pressure atmosphericPressure = Pressure.FromKilopascals(101);
        #endregion

        #region Properties
        public double T_SI
        {
            get 
            {
                return t_SI;
            }
            set
            {
                t_SI = value;
                KSH = ksh.GetKSH(P1_SI, T_SI);
                InvokeChange(nameof(T_SI));

                var gil = PyInit(PythonDllPath);
                try
                {
                    H_SI = Math.Round((double)GetSteamTable().h_pt(P1_SI, T_SI), 3);
                    if(H_SI != double.NaN)
                        X_SI = Math.Round((double)GetSteamTable().x_ph(P1_SI, H_SI), 3);
                }
                finally
                {
                    gil.Dispose();
                }
            }
        } // steam temperature [°C]
        public double W_SI
        {
            get
            {
                return w_SI;
            }
            set
            {
                w_SI = value;
                InvokeChange(nameof(W_SI));
            }
        } // Required flow rate [kg/s]
        public double P1_SI
        {
            get
            {
                return p1_SI;
            }
            set
            {
                p1_SI = value;
                KN = GetKN(P1_SI);
                InvokeChange(nameof(P1_SI));

                var gil = PyInit(PythonDllPath);
                try
                {
                    H_SI = Math.Round((double)GetSteamTable().h_pt(P1_SI, T_SI), 3);
                    X_SI = Math.Round((double)GetSteamTable().x_ph(P1_SI, H_SI), 3);
                }
                finally
                {
                    gil.Dispose();
                }
            }
        } // upstream relieving pressure [bar(a)] (set pressure + atmospheric pressure + allowable overpressure)
        public double H_SI
        {
            get
            {
                return h_SI;
            }
            set
            {
                h_SI = value;
                InvokeChange(nameof(H_SI));
            }
        } // upstream relieving enthalpy [kJ/kg] 
        public double X_SI
        {
            get
            {
                return x_SI;
            }
            set
            {
                x_SI = value;
                InvokeChange(nameof(X_SI));
            }
        } // steam dryness factor [-] 
        public double Kd
        {
            get
            {
                return kd;
            }
            set
            {
                kd = value;
                InvokeChange(nameof(Kd));
            }
        } // effective coefficient of discharge (0.65 for rupture disc)
        public double Kb
        {
            get
            {
                return kb;
            }
            set
            {
                kb = value;
                InvokeChange(nameof(Kb));
            }
        } // capacity correction factor due to backpressure (different only if bellows are used)
        public double Kc
        {
            get
            {
                return kc;
            }
            set
            {
                kc = value;
                InvokeChange(nameof(Kc));
            }
        } // combination correction factor for installations with a rupture disk upstream of the PRV (0.9 with RD)
        public double KN
        {
            get
            {
                return kN;
            }
            set
            {
                kN = value;
                InvokeChange(nameof(KN));
            }
        } // correction factor for the Napier equation
        public double KSH
        {
            get
            {
                return kSH;
            }
            set
            {
                kSH = value;
                InvokeChange(nameof(KSH));
            }
        } // superheat correction factor
        public double PSet
        {
            get
            { 
                return pSet;
            }
            set
            {
                pSet = value;
                P1_SI = (value + atmosphericPressure.Bars) * allowableOverpressure; // + atmospheric pressure, * 10% overpressure 
                KSH = ksh.GetKSH(PSet, T_SI);
                InvokeChange(nameof(PSet));
            }
        } // set pressure bar(g)
        public double DischargeArea
        {
            get
            {
                return dischargeArea;
            }
            set
            {
                InvokeChange(nameof(DischargeArea));
            }
        }

        private string PythonDllPath = @"C:\Users\pechm\AppData\Local\Programs\Python\Python311\python311.dll";
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region Constructor
        public API520Calc()
        {
            //KN = GetKN(P1USC);
            //KSH = GetKSH(P1USC);
        }
        #endregion

        public double GetDischargeArea()
        {
            Pressure pressure = Pressure.FromBars(P1_SI);
            MassFlow massFlow = MassFlow.FromKilogramsPerSecond(W_SI);
            Area area = new Area();

            double W_USC = massFlow.PoundsPerHour;
            double P1_USC = pressure.PoundsForcePerSquareInch;

            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", PythonDllPath);
            PythonEngine.Initialize();
            var gil = Py.GIL();
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
        #endregion

    }
}
