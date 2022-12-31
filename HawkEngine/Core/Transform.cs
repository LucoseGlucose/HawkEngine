using Silk.NET.Maths;
using System.Collections.Generic;

namespace HawkEngine.Core
{
    public sealed class Transform : HawkObject
    {
        public readonly SceneObject owner;

        public Vector3D<float> position;
        public Vector3D<float> rotation;
        public Vector3D<float> scale = new(1f);

        public Vector3D<float> radRotation
        {
            get { return new(Scalar.DegreesToRadians(rotation.X), Scalar.DegreesToRadians(rotation.Y), Scalar.DegreesToRadians(rotation.Z)); }
            set { rotation = new(Scalar.RadiansToDegrees(value.X), Scalar.RadiansToDegrees(value.Y), Scalar.RadiansToDegrees(value.Z)); }
        }
        public Quaternion<float> orientation
        {
            get { return Quaternion<float>.CreateFromYawPitchRoll(radRotation.Y, radRotation.X, radRotation.Z); }
        }
        public Matrix4X4<float> matrix
        {
            get
            {
                if (_parent == null) return Matrix4X4.CreateScale(scale) * Matrix4X4.CreateFromQuaternion(orientation) * Matrix4X4.CreateTranslation(position);
                return Matrix4X4.CreateScale(relativeScale) * Matrix4X4.CreateFromQuaternion(relativeOrientation) * Matrix4X4.CreateTranslation(relativePosition);
            }
        }

        public Vector3D<float> forward
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitZ, orientation); }
        }
        public Vector3D<float> up
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitY, orientation); }
        }
        public Vector3D<float> right
        {
            get { return Vector3D.Transform(Vector3D<float>.UnitX, orientation); }
        }

        private Transform _parent;
        public Transform parent
        {
            get { return _parent; }
            set
            {
                _parent?.children.Remove(this);
                _parent = value;
                _parent?.children.Add(this);
            }
        }
        public readonly List<Transform> children = new();

        public Vector3D<float> relativePosition
        {
            get { return position - (_parent?.position ?? Vector3D<float>.Zero); }
            set { position = value + (_parent?.position ?? Vector3D<float>.Zero); }
        }
        public Vector3D<float> relativeRotation
        {
            get { return rotation - (_parent?.rotation ?? Vector3D<float>.Zero); }
            set { rotation = value + (_parent?.rotation ?? Vector3D<float>.Zero); }
        }
        public Vector3D<float> relativeScale
        {
            get { return (_parent?.scale ?? Vector3D<float>.One) * scale; }
            set { scale = value * (_parent?.scale ?? Vector3D<float>.One); }
        }

        public Vector3D<float> relativeRadRotation
        {
            get { return new(Scalar.DegreesToRadians(relativeRotation.X),
                Scalar.DegreesToRadians(relativeRotation.Y), Scalar.DegreesToRadians(relativeRotation.Z)); }
            set { relativeRotation = new(Scalar.RadiansToDegrees(value.X), Scalar.RadiansToDegrees(value.Y), Scalar.RadiansToDegrees(value.Z)); }
        }
        public Quaternion<float> relativeOrientation
        {
            get { return Quaternion<float>.CreateFromYawPitchRoll(relativeRadRotation.Y, relativeRadRotation.X, relativeRadRotation.Z); }
        }

        public Transform(SceneObject owner) : base(nameof(Transform))
        {
            this.owner = owner;
        }
    }
}
