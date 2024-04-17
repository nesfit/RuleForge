/*
Implementation of DBSCAN and MDBSCAN clustering for strings.

Author: Viktor Rucky

License: MIT

Based on the MDBSCAN algorithm from the paper
Mangling Rules Generation With Density-Based Clustering for Password Guessing
by Shunbin Li, Zhiyu Wang, Ruyun Zhang, Chunming Wu, and Hanguang Luo
(doi: 10.1109/TDSC.2022.3217002).

This implementation was inspired by the implementation of DBSCAN in Scikit-Learn by Lars Buitinck
(https://github.com/scikit-learn/scikit-learn/blob/main/sklearn/cluster/_dbscan_inner.pyx).
*/

//#define PROFILING
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

public class Program
{
    public static int Main(string[] args)
    {
        //Handling and cleaning user input

        if (args.Length != 3 && args.Length != 4){
            Console.Error.WriteLine("\rBad number of arguments.");
            return -1;
        }

        int eps1;
        double eps2;
        int min_count;
        string path;

        if (args.Length == 3){
            eps1 = int.Parse(args[0]);
            eps2 = 0; //Does not matter, but we have to have it initialised.
            min_count = int.Parse(args[1]);
            path = args[2];
        } else {
            eps1 = int.Parse(args[0]);
            eps2 = double.Parse(args[1]);
            min_count = int.Parse(args[2]);
            path = args[3];
        }
        
        FileInfo fi = new FileInfo(path);
        if (!fi.Exists){
            Console.Error.WriteLine("Input file does not exist.");
            return -1;
        }
        
        var eps1NeigbourhoodsCacheName = Path.Combine(fi.DirectoryName,".MDBSCANcache."+eps1.ToString()+"."+fi.Name+".json");

        FileInfo eps1NeigbourhoodsCache = new FileInfo(eps1NeigbourhoodsCacheName);

        ConcurrentDictionary<string,List<string>> neighbourhoods;
        Dictionary<string,int> word_to_cluster_dictionary;
        int trueWordCount;

        #if PROFILING
        Stopwatch stopwatch = new Stopwatch();
        #endif

        if (eps1NeigbourhoodsCache.Exists){
            #if PROFILING
            stopwatch.Start();
            #endif
            neighbourhoods = JsonSerializer.Deserialize<ConcurrentDictionary<string,List<string>>>(File.ReadAllText(eps1NeigbourhoodsCacheName));
            trueWordCount = neighbourhoods.Count;
            word_to_cluster_dictionary = new Dictionary<string, int>(trueWordCount);
            foreach (string entry in neighbourhoods.Keys){
                word_to_cluster_dictionary[entry] = -1;
            }
            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Deserializing cached eps1 neigbourhoods and building clustering dictionary: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif
        } else {

            int wordCountEstimate = (int)(fi.Length / 8); // Eight characters per password is a good guess.

            //Building the SymSpell dictionary and cluster dictionary;
            #if PROFILING
            stopwatch.Start();
            #endif

            var symSpell = new SymSpell(wordCountEstimate, eps1, eps1+1);
            word_to_cluster_dictionary = new Dictionary<string,int>(wordCountEstimate);
            var staging = new SymSpell.SuggestionStage(wordCountEstimate);

            foreach (string line in File.ReadLines(path)){
		        if (line == "") continue;
                word_to_cluster_dictionary.Add(line,-1);
                symSpell.CreateDictionaryEntry(line,1,staging);
            }
            symSpell.CommitStaged(staging);

            trueWordCount = symSpell.WordCount;

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Building SymSpell and clustering dictionary: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif
            
            // Building the epsilon1 neighbourhoods
            
            neighbourhoods = new ConcurrentDictionary<string,List<string>>();
            Parallel.ForEach(word_to_cluster_dictionary.Keys, entry => {
                neighbourhoods[entry] = (from x in symSpell.Lookup(entry,SymSpell.Verbosity.All) select x.term).ToList();
            });

            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Building eps1 neigbhourhoods: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif

            var serializedData = JsonSerializer.Serialize(neighbourhoods);
            File.WriteAllText(eps1NeigbourhoodsCacheName,serializedData);
            #if PROFILING
            stopwatch.Stop();
            Console.Error.WriteLine("Serializing eps1 neigbourhoods and caching to disk: "+stopwatch.Elapsed.TotalSeconds.ToString());
            stopwatch.Restart();
            #endif
        }
        
        //Clustering

        int cluster_index = 0;
        if (args.Length == 3){ //DBSCAN clustering
            var searchStack = new Stack<string>();
            foreach(string initial_entry in word_to_cluster_dictionary.Keys.ToList()){
                if (word_to_cluster_dictionary[initial_entry] != -1) continue;
                var init_neighbourhood = neighbourhoods[initial_entry];

                if (init_neighbourhood.Count < min_count) continue;

                searchStack.Push(initial_entry);

                while (searchStack.Count > 0){
                    var current = searchStack.Pop();
                    
                    if (word_to_cluster_dictionary[current] == -1){
                        word_to_cluster_dictionary[current] = cluster_index;
                        
                        var neighbourhood = neighbourhoods[current];

                        if (neighbourhood.Count >= min_count){
                            foreach (var x in from x in neighbourhood where word_to_cluster_dictionary[x] == -1 select x){
                                searchStack.Push(x);
                            }
                        }

                    }
                }

                cluster_index++;
            }
        } else { //MDBSCAN clustering
            var JaroWinkler = new F23.StringSimilarity.JaroWinkler();
            var searchStack = new Stack<string>();
            foreach(string initial_entry in word_to_cluster_dictionary.Keys.ToList()){
                if (word_to_cluster_dictionary[initial_entry] != -1) continue;
                var init_neighbourhood = neighbourhoods[initial_entry];

                if (init_neighbourhood.Count < min_count) continue;

                searchStack.Push(initial_entry);

                while (searchStack.Count > 0){
                    var current = searchStack.Pop();
                    
                    if (word_to_cluster_dictionary[current] == -1 && JaroWinkler.Distance(initial_entry,current) < eps2){
                        word_to_cluster_dictionary[current] = cluster_index;
                        
                        var neighbourhood = neighbourhoods[current];

                        if (neighbourhood.Count >= min_count){
                            foreach (var x in from x in neighbourhood where word_to_cluster_dictionary[x] == -1 select x){
                                searchStack.Push(x);
                            }
                        }

                    }
                }

                cluster_index++;
            }
        }

        #if PROFILING
        stopwatch.Stop();
        Console.Error.WriteLine("Clustering: "+stopwatch.Elapsed.TotalSeconds.ToString());
        stopwatch.Restart();
        #endif

        //Transforming the clustering output to be useful for output
        var output_dictionary = new Dictionary<int,List<string>>(trueWordCount);

        for (int i = -1; i < cluster_index;i++){
            output_dictionary[i] = new List<string>();
        }

        foreach(var x in word_to_cluster_dictionary){
            output_dictionary[x.Value].Add(x.Key);
        }

        if (output_dictionary[-1].Count == 0){
            output_dictionary.Remove(-1);
        }

        #if PROFILING
        stopwatch.Stop();
        Console.Error.WriteLine("Transforming output: "+stopwatch.Elapsed.TotalSeconds.ToString());
        stopwatch.Restart();
        #endif

        //Output 
        Console.Out.Write(JsonSerializer.Serialize(output_dictionary));

        #if PROFILING
        stopwatch.Stop();
        Console.Error.WriteLine("Outputting: "+stopwatch.Elapsed.TotalSeconds.ToString());
        #endif

        return 0;
    }
}