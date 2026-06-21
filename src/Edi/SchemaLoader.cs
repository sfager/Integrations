using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Integrations.EDI
{
    // JSON schema classes matching the actual 810.json structure
    public class TransactionSetInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    public class ElementDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("required")]
        public string Required { get; set; } = string.Empty; // "M" or "O"
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class SegmentDefinition
    {
        [JsonPropertyName("segmentId")]
        public string SegmentId { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("required")]
        public string Required { get; set; } = string.Empty;
        
        [JsonPropertyName("maxUse")]
        public int MaxUse { get; set; } = 1;
        
        [JsonPropertyName("elements")]
        public List<ElementDefinition> Elements { get; set; } = new List<ElementDefinition>();
    }

    public class LoopDefinition
    {
        [JsonPropertyName("loopId")]
        public string LoopId { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("required")]
        public string Required { get; set; } = string.Empty;
        
        [JsonPropertyName("maxUse")]
        public int MaxUse { get; set; } = 1;
        
        [JsonPropertyName("segments")]
        public List<SegmentDefinition> Segments { get; set; } = new List<SegmentDefinition>();
        
        [JsonPropertyName("loops")]
        public List<LoopDefinition> Loops { get; set; } = new List<LoopDefinition>();
    }

    public class X12Schema
    {
        [JsonPropertyName("transactionSet")]
        public TransactionSetInfo TransactionSet { get; set; } = new TransactionSetInfo();
        
        [JsonPropertyName("headerArea")]
        public List<SegmentDefinition> HeaderArea { get; set; } = new List<SegmentDefinition>();
        
        [JsonPropertyName("detailArea")]
        public List<LoopDefinition> DetailArea { get; set; } = new List<LoopDefinition>();
        
        [JsonPropertyName("summaryArea")]
        public List<SegmentDefinition> SummaryArea { get; set; } = new List<SegmentDefinition>();
    }

    // Simplified internal schema for validation
    public class ElementSchema
    {
        public int Position { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Type { get; set; } = "AN";
        public int MaxUse { get; set; } = 1;
    }

    public class LoopSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Anchor { get; set; } = string.Empty;
        public int MaxUse { get; set; } = 99999;
    }

    public class TransactionSchema
    {
        public string TransactionSetId { get; set; } = string.Empty;
        public string Release { get; set; } = string.Empty;
        public List<ElementSchema> Elements { get; set; } = new List<ElementSchema>();
        public List<LoopSchema> Loops { get; set; } = new List<LoopSchema>();
    }

    public class LocalSchemaLoader : ISchemaLoader
    {
        private readonly string schemaDirectory;
        private readonly Dictionary<string, TransactionSchema> cache = new Dictionary<string, TransactionSchema>();

        public LocalSchemaLoader(string? schemaDirectory = null)
        {
            this.schemaDirectory = schemaDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "Schemas");
        }

        public TransactionSchema? LoadSchema(string release, string transactionSetId)
        {
            var cacheKey = $"{release}_{transactionSetId}";
            if (cache.TryGetValue(cacheKey, out var cached)) 
                return cached;

            var path = Path.Combine(schemaDirectory, release, $"{transactionSetId}.json");
            if (File.Exists(path))
            {
                try
                {
                    var txt = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        var x12Schema = JsonSerializer.Deserialize<X12Schema>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (x12Schema != null)
                        {
                            // Convert X12Schema to TransactionSchema
                            var schema = new TransactionSchema 
                            { 
                                TransactionSetId = x12Schema.TransactionSet.Id,
                                Release = x12Schema.TransactionSet.Version 
                            };
                            
                            // Extract ST segment elements from header area
                            var stSegment = x12Schema.HeaderArea.FirstOrDefault(s => s.SegmentId == "ST");
                            if (stSegment != null)
                            {
                                int pos = 1;
                                foreach (var el in stSegment.Elements)
                                {
                                    schema.Elements.Add(new ElementSchema
                                    {
                                        Position = pos++,
                                        Name = el.Id,
                                        Required = el.Required == "M",
                                        Type = el.Type == "ID" || el.Type.StartsWith("N") ? "N" : "AN",
                                        MaxUse = 1
                                    });
                                }
                            }
                            
                            // Extract loops from detail area
                            foreach (var loop in x12Schema.DetailArea)
                            {
                                var anchorSegment = loop.Segments.FirstOrDefault();
                                if (anchorSegment != null)
                                {
                                    schema.Loops.Add(new LoopSchema
                                    {
                                        Name = loop.LoopId,
                                        Anchor = anchorSegment.SegmentId,
                                        MaxUse = loop.MaxUse
                                    });
                                }
                            }
                            
                            cache[cacheKey] = schema;
                            return schema;
                        }
                    }
                }
                catch
                {
                    // fallthrough to synthesize default
                }
            }

            // synthesize minimal schema for common transaction sets
            if (transactionSetId == "810")
            {
                var synth = new TransactionSchema { TransactionSetId = transactionSetId, Release = release };
                synth.Elements.Add(new ElementSchema { Position = 1, Name = "ST01", Required = true, Type = "AN", MaxUse = 1 });
                synth.Elements.Add(new ElementSchema { Position = 2, Name = "ST02", Required = true, Type = "AN", MaxUse = 1 });
                synth.Loops.Add(new LoopSchema { Name = "L_N1", Anchor = "N1", MaxUse = 200 });
                cache[cacheKey] = synth;
                return synth;
            }

            return null;
        }
    }
}
