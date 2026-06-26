using System;
using System.Collections.Generic;

namespace TestTask.DI
{
    public sealed class SimpleContainer
    {
        private readonly Dictionary<Type, object> _bindings = new();

        public void Bind<T>(T instance) where T : class => _bindings[typeof(T)] = instance;

        public T Resolve<T>() where T : class
        {
            if (_bindings.TryGetValue(typeof(T), out var instance))
                return (T)instance;

            throw new InvalidOperationException($"Dependency {typeof(T).Name} is not registered.");
        }
    }
}
