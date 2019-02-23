using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace KrogerScrape.Client
{
    public class ModelHelper
    {
        public static void SetIfNullOrEmpty(ref List<JToken> _field, List<JToken> value)
        {
            if (value == null || value.Count == 0)
            {
                _field = value;
                return;
            }

            throw new NotImplementedException();
        }
    }
}
