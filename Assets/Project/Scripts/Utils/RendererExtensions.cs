using UnityEngine;

/// <summary>
/// Extension class giúp thao tác với Renderer tối ưu, không sinh rác (Zero GC Alloc).
/// </summary>
public static class RendererExtensions
{
    // Dùng chung 1 block tĩnh duy nhất cho toàn game. 
    // An toàn tuyệt đối vì Unity API chỉ chạy trên Main Thread.
    private static readonly MaterialPropertyBlock SharedBlock = new MaterialPropertyBlock();

    // Cache sẵn một số Property ID phổ biến để gọi cho nhanh (tránh dò string)
    public static readonly int ColorPropId = Shader.PropertyToID("_Color");           // Standard Shader
    public static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");   // URP / Custom Shader
    public static readonly int TintColorPropId = Shader.PropertyToID("_TintColor");   // Particle / Trail

    /// <summary>
    /// Thay đổi màu của Renderer sử dụng MaterialPropertyBlock (Zero Allocation)
    /// </summary>
    /// <param name="renderer">Renderer cần đổi màu</param>
    /// <param name="color">Màu mới</param>
    /// <param name="propertyId">ID của thuộc tính màu trong Shader (mặc định là _BaseColor)</param>
    public static void SetColorOptimized(this Renderer renderer, Color color, int propertyId = 0)
    {
        if (renderer == null) return;

        // Nếu không truyền ID, mặc định dùng _BaseColor (URP/Custom phổ biến nhất)
        if (propertyId == 0) propertyId = BaseColorPropId;

        // Lấy state hiện tại của Renderer đắp vào block (để không đè mất các thông số khác đã set trước đó)
        renderer.GetPropertyBlock(SharedBlock);

        // Gán màu mới
        SharedBlock.SetColor(propertyId, color);

        // Đẩy xuống GPU
        renderer.SetPropertyBlock(SharedBlock);
    }

    /// <summary>
    /// Thay đổi một giá trị Float (ví dụ: Alpha, Smoothness, Outline Width...)
    /// </summary>
    public static void SetFloatOptimized(this Renderer renderer, int propertyId, float value)
    {
        if (renderer == null) return;
        renderer.GetPropertyBlock(SharedBlock);
        SharedBlock.SetFloat(propertyId, value);
        renderer.SetPropertyBlock(SharedBlock);
    }
}