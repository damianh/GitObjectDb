﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitObjectDb.Backends
{
    public class InMemoryBackend : AbstractOdbBackend
    {
        readonly IDictionary<ObjectId, StoreItem> _store = new Dictionary<ObjectId, StoreItem>();
        (ObjectId Id, StoreItem Item)? _lastItem;

        public override bool Exists(ObjectId id) => _store.ContainsKey(id);

        public override int Read(ObjectId id, out UnmanagedMemoryStream data, out ObjectType objectType)
        {
            var lastItem = _lastItem; // Thread safety
            var entry = lastItem.HasValue && lastItem.Value.Id.Equals(id) ? lastItem.Value.Item : _store[id];
            objectType = entry.ObjectType;
            data = Allocate(entry.Data.LongLength);
            using (var reader = new MemoryStream(entry.Data))
            {
                reader.CopyTo(data);
            }
            return (int)ReturnCode.GIT_OK;
        }

        public override int Write(ObjectId id, Stream dataStream, long length, ObjectType objectType)
        {
            var value = new StoreItem
            {
                Data = ReadStream(dataStream, length),
                ObjectType = objectType
            };
            _store[id] = value;
            _lastItem = (id, value);
            return (int)ReturnCode.GIT_OK;
        }
    }
}
