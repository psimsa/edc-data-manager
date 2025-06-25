using System.Collections.Generic;

namespace EdcMgmt.Models
{
    /// <summary>
    /// Represents an energy station identified by EAN.
    /// </summary>
    public class EnergyStation
    {
        public int Id { get; set; }
        public string Ean { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; } // e.g., "ODBER" or "VYROBA"
        public ICollection<AllocationDetail> AllocationDetails { get; set; } = new List<AllocationDetail>();
        public ICollection<EnergyTransfer> TransfersFrom { get; set; } = new List<EnergyTransfer>();
        public ICollection<EnergyTransfer> TransfersTo { get; set; } = new List<EnergyTransfer>();
    }
}