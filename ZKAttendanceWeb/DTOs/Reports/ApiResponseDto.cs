namespace ZKAttendanceWeb.DTOs.Common
{
    /// <summary>
    /// DTO عام لاستجابات API
    /// يمكن استخدامه في أي API Controller
    /// </summary>
    /// <typeparam name="T">نوع البيانات المرتجعة</typeparam>
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        /// <summary>
        /// إنشاء استجابة ناجحة
        /// </summary>
        public static ApiResponseDto<T> SuccessResponse(T data, string message = "تمت العملية بنجاح")
        {
            return new ApiResponseDto<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// إنشاء استجابة فاشلة
        /// </summary>
        public static ApiResponseDto<T> ErrorResponse(string message, T? data = default)
        {
            return new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }
}
