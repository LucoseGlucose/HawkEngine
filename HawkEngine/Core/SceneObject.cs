using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public class SceneObject
    {
        public string name;
        public readonly List<Component> components = new();
        public readonly Transform transform = new();

        public virtual void Create(string name)
        {
            this.name = name;
        }
        public virtual void Destroy()
        {
            foreach (Component c in components)
            {
                c.Destroy();
            }

            components.Clear();
        }

        public void Update()
        {
            foreach (Component c in components)
            {
                c.Update();
            }
        }

        public T AddComponent<T>() where T : Component, new()
        {
            T t = new();
            t.Create(this);
            components.Add(t);

            return t;
        }
        public void RemoveComponent(Component c)
        {
            components.Remove(c);
            c.Destroy();
        }

        public T GetComponent<T>()
        {
            return components.OfType<T>().FirstOrDefault();
        }
        public IEnumerable<T> GetComponents<T>()
        {
            return components.OfType<T>();
        }

        public bool TryGetComponent<T>(out T t)
        {
            t = GetComponent<T>();
            return t != null;
        }
        public bool TryGetComponents<T>(out IEnumerable<T> t)
        {
            t = GetComponents<T>();
            return t.Any();
        }
    }
}
