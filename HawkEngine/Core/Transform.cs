using Silk.NET.Maths;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace HawkEngine.Core
{
    public sealed class Transform : HawkObject
    {
        public readonly SceneObject owner;

        [Utils.DontSerialize] private Vector3D<float> _position;
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

        [Utils.DontSerialize] private Quaternion<float> _rotation = Quaternion<float>.Identity;
        public Quaternion<float> rotation
        {
            get
            {
                if (_parent != null) _rotation = _parent.rotation * _localRotation;
                return _rotation;
            }
            set
            {
                _rotation = value;

                if (_parent == null) _localRotation = _rotation;
                else _localRotation = Quaternion<float>.Inverse(_parent.rotation) * _rotation;
            }
        }

        [Utils.DontSerialize] private Vector3D<float> _scale = Vector3D<float>.One;
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

        [Utils.DontSerialize]
        public Vector3D<float> eulerAngles
        {
            get { return rotation.ToEulerAngles(); }
            set { rotation = value.ToQuaternion(); }
        }

        [Utils.DontSerialize]
        public Matrix4X4<float> matrix
        {
            get
            {
                if (_parent == null) return Matrix4X4.CreateScale(scale) * Matrix4X4.CreateFromQuaternion(rotation) * Matrix4X4.CreateTranslation(position);
                return localMatrix * _parent.matrix;
            }
            set
            {
                if (!Matrix4X4.Decompose(value, out Vector3D<float> s, out Quaternion<float> r, out Vector3D<float> t)) return;

                position = t;
                rotation = r;
                scale = s;
            }
        }

        [Utils.DontSerialize]
        public Vector3D<float> forward
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitZ, rotation); }
            set { rotation = Quaternion<float>.CreateFromAxisAngle(value, 0f); }
        }

        [Utils.DontSerialize]
        public Vector3D<float> up
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitY, rotation); }
            set { rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D.Cross(value, right), 0f); }
        }

        [Utils.DontSerialize]
        public Vector3D<float> right
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitX, rotation); }
            set { rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D.Cross(value, up), 0f); }
        }

        [Utils.DontSerialize] private Transform _parent;
        public Transform parent
        {
            get { return _parent; }
            set
            {
                Vector3D<float> pos = position;
                Quaternion<float> rot = rotation;
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

        [Utils.DontSerialize] private Vector3D<float> _localPosition;
        public Vector3D<float> localPosition
        {
            get
            {
                return _localPosition;
            }
            set
            {
                _localPosition = value;

                if (_parent == null) _position = _localPosition;
                else _position = Vector3D.Transform(_localPosition, _parent.matrix);
            }
        }

        [Utils.DontSerialize] private Quaternion<float> _localRotation = Quaternion<float>.Identity;
        public Quaternion<float> localRotation
        {
            get
            {
                return _localRotation;
            }
            set
            {
                _localRotation = value;

                if (_parent == null) _rotation = _localRotation;
                else _rotation = _parent.rotation * _localRotation;
            }
        }

        [Utils.DontSerialize] private Vector3D<float> _localScale = Vector3D<float>.One;
        public Vector3D<float> localScale
        {
            get
            {
                return _localScale;
            }
            set
            {
                _localScale = value;

                if (_parent == null) _scale = _localScale;
                else _scale = _localScale * _parent.scale;
            }
        }

        [Utils.DontSerialize]
        public Vector3D<float> localEulerAngles
        {
            get { return localRotation.ToEulerAngles(); }
            set { localRotation = value.ToQuaternion(); }
        }

        [Utils.DontSerialize]
        public Matrix4X4<float> localMatrix
        {
            get
            {
                return Matrix4X4.CreateScale(localScale) * Matrix4X4.CreateFromQuaternion(localRotation) * Matrix4X4.CreateTranslation(localPosition);
            }
            set
            {
                if (!Matrix4X4.Decompose(value, out Vector3D<float> s, out Quaternion<float> r, out Vector3D<float> t)) return;

                localPosition = t;
                localRotation = r;
                localScale = s;
            }
        }

        public Transform() : base()
        {

        }
        public Transform(SceneObject owner) : base($"{owner.name} : {nameof(Transform)}")
        {
            this.owner = owner;
        }
    }
}
