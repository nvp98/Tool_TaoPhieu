using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool_DATA_PR.Models.Bkmis_PhoiThep
{
    public class Bkmis_PhoiThepView
    {
        public string ShiftName { get; set; } 
        public DateTime ProductionDate { get; set; }
        public string? MayDuc { get; set; }
        public string? BilletLotCode { get; set; }
        public string? ProductSizeCode { get; set; }
        public string? BilletType { get; set; }
        public string? NumOfBar { get; set; }
        public string? Length { get; set; }
        public string? Weight { get; set; }
        public string? ClassifyCode { get; set; }

    }
}
