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
        private const int N = 1000000;
        private const int THREAD_COUNT = N;
        private const int X0 = 0;
        private const int MAX_TIME_SEC = 300;
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
        private Chart chart;
        private ChartArea area = new ChartArea("y=sin(e^-x)");

        public Form1()
        {
            InitializeComponent();
            chart = new Chart();
            chart.Parent = this;
            chart.Dock = DockStyle.Fill;
            //chart.ResumeLayout();
            chart.ChartAreas.Add(area);
            #region NOpenCL_Init
#if DEBUG
            System.IO.File.WriteAllText("debugkernel.cl", cl_kernel);
#endif
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
            button1.Visible = false;
            TIME = 0;
            while(TIME<MAX_TIME_SEC-1)
            {
               
#if DEBUG
                Console.WriteLine(TIME);
#endif
                //chart1.Visible = false;

                #region WRITE
                /*  
    #if DEBUG
                    Console.WriteLine("set x");
    #endif
                    chart1.ChartAreas[0].AxisX.Maximum = 
                        (TIME * N) + N;
                        ;

                    chart1.ChartAreas[0].AxisX.Minimum = 
                        TIME*N;
                    chart1.ChartAreas[0].AxisX.Crossing = 0;
                    chart1.ChartAreas[0].AxisX.Name = "X";
    #if DEBUG
                    Console.WriteLine("set y");
    #endif
                     chart1.ChartAreas[0].AxisY.Minimum = Y.Max();
                     chart1.ChartAreas[0].AxisY.Maximum = Y.Min();
                    chart1.ChartAreas[0].AxisY.Crossing = 0;
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
                    */
               // chart1.Dispose();
               // var area = new ChartArea("y=sin(e^-x)");
                area.AxisX.Crossing = 0;
                area.AxisX.Minimum =N * TIME
                    ;
                area.AxisX.Maximum =N+( N * TIME)
                    ;
                 area.AxisY.Crossing = 0;
                area.AxisY.Minimum = -1;
                area.AxisY.Maximum = 1; 
                area.AxisX.Interval = N/5;
                area.AxisY.Interval = N/5;
                var series = new Series();
                series.ChartType = SeriesChartType.Line;
                series.ChartArea = "y=sin(e^-x)";
                series.XAxisType = AxisType.Primary;
                series.YAxisType = AxisType.Primary;
                series.XValueType = ChartValueType.Double;
                series.YValueType = ChartValueType.Double;
               // series.
              //  var chart = new Chart();
               
                chart.Series.Add(series);
                series.Color = Color.Red;
               // chart1 = chart;
                // chart1.Series.Clear();
                // var numbs=chart1.Series.Add(series_name);
#if DEBUG
                Console.WriteLine("set points");
#endif
                for(int i=0;i<N; i++)
                {
                    series.Points.Add(new System.Windows.Forms.DataVisualization.Charting.DataPoint((TIME * N) + (i / N), Y[i]));
                }

              //  chart.DataBind();
               // chart.BeginInit();
                // chart1.BeginInit();
                //chart1.EndInit();
                //chart1.Invalidate();
                //chart1.Series[series_name].Enabled = true;
                //chart1.Visible = true;
                #endregion
                TIME += 1;
                chart.BeginInit(); 
                Task delay = Task.Run(delegate { Task.Delay(2000); });
#if DEBUG
                Console.WriteLine("run ocl");
#endif
                #region OCL

                delay.Wait();
                chart.EndInit();
                using(Kernel kern = clprogram.CreateKernel("kernels"))
                {
                    kern.Arguments[0].SetValue(oBuffer);
                    kern.Arguments[1].SetValue(TIME);
                    commandQueue.EnqueueNDRangeKernel(kern, (IntPtr)RoundUp(1, THREAD_COUNT), (IntPtr)1);
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
                Console.WriteLine($"{Y[0]}{" "}:{Y[10]}{" "}:{Y[25]}:max={Y.Max()},min={Y.Min()}");
#endif
               // chart.EndInit();
  //chart.Invalidate();
              //   delay.Wait();
             
               
               // chart.Focus();
               // series.Points.Clear();
//chart.Dispose();
            }
            button1.Visible = true;
        }
        ~Form1(){
            oBuffer.Dispose();
            clprogram.Dispose();
            commandQueue.Dispose();
            context.Dispose();
        }
    }
}
