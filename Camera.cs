using System;
using System.Windows.Forms;
using SlimDX;

namespace SubdivisionRenderer
{
	class Camera
	{
		// World
		public Vector3 Position;
		public Vector3 Rotation;

		// View
		public Vector3 Eye { get; set; }
		public Vector3 Target { get; set; }
		public Vector3 Up { get; set; }

		// Projection
		public float Fov { get; set; }
		public float Aspect { get; set; }

		// Movement
		private const float BaseMoveStep = 3f;
		private float MoveStep { get { return _speed ? BaseMoveStep * 3f : BaseMoveStep; } }

		// Rotation
		private const float BaseRotateStep = 1f;
		private float RotateStep { get { return _speed ? BaseRotateStep * 3f : BaseRotateStep; } }
		
		// Indicators
		private bool _speed;
		private readonly bool[] _moving = new bool[10];

		// Mouse
		private int _mouseX;
		private int _mouseY;
		private bool _mousePressed;

		public Camera()
		{
			Fov = (float) Math.PI * 0.4f;
			Aspect = (float) 800 / 600;

			Reset();
		}

		public void Reset()
		{
			Position = new Vector3();
			Rotation = new Vector3();

			Eye = new Vector3(0f, 0f, -2f);
			Target = Vector3.Zero;
			Up = new Vector3(0f, 1f, 0f);

			/*Eye = new Vector3(-0.33f, 0f, -3.16f);
			Target = new Vector3(-0.33f, 0, -1.16f);
			Rotation = new Vector3(-0.65f, 0f, 0f);*/
		}

		public Matrix World()
		{
			var secPerFrame = 1f / Program.FrameRate;

			if (_moving[(int) Mode.RotatePosY])
			{
				Rotate(new Vector3(0f, RotateStep * secPerFrame, 0f));
			}
			if (_moving[(int) Mode.RotateNegY])
			{
				Rotate(new Vector3(0f, -RotateStep * secPerFrame, 0f));
			}
			if (_moving[(int) Mode.RotatePosX])
			{
				Rotate(new Vector3(RotateStep * secPerFrame, 0f, 0f));
			}
			if (_moving[(int) Mode.RotateNegX])
			{
				Rotate(new Vector3(-RotateStep * secPerFrame, 0f, 0f));
			}
			
			return Matrix.Translation(Position) * Matrix.RotationYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z);
		}

		private Matrix View()
		{
			var direction = Vector3.Normalize(Target - Eye);
			var strafe = Vector3.Normalize(Vector3.Cross(direction, Up));
			var secPerFrame = 1f / Program.FrameRate;

			if (_moving[(int) Mode.MoveForward])
			{
				Target += direction * MoveStep * secPerFrame;
				Eye += direction * MoveStep * secPerFrame;
			}
			if (_moving[(int) Mode.MoveBackward])
			{
				Target -= direction * MoveStep * secPerFrame;
				Eye -= direction * MoveStep * secPerFrame;
			}
			if (_moving[(int) Mode.MoveLeft])
			{
				Target += strafe * MoveStep * secPerFrame;
				Eye += strafe * MoveStep * secPerFrame;
			}
			if (_moving[(int) Mode.MoveRight])
			{
				Target -= strafe * MoveStep * secPerFrame;
				Eye -= strafe * MoveStep * secPerFrame;
			}
			if (_moving[(int) Mode.MoveUp])
			{
				Target += Up * MoveStep * secPerFrame;
				Eye += Up * MoveStep * secPerFrame;
			}
			if (_moving[(int) Mode.MoveDown])
			{
				Target -= Up * MoveStep * secPerFrame;
				Eye -= Up * MoveStep * secPerFrame;
			}

			return Matrix.LookAtLH(Eye, Target, Up);
		}

		public Matrix WorldViewProjection()
		{
			return World() * View() * Matrix.PerspectiveFovLH(Fov, Aspect, 0.1f, 100f);
		}

		private void Rotate(Vector3 rotation)
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

		public void HandleKeyboardStart(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.ShiftKey:
					_speed = true;
					break;
				case Controls.MoveForward:
					_moving[(int) Mode.MoveForward] = true;
					break;
				case Controls.MoveBackward:
					_moving[(int) Mode.MoveBackward] = true;
					break;
				case Controls.MoveLeft:
					_moving[(int) Mode.MoveLeft] = true;
					break;
				case Controls.MoveRight:
					_moving[(int) Mode.MoveRight] = true;
					break;
				case Controls.MoveUp:
					_moving[(int) Mode.MoveUp] = true;
					break;
				case Controls.MoveDown:
					_moving[(int) Mode.MoveDown] = true;
					break;
				case Controls.RotatePosY:
					_moving[(int) Mode.RotatePosY] = true;
					break;
				case Controls.RotateNegY:
					_moving[(int) Mode.RotateNegY] = true;
					break;
				case Controls.RotatePosX:
					_moving[(int) Mode.RotatePosX] = true;
					break;
				case Controls.RotateNegX:
					_moving[(int) Mode.RotateNegX] = true;
					break;
			}
		}

		public void HandleKeyboardEnd(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.ShiftKey:
					_speed = false;
					break;
				case Controls.MoveForward:
					_moving[(int) Mode.MoveForward] = false;
					break;
				case Controls.MoveBackward:
					_moving[(int) Mode.MoveBackward] = false;
					break;
				case Controls.MoveLeft:
					_moving[(int) Mode.MoveLeft] = false;
					break;
				case Controls.MoveRight:
					_moving[(int) Mode.MoveRight] = false;
					break;
				case Controls.MoveUp:
					_moving[(int) Mode.MoveUp] = false;
					break;
				case Controls.MoveDown:
					_moving[(int) Mode.MoveDown] = false;
					break;
				case Controls.RotatePosY:
					_moving[(int) Mode.RotatePosY] = false;
					break;
				case Controls.RotateNegY:
					_moving[(int) Mode.RotateNegY] = false;
					break;
				case Controls.RotatePosX:
					_moving[(int) Mode.RotatePosX] = false;
					break;
				case Controls.RotateNegX:
					_moving[(int) Mode.RotateNegX] = false;
					break;
			}
		}

		public void HandleMouseDown(MouseEventArgs e)
		{
			_mouseX = e.X;
			_mouseY = e.Y;
			_mousePressed = true;
		}

		public void HandleMouseUp(MouseEventArgs e)
		{
			_mousePressed = false;
		}

		public void HandleMouseMove(MouseEventArgs e)
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

		public void HandleMouseWheel(MouseEventArgs e)
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
