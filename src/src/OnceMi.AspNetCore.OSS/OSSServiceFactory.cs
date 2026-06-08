using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace OnceMi.AspNetCore.OSS
{
    public class OSSServiceFactory : IOSSServiceFactory
    {
        private readonly IOptionsMonitor<OSSOptions> optionsMonitor;
        private readonly ICacheProvider _cache;
        private readonly ILoggerFactory logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ConcurrentDictionary<string, IOSSService> _serviceCache = new ConcurrentDictionary<string, IOSSService>(StringComparer.OrdinalIgnoreCase);

        public OSSServiceFactory(IOptionsMonitor<OSSOptions> optionsMonitor
            , ICacheProvider provider
            , ILoggerFactory logger, IServiceProvider serviceProvider)
        {
            this.optionsMonitor = optionsMonitor ?? throw new ArgumentNullException();
            this._cache = provider ?? throw new ArgumentNullException(nameof(IMemoryCache));
            this.logger = logger ?? throw new ArgumentNullException(nameof(ILoggerFactory));
            this.serviceProvider=serviceProvider;
        }

        public IOSSService Create()
        {
            return Create(DefaultOptionName.Name);
        }

        public IOSSService Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DefaultOptionName.Name;
            }
            var options = optionsMonitor.Get(name);
            return Create(options);
        }

        public IOSSService Create(OSSOptions options)
        {
            ValidateOptions(options);
            return CreateService(options);
        }

        public IOSSService GetOrCreate(string name, OSSOptions options = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DefaultOptionName.Name;
            }

            return _serviceCache.GetOrAdd(name, key =>
            {
                // 优先使用传入的 options，其次从 IOptionsMonitor 获取命名配置
                if (options != null && options.Provider != OSSProvider.Invalid)
                {
                    ValidateOptions(options);
                    return CreateService(options);
                }

                var monitoredOptions = optionsMonitor.Get(key);
                if (monitoredOptions == null ||
                    (monitoredOptions.Provider == OSSProvider.Invalid
                    && string.IsNullOrEmpty(monitoredOptions.Endpoint)
                    && string.IsNullOrEmpty(monitoredOptions.SecretKey)
                    && string.IsNullOrEmpty(monitoredOptions.AccessKey)))
                {
                    throw new ArgumentException($"Cannot get OSS option by name '{key}'. Ensure it is registered via AddOSSService or provide valid OSSOptions.");
                }

                ValidateOptions(monitoredOptions);
                return CreateService(monitoredOptions);
            });
        }

        private void ValidateOptions(OSSOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), "OSSOptions can not null.");

            if (options.Provider == OSSProvider.Invalid
                && string.IsNullOrEmpty(options.Endpoint)
                && string.IsNullOrEmpty(options.SecretKey)
                && string.IsNullOrEmpty(options.AccessKey))
                throw new ArgumentException($"Invalid OSSOptions, provider is Invalid and missing endpoint/accessKey/secretKey.");

            if (options.Provider == OSSProvider.Invalid)
                throw new ArgumentNullException(nameof(options.Provider));
            if (options.Provider != OSSProvider.Local)
            {
                if (string.IsNullOrEmpty(options.Endpoint) && options.Provider != OSSProvider.Qiniu)
                    throw new ArgumentNullException(nameof(options.Endpoint), "When your provider is Minio/QCloud/Aliyun/HuaweiCloud, endpoint can not null.");
                if (string.IsNullOrEmpty(options.SecretKey))
                    throw new ArgumentNullException(nameof(options.SecretKey), "SecretKey can not null.");
                if (string.IsNullOrEmpty(options.AccessKey))
                    throw new ArgumentNullException(nameof(options.AccessKey), "AccessKey can not null.");
                if ((options.Provider == OSSProvider.Minio
                    || options.Provider == OSSProvider.QCloud
                    || options.Provider == OSSProvider.Qiniu
                    || options.Provider == OSSProvider.HuaweiCloud)
                    && string.IsNullOrEmpty(options.Region))
                {
                    throw new ArgumentNullException(nameof(options.Region), "When your provider is Minio/QCloud/Qiniu/HuaweiCloud, region can not null.");
                }
            }
        }

        private IOSSService CreateService(OSSOptions options)
        {
            switch (options.Provider)
            {
                case OSSProvider.Aliyun:
                    return new AliyunOSSService(_cache, options);

                case OSSProvider.Minio:
                    return new MinioOSSService(_cache, options);

                case OSSProvider.QCloud:
                    return new QCloudOSSService(_cache, options);

                case OSSProvider.Qiniu:
                    return new QiniuOSSService(_cache, options);

                case OSSProvider.HuaweiCloud:
                    return new HaweiOSSService(_cache, options);

                case OSSProvider.BaiduCloud:
                    return new BaiduOSSService(_cache, options);

                case OSSProvider.Ctyun:
                    return new CtyunOSSService(_cache, options);

                case OSSProvider.Local:
                    return new LocalOSSService(_cache, this.serviceProvider, options);

                default:
                    throw new Exception("Unknow provider type");
            }
        }
    }
}