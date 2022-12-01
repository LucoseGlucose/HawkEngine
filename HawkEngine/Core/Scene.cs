using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace HawkEngine.Core
{
    public class Scene
    {
        public readonly string name;
        public readonly List<SceneObject> objects = new();

        public Scene(string name)
        {
            this.name = name;
        }

        public void Update()
        {
            foreach (SceneObject obj in objects)
            {
                obj.Update();
            }
        }

        public SceneObject CreateObject(string name)
        {
            SceneObject t = new();
            t.Create(name);
            objects.Add(t);

            return t;
        }
        public SceneObject CreateObject<T>(string name) where T : SceneObject, new()
        {
            T t = new();
            t.Create(name);
            objects.Add(t);

            return t;
        }
        public void DestroyObject(SceneObject obj)
        {
            objects.Remove(obj);
            obj.Destroy();
        }

        public T FindComponent<T>() where T : Component
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].TryGetComponent(out T t)) return t;
            }

            return null;
        }
        public List<T> FindComponents<T>() where T : Component
        {
            List<T> list = new();

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].TryGetComponents(out IEnumerable<T> t)) list.AddRange(t);
            }

            return list;
        }
    }
}
