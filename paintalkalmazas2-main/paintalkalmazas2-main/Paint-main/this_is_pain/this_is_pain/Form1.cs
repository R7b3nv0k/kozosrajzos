using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace this_is_pain
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Width = 1920;
            this.Height = 1080;
            bm = new Bitmap(pic.Width, pic.Height);
            g = Graphics.FromImage(bm);
            g.Clear(Color.White);
            pic.Image = bm;
        }
        Bitmap bm;
        Graphics g;
        bool paint = false;
        Point px, py;
        Pen p = new Pen(Color.Black, 1);
        Pen erase = new Pen(Color.White,10);
        int index;
        int x, y, sX, sY, cX, cY;

        ColorDialog cd = new ColorDialog();
        Color new_color;
        // Új mezők a Form1 osztályban
        Stack<Bitmap> undoStack = new Stack<Bitmap>();
        Stack<Bitmap> redoStack = new Stack<Bitmap>();

        // Állapot mentése rajzolás előtt
        private void SaveState()
        {
            undoStack.Push((Bitmap)bm.Clone());
            redoStack.Clear(); // Új műveletnél törlődik a redo stack
        }

        // Visszalépés
        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push((Bitmap)bm.Clone()); // Aktuális állapot mentése a redo stack-be
                bm = undoStack.Pop(); // Visszatérés az előző állapothoz
                g = Graphics.FromImage(bm);
                pic.Image = bm;
                pic.Refresh();
            }
        }

        // Előrelépés
        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push((Bitmap)bm.Clone()); // Aktuális állapot mentése az undo stack-be
                bm = redoStack.Pop(); // Következő állapot betöltése
                g = Graphics.FromImage(bm);
                pic.Image = bm;
                pic.Refresh();
            }
        }

        private void pic_MouseDown(object sender, MouseEventArgs e)
        {
            paint = true;
            py = e.Location;

            cX = e.X;
            cY = e.Y;
            SaveState(); // Állapot mentése


        }

        private void pic_MouseMove(object sender, MouseEventArgs e)
        {
            if (paint)
            {

                if (index == 1)
                {
                    px = e.Location;
                    g.DrawLine(p,px,py);
                    py = px; 
                }
                if (index == 2)
                {
                    px = e.Location;
                    g.DrawLine(erase, px, py);
                    py = px;
                }
            }
            pic.Refresh();
            x = e.X;
            y = e.Y;

            sX = e.X - cX;
            sY = e.Y - cY;
        }

        private void pic_MouseUp(object sender, MouseEventArgs e)
        {
            paint = false;

            sX = x - cX;
            sY = y - cY;

            int rectX = Math.Min(cX, x);
            int rectY = Math.Min(cY, y);
            int rectWidth = Math.Abs(sX);
            int rectHeight = Math.Abs(sY);

            if (index == 3)
            {
                g.DrawEllipse(p, rectX, rectY, rectWidth, rectHeight);
            }
            if (index == 4)
            {
                g.DrawRectangle(p, rectX, rectY, rectWidth, rectHeight);
            }
            if (index == 5)
            {
                g.DrawLine(p, cX, cY, x, y);
            }

            pic.Refresh();  // Biztosítja a frissítést a rajzolás után
        }

        private void pic_Paint(object sender, PaintEventArgs e)
        {
            if (paint)
            {
                Graphics g = e.Graphics;

                int rectX = Math.Min(cX, x);
                int rectY = Math.Min(cY, y);
                int rectWidth = Math.Abs(sX);
                int rectHeight = Math.Abs(sY);

                if (index == 3)
                {
                    g.DrawEllipse(p, rectX, rectY, rectWidth, rectHeight);
                }
                if (index == 4)
                {
                    g.DrawRectangle(p, rectX, rectY, rectWidth, rectHeight);
                }
                if (index == 5)
                {
                    g.DrawLine(p, cX, cY, x, y);
                }
            }
        }

        private void btn_clear_Click(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            pic.Image = bm;
            index = 0;
        }

        private void btn_color_Click(object sender, EventArgs e)
        {
            cd.ShowDialog();
            new_color = cd.Color;
            pic_color.BackColor = cd.Color;
            p.Color = cd.Color;
        }
        static Point set_point(PictureBox pb , Point pt)
        {
            float pX= 1f * pb.Image.Width / pb.Width;
            float pY = 1f * pb.Image.Height / pb.Height;
            return new Point((int)(pt.X * pX), (int)(pt.Y * pY));
        }

        private void color_picker_MouseClick(object sender, MouseEventArgs e)
        {
            Point point = set_point(color_picker, e.Location);
            pic_color.BackColor = ((Bitmap)color_picker.Image).GetPixel(point.X, point.Y);
            new_color = pic_color.BackColor;
            p.Color = pic_color.BackColor;
        }

        private void validate(Bitmap bm, Stack<Point> sp, int x, int y, Color old_color, Color new_color)
        {
            Color cx = bm.GetPixel(x, y);
            if (cx == old_color)
            {
                sp.Push(new Point(x, y));
                bm.SetPixel(x, y,new_color);
            }
        }
        public void Fill(Bitmap bmp, int x, int y, Color newColor)
        {
            Color oldColor = bmp.GetPixel(x, y);
            if (oldColor.ToArgb() == newColor.ToArgb()) return;

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int bytesPerPixel = 4;
            int stride = bmpData.Stride;
            IntPtr ptr = bmpData.Scan0;
            int totalBytes = Math.Abs(stride) * bmp.Height;
            byte[] pixels = new byte[totalBytes];

            // Képadatok másolása tömbbe
            Marshal.Copy(ptr, pixels, 0, totalBytes);

            int targetArgb = oldColor.ToArgb();
            int newArgb = newColor.ToArgb();

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(x, y));

            while (queue.Count > 0)
            {
                Point pt = queue.Dequeue();
                int px = pt.X;
                int py = pt.Y;

                int index = (py * stride) + (px * bytesPerPixel);
                int currentArgb = BitConverter.ToInt32(pixels, index);

                if (currentArgb != targetArgb) continue;

                // Szín beállítása
                pixels[index] = newColor.B;
                pixels[index + 1] = newColor.G;
                pixels[index + 2] = newColor.R;
                pixels[index + 3] = newColor.A;

                // Szomszédos pixelek vizsgálata (gyors, szélességi keresés)
                if (px > 0) queue.Enqueue(new Point(px - 1, py));
                if (px < bmp.Width - 1) queue.Enqueue(new Point(px + 1, py));
                if (py > 0) queue.Enqueue(new Point(px, py - 1));
                if (py < bmp.Height - 1) queue.Enqueue(new Point(px, py + 1));
            }

            // Frissített képadatok másolása vissza
            Marshal.Copy(pixels, 0, ptr, totalBytes);
            bmp.UnlockBits(bmpData);
        }


        private void pic_MouseClick(object sender, MouseEventArgs e)
        {
            if (index == 7)
            {
                Point point = set_point(pic,e.Location);
                Fill(bm, point.X, point.Y, new_color);
            }
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Image(.jpg)|*.jpg|(*.*|*.*";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Bitmap btm = bm.Clone(new Rectangle(0, 0, pic.Width, pic.Height),bm.PixelFormat);
                btm.Save(sfd.FileName, ImageFormat.Jpeg);
            }
        }
        private void ResetButtonColors()
        {
            btn_pencil.BackColor = SystemColors.Control;
            btn_eraser.BackColor = SystemColors.Control;
            btn_fill.BackColor = SystemColors.Control;
            btn_ellipse.BackColor = SystemColors.Control;
            btn_rect.BackColor = SystemColors.Control;
            btn_line.BackColor = SystemColors.Control;
        }


        private void btn_ellipse_Click(object sender, EventArgs e)
        {
            ResetButtonColors();
            btn_ellipse.BackColor = Color.Brown;
            index = 3;
        }

        private void btn_pencil_Click(object sender, EventArgs e)
        {
            ResetButtonColors();
            btn_pencil.BackColor = Color.Brown;
            index = 1;
        }


        private void panel1_Paint(object sender, PaintEventArgs e)
		{

		}

		private void panel2_Paint(object sender, PaintEventArgs e)
		{

		}

		private void pic_color_Click(object sender, EventArgs e)
		{

		}

		private void color_picker_Click(object sender, EventArgs e)
		{

		}

		private void pic_Click(object sender, EventArgs e)
		{

		}

        private void btn_undo_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void btn_redo_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void btn_eraser_Click(object sender, EventArgs e)
        {
            ResetButtonColors();
            btn_eraser.BackColor = Color.Brown;
            index = 2;
        }
        private void btn_fill_Click(object sender, EventArgs e)
        {
            ResetButtonColors();
            btn_fill.BackColor = Color.Brown;
            index = 7;
        }

        private void btn_rect_Click(object sender, EventArgs e)
        {
            ResetButtonColors();
            btn_rect.BackColor = Color.Brown;
            index = 4;
        }

        private void btn_line_Click(object sender, EventArgs e)
        {
            ResetButtonColors();
            btn_line.BackColor = Color.Brown;
            index = 5;
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ellenőrizzük, hogy a felhasználó mentette-e a munkát
            if (!IsSaved()) // Itt egy IsSaved() metódust kell készíteni, amely eldönti, hogy a munka mentve lett-e.
            {
                var result = MessageBox.Show("Biztosan mentés nélkül be akarod zárni a munkádat?", "Kilépés", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // Kilépés mentés nélkül
                    e.Cancel = false;
                }
                else if (result == DialogResult.No)
                {
                    // Mentés
                    btn_save_Click(this, EventArgs.Empty); // Ez menti a munkát a már meglévő mentés funkcióval
                    e.Cancel = false; // Most már kiléphet
                }
                else
                {
                    // Mégse
                    e.Cancel = true;
                }
            }
            else
            {
                // Ha a munka mentve van, akkor kiléphet
                e.Cancel = false;
            }
        }





        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }
        private bool IsSaved()
        {

            return false; 
        }

    }
}
