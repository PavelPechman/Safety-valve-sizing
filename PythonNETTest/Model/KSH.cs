using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace API520
{
    public class KSH
    {
        private string DataPath = @"KSH.csv";
        private string[] ColNames = { "RowID", "300", "400", "500", "600", "700", "800", "900", "1000", "1100", "1200" };
        private List<double> TemperatureAxis = new List<double>();
        private double MinimumTemperature;
        private double MaximumTemperature;
        private double MinimumPressure;
        private double MaximumPressure;
        private List<double> PressureAxis;
        private DataFrame KSHTable;

        public KSH()
        {
            var dataPath = Path.GetFullPath(DataPath);

            // Load the data into the data frame
            var dataFrame = DataFrame.LoadCsv(dataPath, separator: ',', columnNames: ColNames);
            KSHTable = dataFrame;
            PressureAxis = DFToDoubleList(dataFrame["RowID"], null);

            foreach(string name in ColNames)
                if(double.TryParse(name, out double value))
                    TemperatureAxis.Add(value);

            MinimumTemperature = TemperatureAxis.ElementAt(1);
            MaximumTemperature = TemperatureAxis.Last();
            MinimumPressure = PressureAxis.ElementAt(0);
            MaximumPressure = PressureAxis.Last();
        }

        private Dictionary<double, double> ColumnToDictionary(DataFrameColumn column)
        {
            Dictionary<double, double> result = new Dictionary<double, double>();

            for (int i = 0; i < column.Length; i++)
            {
                result.Add(PressureAxis[i], double.Parse(column[i].ToString(), CultureInfo.InvariantCulture));
            }

            return result;
        }

        private List<double> DFToDoubleList(DataFrameColumn column, DataFrameRow row)
        {
            List<double> result = new List<double>();

            if (column != null && row == null)
                for (int i = 0; i < column.Length; i++)
                    result.Add(double.Parse(column[i].ToString(), CultureInfo.InvariantCulture));

            else if (column == null && row != null)
                for (int i = 0; i < row.Count(); i++)
                    result.Add(double.Parse(row[i].ToString(), CultureInfo.InvariantCulture));

            return result.ToList();
        }

        public double GetKSH(double pressure, double temperature)
        {
            try
            {
                Pressure Pressure = Pressure.FromBars(pressure);
                Temperature Temperature = Temperature.FromDegreesCelsius(temperature);

                temperature = Temperature.DegreesFahrenheit;
                pressure = Pressure.PoundsForcePerSquareInch;

                double lowPress = GetNeighbourValue(DFToDoubleList(KSHTable.Columns["RowID"], null),
                    pressure, SearchDirection.Lower);
                double highPress = GetNeighbourValue(DFToDoubleList(KSHTable.Columns["RowID"], null),
                    pressure, SearchDirection.Higher);
                double lowTemp = GetNeighbourValue(TemperatureAxis, temperature, SearchDirection.Lower);
                double highTemp = GetNeighbourValue(TemperatureAxis, temperature, SearchDirection.Higher);

                Dictionary<double, double> lowerTemperatureColumn = ColumnToDictionary(KSHTable.Columns[lowTemp.ToString()]);
                Dictionary<double, double> higherTemperatureColumn = ColumnToDictionary(KSHTable.Columns[lowTemp.ToString()]);

                double lowTempKSH = LinearInterpolation(lowPress, highPress, pressure,
                    lowerTemperatureColumn[lowPress], lowerTemperatureColumn[highPress]);
                double highTempKSH = LinearInterpolation(lowPress, highPress, pressure,
                    higherTemperatureColumn[lowPress], higherTemperatureColumn[highPress]);

                double KSH = LinearInterpolation(lowTemp, highTemp, temperature,
                    lowTempKSH, highTempKSH);

                return KSH;
            }
            catch (Exception)
            {
                Trace.WriteLine("KSH value not found");
            };

            return double.NaN;
        }

        private double LinearInterpolation(double axisA, double axisB, double axisC, double valueA, double valueB)
        {
            if(axisA == axisB && valueA == valueB)
                return valueA;
            else
                return (axisC - axisA) * (valueB - valueA) / (axisB - axisA) + valueA;
        }

        private double GetNeighbourValue(List<double> list, double referenceValue, SearchDirection dir)
        {
            for (int i = 0; i < list.Count; i++)
                if (i == 0 && referenceValue < list[i])
                    return list[i];
                else if (i == list.Count - 1 && referenceValue > list[i])
                    return list[i];
                else if(list[i] < referenceValue && list[i+1] > referenceValue)
                    if(dir == SearchDirection.Lower)
                        return list[i];
                    else if (dir == SearchDirection.Higher)
                        return list[i+1];

            return double.NaN;
        }


        private enum SearchDirection
        {
            Lower,
            Higher
        };
    }
}
