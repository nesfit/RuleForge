
#define PROFILING
using System.Text;
using SoftWx.Match;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;

public class Program
{
    unsafe public static int Main(string[] args)
    {
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
            Console.Error.WriteLine("Reading file: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            long count = words.Length;
            
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
            
            var outFile = MemoryMappedFile.CreateFromFile(Path.Combine(Path.GetDirectoryName(arg),Path.GetFileName(arg)+"_distance_matrix.npy"),FileMode.CreateNew,null,count*count+fileHeader1.Length+fileHeader2.Length,MemoryMappedFileAccess.CopyOnWrite);
            var accessor = outFile.CreateViewAccessor(0,count*count+fileHeader1.Length+fileHeader2.Length,MemoryMappedFileAccess.CopyOnWrite);
            byte* byte_peek = null;
            sbyte* sbyte_peek = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref byte_peek);
            sbyte_peek = (sbyte*)byte_peek;
            long write_offset = 0;
            for (int i = 0;i < fileHeader1.Length;i++,write_offset++){
                byte_peek[write_offset] = fileHeader1[i];
            }
            for (int i = 0;i < fileHeader2.Length;i++,write_offset++){
                byte_peek[write_offset] = (byte)fileHeader2[i];
            }
            
            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Opening output file and writing header: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            Parallel.For(0, count-1, i => {
                sbyte[] line = new sbyte[count - (i+1)];
                long k = 0;
                for(long j = i+1; j < count; j++,k++){
                    line[k] = (sbyte)Distance.Levenshtein(words[i],words[j]);
                }
                //accessor.WriteArray<sbyte>(write_offset+i*count+i+1,line,0,line.Length);
                k = 0;
                for(long j = i+1; j < count; j++,k++){
                    sbyte_peek[write_offset+i*count+j] = line[k];
                }
            });

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Computing half of distance matrix: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            Parallel.For(1, count, i => {
                byte[] line = new byte[i];
                for(long j = 0; j < i; j++){
                    line[j] = accessor.ReadByte(write_offset + j*count + i);
                }
                for(long j = 0; j < i; j++){
                    byte_peek[write_offset + i*count + j] = line[j];
                }
                //accessor.WriteArray<byte>(write_offset + i*count,line,0,line.Length);
            });

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Mirroring distance matrix: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif
        }
        return 0;
    }
}