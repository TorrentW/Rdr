using System;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Reflection;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Datastore.Converters
{
    public class CommandConverter : EmbeddedDocumentConverter<Command>
    {
        public override Command Parse(object value)
        {
            var stringValue = (string) value;

            if (stringValue.IsNullOrWhiteSpace())
            {
                return null;
            }

            // var ordinal = context.DataRecord.GetOrdinal("Name");
            // var contract = context.DataRecord.GetString(ordinal);
            // var impType = typeof (Command).Assembly.FindTypeByName(contract + "Command");

            // if (impType == null)
            // {
            //     throw new CommandNotFoundException(contract);
            // }

            // return Json.Deserialize(stringValue, impType);
            return null;
        }
    }
}
