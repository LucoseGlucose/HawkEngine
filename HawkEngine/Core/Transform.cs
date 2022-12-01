using Silk.NET.Maths;

namespace HawkEngine.Core
{
    public class Transform
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
            get { return Matrix4X4.CreateScale(scale) * Matrix4X4.CreateFromQuaternion(orientation) * Matrix4X4.CreateTranslation(position); }
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
    }
}
