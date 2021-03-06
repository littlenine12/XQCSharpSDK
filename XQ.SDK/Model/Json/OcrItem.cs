﻿using Newtonsoft.Json;

namespace XQ.SDK.Model.Json
{
    public class OcrItem
    {
        /// <summary>
        ///     文字
        /// </summary>
        [JsonProperty(PropertyName = "txt")]
        public string Text { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "a")]
        public string A { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "b")]
        public string B { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "c")]
        public string C { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "d")]
        public string D { get; set; }
    }
}