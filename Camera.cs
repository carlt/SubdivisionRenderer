using System;
using System.Windows.Forms;
using SlimDX;

namespace SubdivisionRenderer
{
	static class Camera
	{
		// World
		public static Vector3 Position;
		public static Vector3 Rotation;

		// View
		public static Vector3 Eye = new Vector3(0f, 0f, -1.5f);
		public static Vector3 Target = Vector3.Zero;
		public static Vector3 Up = new Vector3(0f, 1f, 0f);

		// Projection
		public static float Fov = (float) Math.PI * 0.5f;
		public static float Aspect = (float) 800 / 600;

		// Movement
		private const float BaseMoveStep = 3f;
		private static float MoveStep { get { return _speed ? BaseMoveStep * 3f : BaseMoveStep; } }

		// Rotation
		private const float BaseRotateStep = 1f;
		private static float RotateStep { get { return _speed ? BaseRotateStep * 3f : BaseRotateStep; } }
		
		// Indicators
		private static bool _speed;
		private static readonly bool[] Moving = new bool[10];

		public static Matrix World()
		{
			var secPerFrame = 1f / Program.GetFrameRate();

			if (Moving[(int) Mode.RotatePosY])
			{
				Rotate(new Vector3(0f, RotateStep * secPerFrame, 0f));
			}
			if (Moving[(int) Mode.RotateNegY])
			{
				Rotate(new Vector3(0f, -RotateStep * secPerFrame, 0f));
			}
			if (Moving[(int) Mode.RotatePosX])
			{
				Rotate(new Vector3(RotateStep * secPerFrame, 0f, 0f));
			}
			if (Moving[(int) Mode.RotateNegX])
			{
				Rotate(new Vector3(-RotateStep * secPerFrame, 0f, 0f));
			}
			
			return Matrix.RotationYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z) * Matrix.Translation(Position);
		}

		private static Matrix View()
		{
			var direction = Vector3.Normalize(Target - Eye);
			var strafe = Vector3.Normalize(Vector3.Cross(direction, Up));
			var secPerFrame = 1f / Program.GetFrameRate();

			if (Moving[(int)Mode.MoveForward])
			{
				Target += direction * MoveStep * secPerFrame;
				Eye += direction * MoveStep * secPerFrame;
			}
			if (Moving[(int)Mode.MoveBackward])
			{
				Target -= direction * MoveStep * secPerFrame;
				Eye -= direction * MoveStep * secPerFrame;
			}
			if (Moving[(int)Mode.MoveLeft])
			{
				Target += strafe * MoveStep * secPerFrame;
				Eye += strafe * MoveStep * secPerFrame;
			}
			if (Moving[(int)Mode.MoveRight])
			{
				Target -= strafe * MoveStep * secPerFrame;
				Eye -= strafe * MoveStep * secPerFrame;
			}
			if (Moving[(int)Mode.MoveUp])
			{
				Target += Up * MoveStep * secPerFrame;
				Eye += Up * MoveStep * secPerFrame;
			}
			if (Moving[(int)Mode.MoveDown])
			{
				Target -= Up * MoveStep * secPerFrame;
				Eye -= Up * MoveStep * secPerFrame;
			}

			return Matrix.LookAtLH(Eye, Target, Up);
		}

		public static Matrix WorldViewProjection()
		{
			return World() * View() * Matrix.PerspectiveFovLH(Fov, Aspect, 0.1f, 100f);
		}

		public static void Rotate(Vector3 rotation)
		{
			if (Rotation.X + rotation.X > Math.PI * 2)
				Rotation.X += rotation.X - (float)Math.PI * 2;
			else if (Rotation.X + rotation.X < -Math.PI * 2)
				Rotation.X += rotation.X + (float)Math.PI * 2;
			else
				Rotation.X += rotation.X;

			if (Rotation.Y + rotation.Y > Math.PI * 2)
				Rotation.Y += rotation.Y - (float)Math.PI * 2;
			else if (Rotation.Y + rotation.Y < -Math.PI * 2)
				Rotation.Y += rotation.Y + (float)Math.PI * 2;
			else
				Rotation.Y += rotation.Y;

			if (Rotation.Z + rotation.Z > Math.PI * 2)
				Rotation.Z += rotation.Z - (float)Math.PI * 2;
			else if (Rotation.Z + rotation.Z < -Math.PI * 2)
				Rotation.Z += rotation.Z + (float)Math.PI * 2;
			else
				Rotation.Z += rotation.Z;
		}

		public static void HandleKeyboardStart(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ShiftKey)
			{
				_speed = true;
			}
			else if (e.KeyCode == Controls.MoveForward)
			{
				Moving[(int) Mode.MoveForward] = true;
			} 
			else if (e.KeyCode == Controls.MoveBackward)
			{
				Moving[(int) Mode.MoveBackward] = true;
			}
			else if (e.KeyCode == Controls.MoveLeft)
			{
				Moving[(int) Mode.MoveLeft] = true;
			}
			else if (e.KeyCode == Controls.MoveRight)
			{
				Moving[(int) Mode.MoveRight] = true;
			}
			else if (e.KeyCode == Controls.MoveUp)
			{
				Moving[(int) Mode.MoveUp] = true;
			}
			else if (e.KeyCode == Controls.MoveDown)
			{
				Moving[(int) Mode.MoveDown] = true;
			}
			else if (e.KeyCode == Controls.RotatePosY)
			{
				Moving[(int) Mode.RotatePosY] = true;
			}
			else if (e.KeyCode == Controls.RotateNegY)
			{
				Moving[(int) Mode.RotateNegY] = true;
			}
			else if (e.KeyCode == Controls.RotatePosX)
			{
				Moving[(int) Mode.RotatePosX] = true;
			}
			else if (e.KeyCode == Controls.RotateNegX)
			{
				Moving[(int) Mode.RotateNegX] = true;
			}
		}

		public static void HandleKeyboardEnd(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.ShiftKey)
			{
				_speed = false;
			}
			else if (e.KeyCode == Controls.MoveForward)
			{
				Moving[(int) Mode.MoveForward] = false;
			}
			else if (e.KeyCode == Controls.MoveBackward)
			{
				Moving[(int) Mode.MoveBackward] = false;
			}
			else if (e.KeyCode == Controls.MoveLeft)
			{
				Moving[(int) Mode.MoveLeft] = false;
			}
			else if (e.KeyCode == Controls.MoveRight)
			{
				Moving[(int) Mode.MoveRight] = false;
			}
			else if (e.KeyCode == Controls.MoveUp)
			{
				Moving[(int) Mode.MoveUp] = false;
			}
			else if (e.KeyCode == Controls.MoveDown)
			{
				Moving[(int) Mode.MoveDown] = false;
			}
			else if (e.KeyCode == Controls.RotatePosY)
			{
				Moving[(int) Mode.RotatePosY] = false;
			}
			else if (e.KeyCode == Controls.RotateNegY)
			{
				Moving[(int) Mode.RotateNegY] = false;
			}
			else if (e.KeyCode == Controls.RotatePosX)
			{
				Moving[(int) Mode.RotatePosX] = false;
			}
			else if (e.KeyCode == Controls.RotateNegX)
			{
				Moving[(int) Mode.RotateNegX] = false;
			}
		}

		private enum Mode
		{
			MoveForward,
			MoveBackward,
			MoveLeft,
			MoveRight,
			MoveUp,
			MoveDown,
			RotatePosY,
			RotateNegY,
			RotatePosX,
			RotateNegX
		}
	}
}
