namespace EdcMgmt.Models;

// IdSkupinySdileni;Operace;EANo;DatumOd;DatumDo;EANd1;AlokacniKlic1;EANd2;AlokacniKlic2;EANd3;AlokacniKlic3;EANd4;AlokacniKlic4;EANd5;AlokacniKlic5;Vysledek
// 22313;;859182400302838511;01.06.2025;31.12.9999;859182400611509249;1;;;;;;;;;

public class SkupinaSdileni
{
    public ConsumptionEan[] ConsumptionEans { get; set; } = Array.Empty<ConsumptionEan>();
    public ProductionEan[] ProductionEans { get; set; } = Array.Empty<ProductionEan>();
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;



    public static IEnumerable<SkupinaSdileni> FromExportFile(string csvData)
    {
        var groups = csvData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(l => l.Split(';')).GroupBy(l => l[0]);

        (ConsumptionEan[] consumptionEans, ProductionEan[] productionEans) getEans(string[][] lines)
        {
            var prodEans = new List<ProductionEan>();
            ProductionEan GetOrCreateProdEan(string eanId)
            {
                // return prodEans.FirstOrDefault(e => e.EAN == eanId) ?? new ProductionEan { EAN = eanId };
                if (!prodEans.Any(e => e.EAN == eanId))
                {
                    var newEan = new ProductionEan { EAN = eanId };
                    prodEans.Add(newEan);
                    return newEan;
                }
                return prodEans.First(e => e.EAN == eanId);
            }

            var consEans = new List<ConsumptionEan>();
            ConsumptionEan GetOrCreateConsEan(string eanId)
            {
                if (!consEans.Any(e => e.EAN == eanId))
                {
                    var newEan = new ConsumptionEan { EAN = eanId };
                    consEans.Add(newEan);
                    return newEan;
                }
                return consEans.First(e => e.EAN == eanId);
            }

            foreach (var columnset in lines)
            {
                var consEanId = columnset[2].Trim();

                var consEan = GetOrCreateConsEan(consEanId);

                for (int i = 1; i<6; i++)
                {
                    var ean = columnset[3 + (i * 2)];
                    if (string.IsNullOrWhiteSpace(ean)) continue;

                    var allocationKeyString = columnset[4 + (i * 2)];
                    var allocationKey = string.IsNullOrWhiteSpace(allocationKeyString) ? 0 : Convert.ToDecimal(allocationKeyString.Replace(',', '.'));

                    var existingProdEan = GetOrCreateProdEan(ean);
                    existingProdEan.Allocations.Add(new Allocation(consEan.EAN, (int)(allocationKey * 100)));
                }
            }

            return (consEans.ToArray(), prodEans.ToArray());
        }

        foreach (var group in groups)
        {
            var items = group.ToArray();

            var eans = getEans(items);

            yield return new SkupinaSdileni
            {
                Id = group.Key,
                Description = "",
                ConsumptionEans = eans.consumptionEans,
                ProductionEans = eans.productionEans
            };
        }
    }

    public string ToExportFile()
    {
        throw new NotImplementedException("Serialization not implemented yet.");
    }

    private record ExportLine
    {
        public string IdSkupinySdileni { get; init; } = string.Empty;
        public string Operace { get; init; } = string.Empty;
        public ConsumptionEan? EANo { get; init; }
        public string DatumOd { get; init; } = string.Empty;
        public string DatumDo { get; init; } = string.Empty;
        public ProductionEan[] Eans { get; init; } = Array.Empty<ProductionEan>();
    }
}

public static class OperaceSdileni
{
    public const string Editovat = "Editovat";
    public const string Novy = "Novy";
    public const string Ukonceni = "Ukonceni";
    public const string Zrusit = "Zrusit";
}

public record ConsumptionEan
{
    public required string EAN { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record ProductionEan
{
    public required string EAN { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<Allocation> Allocations { get; init; } = new();
}

public record Allocation(string TargetEan, int AllocationPercent);