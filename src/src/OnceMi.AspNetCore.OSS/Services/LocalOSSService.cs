using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OnceMi.AspNetCore.OSS.Error;
using OnceMi.AspNetCore.OSS.Interface.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OnceMi.AspNetCore.OSS
{
    public class LocalOSSService : BaseOSSService, ILocalOSSService
    {
        private readonly string _rootPath;
        private readonly string _rootUrl;

        public LocalOSSService(ICacheProvider cache, IServiceProvider serviceProvider, OSSOptions options)
           : base(cache, options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), "The OSSOptions can not null");
            IHostingEnvironment env = serviceProvider.GetService<IHostingEnvironment>();
            _rootPath = GetRootPath(env);
            _rootUrl = BuildRootUrl(options);
        }

        private string GetRootPath(IHostingEnvironment env)
        {
            if (string.IsNullOrEmpty(env.WebRootPath))
                env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (string.IsNullOrEmpty(env.WebRootPath))
                env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var rootPath = env.WebRootPath;
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            return rootPath;
        }

        private string BuildRootUrl(OSSOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Endpoint))
                return "/";   // 相对路径

            string scheme = options.IsEnableHttps ? "https" : "http";
            string host = options.Endpoint.TrimEnd('/');
            return $"{scheme}://{host}/";
        }

        private void ExceptionHandling(Action ioAction)
        {
            try
            {
                ioAction();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new OSSException(OSSErrorCode.InvalidAccess.ToOSSError(), ex);
            }
            catch (ArgumentException ex)
            {
                throw new OSSException(OSSErrorCode.InvalidBlobName.ToOSSError(), ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new OSSException(OSSErrorCode.ContainerNotFound.ToOSSError(), ex);
            }
            catch (NotSupportedException ex)
            {
                throw new OSSException(OSSErrorCode.InvalidBlobName.ToOSSError(), ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new OSSException(OSSErrorCode.FileNotFound.ToOSSError(), ex);
            }
            catch (IOException ex)
            {
                throw new OSSException(OSSErrorCode.BlobInUse.ToOSSError(), ex);
            }
            catch (Exception ex)
            {
                throw new OSSException(OSSErrorCode.GenericException.ToOSSError(), ex);
            }
        }

        private T ExceptionHandling<T>(Func<T> ioFunc)
        {
            try
            {
                return ioFunc();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new OSSException(OSSErrorCode.InvalidAccess.ToOSSError(), ex);
            }
            catch (ArgumentException ex)
            {
                throw new OSSException(OSSErrorCode.InvalidBlobName.ToOSSError(), ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new OSSException(OSSErrorCode.ContainerNotFound.ToOSSError(), ex);
            }
            catch (NotSupportedException ex)
            {
                throw new OSSException(OSSErrorCode.InvalidBlobName.ToOSSError(), ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new OSSException(OSSErrorCode.FileNotFound.ToOSSError(), ex);
            }
            catch (IOException ex)
            {
                throw new OSSException(OSSErrorCode.BlobInUse.ToOSSError(), ex);
            }
            catch (Exception ex)
            {
                throw new OSSException(OSSErrorCode.GenericException.ToOSSError(), ex);
            }
        }

        #region Bucket 管理

        public Task<bool> BucketExistsAsync(string bucketName)
        {
            return Task.FromResult(ExceptionHandling(() =>
               Directory.Exists(Path.Combine(_rootPath, bucketName))));
        }

        public Task<bool> CreateBucketAsync(string bucketName)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var dirPath = Path.Combine(_rootPath, bucketName);
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                return true;
            }));
        }

        public Task<bool> RemoveBucketAsync(string bucketName)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var dirPath = Path.Combine(_rootPath, bucketName);
                // 删除空目录，非空时抛出 IOException（由 ExceptionHandling 转换为 OSSException）
                Directory.Delete(dirPath, false);
                return true;
            }));
        }

        public Task<List<Bucket>> ListBucketsAsync()
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var buckets = new List<Bucket>();
                if (Directory.Exists(_rootPath))
                {
                    foreach (var dir in Directory.GetDirectories(_rootPath))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        buckets.Add(new Bucket
                        {
                            Name = dirInfo.Name,
                            CreationDate = dirInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }
                return buckets;
            }));
        }

        #endregion Bucket 管理

        #region 对象基本操作

        public Task<bool> ObjectsExistsAsync(string bucketName, string objectName)
        {
            return Task.FromResult(ExceptionHandling(() =>
                File.Exists(Path.Combine(_rootPath, bucketName, objectName))));
        }

        public async Task<bool> PutObjectAsync(string bucketName, string objectName, Stream data, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                return ExceptionHandling(() =>
                {
                    // check file exists
                    var filePath = Path.Combine(_rootPath, bucketName, objectName);
                    if (File.Exists(filePath))
                        throw new OSSException(OSSErrorCode.BlobInUse.ToOSSError());
                    //check directory
                    var fileInfo = new FileInfo(filePath);
                    if (!Directory.Exists(fileInfo.DirectoryName))
                        Directory.CreateDirectory(fileInfo.DirectoryName);

                    using (var file = File.Create(filePath))
                    {
                        data.CopyTo(file);
                    }

                    return true;
                });
            });
        }

        public Task<bool> PutObjectAsync(string bucketName, string objectName, string filePath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var destPath = Path.Combine(_rootPath, bucketName, objectName);
                if (File.Exists(destPath))
                    throw new OSSException(OSSErrorCode.BlobInUse.ToOSSError());

                var destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(filePath, destPath, false);
                return true;
            }));
        }

        public Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                ExceptionHandling(() =>
                {
                    var filePath = Path.Combine(_rootPath, bucketName, objectName);
                    using (var stream = File.OpenRead(filePath))
                    {
                        callback(stream);
                    }
                });
            }, cancellationToken);
        }

        public Task GetObjectAsync(string bucketName, string objectName, string fileName, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                ExceptionHandling(() =>
                {
                    var sourcePath = Path.Combine(_rootPath, bucketName, objectName);
                    File.Copy(sourcePath, fileName, true);
                });
            }, cancellationToken);
        }

        public async Task<bool> RemoveObjectAsync(string bucketName, string objectName)
        {
            return await Task.Run(() =>
            {
                return ExceptionHandling(() =>
                {
                    var filePath = Path.Combine(_rootPath, bucketName, objectName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    return true;
                });
            });
        }

        public Task<bool> RemoveObjectAsync(string bucketName, List<string> objectNames)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                foreach (var name in objectNames)
                {
                    var filePath = Path.Combine(_rootPath, bucketName, name);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                return true;
            }));
        }

        public Task<bool> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var sourcePath = Path.Combine(_rootPath, bucketName, objectName);
                var targetName = destObjectName ?? objectName;
                var destPath = Path.Combine(_rootPath, destBucketName, targetName);

                var destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                // 不允许覆盖，若目标存在则抛出异常
                File.Copy(sourcePath, destPath, false);
                return true;
            }));
        }

        public Task<List<Item>> ListObjectsAsync(string bucketName, string prefix = null)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var dirPath = Path.Combine(_rootPath, bucketName);
                if (!Directory.Exists(dirPath))
                    return new List<Item>();

                // 使用所有目录下递归搜索文件
                var searchPattern = string.IsNullOrEmpty(prefix) ? "*" : $"{prefix}*";
                var files = Directory.GetFiles(dirPath, searchPattern, SearchOption.AllDirectories);

                var items = new List<Item>();
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    // 计算相对路径作为 Key，统一使用 '/'
                    var relativePath = file.Replace(dirPath, "").TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');
                    items.Add(new Item
                    {
                        Key = relativePath,
                        Size = (ulong)fileInfo.Length,
                        LastModifiedDateTime = fileInfo.LastWriteTimeUtc,
                    });
                }
                return items;
            }));
        }

        public Task<ItemMeta> GetObjectMetadataAsync(string bucketName, string objectName, string versionID = null, string matchEtag = null, DateTime? modifiedSince = null)
        {
            return Task.Run(() => ExceptionHandling(() =>
            {
                var filePath = Path.Combine(_rootPath, bucketName, objectName);
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    throw new FileNotFoundException("Object not found", filePath);

                // 注意：versionID、matchEtag、modifiedSince 在本地实现中可忽略或按需校验
                return new ItemMeta
                {
                    ObjectName = objectName,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTimeUtc,
                    ETag = fileInfo.LastWriteTimeUtc.Ticks.ToString() // 简单生成 ETag
                };
            }));
        }

        #endregion 对象基本操作

        public Task<AccessMode> GetBucketAclAsync(string bucketName)
        {
            throw new NotImplementedException();
        }

        public Task<AccessMode> GetObjectAclAsync(string bucketName, string objectName)
        {
            throw new NotImplementedException();
        }

        public Task<string> PresignedGetObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            string relativePath = $"{bucketName}/{objectName.TrimStart('/')}";
            string fullUrl = $"{_rootUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}?expires={expiresInt}";
            return Task.FromResult(fullUrl);
        }

        public Task<string> PresignedPutObjectAsync(string bucketName, string objectName, int expiresInt)
        {
            throw new NotImplementedException();
        }

        public Task<AccessMode> RemoveObjectAclAsync(string bucketName, string objectName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetBucketAclAsync(string bucketName, AccessMode mode)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetObjectAclAsync(string bucketName, string objectName, AccessMode mode)
        {
            throw new NotImplementedException();
        }
    }
}