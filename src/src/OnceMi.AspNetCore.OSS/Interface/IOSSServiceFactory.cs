using Minio;

namespace OnceMi.AspNetCore.OSS
{
    public interface IOSSServiceFactory
    {
        /// <summary>
        /// 使用默认配置创建 OSS 服务。
        /// </summary>
        IOSSService Create();

        /// <summary>
        /// 根据已注册的命名配置创建 OSS 服务。
        /// </summary>
        /// <param name="name">配置名称，对应 AddOSSService 注册时的 name 参数。</param>
        IOSSService Create(string name);

        /// <summary>
        /// 根据指定的 OSSOptions 直接创建 OSS 服务（不依赖 IOptionsMonitor）。
        /// 适用于从数据库或其他外部配置动态构建 OSS 配置。
        /// </summary>
        /// <param name="options">OSS 配置选项。</param>
        IOSSService Create(OSSOptions options);

        /// <summary>
        /// 按名称获取或创建 OSS 服务（带缓存）。
        /// 优先使用 options 参数创建；若 options 为 null 或无效，则回退到 IOptionsMonitor 的命名配置。
        /// 同一 name 只创建一次，后续调用返回缓存实例。
        /// </summary>
        /// <param name="name">服务标识名称，用于缓存 key。</param>
        /// <param name="options">OSS 配置选项，为 null 时使用已注册的命名配置。</param>
        IOSSService GetOrCreate(string name, OSSOptions options = null);
    }
}