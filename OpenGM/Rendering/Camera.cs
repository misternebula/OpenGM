using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace OpenGM.Rendering
{
	public class Camera
	{
		public int ID;

		private Matrix4 _projectionMatrix;
		private Matrix4 _viewMatrix;
		private Matrix4 _viewProjectionMatrix;
		private Matrix4 _inverseProjectionMatrix;
		private Matrix4 _inverseViewMatrix;
		private Matrix4 _inverseViewProjectionMatrix;

		public Matrix4 ProjectionMatrix
		{
			get
			{
				if (VertexManager.RenderTargetActive)
				{
					return _projectionMatrix;
				}

				// flip for backbuffer drawing
				var flipMat = Matrix4.Identity;
				flipMat[1, 1] = -1;

				return _projectionMatrix * flipMat; // TODO right way round?
			}
		}

		public Matrix4 ViewMatrix => _viewMatrix;
		public Matrix4 ViewProjectionMatrix => _viewProjectionMatrix;
		public Matrix4 InverseProjectionMatrix => _inverseProjectionMatrix;
		public Matrix4 InverseViewMatrix => _inverseViewMatrix;
		public Matrix4 InverseViewProjectionMatrix => _inverseViewProjectionMatrix;

		public double ViewX;
		public double ViewY;
		public double ViewWidth;
		public double ViewHeight;
		public double SpeedX;
		public double SpeedY;
		public double BorderX;
		public double BorderY;
		public double ViewAngle;
		public GamemakerObject? TargetInstance = null;
		public bool Is2D = true;

		public bool IsOrthoProj()
		{
			return _projectionMatrix[2, 3] == 0; // index 11
		}

		public void SetViewMat(Matrix4 viewMat)
		{
			_viewMatrix = viewMat;
			_inverseViewMatrix = _viewMatrix.Inverted();
			_viewProjectionMatrix = _viewMatrix * _projectionMatrix; // TODO right way round?
			_inverseViewProjectionMatrix = _viewProjectionMatrix.Inverted();

			Update2D();
		}

		public void SetProjMat(Matrix4 projMat)
		{
			_projectionMatrix = projMat;
			_inverseProjectionMatrix = _projectionMatrix.Inverted();
			_viewProjectionMatrix = _viewMatrix * _projectionMatrix; // TODO right way round?
			_inverseViewProjectionMatrix = _viewProjectionMatrix.Inverted();

			Update2D();
		}

		public void Update2D()
		{
			if (IsOrthoProj())
			{
				if (_projectionMatrix[1, 0] == 0.0 &&	// 4
				    _projectionMatrix[2, 0] == 0.0 &&   // 8
				    _projectionMatrix[0, 1] == 0.0 &&   // 1
				    _projectionMatrix[2, 1] == 0.0 &&   // 9
				    _projectionMatrix[0, 2] == 0.0 &&   // 2
				    _projectionMatrix[1, 2] == 0.0)		// 6
				{
					if (_viewMatrix[0, 2] == 0.0 &&     // 2
					    _viewMatrix[1, 2] == 0.0)		// 6
					{
						Is2D = true;
						return;
					}
				}
			}

			Is2D = false;
		}

		public Vector3d GetCamPos()
		{
			return new Vector3d(
				_inverseViewMatrix[3, 0],
				_inverseViewMatrix[3, 1],
				_inverseViewMatrix[3, 2]
			);
		}

		public Vector3d GetCamDir()
		{
			return new Vector3d(
				_viewMatrix[0, 2],
				_viewMatrix[1, 2],
				_viewMatrix[2, 2]
			).Normalized();
		}

		public Vector3d GetCamUp()
		{
			return new Vector3d(
				_viewMatrix[0, 1],
				_viewMatrix[1, 1],
				_viewMatrix[2, 1]
			).Normalized();
		}

		public Vector3d GetCamRight()
		{
			return new Vector3d(
				_viewMatrix[0, 0],
				_viewMatrix[1, 0],
				_viewMatrix[2, 0]
			).Normalized();
		}
	}
}
