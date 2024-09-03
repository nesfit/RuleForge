
using System.Text;
using SoftWx.Match;
using System.IO.MemoryMappedFiles;

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
            
            var outFile = MemoryMappedFile.CreateFromFile(Path.Combine(Path.GetDirectoryName(arg),Path.GetFileName(arg)+"_distance_matrix.npy"),FileMode.CreateNew,null,count*count+fileHeader1.Length+fileHeader2.Length);
            var accessor = outFile.CreateViewAccessor();
            long write_offset = 0;
            for (int i = 0;i < fileHeader1.Length;i++,write_offset++){
                accessor.Write(write_offset,fileHeader1[i]);
            }
            for (int i = 0;i < fileHeader2.Length;i++,write_offset++){
                accessor.Write(write_offset,fileHeader2[i]);
            }
            
            Parallel.For(0, count, i => {
                for(int j = i+1; j < count; j++){
                    accessor.Write(write_offset + i*count + j,(sbyte)Distance.Levenshtein(words[i],words[j]));
                }
            });

            Parallel.For(0, count, i => {
                for(int j = 0; j < i; j++){
                    accessor.Write(write_offset + i*count + j,accessor.ReadByte(write_offset + j*count + i));
                }
            });
        }
        return 0;
    }
}