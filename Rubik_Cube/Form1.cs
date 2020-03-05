using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rubik_Cube
{
    public partial class Form1 : Form
    {

        private OpenCamera OpenCamera;
        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            this.cmbSerials.Items.AddRange(SerialPort.GetPortNames());
            this.cmbSerials.SelectedIndex = this.cmbSerials.Items.Count - 1;//Arduino一般在最后一个串口

            //打开视频
            OpenCamera = new OpenCamera(pictureBox1);
            //动态加载图片到 pictureBox1
            OpenCamera.UpdatePictrueImage();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            OpenCamera = new OpenCamera(pictureBox1);
            //上摄像头截图到pictureBox2
            pictureBox2.Image = OpenCamera.SaveImage();
            //上摄像头左边透视变换到pictureBox3
            pictureBox3.Image = OpenCamera.SaveWarp();
            MessageBox.Show(OpenCamera.Font(1));
            //上摄像头右边透视变换到pictureBox4
            pictureBox4.Image = OpenCamera.SaveWarp_UPRight();
            MessageBox.Show(OpenCamera.Font(2));
            //棱和点构成列表
            OpenCamera.List_INPUT();
            //魔方计算步长
            //OpenCamera.List_OUPUT();
            //发送步长给arduino
            //OpenCamera.Send_COM();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox5.Image = OpenCamera.SaveWarp_DOWNLeft();
            MessageBox.Show(OpenCamera.Font(3));
            pictureBox6.Image = OpenCamera.SaveWarp_DOWNRight();
            MessageBox.Show(OpenCamera.Font(4));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pictureBox7.Image = OpenCamera.SaveWarp_ChangeRight();
            MessageBox.Show(OpenCamera.Font(5));
            MessageBox.Show(OpenCamera.Test());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //pictureBox8.Image = OpenCamera.SaveWarp_ChangeLeft();
            //MessageBox.Show(OpenCamera.Font(6));
            InitialSerialPort();
        }


        private SerialPort port = null;
        /// <summary>
        /// 串口写入数据
        /// </summary>
        private void InitialSerialPort()
        {
            try
            {
                string str = "s"; 
                //选取串口号
                string portName = this.cmbSerials.SelectedItem.ToString();
                //实例化串口
                port = new SerialPort(portName, 9600);
                port.Encoding = Encoding.ASCII;
                port.Open();
                port.Write(str);
                port.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化串口发生错误：" + ex.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

       
    }
}
