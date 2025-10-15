using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool_DATA_PR.Models.Bkmis_PhoiThep
{

    [Table("BK_PhoiThep")]
    public class BK_PhoiThep
    {
        [Key]
        public int ID { get; set; }

        public int? Ca { get; set; }

        [StringLength(10)]
        public string? Kip { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgaySX { get; set; }

        [StringLength(20)]
        public string? KichThuoc { get; set; }

        // float trong SQL Server thường map double ở .NET
        public double? ChieuDai { get; set; }

        [Required]
        [StringLength(20)]
        public string Me { get; set; } = string.Empty; // BilletLotCode

        [StringLength(10)]
        public string? Mac { get; set; } // GradeCode

        [StringLength(10)]
        public string? MauThu { get; set; }

        public int? MayDuc { get; set; }

        public int? SoThanh { get; set; }

        public double? TongKhoiLuog { get; set; } // Weight

        public int? LoaiID { get; set; }

        [StringLength(50)]
        public string? LoaiPhoi { get; set; } // BilletType

        [StringLength(10)]
        public string? TenLoai { get; set; } // ClassifyCode

        public DateTime? NgayTaoBK { get; set; }
        public string? TenPhanLoai { get; set; }

    }
}

