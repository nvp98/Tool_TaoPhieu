using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Tool_DATA_PR.Context;
using Tool_DATA_PR.Models.Bkmis_PhoiThep;

namespace Tool_DATA_PR.Service
{
    public class DataBKMISService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataBKMISService> _logger;
        private readonly IConfiguration _configuration;
        public DataBKMISService(IServiceProvider serviceProvider, ILogger<DataBKMISService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task RunAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var bkDbContext = scope.ServiceProvider.GetRequiredService<BkDbContext>();

            // Lấy thông tin ca/kíp từ AppDbContext (database chính)
            DateTime now = DateTime.Now;
            TimeSpan startCa1 = new TimeSpan(8, 0, 0);
            string tenCa = now.TimeOfDay >= startCa1 && now.TimeOfDay < new TimeSpan(20, 0, 0) ? "1" : "2";

            DateTime ngayLamViec = now.TimeOfDay < startCa1 ? now.Date.AddDays(-1) : now.Date;
            string ngayStr = ngayLamViec.ToString("yyyy-MM-dd");
            //string ngayStr = "2025-10-13";
            var kip = await dbContext.Tbl_Kip
                .FirstOrDefaultAsync(x => x.NgayLamViec.Value.Date == ngayLamViec && x.TenCa == tenCa);

            if (kip == null)
            {
                _logger.LogWarning("Không tìm thấy ca/kíp cho {ngay}", ngayLamViec);
                GhiLogFile($"Không tìm thấy ca/kíp cho ngày {ngayLamViec:yyyy-MM-dd}");
                return;
            }

            GhiLogFile($"[INFO] Tìm thấy ca/kíp: Ngày {ngayLamViec:yyyy-MM-dd}, Ca {kip.TenCa}, Kíp {kip.TenKip}, ID_Kip {kip.ID_Kip}");

            string caKipCode = kip.TenCa + kip.TenKip;
            //string caKipCode = "2C";
            int ? ca = int.TryParse(kip.TenCa, out var caValue) ? caValue : (int?)null;
            //int ca = 2;
            //string? kipStr = "C";
             string? kipStr = kip.TenKip;
            string mysqlConnStr = _configuration.GetConnectionString("MySqlConnection");

            var deleted = await bkDbContext.Database.ExecuteSqlInterpolatedAsync(
             $"DELETE FROM BK_PhoiThep WHERE NgaySX = {ngayStr} AND Ca = {ca} AND Kip = {kipStr}");
          //  _logger.LogInformation("Đã xóa {count} dòng cũ trong BK_PhoiThep cho NgaySX={date}, Ca={ca}, Kip={kip}.", deleted;

            // --- BƯỚC 1: LẤY DỮ LIỆU PHÔI THÉP VÀ INSERT ---
            var phoiThepList = await GetPhoiThepFromBKMIS(caKipCode, ngayStr, mysqlConnStr);

            foreach (var phoi in phoiThepList)
            {
                var soMePhoi = phoi.BilletLotCode?.Trim() ?? string.Empty;
                bool daTonTai = await bkDbContext.BK_PhoiThep
                    .AnyAsync(x => x.Me == soMePhoi && x.NgaySX == ngayLamViec);

                if (daTonTai)
                    continue;

                var entity = new BK_PhoiThep
                {
                    Ca = ca,
                    Kip = kipStr,
                    //NgaySX = ngayLamViec,
                    NgaySX = phoi.ProductionDate,
                    KichThuoc = phoi.ProductSizeCode,
                    ChieuDai = double.TryParse(phoi.Length, out var length) ? (double?)length : null,
                    Me = soMePhoi,
                    Mac = null,
                    MauThu = null,
                    MayDuc = int.TryParse(phoi.MayDuc, out var md) ? md : (int?)null,
                    SoThanh = int.TryParse(phoi.NumOfBar, out var result) ? result : (int?)null,
                    TongKhoiLuog = double.TryParse(phoi.Weight, out var weight) ? (double?)weight : null,
                    LoaiID = null,
                    LoaiPhoi = phoi.BilletType,
                    TenLoai = phoi.ClassifyCode,
                    NgayTaoBK = null,
                    TenPhanLoai = null
                };

                bkDbContext.BK_PhoiThep.Add(entity);
            }
            await bkDbContext.SaveChangesAsync();

            var t1 = GetNuocThepFromBKMIS(1, caKipCode, ngayStr, mysqlConnStr);
            var t2 = GetNuocThepFromBKMIS(2, caKipCode, ngayStr, mysqlConnStr);

            await Task.WhenAll(t1, t2);

            var nuocThepList = t1.Result.Concat(t2.Result).Where(nt => !string.IsNullOrWhiteSpace(nt.BilletLotCode)).ToList();
           // DateTime ngay = DateTime.Parse(ngayStr);

            var dsPhoiThep = await bkDbContext.BK_PhoiThep
                .Where(x=> x.NgaySX == ngayLamViec && x.Ca == ca && x.Kip == kipStr)
                .Select(x=> x.Me).ToListAsync();
   
            var ntLookup = nuocThepList
            .GroupBy(nt => nt.BilletLotCode!.Trim())
            .ToDictionary(g => g.Key, g => g.ToList());

            int updated = 0;

            foreach (var me in dsPhoiThep)
            {
                string? mac = null;
                string? mauThu = null;
                string? classifyName = null;
                int? loaiId = null;

                static bool NonEmpty(string? s) => !string.IsNullOrWhiteSpace(s);

                if (ntLookup.TryGetValue(me, out var listForMe) && listForMe is { Count: > 0 })
                {
                    // Lặp qua, gặp bản ghi “đủ dữ liệu” là lấy và BREAK luôn
                    foreach (var n in listForMe)
                    {
                        if (NonEmpty(n.ProductGrade)
                            && NonEmpty(n.BilletSampleName)
                            && NonEmpty(n.ClassifyName)
                            && NonEmpty(n.ClassifyID)
                            && int.TryParse(n.ClassifyID, out var parsed))
                        {
                            mac = n.ProductGrade!.Trim();
                            mauThu = n.BilletSampleName!.Trim();
                            classifyName = n.ClassifyName!.Trim();
                            loaiId = parsed;
                            break; // dừng tại bản ghi đủ dữ liệu đầu tiên
                        }
                    }

                    // TÙY CHỌN: Nếu không tìm được bản ghi đủ dữ liệu, bật “fallback” từng trường (best-effort)
                    // if (mac is null)
                    //     mac = listForMe.Select(x => x.ProductGrade).FirstOrDefault(NonEmpty)?.Trim();
                    // if (mauThu is null)
                    //     mauThu = listForMe.Select(x => x.BilletSampleName).FirstOrDefault(NonEmpty)?.Trim();
                    // if (classifyName is null)
                    //     classifyName = listForMe.Select(x => x.ClassifyName).FirstOrDefault(NonEmpty)?.Trim();
                    // if (loaiId is null)
                    // {
                    //     var classifyIdStr = listForMe.Select(x => x.ClassifyID).FirstOrDefault(NonEmpty);
                    //     if (NonEmpty(classifyIdStr) && int.TryParse(classifyIdStr, out var parsed))
                    //         loaiId = parsed;
                    // }
                }

                // Cập nhật tất cả bản ghi phôi cùng số mẻ theo ngày/ca/kíp
                var phoiRows = await bkDbContext.BK_PhoiThep
                    .Where(x => x.NgaySX == ngayLamViec
                                && x.Ca == ca
                                && x.Kip == kipStr
                                && x.Me != null
                                && x.Me.Trim() == me)
                    .ToListAsync();

                foreach (var row in phoiRows)
                {
                    row.Mac = mac;
                    row.MauThu = mauThu;
                    row.TenPhanLoai = classifyName;
                    row.LoaiID = loaiId;
                    updated++;
                }
            }


            if (updated > 0)
            {
                await bkDbContext.SaveChangesAsync();
                _logger.LogInformation("Đã cập nhật {count} dòng BK_PhoiThep từ nước thép (per-field fallback).", updated);
            }
            else
            {
                _logger.LogInformation("Không có dòng nào cần cập nhật từ nước thép.");
            }

        }

