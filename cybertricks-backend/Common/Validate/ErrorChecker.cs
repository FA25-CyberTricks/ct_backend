using System.Text.RegularExpressions;
using ct.backend.Features;
using ct.backend.Common.Validate;
using ct.backend.Common.Constants;

namespace ct.backend.Common.Error
{
    /// <summary>
    /// Bộ helper kiểm tra (validate) giá trị và tự động ghi lỗi vào <see cref="AbstractResponse{T}"/>.
    /// Tất cả hàm trả về <c>true</c> nếu hợp lệ; <c>false</c> nếu không hợp lệ (và đã add lỗi).
    /// </summary>
    public static class ErrorChecker
    {
        /// <summary>
        /// Kiểm tra bắt buộc (required).
        /// Hợp lệ khi: khác null; nếu là string thì không rỗng/không toàn khoảng trắng;
        /// nếu là Guid thì khác Guid.Empty; nếu là IEnumerable&lt;object&gt; thì có phần tử.
        /// </summary>
        public static bool Required<TResp>(AbstractResponse<TResp> response, object? value, string field, string message = MessageCodes.E011)
        {
            var ok =
                value switch
                {
                    null => false,
                    string s => !string.IsNullOrWhiteSpace(s),
                    Guid g => g != Guid.Empty,
                    IEnumerable<object> e => e.Any(),
                    _ => true
                };

            if (!ok) response.AddError(message, field);
            return ok;
        }

        /// <summary>
        /// Kiểm tra khác null (không xét rỗng cho string/collection).
        /// </summary>
        public static bool NotNull<TResp>(AbstractResponse<TResp> response, object? value, string field, string message = MessageCodes.E011)
        {
            var ok = value is not null;
            if (!ok) response.AddError(message, field);
            return ok;
        }

        /// <summary>
        /// Kiểm tra độ dài tối thiểu cho chuỗi.
        /// </summary>
        public static bool MinLength<TResp>(AbstractResponse<TResp> response, string? value, int min, string field, string message = MessageCodes.E011)
        {
            var ok = !string.IsNullOrEmpty(value) && value.Length >= min;
            if (!ok) response.AddError(message, field);
            return ok;
        }

        /// <summary>
        /// Kiểm tra độ dài tối đa cho chuỗi.
        /// </summary>
        public static bool MaxLength<TResp>(AbstractResponse<TResp> response, string? value, int max, string field, string message = MessageCodes.E011)
        {
            var ok = value is null || value.Length <= max;
            if (!ok) response.AddError(message, field);
            return ok;
        }

        /// <summary>
        /// Kiểm tra giá trị kiểu số/so sánh được có nằm trong khoảng [min, max] hay không.
        /// </summary>
        public static bool Range<TResp, T>(AbstractResponse<TResp> response, T? value, T min, T max, string field, string message = MessageCodes.E011)
            where T : struct, IComparable<T>
        {
            if (value is null) { response.AddError(message, field); return false; }
            var ok = value.Value.CompareTo(min) >= 0 && value.Value.CompareTo(max) <= 0;
            if (!ok) response.AddError(message, field);
            return ok;
        }

        // ======= Formats =======

        /// <summary>
        /// Regex kiểm tra email ở mức cơ bản (không quá khắt khe theo RFC).
        /// </summary>
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Kiểm tra định dạng email (cơ bản).
        /// </summary>
        public static bool Email<TResp>(AbstractResponse<TResp> response, string? email, string field, string message = MessageCodes.E003)
        {
            var ok = !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
            if (!ok) response.AddError(message, field);
            return ok;
        }

        /// <summary>
        /// Regex kiểm tra số điện thoại đơn giản (9–11 chữ số, cho phép dấu + đầu).
        /// </summary>
        private static readonly Regex PhoneRegex = new(@"^\+?\d{9,11}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Kiểm tra định dạng số điện thoại (đơn giản).
        /// </summary>
        public static bool Phone<TResp>(AbstractResponse<TResp> response, string? phone, string field, string message = MessageCodes.E004)
        {
            var ok = !string.IsNullOrWhiteSpace(phone) && PhoneRegex.IsMatch(phone);
            if (!ok) response.AddError(message, field);
            return ok;
        }

        /// <summary>
        /// Kiểm tra chuỗi khớp với một biểu thức chính quy tùy chọn.
        /// </summary>
        public static bool Matches<TResp>(AbstractResponse<TResp> response, string? value, Regex pattern, string field, string message = MessageCodes.E011)
        {
            var ok = !string.IsNullOrWhiteSpace(value) && pattern.IsMatch(value);
            if (!ok) response.AddError(message, field);
            return ok;
        }

        // ======= Predicate tuỳ biến =======

        /// <summary>
        /// Kiểm tra bằng một predicate tùy biến.
        /// </summary>
        public static bool Check<TResp, T>(AbstractResponse<TResp> response, T value, Func<T, bool> predicate, string field, string message = MessageCodes.E011)
        {
            var ok = predicate(value);
            if (!ok) response.AddError(message, field);
            return ok;
        }
    }
}
