using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Lzp.Form
{
    /// <summary>
    /// 透明遮罩层
    /// </summary>
    [ToolboxBitmap(typeof(MaskLayer))]
    class MaskLayer : Control
    {
        private bool _transparentBG = true;//是否使用透明遮罩,默认为True
        private byte _alpha = 125;//遮罩透明度,0~255

        //private Container components = new Container();
        /// <summary>
        /// 遮罩层默认构造函数, 显示Loading图片
        /// </summary>
        public MaskLayer() : this(125, true) { }

        /// <summary>
        /// 遮罩层构造函数
        /// </summary>
        /// <param name="Alpha">遮罩透明度</param>
        /// <param name="showLoadingImage">是否显示Loading图片</param>
        public MaskLayer(byte Alpha, bool showLoadingImage)
        {
            Dock = DockStyle.Fill;
            SetStyle(ControlStyles.Opaque, true);
            base.CreateControl();
            this._alpha = Alpha;

            if (showLoadingImage)
            {
                PictureBox pictureBox_Loading = new PictureBox();
                pictureBox_Loading.BackColor = Color.White;
                pictureBox_Loading.Image = Properties.Resources.loading;
                pictureBox_Loading.Name = "pictureBox_Loading";
                pictureBox_Loading.Size = new Size(48, 48);
                pictureBox_Loading.SizeMode = PictureBoxSizeMode.AutoSize;
                Point Location = new Point(this.Location.X + (this.Width - pictureBox_Loading.Width) / 2, this.Location.Y + (this.Height - pictureBox_Loading.Height) / 2);
                pictureBox_Loading.Location = Location;
                pictureBox_Loading.Anchor = AnchorStyles.None;
                this.Controls.Add(pictureBox_Loading);
            }
        }

        /// <summary>
        /// 显示遮罩层
        /// </summary>
        public new void Show()
        {
            BringToFront();
            Enabled = true;
            Visible = true;
        }
        /// <summary>
        /// 隐藏遮罩层
        /// </summary>
        public new void Hide()
        {
            Visible = false;
            Enabled = false;
        }

        /// <summary>
        /// 自定义绘制窗体
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            float vlblControlWidth;
            float vlblControlHeight;

            Pen labelBorderPen;
            SolidBrush labelBackColorBrush;

            if (_transparentBG)
            {
                Color drawColor = Color.FromArgb(this._alpha, this.BackColor);
                labelBorderPen = new Pen(drawColor, 0);
                labelBackColorBrush = new SolidBrush(drawColor);
            }
            else
            {
                labelBorderPen = new Pen(this.BackColor, 0);
                labelBackColorBrush = new SolidBrush(this.BackColor);
            }
            base.OnPaint(e);
            vlblControlWidth = this.Size.Width;
            vlblControlHeight = this.Size.Height;
            e.Graphics.DrawRectangle(labelBorderPen, 0, 0, vlblControlWidth, vlblControlHeight);
            e.Graphics.FillRectangle(labelBackColorBrush, 0, 0, vlblControlWidth, vlblControlHeight);

        }
        /// <summary>
        /// 覆盖 创建参数
        /// </summary>
        protected override CreateParams CreateParams//v1.10 
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20;  // 开启 WS_EX_TRANSPARENT,使控件支持透明
                return cp;
            }
        }

        [Category("MaskLayer"), Description("是否使用透明遮罩,默认为True")]
        public bool TransparentBG
        {
            get { return _transparentBG; }
            set
            {
                _transparentBG = value;
                this.Invalidate();
            }
        }

        [Category("MaskLayer"), Description("设置遮罩透明度,0~255")]
        public byte Alpha
        {
            get { return _alpha; }
            set
            {
                _alpha = value;
                if(_transparentBG)
                    this.Invalidate();
            }
        }
    }
}
