using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchProxy.Common
{
    public static class Constants
    {
        // Attributes that we are expecting to be on the search document and used for pricing and visibility fitering by OC
        public static class KnownSearchAttributes
        {
            public const string Active = "active";
            public const string Buyers = "buyers";
            public const string Marketplace = "marketplace";
            public const string UserGroups = "usergroups";
            public const string Suppliers = "suppliers";
        }
    }
}
