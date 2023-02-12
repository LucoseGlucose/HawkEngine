using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public class Component : HawkObject
    {
        public SceneObject owner { get; protected set; }
        public Transform transform { get { return owner.transform; } }
        public bool enabled = true;

        public Component() : base(nameof(Component))
        {

        }
        protected override void Create()
        {
            base.Create();

            name = $"{owner.name} : {GetType().Name}";
        }
        public virtual void Create(SceneObject owner)
        {
            this.owner = owner;
            name = $"{owner.name} : {GetType().Name}";
        }
        public virtual void Destroy()
        {
            owner = null;
        }
        public virtual void Update()
        {
            if (!enabled) return;
        }
    }
}
