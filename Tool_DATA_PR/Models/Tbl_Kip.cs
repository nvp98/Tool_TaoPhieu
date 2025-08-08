using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool_DATA_PR.Models
{
    public class Tbl_Kip
    {
        [Key]
        public int ID_Kip { get; set; }
        public DateTime? NgayLamViec { get; set; }
        public string? TenKip { get; set; }
        public string? TenCa { get; set; }
        [NotMapped]
        public Nullable<DateTime> TuNgay { get; set; }
        [NotMapped]
        public Nullable<DateTime> DenNgay { get; set; }
    }
}
