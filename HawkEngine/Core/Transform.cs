using Silk.NET.Maths;
using System.Collections.Generic;

namespace HawkEngine.Core
{
    public sealed class Transform : HawkObject
    {
        public readonly SceneObject owner;

        private Vector3D<float> _position;
        public Vector3D<float> position
        {
            get
            {
                if (_parent != null) _position = Vector3D.Transform(_localPosition, _parent.matrix);
                return _position;
            }
            set
            {
                _position = value;

                if (_parent == null) _localPosition = _position;
                else _localPosition = Vector3D.Transform(_position, _parent.matrix.Inverse());
            }
        }

        private Vector3D<float> _rotation;
        public Vector3D<float> rotation
        {
            get
            {
                if (_parent != null) _rotation = _localRotation;
                return _rotation;
            }
            set
            {
                _rotation = value;

                if (_parent == null) _localRotation = _rotation;
                else _localRotation = _rotation;
            }
        }

        private Vector3D<float> _scale = Vector3D<float>.One;
        public Vector3D<float> scale
        {
            get
            {
                if (_parent != null) _scale = _localScale * _parent.scale;
                return _scale;
            }
            set
            {
                _scale = value;

                if (_parent == null) _localScale = _scale;
                else _localScale = _scale / _parent.scale;
            }
        }

        public Quaternion<float> orientation
        {
            get { return rotation.ToQuaternion(); }
            set { rotation = value.ToEulerAngles(); }
        }

        public Matrix4X4<float> matrix
        {
            get
            {
                if (_parent == null) return Matrix4X4.CreateScale(scale) * Matrix4X4.CreateFromQuaternion(orientation) * Matrix4X4.CreateTranslation(position);
                return localMatrix * _parent.matrix;
            }
            set
            {
                if (!Matrix4X4.Decompose(value, out Vector3D<float> s, out Quaternion<float> r, out Vector3D<float> t)) return;

                position = t;
                orientation = r;
                scale = s;
            }
        }

        public Vector3D<float> forward
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitZ, orientation); }
            set { orientation = Quaternion<float>.CreateFromAxisAngle(value, 0f); }
        }
        public Vector3D<float> up
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitY, orientation); }
            set { orientation = Quaternion<float>.CreateFromAxisAngle(Vector3D.Cross(value, right), 0f); }
        }
        public Vector3D<float> right
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitX, orientation); }
            set { orientation = Quaternion<float>.CreateFromAxisAngle(Vector3D.Cross(value, up), 0f); }
        }

        private Transform _parent;
        public Transform parent
        {
            get { return _parent; }
            set
            {
                Vector3D<float> pos = position;
                Vector3D<float> rot = rotation;
                Vector3D<float> scl = scale;

                _parent?.children.Remove(this);
                _parent = value;
                _parent?.children.Add(this);

                position = pos;
                rotation = rot;
                scale = scl;
            }
        }
        public readonly List<Transform> children = new();

        private Vector3D<float> _localPosition;
        public Vector3D<float> localPosition
        {
            get
            {
                if (_parent == null) _localPosition = _position;
                return _localPosition;
            }
            set
            {
                _localPosition = value;

                if (_parent == null) _position = _localPosition;
                else _position = Vector3D.Transform(_localPosition, _parent.matrix);
            }
        }

        private Vector3D<float> _localRotation;
        public Vector3D<float> localRotation
        {
            get
            {
                if (_parent != null) _localRotation = _rotation;
                return _localRotation;
            }
            set
            {
                _localRotation = value;

                if (_parent == null) _rotation = _localRotation;
                else _rotation = _localRotation;
            }
        }

        private Vector3D<float> _localScale = Vector3D<float>.One;
        public Vector3D<float> localScale
        {
            get
            {
                if (_parent != null) _localScale = _scale / _parent.scale;
                return _localScale;
            }
            set
            {
                _localScale = value;

                if (_parent == null) _scale = _localScale;
                else _scale = _localScale * _parent.scale;
            }
        }

        public Quaternion<float> localOrientation
        {
            get { return localRotation.ToQuaternion(); }
            set { localRotation = value.ToEulerAngles(); }
        }

        public Matrix4X4<float> localMatrix
        {
            get
            {
                return Matrix4X4.CreateScale(localScale) * Matrix4X4.CreateFromQuaternion(localOrientation) * Matrix4X4.CreateTranslation(localPosition);
            }
            set
            {
                if (!Matrix4X4.Decompose(value, out Vector3D<float> s, out Quaternion<float> r, out Vector3D<float> t)) return;

                localPosition = t;
                localOrientation = r;
                localScale = s;
            }
        }

        public Transform(SceneObject owner) : base(nameof(Transform))
        {
            this.owner = owner;
        }
    }
}
