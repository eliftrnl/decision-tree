using System;
using System.Collections.Generic;

namespace DecisionTree.Api.Entities
{
    public class DecisionTree
    {
        public int Id { get; set; }

        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;

        public StatusCode StatusCode { get; set; } = StatusCode.Active;

        /// <summary>
        /// Schema version for tracking column/table changes
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<DecisionTreeTable> Tables { get; set; } = new List<DecisionTreeTable>();
    }
}

