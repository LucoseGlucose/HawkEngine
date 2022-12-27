﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEngine.Core
{
    public class Component : HawkObject
    {
        public SceneObject owner { get; private set; }
        public Transform transform { get { return owner.transform; } }

        public virtual void Create(SceneObject owner)
        {
            GenRandomID();
            this.owner = owner;
        }
        public virtual void Destroy()
        {
            owner = null;
        }
        public virtual void Update()
        {

        }
    }
}
