
//#define PROFILING
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SoftWx.Match;
public class Program
{
    unsafe public static int Main(string[] args)
    {
        if (sizeof(nuint) < 8){
            Console.Error.WriteLine("Distance matrix generator cannot run on 32-bit systems.");
            return 2;
        }
        
        foreach(var arg in args){
            if (!File.Exists(arg)){
                Console.Error.WriteLine("File \""+arg+"\" does not exist.");
                return 1;
            }
        }

        foreach(var arg in args){
            #if PROFILING
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            #endif
            
            string[] words = File.ReadAllLines(arg);
            
            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Reading input file: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif
            
            long count = words.LongLength;
            sbyte* distanceMatrix = (sbyte*)NativeMemory.Alloc((nuint)count*(nuint)count, sizeof(sbyte));
            Parallel.For(0, count, i => {
                for(long j = i+1; j < count; j++){
                    distanceMatrix[i*count+j] = (sbyte)Distance.Levenshtein(words[i],words[j]);
                }
            });

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Calculating half the distance matrix: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            Parallel.For(0, count, i => {
                for(long j = 0; j < i; j++){
                    distanceMatrix[i*count+j] = distanceMatrix[j*count+i];
                }
            });
            
            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Mirroring the distance matrix: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            for (long i = 0; i < count; i++){
                distanceMatrix[i*count + i] = (sbyte)0;
            }

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Zeroing the distance matrix diagonal: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            var matrixOutputFile = File.Open(Path.Combine(Path.GetDirectoryName(arg),Path.GetFileNameWithoutExtension(arg)+"_distance_matrix.npy"),FileMode.Create);
            string fileHeader2 = string.Format("{{'descr': '|i1', 'fortran_order': False, 'shape': ({0}, {1}), }}", count, count);
            var headerOvershoot = (11 + fileHeader2.Length) % 64;
            if (headerOvershoot != 0){
                fileHeader2 = fileHeader2.PadRight(fileHeader2.Length + (64 - headerOvershoot));
            }
            fileHeader2 = fileHeader2 + '\n';
            byte headerLengthLowerByte = (byte)(fileHeader2.Length % 255);
            byte headerLengthHigherByte = (byte)(fileHeader2.Length / 255);
            byte[] fileHeader1 = {
                (byte)0x93,
                (byte)0x4e,
                (byte)0x55,
                (byte)0x4d,
                (byte)0x50,
                (byte)0x59,
                (byte)0x01,
                (byte)0x00,
                headerLengthLowerByte,
                headerLengthHigherByte
                };
            matrixOutputFile.Write(fileHeader1);
            matrixOutputFile.Write(Encoding.ASCII.GetBytes(fileHeader2));

            byte* current_pointer = (byte*)distanceMatrix;
            byte* end_pointer = current_pointer + count*count;
            while(current_pointer < end_pointer){
                int stride = end_pointer - current_pointer > int.MaxValue ? int.MaxValue : (int)(end_pointer - current_pointer);
                var span = new ReadOnlySpan<byte>(current_pointer,stride);
                matrixOutputFile.Write(span);
                current_pointer += stride;
            }

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Writing the distance matrix: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            NativeMemory.Free(distanceMatrix);
            matrixOutputFile.Close();

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Closing file (probably also flushing) and deallocating distance matrix: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif
        }
        return 0;
    }
}