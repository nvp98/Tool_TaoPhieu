using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool_DATA_PR.Models.Bkmis_PhoiThep
{
    public class Bkmis_NuocThepView
    {
        public string? ShiftName { get; set; }
 
        public string? BilletLotCode { get; set; }
        public DateTime ProductionDate { get; set; }
        public string? OvenCode { get; set; }
        public string? ClassifyID { get; set; }
        public string? ClassifyName { get; set; }
        public string? ProductGrade { get; set; }
        public string? BilletSampleName { get; set; }
        public DateTime? InputTime { get; set; }
    }
}
