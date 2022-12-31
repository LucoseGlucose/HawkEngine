using Silk.NET.Maths;

namespace HawkEngine.Core
{
    public sealed class Transform : HawkObject
    {
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
                if (parent == null) return Matrix4X4.CreateScale(scale) * Matrix4X4.CreateFromQuaternion(orientation) * Matrix4X4.CreateTranslation(position);
                else return parent.matrix * relativeMatrix;
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

        public Vector3D<float> relativePosition;
        public Vector3D<float> relativeRotation;
        public Vector3D<float> relativeScale = new(1f);

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
        public Matrix4X4<float> relativeMatrix
        {
            get { return Matrix4X4.CreateScale(relativeScale) *
                    Matrix4X4.CreateFromQuaternion(relativeOrientation) * Matrix4X4.CreateTranslation(relativePosition); }
        }

        public Transform parent;

        public Transform() : base(nameof(Transform))
        {
            
        }
    }
}