        private async Task<List<Bkmis_NuocThepView>> GetNuocThepFromBKMIS(int viewnuocthep, string caKipCode, string ngay, string connectionString)
        {
            var result = new List<Bkmis_NuocThepView>();

            // Xác định tên bảng theo ID lò cao
            string table = viewnuocthep switch
            {
                1 => "view_dq1_nmlt_nuocthep",
                2 => "view_dq2_nmlt_nuocthep",
                _ => ""
            };

            if (string.IsNullOrEmpty(table))
            {
                _logger.LogWarning("Không xác định được bảng nước thép với viewnuocthep = {viewnuocthep}", viewnuocthep);
                return result;
            }

            // Tạo câu truy vấn
            string query = "SELECT ShiftName, ClassifyName, BilletLotCode, ProductionDate, OvenCode, ClassifyID, ProductGrade, BilletSampleName " +
                           $"FROM bkmis_kcshpsdq.{table} " +
                           $"WHERE ShiftName = '{caKipCode}' AND ProductionDate = '{ngay}'";

            _logger.LogInformation("Thực thi truy vấn MySQL nước thép: {query}", query);
            GhiLogFile($"Thực thi truy vấn MySQL nước thép: {query}");

            try
            {
                using var conn = new MySqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new Bkmis_NuocThepView
                    {
                        ShiftName = reader["ShiftName"]?.ToString(),
                        ClassifyName = reader["ClassifyName"]?.ToString(),
                        BilletLotCode = reader["BilletLotCode"]?.ToString().Trim(),
                        ProductionDate = Convert.ToDateTime(reader["ProductionDate"]),
                        OvenCode = reader["OvenCode"]?.ToString(),
                        ClassifyID = reader["ClassifyID"]?.ToString(),
                        ProductGrade = reader["ProductGrade"]?.ToString(),
                        BilletSampleName = reader["BilletSampleName"]?.ToString(),
                    });
                }

