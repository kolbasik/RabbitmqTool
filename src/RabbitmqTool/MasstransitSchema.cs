using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace RabbitmqTool
{
    public static class MasstransitSchema
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Validate(RabbitmqSchema schema)
        {
            var exchanges = new HashSet<string>(schema.Exchanges.Select(x => x.Name));
            var queues = new HashSet<string>(schema.Queues.Select(x => x.Name));

            var masstransit = new HashSet<string>(exchanges);
            masstransit.IntersectWith(queues);
            foreach (var element in masstransit)
            {
                Log.Debug($"Found: '{element}'.");
            }

            masstransit.ExceptWith(schema.Bindings.Where(x => string.Equals(x.Source, x.Destination, StringComparison.OrdinalIgnoreCase)).Select(x => x.Source));
            foreach (var element in masstransit)
            {
                Log.Warn($"Missing binding: '{element}' exchange -> '{element}' queue.");
            }
        }
    }
}