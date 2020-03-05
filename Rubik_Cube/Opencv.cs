
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;

namespace Rubik_Cube
{
    /// <summary>
    /// 打开摄像头并动态显示图片到图片框的业务逻辑类
    /// </summary>
    public class OpenCamera
    {

        //摄像头图像
        const int FrameWidth = 300;
        const int FrameHeight = 300;
        //摄像中心
        const int row_center = 300 / 2;
        const int col_center = 123;
        //摄像区域
        const int height_center = 80;
        const int width_center = 80;
        const int col_corcer = 60;
        //透视图像
        const int Warp_width = 100;
        const int Warp_Height = 100;

        //魔方6个面的像素数组
        Vec3b[,] Magic = new Vec3b[6, 9];
        //魔方标记数组
        string[] Mark_magic = new string[6];
        //颜色区间{R:红色;L:橙色;F:蓝色;B:绿色;U:黄色;D:白色}
        int[] Hmin = {20,30,40,50,60,70 };
        int[] Smin = {100,110,120,130,140,150 };
        int[] Vmin = { 20,20,20,20,20,20,20};
        int[] Hmax = {255,255,255,255,255,255 };
        int[] Smax = {20,20,20,20,20,20,20 };
        int[] Vmax = {255,255,255,255,255,255 };
        //6个面颜色标记
        static string faces = "RLFBUD";
        string[] Face = { "第一面","第二面","第三面","第四面","第五面","第六面"};
        static char[] order = "AECGBFDHIJKLMSNTROQP".ToCharArray();
        static char[] bithash = "TdXhQaRbEFIJUZfijeYV".ToCharArray();
        static char[] perm = "AIBJTMROCLDKSNQPEKFIMSPRGJHLNTOQAGCEMTNSBFDHORPQ".ToCharArray();
        static char[] pos = new char[20];
        static char[] ori = new char[20];
        static char[] val = new char[20];
        static char[][] tables = new char[8][];
        static int[] move = new int[20];
        static int[] moveamount = new int[20];
        static int phase = 0;
        static int[] tablesize = { 1, 4096, 6561, 4096, 256, 1536, 13824, 576 };
        static int CHAROFFSET = 65;

        //更新图片线程用的委托回调变量
        internal delegate void UpdateImageDelegate();
        internal UpdateImageDelegate UpdateImageThreadCallBack_OC;


        /// <summary>
        /// 用于获取图片框1
        /// </summary>
        internal static PictureBox PictureBox { set; get; }
        /// <summary>
        /// 用于获取或存储摄像头设备变量
        /// </summary>
        internal static VideoCapture VideoCapture_OS;

        /// <summary>
        /// 构造方法，用于传递<see cref="OpenCameraWithOpencvSharp.MainFrm.pictureBox1"/>参数
        /// </summary>
        /// <param name="pictureBox"></param>
        public OpenCamera(PictureBox pictureBox)
        {
            PictureBox = pictureBox;

        }

        
        /// <summary>
        /// 更新图片进程
        /// </summary>
        public void UpdatePictrueImage()
        {
            try
            {
                UpdateImageThreadCallBack_OC = new UpdateImageDelegate(UpdateImage);
                Thread thread = new Thread(new ThreadStart(UpdateImageThreadCallBack_OC))
                {
                    IsBackground = true
                };
                thread.Start();
                Thread.Sleep(40);

            }
            catch (Exception)
            {

                throw new Exception();
            }

        }
        /// <summary>
        /// 更新图片方法，用于<see cref="UpdateImageThreadCallBack_OC",传递方法/>
        /// </summary>
        private void UpdateImage()
        {
            while (true) { 

                VideoCapture_OS = InitVideoCapture();
                Mat mat = new Mat();
                
                if (VideoCapture_OS.Read(mat))
                {
                    Mat dst = Mask(mat);
                    dst = UP_Extent(dst);
                    Image image = (Image)dst.ToBitmap();
            
                    PictureBox.Image = image;
                }
            }

        }

        /// <summary>
        /// 上摄像头截图
        /// </summary>
        /// <returns></returns>
        public Image SaveImage()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = UP_Extent(mat);
            Image image = (Image)dst.ToBitmap();
            return image;
        }


