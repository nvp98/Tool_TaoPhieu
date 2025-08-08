using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool_DATA_PR.Models
{
    public class Tbl_XeGoong
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn Lò Cao")]
        public int? ID_LoCao { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập khối lượng")]
        [Range(1, double.MaxValue, ErrorMessage = "Khối lượng phải lớn hơn 0")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? KL_Xe { get; set; }

    }
}
