using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Virinco.WATS.Interface;

namespace Virinco.WATS.Converter.Klippel
{
    public class KlippelLogConverter : IReportConverter_v2
    {
        Dictionary<string, string> parameters;
        public KlippelLogConverter() : base()
        {
            parameters = new Dictionary<string, string>()
            {
                {"partNumber","PartNumber1" },
                {"partRevision","1.0" },
                {"sequenceName","SequenceName1" },
                {"sequenceVersion","1.0.0" },
                {"operationTypeCode","10" }
            };
        }

        public KlippelLogConverter(Dictionary<string, string> args)
        {
            parameters = args;
        }

        public Dictionary<string, string> ConverterParameters => parameters;

        public void CleanUp()
        {
        }

        public Report ImportReport(TDM api, Stream file)
        {
            Dictionary<string, string> headerData = new Dictionary<string, string>();
            using (TextReader reader = new StreamReader(file))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line.Contains("="))
                    {
                        string[] keyValue = line.Split(new char[] { '=' });
                        headerData.Add(keyValue[0], keyValue[1]);
                    }
                    line = reader.ReadLine();
                }
            }
            // Individual data files (e.g. Impedance-imp.txt) don't have Cfg_SerialNumber;
            // only the main DUT file does. Skip data files silently.
            if (!headerData.ContainsKey("Cfg_SerialNumber"))
                return null;

            UUTReport uut = CreateReportFromHeader(headerData, api);
            //2020-5-20-135-5-14-10-1-37-563-120-1.589E+09
            Regex regex = new Regex(@"(?<Year>\d+)-(?<Month>\d+)-(?<WeekNo>-*\d+)-(?<JDay>-*\d+)-(?<WDay>\d+)-(?<Day>\d+)-(?<Hour>\d+)-(?<Minute>\d+)-(?<Second>\d+)-(?<MSec>\d+)-(?<UTCOffset>\d+)");
            Match match = regex.Match(headerData["Cfg_DutStartTime"]);
            uut.StartDateTime = new DateTime(
                int.Parse(match.Groups["Year"].Value),
                int.Parse(match.Groups["Month"].Value),
                int.Parse(match.Groups["Day"].Value),
                int.Parse(match.Groups["Hour"].Value),
                int.Parse(match.Groups["Minute"].Value),
                int.Parse(match.Groups["Second"].Value),
                int.Parse(match.Groups["MSec"].Value)
                );
            uut.StartDateTimeUTC = uut.StartDateTime.AddMinutes(-int.Parse(match.Groups["UTCOffset"].Value));
            string? sourceFileName = api.ConversionSource?.SourceFile?.Name;
            string? sourceDirName = api.ConversionSource?.SourceFile?.DirectoryName;
            if (sourceFileName != null)
                uut.AddMiscUUTInfo("FileName", sourceFileName);
            if (sourceDirName == null || sourceFileName == null)
                return uut;
            string testDataFolderName = $"{sourceDirName}\\{Path.GetFileNameWithoutExtension(sourceFileName)}";
            if (!Directory.Exists(testDataFolderName))
                return uut;
            string[] dataFileNames = Directory.GetFiles(testDataFolderName, "*.txt");
            foreach (string fileName in dataFileNames)
            {
                ReadDataFile(uut, fileName);
            }
            uut.Status = headerData["Ctrl_OverallVerdict"] == "1" ? UUTStatusType.Passed : UUTStatusType.Failed;
            //Code for overall verdict: -1: Void, no result 0: Fail, nok 1: Pass, ok 2: Warning 3: Noise 4: Invalid
            return uut;
        }

        SequenceCall currentSequence = null;
        private void ReadDataFile(UUTReport uut, string fileName)
        {
            string[] seqName = Path.GetFileNameWithoutExtension(fileName).Split(new char[] { '-' });
            if (currentSequence == null || currentSequence.Name != seqName[0])
                currentSequence = uut.GetRootSequenceCall().AddSequenceCall(seqName[0]);
            using (TextReader reader = new StreamReader(fileName))
            {
                string line = reader.ReadLine();
                string[] headers = line.Split(new char[] { '\t' });
                line = reader.ReadLine();
                List<double> x = new List<double>();
                List<double> y = new List<double>();
                List<double> min = new List<double>();
                List<double> max = new List<double>();
                while (line != null)
                {
                    if (headers[0] == "name") //Single measure
                    {
                        NumericLimitStep numericLimitStep = currentSequence.AddNumericLimitStep(line.Split(new char[] { '\t' }).First());
                        double[] values = ReadDoubles(line);
                        //Format: name	value	max	min
                        if (double.IsNaN(values[2])) //No limits
                            numericLimitStep.AddTest(values[1], "");
                        else if (!double.IsNaN(values[3]) && !double.IsNaN(values[2]))
                            numericLimitStep.AddTest(values[1], CompOperatorType.GELE, values[3], values[2], "");
                        else
                            throw new NotImplementedException("Assumed no limits or max / min");
                    }
                    else if (headers[0] == "frq")
                    {
                        double[] values = ReadDoubles(line);
                        if (values.Length == 1) { } //No Y value, skip
                        else
                        {
                            x.Add(values[0]);
                            y.Add(values[1]);
                            if (values.Length == 4)
                            {
                                max.Add(values[2]);
                                min.Add(values[3]);
                            }
                            else if (values.Length == 3)
                                max.Add(values[2]);
                            else throw new NotImplementedException("Assumed x,y,min,max or x,y,max");
                        }
                    }
                    else throw new NotImplementedException("Assumed either name or frq in header");
                    line = reader.ReadLine();
                }
                if (headers[0] == "frq")
                {
                    NumericLimitStep numericLimitStep = currentSequence.AddNumericLimitStep(headers[1]);
                    numericLimitStep.AddMultipleTest(y.Average(), "Hz", "avg");
                    if (min.Count > 0)
                        numericLimitStep.AddMultipleTest(y.Min(), "Hz", "min");
                    if (max.Count > 0)
                        numericLimitStep.AddMultipleTest(y.Max(), "Hz", "max");
                    int errorCount = 0;
                    for (int i = 0; i < y.Count; i++)
                    {
                        if (i < max.Count && double.IsNaN(max[i])) //Skip if limit is Nan or not exist
                            continue;
                        if (i < min.Count && double.IsNaN(min[i]))
                            continue;
                        if (y[i] > max[i]) errorCount++;
                        if (min.Count > 0 && y[i] < min[i]) errorCount++;
                    }
                    numericLimitStep.AddMultipleTest(errorCount, CompOperatorType.LT, 1, "#", "OutOfBounds");
                    Chart chart = numericLimitStep.AddChart(ChartType.LineLogX, headers[1], "Frequency", "Hz", "res", "");
                    chart.AddSeries("values", x.ToArray(), y.ToArray());
                    if (min.Count > 0)
                        chart.AddSeries("min", x.ToArray(), min.ToArray());
                    if (max.Count > 0)
                        chart.AddSeries("max", x.ToArray(), max.ToArray());
                }
            }
        }

        private double[] ReadDoubles(string line)
        {
            string[] elements = line.Split(new char[] { '\t' });
            List<double> dl = new List<double>();
            foreach (string element in elements)
            {
                double d = double.NaN;
                if (double.TryParse(element, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                    dl.Add(d);
                else
                    dl.Add(double.NaN);
            }
            return dl.ToArray();
        }

        private UUTReport CreateReportFromHeader(Dictionary<string, string> headerData, TDM api)
        {
            string userName = headerData.TryGetValue("Cfg_UserName", out string? u) ? u : "";
            UUTReport uut = api.CreateUUTReport(userName, parameters["partNumber"], parameters["partRevision"], headerData["Cfg_SerialNumber"],
                parameters["operationTypeCode"], parameters["sequenceName"], parameters["sequenceVersion"]);
            if (headerData.TryGetValue("Cfg_LoginMode", out string? loginMode))
                uut.AddMiscUUTInfo("Cfg_LoginMode", loginMode);
            if (headerData.TryGetValue("Cfg_Speaker", out string? speaker))
                uut.AddMiscUUTInfo("Cfg_Speaker", speaker);
            return uut;
        }
    }
}
