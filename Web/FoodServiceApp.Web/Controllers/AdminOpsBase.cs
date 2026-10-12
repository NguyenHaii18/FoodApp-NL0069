using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodServiceApp.Web.Data;

namespace FoodServiceApp.Web.Controllers
{
    /// <summary>Tiện ích dùng chung cho các trang vận hành của Quản trị (quyền, xuất CSV).</summary>
    public abstract class AdminOpsBase : QuanTriBaseController
    {
        protected readonly AppDbContext Db;
        protected AdminOpsBase(AppDbContext db) { Db = db; }

        protected int MaTK => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>SuperAdmin / QuanLy / NhanVien (đọc từ CSDL, không tin claim cũ).</summary>
        protected async Task<string> CapQuyenAsync() =>
            await Db.QuanTris.Where(q => q.MaTK == MaTK).Select(q => q.CapQuyenHan).FirstOrDefaultAsync() ?? "";

        protected async Task<bool> LaQuanLyTroLenAsync() { var c = await CapQuyenAsync(); return c == "SuperAdmin" || c == "QuanLy"; }

        /// <summary>Trả file CSV (UTF-8 có BOM để Excel đọc đúng tiếng Việt).</summary>
        protected FileContentResult Csv(string tenFile, IEnumerable<string> tieuDe, IEnumerable<IEnumerable<object?>> dong)
        {
            static string Q(object? v)
            {
                var t = v switch { null => "", DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"), decimal m => m.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), _ => v.ToString() ?? "" };
                // chống CSV injection khi mở bằng Excel
                if (t.Length > 0 && (t[0] == '=' || t[0] == '+' || t[0] == '-' || t[0] == '@')) t = "'" + t;
                return "\"" + t.Replace("\"", "\"\"") + "\"";
            }
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", tieuDe.Select(Q)));
            foreach (var r in dong) sb.AppendLine(string.Join(",", r.Select(Q)));
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"{tenFile}_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        protected static readonly string[] TrangThaiCuoi = { "DaGiao", "DaHuy" };
    }
}
