using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EdcMgmt.Models;

namespace EdcMgmt.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            // Use correct relative paths for CSV files
            string configPath = Path.Combine("DataSamples", "Export-odberovych-EAN-2025-06-21.csv");
            string timeseriesPath = Path.Combine("DataSamples", "Export-dat-2025-06-21-11-07-report-21.06.2025_11_07.csv");

            // Load all stations from both files
            var stations = LoadStations(configPath, timeseriesPath, out var eanToStation);

            // Load allocation configurations
            var allocationConfigs = LoadAllocationConfigurations(configPath, eanToStation);

            Console.WriteLine("Loaded Allocation Configurations:");
            foreach (var config in allocationConfigs)
            {
                Console.WriteLine($"  Config: {config.Name}, Valid: {config.ValidFrom:yyyy-MM-dd} to {(config.ValidTo?.ToString("yyyy-MM-dd") ?? "∞")}");
                foreach (var detail in config.AllocationDetails)
                {
                    var station = stations.FirstOrDefault(s => s.Id == detail.EnergyStationId);
                    Console.WriteLine($"    -> {station?.Ean ?? "?"} : {detail.Share * 100}%");
                }
            }

            // Load time-series data
            var (transfers, periods) = LoadEnergyTransfers(timeseriesPath, eanToStation);

            Console.WriteLine($"\nLoaded {transfers.Count} Energy Transfers (first 5 shown):");
            foreach (var t in transfers.Take(5))
            {
                var from = stations.FirstOrDefault(s => s.Id == t.FromStationId)?.Ean ?? "?";
                var to = stations.FirstOrDefault(s => s.Id == t.ToStationId)?.Ean ?? "?";
                var period = periods.FirstOrDefault(p => p.Id == t.TimePeriodId);
                Console.WriteLine($"  {period?.Start:yyyy-MM-dd HH:mm} | {from} -> {to} : {t.Value} {t.Unit}");
            }

            // Sample query: All transfers for a specific producer
            var sampleProducer = stations.FirstOrDefault();
            if (sampleProducer != null)
            {
                var producerTransfers = transfers.Where(t => t.FromStationId == sampleProducer.Id).ToList();
                Console.WriteLine($"\nSample: All transfers for producer {sampleProducer.Ean} (showing up to 5):");
                foreach (var t in producerTransfers.Take(5))
                {
                    var to = stations.FirstOrDefault(s => s.Id == t.ToStationId)?.Ean ?? "?";
                    var period = periods.FirstOrDefault(p => p.Id == t.TimePeriodId);
                    Console.WriteLine($"  {period?.Start:yyyy-MM-dd HH:mm} | {sampleProducer.Ean} -> {to} : {t.Value} {t.Unit}");
                }
            }
        }

        static List<EnergyStation> LoadStations(string configPath, string timeseriesPath, out Dictionary<string, EnergyStation> eanToStation)
        {
            var stations = new Dictionary<string, EnergyStation>();
            int nextId = 1;

            // From config file
            using (var reader = new StreamReader(configPath, System.Text.Encoding.UTF8, true))
            {
                string header = reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length < 13) continue;

                    var eano = parts[2].Trim();
                    if (!stations.ContainsKey(eano))
                        stations[eano] = new EnergyStation { Id = nextId++, Ean = eano };

                    for (int i = 0; i < 5; i++)
                    {
                        var eand = parts[5 + i].Trim();
                        if (!string.IsNullOrEmpty(eand) && !stations.ContainsKey(eand))
                            stations[eand] = new EnergyStation { Id = nextId++, Ean = eand };
                    }
                }
            }

            // From timeseries file
            using (var reader = new StreamReader(timeseriesPath, System.Text.Encoding.UTF8, true))
            {
                string header = reader.ReadLine();
                var columns = header.Split(';');
                for (int i = 3; i < columns.Length; i++)
                {
                    var eanPair = columns[i].Split('-');
                    if (eanPair.Length == 2)
                    {
                        var to = eanPair[0].Trim();
                        var from = eanPair[1].Trim();
                        if (!stations.ContainsKey(from))
                            stations[from] = new EnergyStation { Id = nextId++, Ean = from };
                        if (!stations.ContainsKey(to))
                            stations[to] = new EnergyStation { Id = nextId++, Ean = to };
                    }
                }
            }

            eanToStation = stations;
            return stations.Values.ToList();
        }

        static List<AllocationConfiguration> LoadAllocationConfigurations(string path, Dictionary<string, EnergyStation> eanToStation)
        {
            var configs = new List<AllocationConfiguration>();
            var culture = new CultureInfo("cs-CZ");
            using (var reader = new StreamReader(path, System.Text.Encoding.UTF8, true))
            {
                string header = reader.ReadLine();
                int configId = 1, detailId = 1;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length < 13) continue;

                    var eano = parts[2].Trim();
                    var validFrom = DateTime.ParseExact(parts[3], "dd.MM.yyyy", culture);
                    var validTo = parts[4] == "31.12.9999"
                        ? (DateTime?)null
                        : DateTime.ParseExact(parts[4], "dd.MM.yyyy", culture);

                    var config = new AllocationConfiguration
                    {
                        Id = configId++,
                        Name = $"Config for {eano}",
                        ValidFrom = validFrom,
                        ValidTo = validTo,
                        AllocationDetails = new List<AllocationDetail>()
                    };

                    for (int i = 0; i < 5; i++)
                    {
                        var eand = parts[5 + i].Trim();
                        var key = parts[10 + i].Trim();
                        if (!string.IsNullOrEmpty(eand) && !string.IsNullOrEmpty(key) && eanToStation.ContainsKey(eand))
                        {
                            if (decimal.TryParse(key, NumberStyles.Any, culture, out decimal share))
                            {
                                config.AllocationDetails.Add(new AllocationDetail
                                {
                                    Id = detailId++,
                                    AllocationConfigurationId = config.Id,
                                    EnergyStationId = eanToStation[eand].Id,
                                    Share = share / 100m
                                });
                            }
                        }
                    }
                    configs.Add(config);
                }
            }
            return configs;
        }

        static (List<EnergyTransfer>, List<TimePeriod>) LoadEnergyTransfers(string path, Dictionary<string, EnergyStation> eanToStation)
        {
            var transfers = new List<EnergyTransfer>();
            var periods = new List<TimePeriod>();
            var culture = new CultureInfo("cs-CZ");
            int transferId = 1, periodId = 1;
            using (var reader = new StreamReader(path, System.Text.Encoding.UTF8, true))
            {
                string header = reader.ReadLine();
                var columns = header.Split(';');
                var eanPairs = columns.Skip(3).ToArray();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(';');
                    if (parts.Length < 4) continue;

                    var date = DateTime.ParseExact(parts[0], "dd.MM.yyyy", culture);
                    var timeFrom = TimeSpan.Parse(parts[1]);
                    var timeTo = TimeSpan.Parse(parts[2]);
                    var start = date.Add(timeFrom);
                    var end = date.Add(timeTo);

                    var period = new TimePeriod
                    {
                        Id = periodId++,
                        Start = start,
                        End = end
                    };
                    periods.Add(period);

                    for (int i = 3; i < parts.Length; i++)
                    {
                        var valueStr = parts[i].Trim();
                        if (string.IsNullOrEmpty(valueStr)) continue;
                        if (decimal.TryParse(valueStr, NumberStyles.Any, culture, out decimal value))
                        {
                            var eanPair = columns[i].Split('-');
                            if (eanPair.Length == 2 && eanToStation.ContainsKey(eanPair[1]) && eanToStation.ContainsKey(eanPair[0]))
                            {
                                transfers.Add(new EnergyTransfer
                                {
                                    Id = transferId++,
                                    FromStationId = eanToStation[eanPair[1]].Id,
                                    ToStationId = eanToStation[eanPair[0]].Id,
                                    TimePeriodId = period.Id,
                                    Value = value,
                                    Unit = "MWh"
                                });
                            }
                        }
                    }
                }
            }
            return (transfers, periods);
        }
    }
}
