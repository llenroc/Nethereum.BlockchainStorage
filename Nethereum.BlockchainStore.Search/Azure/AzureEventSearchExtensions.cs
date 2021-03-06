﻿using Microsoft.Azure.Search.Models;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Nethereum.BlockchainStore.Search.Azure
{
    public static class AzureEventSearchExtensions
    {
        public const string SuggesterName = "sg";

        public static object ToAzureDocument<TEvent>(this EventLog<TEvent> log, EventSearchIndexDefinition<TEvent> indexDefinition) where TEvent : class, new()
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var field in indexDefinition.Fields)
            {
                var azureField = field.ToAzureField();
                var val = field.GetValue(log)?.ToAzureFieldValue();
                if (val != null)
                {
                    dictionary.Add(azureField.Name, val);
                }
            }

            return dictionary;
        }
        
        public static Index ToAzureIndex(this SearchIndexDefinition searchIndex)
        {
            var index = new Index
            {
                Name = searchIndex.IndexName.ToLower(), 
                Fields = searchIndex.Fields.ToAzureFields(), 
                Suggesters = searchIndex.Fields.ToAzureSuggesters()
            };

            return index;
        }

        public static Suggester[] ToAzureSuggesters(this IEnumerable<SearchField> fields)
        {
            var suggesterFields = fields
                .Where(f => f.IsSuggester && f.IsSearchable)
                .OrderBy(f => f.SuggesterOrder)
                .ToArray();

            if (!suggesterFields.Any()) return Array.Empty<Suggester>();
            return new[] {new Suggester(SuggesterName, suggesterFields.Select(f => f.Name.ToLower()).ToArray())};
        }

        public static Field[] ToAzureFields(this IEnumerable<SearchField> fields)
        {
            return fields.Select(ToAzureField).ToArray();
        }

        public static Field ToAzureField(this SearchField f)
        {
            return new Field(f.Name.ToLower(), f.DataType.ToAzureDataType())
            {
                IsKey = f.IsKey,
                IsFilterable = f.IsFilterable,
                IsSortable = f.IsSortable,
                IsSearchable = f.IsSearchable,
                IsFacetable = f.IsFacetable,
            };
        }

        public static object ToAzureFieldValue(this object val)
        {
            if (val == null) return null;

            if(val is string) return val;
            if(val is bool) return val;
            if(val is HexBigInteger hexBigInteger) return hexBigInteger.Value.ToString();
            if(val is BigInteger bigInteger) return bigInteger.ToString();
            if (val is byte[] byteArray) return Encoding.UTF8.GetString(byteArray);

            return val.ToString();
        }

        public static DataType ToAzureDataType(this Type type)
        {
            if(type == typeof(string)) return DataType.String;
            if(type == typeof(bool)) return DataType.Boolean;
            if(type == typeof(HexBigInteger)) return DataType.String;
            if(type == typeof(BigInteger)) return DataType.String;

            return DataType.String;
        }

        public static IList<string> FacetableFieldNames(this Index index)
        {
            return index.Fields.Where(f => f.IsFacetable).Select(f => f.Name).ToList();
        }
    }
}