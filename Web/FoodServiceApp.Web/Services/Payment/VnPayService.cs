using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using FoodServiceApp.Web.Services.Configs;

namespace FoodServiceApp.Web.Services.Payment
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _settings;
        private readonly HttpClient _http;

        public VnPayService(IOptions<VnPaySettings> options, HttpClient http)
        {
            _settings = options.Value;
            _http = http;
        }

        public async Task<VnPayRefundResult> RefundAsync(int orderId, long amountVnd, long tongTienGoc, string maGiaoDichCong, string ngayGiaoDichCong, string nguoiThucHien, string lyDo, string ip)
        {
            if (_settings.TmnCode.StartsWith("DIEN_") || _settings.HashSecret.StartsWith("DIEN_"))
                return new VnPayRefundResult(false, true, "Chưa cấu hình VnPay (TmnCode/HashSecret) nên chưa gọi được cổng hoàn tiền.");
            if (string.IsNullOrWhiteSpace(ngayGiaoDichCong))
                return new VnPayRefundResult(false, false, "Thiếu ngày giao dịch của cổng (vnp_PayDate) — giao dịch này được ghi trước khi hệ thống lưu trường đó. Hoàn tiền thủ công trên cổng merchant rồi xác nhận.");

            var requestId = Guid.NewGuid().ToString("N");
            var loai = amountVnd >= tongTienGoc ? "02" : "03";
            var now = DateTime.Now.ToString("yyyyMMddHHmmss");
            var info = ("Hoan tien don " + orderId + ": " + lyDo);
            if (info.Length > 200) info = info[..200];
            var amount = (amountVnd * 100).ToString();
            var txnRef = orderId.ToString();
            var data = string.Join("|", requestId, _settings.Version, "refund", _settings.TmnCode, loai, txnRef, amount, maGiaoDichCong, ngayGiaoDichCong, nguoiThucHien, now, ip, info);
            var body = new Dictionary<string, string>
            {
                ["vnp_RequestId"] = requestId, ["vnp_Version"] = _settings.Version, ["vnp_Command"] = "refund",
                ["vnp_TmnCode"] = _settings.TmnCode, ["vnp_TransactionType"] = loai, ["vnp_TxnRef"] = txnRef,
                ["vnp_Amount"] = amount, ["vnp_OrderInfo"] = info, ["vnp_TransactionNo"] = maGiaoDichCong,
                ["vnp_TransactionDate"] = ngayGiaoDichCong, ["vnp_CreateBy"] = nguoiThucHien, ["vnp_CreateDate"] = now,
                ["vnp_IpAddr"] = ip, ["vnp_SecureHash"] = HmacSha512(_settings.HashSecret, data),
            };
            try
            {
                using var resp = await _http.PostAsJsonAsync(_settings.RefundUrl, body);
                var text = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return new VnPayRefundResult(false, false, $"Cổng trả HTTP {(int)resp.StatusCode}.");
                var r = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(text) ?? new();
                string G(string k) => r.TryGetValue(k, out var v) ? (v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";
                // Xác thực chữ ký phản hồi (theo tài liệu VNPay 2.1.0)
                var rdata = string.Join("|", G("vnp_ResponseId"), G("vnp_Command"), G("vnp_ResponseCode"), G("vnp_Message"), G("vnp_TmnCode"), G("vnp_TxnRef"),
                    G("vnp_Amount"), G("vnp_BankCode"), G("vnp_PayDate"), G("vnp_TransactionNo"), G("vnp_TransactionType"), G("vnp_TransactionStatus"), G("vnp_OrderInfo"));
                if (!string.Equals(HmacSha512(_settings.HashSecret, rdata), G("vnp_SecureHash"), StringComparison.OrdinalIgnoreCase))
                    return new VnPayRefundResult(false, false, "Không xác thực được chữ ký phản hồi của cổng. Kiểm tra giao dịch trên cổng merchant trước khi thử lại.");
                var code = G("vnp_ResponseCode");
                if (code == "00") return new VnPayRefundResult(true, false, "VNPay đã chấp nhận hoàn tiền: " + G("vnp_Message"), G("vnp_TransactionNo"));
                return new VnPayRefundResult(false, false, $"VNPay từ chối (mã {code}): {G("vnp_Message")}");
            }
            catch (Exception ex)
            {
                return new VnPayRefundResult(false, false, "Không kết nối được cổng hoàn tiền: " + ex.Message);
            }
        }

        public string CreatePaymentUrl(VnPayCreateRequest request)
        {
            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = _settings.Version,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _settings.TmnCode,
                ["vnp_Amount"] = ((long)(request.AmountVnd * 100)).ToString(), // VNPay yêu cầu nhân 100
                ["vnp_CurrCode"] = _settings.CurrCode,
                ["vnp_TxnRef"] = request.OrderId.ToString(),
                ["vnp_OrderInfo"] = request.OrderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = _settings.Locale,
                ["vnp_ReturnUrl"] = request.ReturnUrl ?? _settings.ReturnUrl,
                ["vnp_IpAddr"] = request.IpAddress,
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            };

            var (queryString, hashData) = BuildQuery(vnpParams);
            var secureHash = HmacSha512(_settings.HashSecret, hashData);

            return $"{_settings.BaseUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public VnPayCallbackResult ValidateCallback(IQueryCollection query)
        {
            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
            string receivedHash = string.Empty;

            foreach (var kv in query)
            {
                if (kv.Key == "vnp_SecureHash" || kv.Key == "vnp_SecureHashType")
                {
                    if (kv.Key == "vnp_SecureHash") receivedHash = kv.Value!;
                    continue;
                }
                if (kv.Key.StartsWith("vnp_"))
                    vnpParams[kv.Key] = kv.Value!;
            }

            var (_, hashData) = BuildQuery(vnpParams);
            var computedHash = HmacSha512(_settings.HashSecret, hashData);

            var isValid = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
            var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode", "");
            var orderId = int.Parse(vnpParams.GetValueOrDefault("vnp_TxnRef", "0"));
            var amount = long.Parse(vnpParams.GetValueOrDefault("vnp_Amount", "0")) / 100;
            var transactionId = vnpParams.GetValueOrDefault("vnp_TransactionNo", "");

            return new VnPayCallbackResult(
                IsValidSignature: isValid,
                IsSuccess: isValid && responseCode == "00",
                OrderId: orderId,
                TransactionId: transactionId,
                Amount: amount,
                PayDate: vnpParams.GetValueOrDefault("vnp_PayDate", "")
            );
        }

        private static (string QueryString, string HashData) BuildQuery(SortedDictionary<string, string> vnpParams)
        {
            var sb = new StringBuilder();
            foreach (var kv in vnpParams)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value)).Append('&');
            }
            var full = sb.ToString().TrimEnd('&');
            return (full, full);
        }

        private static string HmacSha512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