        /// <summary>
        /// 上摄像头左边透视变换
        /// </summary>
        /// <returns></returns>
        public Image SaveWarp()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = UPLeft_WarpAffine(mat);
            dst = Well(dst);
            Image image = (Image)dst.ToBitmap();
            return image;
        }
        /// <summary>
        /// 上摄像头右边透视变换
        /// </summary>
        /// <returns></returns>
        public Image SaveWarp_UPRight()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = UPRight_WarpAffine(mat);
            dst = Well(dst);
            Image image = (Image)dst.ToBitmap();
            return image;
        }

        /// <summary>
        /// 下摄像头左边透视变换
        /// </summary>
        /// <returns></returns>
        public Image SaveWarp_DOWNLeft()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = DownLeft_WarpAffine(mat);
            dst = Well(dst);
            Image image = (Image)dst.ToBitmap();
            return image;
        }

        /// <summary>
        /// 下摄像头右边透视变换
        /// </summary>
        /// <returns></returns>
        public Image SaveWarp_DOWNRight()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = DownRight_WarpAffine(mat);
            dst = Well(dst);
            Image image = (Image)dst.ToBitmap();
            return image;
        }

        /// <summary>
        /// 转动摄像头右边透视变换
        /// </summary>
        /// <returns></returns>
        public Image SaveWarp_ChangeRight()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = ChangeRight_WarpAffine(mat);
            dst = Well(dst);
            Image image = (Image)dst.ToBitmap();
            return image;
        }

        /// <summary>
        /// 转动摄像头左边透视变换
        /// </summary>
        /// <returns></returns>
        public Image SaveWarp_ChangeLeft()
        {
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            mat = Mask(mat);
            Mat dst = ChangeLeft_WarpAffine(mat);
            dst = Well(dst);
            Image image = (Image)dst.ToBitmap();
            return image;
        }

        /// <summary>
        /// 构造棱和角的列表
        /// </summary>
        public static void List_INPUT()
        {

        }
        
        private static Mat Color_ORC(Mat src)
        {
            Mat dst = new Mat();
            //BGR2HSV   
            Cv2.CvtColor(src, dst, ColorConversionCodes.BGR2HSV);
            //设定颜色范围
            //白色区域
            Scalar scalar1 = new Scalar(22, 0, 50);
            Scalar scalar2 = new Scalar(44, 255, 255);
            //去除颜色范围外的其余颜色
            Cv2.InRange(src, scalar1, scalar2, dst);
            //去噪声
            Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            Cv2.MorphologyEx(dst, dst, MorphTypes.Open, element);

            //闭操作
            Cv2.MorphologyEx(dst, dst, MorphTypes.Close, element);
            Cv2.FindContours(dst,out OpenCvSharp.Point[][] counters, out HierarchyIndex[] x,RetrievalModes.External,ContourApproximationModes.ApproxSimple);
            OpenCvSharp.Point[] points = counters[1];
            return dst;
        }

        /// <summary>
        /// 魔方区域添加红色线框
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat UP_Extent(Mat src)
        {
            //白色
            Scalar color = new Scalar(255, 255, 255);
            //线条的宽度
            int lineThickness = 1;
            //设置多边形的顶点
            List<OpenCvSharp.Point> pts1 = new List<OpenCvSharp.Point>
            {
                new OpenCvSharp.Point(row_center, col_center+height_center), new OpenCvSharp.Point(row_center, col_center - height_center), new OpenCvSharp.Point(row_center-width_center, col_center -col_corcer), new OpenCvSharp.Point(row_center-width_center, col_center + col_corcer)
            };
            List<OpenCvSharp.Point> pts2 = new List<OpenCvSharp.Point>
            {
                new OpenCvSharp.Point(row_center, col_center + height_center), new OpenCvSharp.Point(row_center, col_center - height_center), new OpenCvSharp.Point(row_center+width_center, col_center -col_corcer), new OpenCvSharp.Point(row_center+width_center, col_center + col_corcer)
            };
            List<List<OpenCvSharp.Point>> pp = new List<List<OpenCvSharp.Point>>() { pts1,pts2 };
            
            //构造多边形
            Cv2.Polylines(src,pp, true, color, lineThickness);
            return src;
        }
        /// <summary>
        /// 抓取指定区域
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat Mask(Mat src)
        {
            //设置多边形的顶点
            List<OpenCvSharp.Point> pts1 = new List<OpenCvSharp.Point>
            {
                new OpenCvSharp.Point(row_center+width_center, col_center -col_corcer),new OpenCvSharp.Point(row_center, col_center-height_center), new OpenCvSharp.Point(row_center-width_center, col_center - col_corcer),new OpenCvSharp.Point(row_center-width_center, col_center +col_corcer), new OpenCvSharp.Point(row_center, col_center + height_center),new OpenCvSharp.Point(row_center+width_center, col_center + col_corcer)
            };
            List<List<OpenCvSharp.Point>> p1 = new List<List<OpenCvSharp.Point>>() { pts1 };

            Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
            Cv2.DrawContours(mask, p1, -1, Scalar.All(255),Cv2.FILLED);
            Mat maskimage = new Mat();
            src.CopyTo(maskimage, mask);
            return maskimage;
        }
        /// <summary>
        /// 透视图绘制井字格和添加中点像素
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Mat Well(Mat src)
        {
            //白色
            Scalar color = new Scalar(255, 0, 0);
            //2条直线
            OpenCvSharp.Point p1 = new OpenCvSharp.Point(Warp_width*1/3, 0);
            OpenCvSharp.Point p2 = new OpenCvSharp.Point(Warp_width*1/3, Warp_Height);
            OpenCvSharp.Point p3 = new OpenCvSharp.Point(Warp_width * 2 / 3, 0);
            OpenCvSharp.Point p4 = new OpenCvSharp.Point(Warp_width * 2 / 3, Warp_Height);
            //2条横线
            OpenCvSharp.Point p5 = new OpenCvSharp.Point(0, Warp_Height*1/3);
            OpenCvSharp.Point p6 = new OpenCvSharp.Point(Warp_width, Warp_Height * 1 / 3);
            OpenCvSharp.Point p7 = new OpenCvSharp.Point(0, Warp_Height * 2 / 3);
            OpenCvSharp.Point p8 = new OpenCvSharp.Point(Warp_width, Warp_Height * 2 / 3);
            Cv2.Line(src, p1, p2, color);
            Cv2.Line(src, p3, p4, color);
            Cv2.Line(src, p5, p6, color);
            Cv2.Line(src, p7, p8, color);
            return src;
        }
        /// <summary>
        /// 测试用
        /// </summary>
        /// <returns></returns>
        public string Font(int k)
        {
            string str = "";
            Mat dst = new Mat();
            VideoCapture_OS = InitVideoCapture();
            Mat mat = VideoCapture_OS.RetrieveMat();
            switch (k)
            {
                case 1:
                    //捕获指定区域
                    mat = Mask(mat);
                    //指定区域左透视变换
                    dst = UPLeft_WarpAffine(mat);
                    break;
                case 2:
                    mat = Mask(mat);
                    dst = UPRight_WarpAffine(mat);
                    break;
                case 3:
                    mat = Mask(mat);
                    dst = DownLeft_WarpAffine(mat);
                    break;
                case 4:
                    mat = Mask(mat);
                    dst = DownRight_WarpAffine(mat);
                    break;
                case 5:
                    mat = Mask(mat);
                    dst = ChangeRight_WarpAffine(mat);
                    break;
                case 6:
                    mat = Mask(mat);
                    dst = ChangeLeft_WarpAffine(mat);
                    break;

            }
            /***for (int k = 0; k < 9; k++)
            {
                int sumx = 0;
                int sumy = 0;
                int sumz = 0;
                int meanx = 0;
                int meany = 0;
                int meanz = 0;
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; i++)
                    {
                        OpenCvSharp.Point zhong = new OpenCvSharp.Point(Warp_width / 3 * (i + 1) / 10, Warp_Height / 3 * (j + 1) / 10);
                        Vec3b value = dst.Get<Vec3b>(zhong.X, zhong.Y);
                        sumx = sumx + value.Item0;
                        sumy = sumy + value.Item1;
                        sumz = sumz + value.Item2;
                    }
                }
                meanx = sumx / 81;
                meany = sumy / 81;
                meanz = sumz / 81;
                points[k] = "[" + meanx + "," + meany + "," + meanz + "]";
            }****/
            //寻找9个中点获取像素值
            OpenCvSharp.Point[] points = { new OpenCvSharp.Point(Warp_width*1/6,Warp_Height*1/6),
                                           new OpenCvSharp.Point(Warp_width*1/6,Warp_Height*3/6),
                                           new OpenCvSharp.Point(Warp_width*1/6,Warp_Height*5/6),
                                           new OpenCvSharp.Point(Warp_width*3/6,Warp_Height*1/6),
                                           new OpenCvSharp.Point(Warp_width*3/6,Warp_Height*3/6),
                                           new OpenCvSharp.Point(Warp_width*3/6,Warp_Height*5/6),
                                           new OpenCvSharp.Point(Warp_width*5/6,Warp_Height*1/6),
                                           new OpenCvSharp.Point(Warp_width*5/6,Warp_Height*3/6),
                                           new OpenCvSharp.Point(Warp_width*5/6,Warp_Height*5/6)};
            //9个点存入魔方数组
            for (int i = 0; i < points.Length; i++)
            {
                Vec3b value = dst.Get<Vec3b>(points[i].X, points[i].Y);
                Magic[k-1,i] = value;
                str = str +"("+points[i].X.ToString()+","+points[i].Y.ToString()+")"+ ":[" + value.Item0.ToString() + "," + value.Item1.ToString() + "," + value.Item2.ToString() + "]\n";
            }
            str = Face[k-1] +":\n"+ str;
            return str;
        }
        /// <summary>
        /// 上摄像头左边透视变换
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat UPLeft_WarpAffine(Mat src)
        {
            Mat dst = new Mat();
            //输入仿射点
            Point2f[] srcpoint = {new OpenCvSharp.Point(row_center-width_center,col_center-col_corcer),new OpenCvSharp.Point(row_center,col_center-height_center),
            new OpenCvSharp.Point(row_center,col_center+height_center),new OpenCvSharp.Point(row_center-width_center,col_center+col_corcer)};
            //输出仿射点
            Point2f[] dstpoint = {new OpenCvSharp.Point(0,0),new OpenCvSharp.Point(Warp_width,0),
            new OpenCvSharp.Point(Warp_width,Warp_Height),new OpenCvSharp.Point(0,Warp_Height)};
            //透视法
            Mat M = Cv2.GetPerspectiveTransform(srcpoint, dstpoint);
            //透视变换
            Cv2.WarpPerspective(src, dst, M, new OpenCvSharp.Size(Warp_width,Warp_Height));
            return dst;
        }

        /// <summary>
        /// 上摄像头右边透视变换
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat UPRight_WarpAffine(Mat src)
        {
            Mat dst = new Mat();
            //输入仿射点
            Point2f[] srcpoint = {new OpenCvSharp.Point(row_center,col_center-height_center),new OpenCvSharp.Point(row_center+width_center,col_center-col_corcer),
            new OpenCvSharp.Point(row_center+width_center,col_center+col_corcer),new OpenCvSharp.Point(row_center,col_center+height_center)};
            //输出仿射点
            Point2f[] dstpoint = {new OpenCvSharp.Point(0,0),new OpenCvSharp.Point(Warp_width,0),
            new OpenCvSharp.Point(Warp_width,Warp_Height),new OpenCvSharp.Point(0,Warp_Height)};
            //透视法
            Mat M = Cv2.GetPerspectiveTransform(srcpoint, dstpoint);
            //透视变换
            Cv2.WarpPerspective(src, dst, M, new OpenCvSharp.Size(Warp_width, Warp_Height));
            
            return dst;
        }
        /// <summary>
        /// 下摄像头左边透视变换
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat DownLeft_WarpAffine(Mat src)
        {
            Mat dst = new Mat();
            //输入仿射点
            Point2f[] srcpoint = {new OpenCvSharp.Point(row_center,col_center-height_center),new OpenCvSharp.Point(row_center+width_center,col_center-col_corcer),
            new OpenCvSharp.Point(row_center+width_center,col_center+col_corcer),new OpenCvSharp.Point(row_center,col_center+height_center)};
            //输出仿射点
            Point2f[] dstpoint = {new OpenCvSharp.Point(0,0),new OpenCvSharp.Point(Warp_width,0),
            new OpenCvSharp.Point(Warp_width,Warp_Height),new OpenCvSharp.Point(0,Warp_Height)};
            //透视法
            Mat M = Cv2.GetPerspectiveTransform(srcpoint, dstpoint);
            //透视变换
            Cv2.WarpPerspective(src, dst, M, new OpenCvSharp.Size(Warp_width, Warp_Height));
            Cv2.Flip(dst, dst, FlipMode.X);
            Cv2.Flip(dst, dst, FlipMode.Y);
            return dst;
        }
        /// <summary>
        /// 下摄像头右边透视变换
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat DownRight_WarpAffine(Mat src)
        {
            Mat dst = new Mat();
            //输入仿射点
            Point2f[] srcpoint = {new OpenCvSharp.Point(row_center-width_center,col_center-col_corcer),new OpenCvSharp.Point(row_center,col_center-height_center),
            new OpenCvSharp.Point(row_center,col_center+height_center),new OpenCvSharp.Point(row_center-width_center,col_center+col_corcer)};
            //输出仿射点
            Point2f[] dstpoint = {new OpenCvSharp.Point(0,0),new OpenCvSharp.Point(Warp_width,0),
            new OpenCvSharp.Point(Warp_width,Warp_Height),new OpenCvSharp.Point(0,Warp_Height)};
            //透视法
            Mat M = Cv2.GetPerspectiveTransform(srcpoint, dstpoint);
            //透视变换
            Cv2.WarpPerspective(src, dst, M, new OpenCvSharp.Size(Warp_width, Warp_Height));
            Cv2.Flip(dst, dst, FlipMode.X);
            Cv2.Flip(dst, dst, FlipMode.Y);
            return dst;
        }

        /// <summary>
        /// 右机械臂转动上摄像头右边透视变换
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat ChangeRight_WarpAffine(Mat src)
        {
            Mat dst = new Mat();
            //输入仿射点
            Point2f[] srcpoint = {new OpenCvSharp.Point(row_center-width_center,col_center-col_corcer),new OpenCvSharp.Point(row_center,col_center-height_center),
            new OpenCvSharp.Point(row_center,col_center+height_center),new OpenCvSharp.Point(row_center-width_center,col_center+col_corcer)};
            //输出仿射点
            Point2f[] dstpoint = {new OpenCvSharp.Point(0,0),new OpenCvSharp.Point(Warp_width,0),
            new OpenCvSharp.Point(Warp_width,Warp_Height),new OpenCvSharp.Point(0,Warp_Height)};
            //透视法
            Mat M = Cv2.GetPerspectiveTransform(srcpoint, dstpoint);
            //透视变换
            Cv2.WarpPerspective(src, dst, M, new OpenCvSharp.Size(Warp_width, Warp_Height));
            Cv2.Transpose(dst, dst);
            Cv2.Flip(dst, dst, 0);
            return dst;
        }

        /// <summary>
        /// 左机械臂转动下摄像头左边透视变换
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Mat ChangeLeft_WarpAffine(Mat src)
        {
            Mat dst = new Mat();
            //输入仿射点
            Point2f[] srcpoint = {new OpenCvSharp.Point(row_center,col_center-height_center),new OpenCvSharp.Point(row_center+width_center,col_center-col_corcer),
            new OpenCvSharp.Point(row_center+width_center,col_center+col_corcer),new OpenCvSharp.Point(row_center,col_center+height_center)};
            //输出仿射点
            Point2f[] dstpoint = {new OpenCvSharp.Point(0,0),new OpenCvSharp.Point(Warp_width,0),
            new OpenCvSharp.Point(Warp_width,Warp_Height),new OpenCvSharp.Point(0,Warp_Height)};
            //透视法
            Mat M = Cv2.GetPerspectiveTransform(srcpoint, dstpoint);
            //透视变换
            Cv2.WarpPerspective(src, dst, M, new OpenCvSharp.Size(Warp_width, Warp_Height));
            Cv2.Transpose(dst, dst);
            Cv2.Flip(dst, dst,FlipMode.Y);
            return dst;
        }


        /// <summary>
        /// 颜色区域标记6个面颜色数组
        /// </summary>
        /// <param name="Magic"></param>
        /// <returns></returns>
        private string[] Mark_magics(Vec3b[,] Magic)
        {
            //循环6个面
            for (int k = 0; k < 6; k++)
            {
                //循环9个格子
                for (int i = 0; i < 9; i++)
                {
                    //循环6种颜色
                    for (int j = 0; j < 6; j++)
                    {
                        //满足颜色区间
                        if (Magic[k, i].Item0 > Hmin[j]&& Magic[k, i].Item0 < Hmax[j] && Magic[k, i].Item1 > Smin[j]&& Magic[k, i].Item1 < Smax[j]&& Magic[k, i].Item2 > Vmin[j]&& Magic[k, i].Item2 < Vmax[j])
                        {
                            //添加颜色标记
                            Mark_magic[k] = Mark_magic[k]+faces.Substring(j,1);

                        }
                    }
                }
            }
            return Mark_magic;
        }

        //颜色区间{R:红色;L:粉红色;F:蓝色;B:绿色;U:黄色;D:白色}
        public string Test()
        {                        
            string str = "";
            string[] strs = { "UUBUUBUUB", "RRRRRRRRR", "FDDFDDFDD", "LLLLLLLLL", "DDDBBBBBB", "FFFFFFUUU" };
            //string Input = "RU LF UB DR DL BL UL FU BD RF BR FD LDF LBD FUL RFD UFR RDB UBL RBU";
            string Output = GetResult(Input(strs));
            return Output;
        }
        /// <summary>
        /// 按位置解析魔方数组
        /// </summary>
        /// <param name="Mark_magic"></param>
        /// <returns></returns>
        private string Input(string[] Mark_magic)
        {

            //12条棱和8个角[UF UR UB UL DF DR DB DL FR FL BR BL UFR URB UBL ULF DRF DFL DLB DBR]
            string argv =     Mark_magic[0].Substring(7,1)+Mark_magic[5].Substring(5,1)+" "+//UF
                              Mark_magic[0].Substring(5,1)+Mark_magic[1].Substring(3,1)+" "+//UR
                              Mark_magic[0].Substring(1,1)+Mark_magic[4].Substring(5,1)+" "+//UB
                              Mark_magic[0].Substring(3,1)+Mark_magic[3].Substring(5,1)+" "+//UL
                              Mark_magic[2].Substring(7,1)+Mark_magic[5].Substring(3,1)+" "+//DF
                              Mark_magic[2].Substring(3,1)+Mark_magic[1].Substring(5,1)+" "+//DR
                              Mark_magic[2].Substring(1,1)+Mark_magic[4].Substring(3,1)+" "+//DB
                              Mark_magic[2].Substring(5,1)+Mark_magic[3].Substring(3,1)+" "+//DL
                              Mark_magic[5].Substring(7,1)+Mark_magic[1].Substring(7,1)+" "+//FR
                              Mark_magic[5].Substring(1,1)+Mark_magic[3].Substring(7,1)+" "+//FL
                              Mark_magic[4].Substring(1,1)+Mark_magic[1].Substring(1,1)+" "+//BR
                              Mark_magic[4].Substring(7,1)+Mark_magic[3].Substring(1,1)+" "+//BL

                              Mark_magic[0].Substring(8,1)+Mark_magic[5].Substring(8,1)+Mark_magic[1].Substring(6,1)+" "+//UFR
                              Mark_magic[0].Substring(2,1)+Mark_magic[1].Substring(0,1)+Mark_magic[4].Substring(2,1)+" "+//URB
                              Mark_magic[0].Substring(0,1)+Mark_magic[4].Substring(8,1)+Mark_magic[3].Substring(2,1)+" "+//UBL
                              Mark_magic[0].Substring(6,1)+Mark_magic[3].Substring(8,1)+Mark_magic[5].Substring(2,1)+" "+//ULF
                              Mark_magic[2].Substring(6,1)+Mark_magic[1].Substring(8,1)+Mark_magic[5].Substring(6,1)+" "+//DRF
                              Mark_magic[2].Substring(8,1)+Mark_magic[5].Substring(0,1)+Mark_magic[3].Substring(6,1)+" "+//DFL
                              Mark_magic[2].Substring(2,1)+Mark_magic[3].Substring(0,1)+Mark_magic[4].Substring(6,1)+" "+//DLB
                              Mark_magic[2].Substring(0,1)+Mark_magic[4].Substring(0,1)+Mark_magic[1].Substring(2,1);    //DBR
            return argv;
        }


        /// <summary>
        /// 初始化摄像头并设置摄像头宽高参数
        /// </summary>
        /// <returns>摄像头调用对象</returns>
        private static VideoCapture InitVideoCapture()
        {
            if (VideoCapture_OS == null)
            {
                VideoCapture_OS = OpenCvSharp.VideoCapture.FromCamera(CaptureDevice.Any);
                VideoCapture_OS.Set(CaptureProperty.FrameWidth, FrameWidth);
                VideoCapture_OS.Set(CaptureProperty.FrameHeight, FrameHeight);
            }

            return VideoCapture_OS;
        }







        static private int Char2Num(char c)
        {
            return (int)c - CHAROFFSET;
        }

        static private void cycle(char[] p, char[] a, int offset)
        {
            char temp = p[Char2Num(a[0 + offset])];
            p[Char2Num(a[0 + offset])] = p[Char2Num(a[1 + offset])];
            p[Char2Num(a[1 + offset])] = temp;
            temp = p[Char2Num(a[0 + offset])];
            p[Char2Num(a[0 + offset])] = p[Char2Num(a[2 + offset])];
            p[Char2Num(a[2 + offset])] = temp;
            temp = p[Char2Num(a[0 + offset])];
            p[Char2Num(a[0 + offset])] = p[Char2Num(a[3 + offset])];
            p[Char2Num(a[3 + offset])] = temp;
        }

        static private void twist(int i, int a)
        {
            i -= CHAROFFSET;
            ori[i] = Convert.ToChar(((int)ori[i] + a + 1) % val[i]);
        }

        static private void reset()
        {
            for (int i = 0; i < 20; pos[i] = Convert.ToChar(i), ori[i++] = '\0') ;
        }

        static private int permtonum(char[] p, int offset)
        {
            int n = 0;
            for (int a = 0; a < 4; a++)
            {
                n *= 4 - a;
                for (int b = a; ++b < 4;)
                    if (p[b + offset] < p[a + offset]) n++;
            }
            return n;
        }

        static private void numtoperm(char[] p, int n, int o)
        {
            //p += o;
            p[3 + o] = Convert.ToChar(o);
            for (int a = 3; a-- > 0;)
            {
                p[a + o] = Convert.ToChar(n % (4 - a) + o);
                n /= 4 - a;
                for (int b = a; ++b < 4;)
                    if (p[b + o] >= p[a + o]) p[b + o]++;
            }
        }

        static private int getposition(int t)
        {
            int i = -1, n = 0;
            switch (t)
            {
                case 1:
                    for (; ++i < 12;) n += ((int)ori[i]) << i;
                    break;
                case 2:
                    for (i = 20; --i > 11;) n = n * 3 + (int)ori[i];
                    break;
                case 3:
                    for (; ++i < 12;) n += ((((int)pos[i]) & 8) > 0) ? (1 << i) : 0;
                    break;
                case 4:
                    for (; ++i < 8;) n += ((((int)pos[i]) & 4) > 0) ? (1 << i) : 0;
                    break;
                case 5:
                    int[] corn = new int[8];
                    int[] corn2 = new int[4];
                    int j, k, l;
                    k = j = 0;
                    for (; ++i < 8;)
                        if (((l = pos[i + 12] - 12) & 4) > 0)
                        {
                            corn[l] = k++;
                            n += 1 << i;
                        }
                        else corn[j++] = l;
                    for (i = 0; i < 4; i++) corn2[i] = corn[4 + corn[i]];
                    for (; --i > 0;) corn2[i] ^= corn2[0];

                    n = n * 6 + corn2[1] * 2 - 2;
                    if (corn2[3] < corn2[2]) n++;
                    break;
                case 6:
                    n = permtonum(pos, 0) * 576 + permtonum(pos, 4) * 24 + permtonum(pos, 12);
                    break;
                case 7:
                    n = permtonum(pos, 8) * 24 + permtonum(pos, 16);
                    break;

            }
            return n;
        }

        static private void setposition(int t, int n)
        {
            int i = 0, j = 12, k = 0;
            char[] corn = "QRSTQRTSQSRTQTRSQSTRQTSR".ToCharArray();
            reset();
            switch (t)
            {
                // case 0 does nothing so leaves cube solved
                case 1://edgeflip
                    for (; i < 12; i++, n >>= 1) ori[i] = Convert.ToChar(n & 1);
                    break;
                case 2://cornertwist
                    for (i = 12; i < 20; i++, n /= 3) ori[i] = Convert.ToChar(n % 3);
                    break;
                case 3://middle edge choice
                    for (; i < 12; i++, n >>= 1) pos[i] = Convert.ToChar(8 * n & 8);
                    break;
                case 4://ud slice choice
                    for (; i < 8; i++, n >>= 1) pos[i] = Convert.ToChar(4 * n & 4);
                    break;
                case 5://tetrad choice,parity,twist
                    int offset = n % 6 * 4;
                    n /= 6;
                    for (; i < 8; i++, n >>= 1)
                        pos[i + 12] = Convert.ToChar(((n & 1) > 0) ? corn[offset + k++] - CHAROFFSET : j++);
                    break;
                case 6://slice permutations
                    numtoperm(pos, n % 24, 12); n /= 24;
                    numtoperm(pos, n % 24, 4); n /= 24;
                    numtoperm(pos, n, 0);
                    break;
                case 7://corner permutations
                    numtoperm(pos, n / 24, 8);
                    numtoperm(pos, n % 24, 16);
                    break;
            }
        }

        static private void domove(int m)
        {
            //char* p = perm + 8 * m;
            int offset = 8 * m;
            int i = 8;
            //cycle the edges
            cycle(pos, perm, offset);
            cycle(ori, perm, offset);
            //cycle the corners
            cycle(pos, perm, offset + 4);
            cycle(ori, perm, offset + 4);
            //twist corners if RLFB
            if (m < 4)
                for (; --i > 3;) twist(perm[i + offset], i & 1);
            //flip edges if FB
            if (m < 2)
                for (i = 4; i-- > 0;) twist(perm[i + offset], 0);
        }

        static private void filltable(int ti)
        {
            int n = 1, l = 1, tl = tablesize[ti];
            char[] tb = new char[tl];
            tables[ti] = tb;
            for (int i = 0; i < tb.Length; i++) tb[i] = '\0';

            reset();
            tb[getposition(ti)] = Convert.ToChar(1);

            // while there are positions of depth l
            while (n > 0)
            {
                n = 0;
                // find each position of depth l
                for (int i = 0; i < tl; i++)
                {
                    if (tb[i] == l)
                    {
                        //construct that cube position
                        setposition(ti, i);
                        // try each face any amount
                        for (int f = 0; f < 6; f++)
                        {
                            for (int q = 1; q < 4; q++)
                            {
                                domove(f);
                                // get resulting position
                                int r = getposition(ti);
                                // if move as allowed in that phase, and position is a new one
                                if ((q == 2 || f >= (ti & 6)) && tb[r] == '\0')
                                {
                                    // mark that position as depth l+1
                                    tb[r] = Convert.ToChar(l + 1);
                                    n++;
                                }
                            }
                            domove(f);
                        }
                    }
                }
                l++;
            }
        }

        static private bool searchphase(int movesleft, int movesdone, int lastmove)
        {
            if (tables[phase][getposition(phase)] - 1 > movesleft ||
                tables[phase + 1][getposition(phase + 1)] - 1 > movesleft) return false;

            if (movesleft == 0) return true;

            for (int i = 6; i-- > 0;)
            {
                if ((i - lastmove != 0) && ((i - lastmove + 1) != 0 || ((i | 1) != 0)))
                {
                    move[movesdone] = i;
                    for (int j = 0; ++j < 4;)
                    {
                        domove(i);
                        moveamount[movesdone] = j;
                        if ((j == 2 || i >= phase) &&
                            searchphase(movesleft - 1, movesdone + 1, i)) return true;
                    }
                    domove(i);
                }
            }
            return false;
        }

        public string GetResult(string sInput)
        {
            phase = 0;

            string[] argv = sInput.Split(' ');
            string sOutput = "";

            if (argv.Length != 20)
            {
                return "error";
            }

            int f, i = 0, j = 0, k = 0, pc, mor;

            for (; k < 20; k++) val[k] = Convert.ToChar(k < 12 ? 2 : 3);
            for (; j < 8; j++) filltable(j);

            for (; i < 20; i++)
            {
                f = pc = k = mor = 0;
                for (; f < val[i]; f++)
                {
                    j = faces.IndexOf(argv[i][f]);
                    if (j > k) { k = j; mor = f; }
                    pc += 1 << j;
                }
                for (f = 0; f < 20; f++)
                    if (pc == bithash[f] - 64) break;

                pos[order[i] - CHAROFFSET] = Convert.ToChar(f);
                ori[order[i] - CHAROFFSET] = Convert.ToChar(mor % val[i]);
            }
            for (; phase < 8; phase += 2)
            {
                for (j = 0; !searchphase(j, 0, 9); j++) ;
                for (i = 0; i < j; i++)
                {
                    sOutput += "FBRLUD"[move[i]] + "" + moveamount[i].ToString();
                    sOutput += " ";
                }
            }

            return sOutput;
        }
    }


    /// <summary>
    ///时间管理类
    /// </summary>
    public class TimeManager
    {
        /// <summary>
        /// 更新时间线程方法的委托
        /// </summary>
        private delegate void SetTimeThreadDelegate();
        /// <summary>
        /// 更新时间线程的回调方法
        /// </summary>
        private SetTimeThreadDelegate SetTimeThreadCallBack;

        private delegate void UpdateTimeDelegate(string text);
        private UpdateTimeDelegate UpdateTimeCallBack;

        /// <summary>
        /// 布尔变量用于更新时间线程方法
        /// </summary>
        internal bool TimeBegin = true;

        /// <summary>
        /// 用于获取时间标签
        /// </summary>
        public Label LabelTime { get; private set; }

        public TimeManager(Label label)
        {
            LabelTime = label;
        }
        public TimeManager()
        {

        }
        /// <summary>
        /// 用于窗体加载时间中的方法
        /// </summary>
        internal void TimeWorkForFromLoad()
        {
            //声明更新时间线程回调
            SetTimeThreadCallBack = new SetTimeThreadDelegate(SetTimeForCallBack);
            //
            UpdateTimeCallBack = new UpdateTimeDelegate(UpdateTime);
            //更新时间线程
            Thread thread = new Thread(new ThreadStart(SetTimeThreadCallBack))
            {
                IsBackground = true
            };
            thread.Start();

            Thread.Sleep(40);
        }
        /// <summary>
        /// 更新时间回调方法
        /// </summary>
        private void SetTimeForCallBack()
        {

            while (TimeBegin)
            {
                if (LabelTime.InvokeRequired)
                {
                    LabelTime.Invoke(UpdateTimeCallBack, new object[] { DateTime.Now.ToString() });

                }

            }

        }
        /// <summary>
        /// 更新时间方法
        /// </summary>
        /// <param name="time"></param>
        private void UpdateTime(string time)
        {
            LabelTime.Text = time;
        }

        private SerialPort port = null;
        

    }


}
