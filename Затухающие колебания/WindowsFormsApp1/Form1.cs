using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using NOpenCL;
namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // Device device = 0;
        private const int N = 1000;
        private const int THREAD_COUNT = 1000;
        private const int X0 = -1000;
        private const int MAX_TIME_SEC = 60 * 2;
        #region OCL
 private Platform platform;
        private Device device;
        private Context context;
        private CommandQueue commandQueue;
        private NOpenCL.Program clprogram;
        private NOpenCL.Buffer oBuffer;
        private readonly string cl_kernel = @"#define V 0.001

__kernel void kernels( __global double *oBuffer,int TIME){
int i=get_global_id(0);
while(i<" + N.ToString()+@"){
int t=(i/"+N.ToString()+@")+("+N.ToString()+@"*TIME);
double xt="+X0.ToString()+@"+(t*V);
double yt=sin(pow(M_E,-xt));
oBuffer[i]=yt;
i+="+THREAD_COUNT.ToString()+@";
}
}";
        #endregion
        private int TIME = 0;
        private double[] Y = new double[N];
        public Form1()
        {
            InitializeComponent();
            #region NOpenCL_Init
            platform = Platform.GetPlatforms()[0];
            device = platform.GetDevices()[0];
            context = Context.Create(device);
            commandQueue = context.CreateCommandQueue(device);
            clprogram=context.CreateProgramWithSource(cl_kernel);
            clprogram.Build();
            oBuffer = context.CreateBuffer(MemoryFlags.WriteOnly, sizeof(double) * N);
            #endregion
        }
        private static int RoundUp(int groupSize, int globalSize)
        {
            int r = globalSize % groupSize;
            if (r == 0)
                return globalSize;

            return globalSize + groupSize - r;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            const string series_name = "Function";
            while(TIME<MAX_TIME_SEC-1)
            {
#if DEBUG
                Console.WriteLine(TIME);
#endif
                chart1.Visible = false;
                #region WRITE
                // chart1.ChartAreas[0] = new ChartArea();
               // chart1.ChartAreas[0].AxisX = new Axis();
#if DEBUG
                Console.WriteLine("set x");
#endif
                chart1.ChartAreas[0].AxisX.Maximum = //Y.Max();
                    (TIME * N) + N;
                    ;
               // chart1.ChartAreas[0].AxisX.
                chart1.ChartAreas[0].AxisX.Minimum = //Y.Min();
                    TIME*N;
                chart1.ChartAreas[0].AxisX.Name = "X";
#if DEBUG
                Console.WriteLine("set y");
#endif
                chart1.ChartAreas[0].AxisY.Minimum = Y.Max();
                chart1.ChartAreas[0].AxisY.Maximum = Y.Min();
                chart1.ChartAreas[0].AxisY.Name = "Y";
#if DEBUG
                Console.WriteLine("set type");
#endif
                chart1.Series[series_name].XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
                chart1.Series[series_name].YValueType = ChartValueType.Double;
                chart1.Series[series_name].XAxisType = AxisType.Primary;
                chart1.Series[series_name].YAxisType = AxisType.Primary;
#if DEBUG
                Console.WriteLine("set color");


                chart1.Series[series_name].Color = Color.Beige;
#endif
                chart1.Series[series_name].Points.Clear();
                chart1.Series[series_name].ResetIsValueShownAsLabel();
chart1.ResetAutoValues();
#if DEBUG
                Console.WriteLine("set points");
#endif
                for(int i=0;i<N; i++)
                {
                    chart1.Series[series_name].Points.Add(new System.Windows.Forms.DataVisualization.Charting.DataPoint((TIME * N) + (i / N), Y[i]));
                }
                chart1.Series[series_name].Enabled = true;
                chart1.Visible = true;
                #endregion
                TIME += 1;
                Task delay = Task.Run(delegate { Task.Delay(1000*100); });
#if DEBUG
                Console.WriteLine("run ocl");
#endif
                #region OCL
                using(Kernel kern = clprogram.CreateKernel("kernels"))
                {
                    kern.Arguments[0].SetValue(oBuffer);
                    kern.Arguments[1].SetValue(TIME);
                    commandQueue.EnqueueNDRangeKernel(kern, (IntPtr)RoundUp(256, THREAD_COUNT), (IntPtr)256);
                    commandQueue.Finish();
                    unsafe
                    {
                        fixed(double* o = Y)
                        {
                            commandQueue.EnqueueReadBuffer(oBuffer, false, 0, N * sizeof(double), (IntPtr)o);
                        }
                    }
                }

                #endregion
#if DEBUG
                Console.WriteLine("end cl");
#endif
                 delay.Wait();

            }
        }
        ~Form1(){
            oBuffer.Dispose();
            clprogram.Dispose();
            commandQueue.Dispose();
            context.Dispose();
        }
    }
}
