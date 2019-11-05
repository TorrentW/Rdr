using System;
using System.Collections.Generic;
using Dapper;
using Dapper.Contrib.Extensions;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Blacklisting;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Download;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.NetImport;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using static Dapper.SqlMapper;

namespace NzbDrone.Core.Datastore
{
    public static class TableMapping
    {
        public static void Map()
        {
            SqlMapperExtensions.TableNameMapper = TableNameMapping;

            RegisterMappers();
        }

        public static string TableNameMapping(Type entityType)
        {
            if (entityType == typeof(Config))
            {
                return "Config";
            }
            else if (entityType == typeof(DownloadClientDefinition))
            {
                return "DownloadClients";
            }
            else if (entityType == typeof(IndexerDefinition))
            {
                return "Indexers";
            }
            else if (entityType == typeof(NetImportDefinition))
            {
                return "NetImport";
            }
            else if (entityType == typeof(NotificationDefinition))
            {
                return "Notifications";
            }
            else if (entityType == typeof(MetadataDefinition))
            {
                return "Metadata";
            }
            else if (entityType == typeof(History.History))
            {
                return "History";
            }
            else if (entityType == typeof(NamingConfig))
            {
                return "NamingConfig";
            }
            else if (entityType == typeof(Blacklist))
            {
                return "Blacklist";
            }
            else if (entityType == typeof(OtherExtraFile))
            {
                return "ExtraFiles";
            }
            else if (entityType == typeof(CommandModel))
            {
                return "Commands";
            }
            else if (entityType == typeof(IndexerStatus))
            {
                return "IndexerStatus";
            }
            else if (entityType == typeof(DownloadClientStatus))
            {
                return "DownloadClientStatus";
            }

            return entityType.Name + "s";
        }

        private static void RegisterMappers()
        {
            RegisterEmbeddedConverter();
            RegisterProviderSettingConverter();

            SqlMapper.AddTypeHandler(new DapperQualityIntConverter());
            SqlMapper.AddTypeHandler(new DapperCustomFormatIntConverter());
            SqlMapper.AddTypeHandler(new DapperLanguageIntConverter());
            SqlMapper.AddTypeHandler(new OsPathConverter());
            SqlMapper.AddTypeHandler(new GuidConverter());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<Language>>(new LanguageIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ProfileQualityItem>>(new QualityIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ProfileFormatItem>>(new CustomFormatIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<FormatTag>>(new QualityTagStringConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<QualityModel>(new CustomFormatIntConverter(), new QualityIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<Dictionary<string, string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<IDictionary<string, string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<int>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<KeyValuePair<string, int>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedMovieInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ReleaseInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<HashSet<int>>());
        }

        private static void RegisterProviderSettingConverter()
        {
            var settingTypes = typeof(IProviderConfig).Assembly.ImplementationsOf<IProviderConfig>();

            var providerSettingConverter = new ProviderSettingConverter();
            foreach (var embeddedType in settingTypes)
            {
                SqlMapper.AddTypeHandler(embeddedType, providerSettingConverter);
            }
        }

        private static void RegisterEmbeddedConverter()
        {
            var embeddedTypes = typeof(IEmbeddedDocument).Assembly.ImplementationsOf<IEmbeddedDocument>();

            var embeddedConverterDefinition = typeof(EmbeddedDocumentConverter<>).GetGenericTypeDefinition();
            var genericListDefinition = typeof(List<>).GetGenericTypeDefinition();

            foreach (var embeddedType in embeddedTypes)
            {
                var embeddedListType = genericListDefinition.MakeGenericType(embeddedType);

                RegisterEmbeddedConverter(embeddedType, embeddedConverterDefinition);
                RegisterEmbeddedConverter(embeddedListType, embeddedConverterDefinition);
            }
        }

        private static void RegisterEmbeddedConverter(Type embeddedType, Type embeddedConverterDefinition)
        {
            var embeddedConverterType = embeddedConverterDefinition.MakeGenericType(embeddedType);
            var converter = (ITypeHandler) Activator.CreateInstance(embeddedConverterType);

            SqlMapper.AddTypeHandler(embeddedType, converter);
        }
    }
}
