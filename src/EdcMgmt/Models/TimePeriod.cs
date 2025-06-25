using System;
using System.Collections.Generic;

namespace EdcMgmt.Models
{
    /// <summary>
    /// Represents a fixed 15-minute time period.
    /// </summary>
    public class TimePeriod
    {
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public ICollection<EnergyTransfer> EnergyTransfers { get; set; } = new List<EnergyTransfer>();
    }
}