                var lotCodes = result.Select(r => r.BilletLotCode).Distinct().ToList();
                _logger.LogInformation("Danh sách mẻ nước thép đã đọc: {lotCodes}", string.Join(", ", lotCodes));
                GhiLogFile($"[INFO] Đã đọc {result.Count} dòng từ nước thép bảng {table}");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc dữ liệu nước thép từ bảng {table}", table);
                GhiLogFile($"[ERROR] Lỗi khi đọc dữ liệu nước thép từ bảng {table} | Exception: {ex.Message}");
            }

            return result;
        }

        private async Task<List<Bkmis_PhoiThepView>> GetPhoiThepFromBKMIS(string caKipCode, string ngay, string connectionString)
        {
            var result = new List<Bkmis_PhoiThepView>();
            const string table = "view_dq1_nmlt_sanluongphoi";

            string query = @"
                SELECT 
                    ShiftName,
                    ProductionDate,
                    MayDuc,
                    BilletLotCode,
                    ProductSizeCode,
                    BilletType,
                    Length,
                    Weight,
                    NumOfBar,
                    ClassifyCode
                FROM bkmis_kcshpsdq." + table + @"
                WHERE ShiftName = @ShiftName AND ProductionDate = @ProductionDate";

            _logger.LogInformation("Thực thi truy vấn MySQL phôi thép: {query}", query);
            GhiLogFile($"Thực thi truy vấn MySQL phôi thép: {query}");

            try
            {
                using var conn = new MySqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ShiftName", caKipCode);
                cmd.Parameters.AddWithValue("@ProductionDate", ngay);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new Bkmis_PhoiThepView
                    {
                        ShiftName = reader["ShiftName"]?.ToString() ?? string.Empty,
                        ProductionDate = Convert.ToDateTime(reader["ProductionDate"]),
                        MayDuc = reader["MayDuc"]?.ToString(),
                        BilletLotCode = reader["BilletLotCode"]?.ToString().Trim(),
                        ProductSizeCode = reader["ProductSizeCode"]?.ToString(),
                        BilletType = reader["BilletType"]?.ToString(),
                        Length = reader["Length"].ToString(),
                        Weight = reader["Weight"].ToString(),
                        NumOfBar = reader["NumOfBar"].ToString(),
                        ClassifyCode = reader["ClassifyCode"]?.ToString()
                    });
                }

                var billetLots = result.Select(r => r.BilletLotCode).Distinct().ToList();
                _logger.LogInformation("Danh sách mẻ phôi thép đã đọc: {lots}", string.Join(", ", billetLots));
                GhiLogFile($"[INFO] Đã đọc {result.Count} dòng từ phôi thép bảng {table}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc dữ liệu phôi thép từ bảng {table}", table);
                GhiLogFile($"[ERROR] Lỗi khi đọc dữ liệu phôi thép từ bảng {table} | Exception: {ex.Message}");
            }

            return result;
        }

        private void GhiLogFile(string message)
        {
            _logger.LogInformation(message);
        }
    }

}