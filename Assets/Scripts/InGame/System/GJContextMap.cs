using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameJam.InGame.System
{
    public class GJContextMap
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T instance) where T : class
        {
            if (instance is not IGJContext)
            {
                throw new Exception($"To register in ContextMap, the class must implement IGJContext. Class being registered: {typeof(T)}");
            }
            _services[typeof(T)] = instance;
        }

        public T GetRegisteredInstance<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var instance))
            {
                return instance as T;
            }
            throw new Exception($"This class is not registered in ContextMap: {typeof(T)}");
        }

        public async UniTask InitializeAllContextAsync()
        {
            var initTasks = new List<UniTask>();

            foreach (var service in _services.Values)
            {
                if (service is IGJContext initializable)
                {
                    initTasks.Add(initializable.InitAsync());
                }
            }

            await UniTask.WhenAll(initTasks);
        }

        public void WireContextDependencyAfterInit()
        {
            foreach (var service in _services.Values)
            {
                if (service is IGJContext initializable)
                {
                    initializable.WireContextAfterInit(this);
                }
            }
        }
    }
}
