
using System.Text;
using SoftWx.Match;

public class Program
{
    public static int Main(string[] args)
    {
        foreach(var arg in args){
            if (!File.Exists(arg)){
                Console.Error.WriteLine("File \""+arg+"\" does not exist.");
                return 1;
            }
        }

        foreach(var arg in args){
            string[] words = File.ReadAllLines(arg);
            int count = words.Length;
            sbyte[,] distanceMatrix = new sbyte[count,count];
            Parallel.For(0, count, i => {
                for(int j = i+1; j < count; j++){
                    distanceMatrix[i,j] = (sbyte)Distance.Levenshtein(words[i],words[j]);
                }
            });

            Parallel.For(0, count, i => {
                for(int j = 0; j < i; j++){
                    distanceMatrix[i,j] = distanceMatrix[j,i];
                }
            });
            
            var matrixOutputFile = File.Open(Path.Combine(Path.GetDirectoryName(arg),Path.GetFileName(arg)+"_distance_matrix.npy"),FileMode.Create);
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
            unsafe {
                fixed (sbyte* p = distanceMatrix)
                {
                    var span = new Span<byte>((byte*)p,distanceMatrix.Length);
                    matrixOutputFile.Write(span);
                }
            }
        }
        return 0;
    }
}