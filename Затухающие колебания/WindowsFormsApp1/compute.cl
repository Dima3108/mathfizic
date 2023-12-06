#define V 0.001
void kernels( __global double *oBuffer,int TIME,int X0,int N){
int i=get_global_id(0);
int t=i+(N*TIME);
double xt=X0+(t*V);
double yt=sin(pow(M_E,-xt));
oBuffer[i]=yt;
}