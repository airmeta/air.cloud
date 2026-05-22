using Air.Cloud.Core.Standard.AmazonS3.Options;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Air.Cloud.Modules.AmazonS3.ClientFactory
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 客户端工厂实现</para>
    /// <para>en-us:Amazon S3 client factory implementation</para>
    /// </summary>
    public class AmazonS3ClientFactory : IAmazonS3ClientFactory
    {
        private readonly AmazonS3Options _options;

        /// <summary>
        /// <para>zh-cn:构造 Amazon S3 客户端工厂</para>
        /// <para>en-us:Construct Amazon S3 client factory</para>
        /// </summary>
        public AmazonS3ClientFactory(IOptions<AmazonS3Options> options)
        {
            if (options?.Value == null)
            {
                throw new ArgumentNullException(nameof(options), "AmazonS3Options cannot be null.");
            }

            _options = options.Value;
        }

        /// <summary>
        /// <para>zh-cn:根据配置键创建客户端</para>
        /// <para>en-us:Create client by configuration key</para>
        /// </summary>
        public object CreateClient(string key)
        {
            if (!_options.Tokens.TryGetValue(key, out var option) || option == null)
            {
                throw new KeyNotFoundException($"AmazonS3 option key '{key}' is not configured.");
            }

            var clientConfig = MergeClientConfig(_options.ClientConfig, option.ClientConfig, option.ForceReplaceParentClientConfig);
            var config = BuildConfig(clientConfig);

            if (option.Token is not null)
            {
                if (!string.IsNullOrWhiteSpace(option.Token.Url) && string.IsNullOrWhiteSpace(config.ServiceURL))
                {
                    config.ServiceURL = option.Token.Url;
                }

                if (!string.IsNullOrWhiteSpace(option.Token.Path))
                {
                    config.ForcePathStyle = option.Token.Path.Equals("path", StringComparison.OrdinalIgnoreCase);
                }

                if (!string.IsNullOrWhiteSpace(option.Token.AccessKey) && !string.IsNullOrWhiteSpace(option.Token.SecretKey))
                {
                    var credentials = new BasicAWSCredentials(option.Token.AccessKey, option.Token.SecretKey);
                    return new AmazonS3Client(credentials, config);
                }
            }

            return new AmazonS3Client(config);
        }

        /// <summary>
        /// <para>zh-cn:根据配置键创建强类型客户端</para>
        /// <para>en-us:Create strongly typed client by configuration key</para>
        /// </summary>
        public TClient CreateClient<TClient>(string key) where TClient : class
        {
            var client = CreateClient(key);
            if (client is TClient typed)
            {
                return typed;
            }

            throw new InvalidCastException($"Client can not cast to {typeof(TClient).FullName}.");
        }

        private static AmazonS3Config BuildConfig(AmazonS3ClientConfigOption option)
        {
            var config = new AmazonS3Config();
            if (option == null)
            {
                return config;
            }

            if (!string.IsNullOrWhiteSpace(option.RegionEndpoint))
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(option.RegionEndpoint);
            }

            ApplyByName(config, option);
            return config;
        }

        private static void ApplyByName(AmazonS3Config config, AmazonS3ClientConfigOption option)
        {
            var optionProperties = typeof(AmazonS3ClientConfigOption).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var optionProperty in optionProperties)
            {
                var value = optionProperty.GetValue(option);
                if (value == null || optionProperty.Name == nameof(AmazonS3ClientConfigOption.RegionEndpoint))
                {
                    continue;
                }

                var configProperty = typeof(AmazonS3Config).GetProperty(optionProperty.Name, BindingFlags.Public | BindingFlags.Instance);
                if (configProperty == null || !configProperty.CanWrite)
                {
                    continue;
                }

                try
                {
                    var targetType = Nullable.GetUnderlyingType(configProperty.PropertyType) ?? configProperty.PropertyType;
                    if (targetType.IsAssignableFrom(value.GetType()))
                    {
                        configProperty.SetValue(config, value);
                        continue;
                    }

                    if (targetType.IsEnum && value is string enumText)
                    {
                        configProperty.SetValue(config, Enum.Parse(targetType, enumText, true));
                        continue;
                    }

                    if (value is IConvertible)
                    {
                        configProperty.SetValue(config, Convert.ChangeType(value, targetType));
                    }
                }
                catch
                {
                }
            }
        }

        private static AmazonS3ClientConfigOption MergeClientConfig(AmazonS3ClientConfigOption parent, AmazonS3ClientConfigOption child, bool forceReplace)
        {
            if (forceReplace)
            {
                return child ?? new AmazonS3ClientConfigOption();
            }

            if (parent == null && child == null)
            {
                return new AmazonS3ClientConfigOption();
            }

            if (parent == null)
            {
                return child;
            }

            if (child == null)
            {
                return parent;
            }

            var result = new AmazonS3ClientConfigOption();
            var properties = typeof(AmazonS3ClientConfigOption).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var childValue = property.GetValue(child);
                var parentValue = property.GetValue(parent);
                property.SetValue(result, childValue ?? parentValue);
            }

            return result;
        }
    }
}
