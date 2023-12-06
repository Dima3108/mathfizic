#define V 0.001

__kernel void kernels( __global double *oBuffer,int TIME){
int i=get_global_id(0);
while(i<1000000){
int t=(i/1000000)+(1000000*TIME);
double xt=0+(t*V);
double yt=sin(pow(M_E,-xt));
oBuffer[i]=yt;
i+=1000000;
}
}