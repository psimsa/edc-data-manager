using System.Collections.Generic;

namespace EdcMgmt.Models
{
    /// <summary>
    /// Represents a detail of an allocation configuration, linking an EnergyStation to a configuration.
    /// </summary>
    public class AllocationDetail
    {
        public int Id { get; set; }
        public int AllocationConfigurationId { get; set; }
        public AllocationConfiguration? AllocationConfiguration { get; set; }
        public int EnergyStationId { get; set; }
        public EnergyStation? EnergyStation { get; set; }
        public decimal Share { get; set; }
    }
}