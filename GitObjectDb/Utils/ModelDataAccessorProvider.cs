﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace GitObjectDb.Utils
{
    public class ModelDataAccessorProviderModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ModelDataAccessorProvider>().Named<IModelDataAccessorProvider>("handler");
            builder.RegisterDecorator<IModelDataAccessorProvider>(
                inner => new CachedModelDataAccessorProvider(inner),
                fromKey: "handler");
        }
    }

    public interface IModelDataAccessorProvider
    {
        IModelDataAccessor Get(Type type);
    }

    public class ModelDataAccessorProvider : IModelDataAccessorProvider
    {
        public IModelDataAccessor Get(Type type) => new ModelDataAccessor(type);
    }

    public class CachedModelDataAccessorProvider : IModelDataAccessorProvider
    {
        readonly IModelDataAccessorProvider _inner;
        readonly ConcurrentDictionary<Type, IModelDataAccessor> _cache = new ConcurrentDictionary<Type, IModelDataAccessor>();

        public CachedModelDataAccessorProvider(IModelDataAccessorProvider inner) =>
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public IModelDataAccessor Get(Type type) => _cache.GetOrAdd(type, _inner.Get);
    }
}
