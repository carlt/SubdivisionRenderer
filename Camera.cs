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

		// Mouse
		private static int _mouseX;
		private static int _mouseY;
		private static bool _mousePressed;

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
			
			return Matrix.Translation(Position) * Matrix.RotationYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z);
		}

		private static Matrix View()
		{
			var direction = Vector3.Normalize(Target - Eye);
			var strafe = Vector3.Normalize(Vector3.Cross(direction, Up));
			var secPerFrame = 1f / Program.GetFrameRate();

			if (Moving[(int) Mode.MoveForward])
			{
				Target += direction * MoveStep * secPerFrame;
				Eye += direction * MoveStep * secPerFrame;
			}
			if (Moving[(int) Mode.MoveBackward])
			{
				Target -= direction * MoveStep * secPerFrame;
				Eye -= direction * MoveStep * secPerFrame;
			}
			if (Moving[(int) Mode.MoveLeft])
			{
				Target += strafe * MoveStep * secPerFrame;
				Eye += strafe * MoveStep * secPerFrame;
			}
			if (Moving[(int) Mode.MoveRight])
			{
				Target -= strafe * MoveStep * secPerFrame;
				Eye -= strafe * MoveStep * secPerFrame;
			}
			if (Moving[(int) Mode.MoveUp])
			{
				Target += Up * MoveStep * secPerFrame;
				Eye += Up * MoveStep * secPerFrame;
			}
			if (Moving[(int) Mode.MoveDown])
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

		public static void Reset()
		{
			Position = new Vector3();
			Rotation = new Vector3();

			Eye = new Vector3(0f, 0f, -1.5f);
			Target = Vector3.Zero;
			Up = new Vector3(0f, 1f, 0f);
		}

		private static void Rotate(Vector3 rotation)
		{
			if (Rotation.X + rotation.X > Math.PI * 2)
				Rotation.X += rotation.X - (float) Math.PI * 2;
			else if (Rotation.X + rotation.X < -Math.PI * 2)
				Rotation.X += rotation.X + (float) Math.PI * 2;
			else
				Rotation.X += rotation.X;

			if (Rotation.Y + rotation.Y > Math.PI * 2)
				Rotation.Y += rotation.Y - (float) Math.PI * 2;
			else if (Rotation.Y + rotation.Y < -Math.PI * 2)
				Rotation.Y += rotation.Y + (float) Math.PI * 2;
			else
				Rotation.Y += rotation.Y;

			if (Rotation.Z + rotation.Z > Math.PI * 2)
				Rotation.Z += rotation.Z - (float) Math.PI * 2;
			else if (Rotation.Z + rotation.Z < -Math.PI * 2)
				Rotation.Z += rotation.Z + (float) Math.PI * 2;
			else
				Rotation.Z += rotation.Z;
		}

		public static void HandleKeyboardStart(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.ShiftKey:
					_speed = true;
					break;
				case Controls.MoveForward:
					Moving[(int) Mode.MoveForward] = true;
					break;
				case Controls.MoveBackward:
					Moving[(int) Mode.MoveBackward] = true;
					break;
				case Controls.MoveLeft:
					Moving[(int) Mode.MoveLeft] = true;
					break;
				case Controls.MoveRight:
					Moving[(int) Mode.MoveRight] = true;
					break;
				case Controls.MoveUp:
					Moving[(int) Mode.MoveUp] = true;
					break;
				case Controls.MoveDown:
					Moving[(int) Mode.MoveDown] = true;
					break;
				case Controls.RotatePosY:
					Moving[(int) Mode.RotatePosY] = true;
					break;
				case Controls.RotateNegY:
					Moving[(int) Mode.RotateNegY] = true;
					break;
				case Controls.RotatePosX:
					Moving[(int) Mode.RotatePosX] = true;
					break;
				case Controls.RotateNegX:
					Moving[(int) Mode.RotateNegX] = true;
					break;
			}
		}

		public static void HandleKeyboardEnd(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.ShiftKey:
					_speed = false;
					break;
				case Controls.MoveForward:
					Moving[(int) Mode.MoveForward] = false;
					break;
				case Controls.MoveBackward:
					Moving[(int) Mode.MoveBackward] = false;
					break;
				case Controls.MoveLeft:
					Moving[(int) Mode.MoveLeft] = false;
					break;
				case Controls.MoveRight:
					Moving[(int) Mode.MoveRight] = false;
					break;
				case Controls.MoveUp:
					Moving[(int) Mode.MoveUp] = false;
					break;
				case Controls.MoveDown:
					Moving[(int) Mode.MoveDown] = false;
					break;
				case Controls.RotatePosY:
					Moving[(int) Mode.RotatePosY] = false;
					break;
				case Controls.RotateNegY:
					Moving[(int) Mode.RotateNegY] = false;
					break;
				case Controls.RotatePosX:
					Moving[(int) Mode.RotatePosX] = false;
					break;
				case Controls.RotateNegX:
					Moving[(int) Mode.RotateNegX] = false;
					break;
			}
		}

		public static void HandleMouseDown(MouseEventArgs e)
		{
			_mouseX = e.X;
			_mouseY = e.Y;
			_mousePressed = true;
		}

		public static void HandleMouseUp(MouseEventArgs e)
		{
			_mousePressed = false;
		}

		public static void HandleMouseMove(MouseEventArgs e)
		{
			if (!_mousePressed) return;

			int deltaX;
			int deltaY;
			switch (e.Button)
			{
				case MouseButtons.Left:
					deltaX = e.X - _mouseX;
					deltaY = e.Y - _mouseY;
			
					Rotate(new Vector3(deltaX / -360f, deltaY / -360f, 0f));

					_mouseX = e.X;
					_mouseY = e.Y;
					break;

				case MouseButtons.Right:
					deltaX = e.X - _mouseX;
					deltaY = e.Y - _mouseY;
			
					Position += new Vector3(deltaX / 360f, deltaY / -360f, 0f);

					_mouseX = e.X;
					_mouseY = e.Y;
					break;
			}
		}

		public static void HandleMouseWheel(MouseEventArgs e)
		{
			var direction = Vector3.Normalize(Target - Eye);

			if (e.Delta > 0)
			{
				Target += direction * MoveStep * 0.25f;
				Eye += direction * MoveStep * 0.25f;
			}
			else
			{
				Target -= direction * MoveStep * 0.25f;
				Eye -= direction * MoveStep * 0.25f;
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
