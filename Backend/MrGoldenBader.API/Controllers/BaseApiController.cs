using Microsoft.AspNetCore.Mvc;

namespace MrGoldenBader.API.Controllers;

/// <summary>
/// وحدة التحكم الأساسية - جميع الـ Controllers ترث منها
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// إرجاع نتيجة نجاح مع بيانات - للتوافق مع ActionResult<T>
    /// </summary>
    protected ActionResult<T> Success<T>(T data, string message = "تمت العملية بنجاح")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// إرجاع نتيجة نجاح مع بيانات - للتوافق مع IActionResult
    /// </summary>
    protected IActionResult SuccessResult<T>(T data, string message = "تمت العملية بنجاح")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// إرجاع نتيجة خطأ
    /// </summary>
    protected ActionResult Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        });
    }
}

/// <summary>
/// نموذج الاستجابة الموحد
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// هل نجحت العملية
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// رسالة العملية
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// البيانات
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// الطابع الزمني
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
